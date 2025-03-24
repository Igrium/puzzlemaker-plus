using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace PuzzlemakerPlus;

[GlobalClass]
public partial class WorldMeshGenerator : RefCounted
{
    /// <summary>
    /// Called after the primary geometry of the world has been generated.
    /// </summary>
    /// <param name="mesh">The quad mesh. Caches may still be being computed!</param>
    [Signal]
    public delegate void QuadsComputedEventHandler(SimpleQuadMesh mesh);

    /// <summary>
    /// Called after all caches have finished computing.
    /// </summary>
    /// <param name="mesh"></param>
    [Signal]
    public delegate void CachesComputedEventHandler(SimpleQuadMesh mesh);
    
    /// <summary>
    /// Called once the edge model has been generated.
    /// </summary>
    /// <param name="edgeMesh">The edge model.</param>
    [Signal]
    public delegate void EdgeModelGeneratedEventHandler(ArrayMesh edgeMesh);

    /// <summary>
    /// Called once all generation has completed. It's only safe to modify instance variables after this signal has been emit.
    /// </summary>
    [Signal]
    public delegate void GenerationCompleteEventHandler();

    public ArrayMesh? Mesh { get; set; }
    public ConcavePolygonShape3D? Collision { get; set; }
    /// <summary>
    ///  If set, and edge model will be generated and placed in this mesh.
    /// </summary>
    public ArrayMesh? EdgeMesh { get; set; }
    public PuzzlemakerWorld World { get; set; }
    public Vector3I ChunkPos { get; set; }
    public bool Invert { get; set; }

    public bool ComputeVertexCache { get; set; } = true;
    public bool ComputeEdgeCache { get; set; } = true;
    public bool DuplicateMeshForEdges { get; set; } = false;

    public Array<Material>? WallTextureOverride { get; set; }

    public WorldMeshGenerator(ArrayMesh? mesh, ConcavePolygonShape3D? collision, PuzzlemakerWorld world, Vector3I chunkPos, bool invert = true)
    {
        Mesh = mesh;
        Collision = collision;
        World = world;
        ChunkPos = chunkPos;
        Invert = invert;
    }

    public WorldMeshGenerator(PuzzlemakerWorld world)
    {
        World = world;
    }

    public WorldMeshGenerator()
    {
        World = EditorState.Instance.World;
    }

    /// <summary>
    /// Intended to be called from GDScript. Use Task.Run(DoGreedyMesh) for C#.
    /// </summary>
    public async void DoGreedyMeshThreaded()
    {
        await Task.Run(DoGreedyMesh);
    }

    /// <summary>
    /// Perform the greedy mesh process synchronously.
    /// </summary>
    /// <returns></returns>
    public SimpleQuadMesh DoGreedyMesh()
    {
        // Base greedy mesh
        List<Quad> quads = new List<Quad>();
        ChunkView<PuzzlemakerVoxel> view = new(World, ChunkPos);
        GreedyMesh.DoGreedyMesh(view, quads.Add, uvScale: .25f);

        SimpleQuadMesh quadMesh = new SimpleQuadMesh(quads);

        // Add to arraymesh
        if (Mesh != null)
        {
            MultiMeshBuilder builder = new();
            foreach (var quad in quadMesh)
            {
                builder.AddQuad(quad);
            }
            Material[] texutres = 
                WallTextureOverride != null ? WallTextureOverride.ToArray() : 
                EditorState.Instance.GetEditorTheme().WallTextures.ToArray();

            builder.ToMesh(Mesh, texutres);
        }

        // Collision
        if (Collision != null)
        {
            PolygonShapeBuilder builder = new PolygonShapeBuilder();
            foreach (var quad in quadMesh)
            {
                builder.AddQuad(quad);
            }
            builder.ToShape(Collision);
        }

        RunOnMainThread(() => EmitSignalQuadsComputed(quadMesh));

        // Compute caches
        if (ComputeVertexCache || EdgeMesh != null)
            quadMesh.ComputeVertexCache();
        if (ComputeEdgeCache || EdgeMesh != null)
            quadMesh.ComputeEdgeCache();

        RunOnMainThread(() => EmitSignalCachesComputed(quadMesh));

        // Edge mesh
        if (EdgeMesh != null)
        {
            SimpleQuadMesh edgeQuadMesh = DuplicateMeshForEdges ? new SimpleQuadMesh(quadMesh) : quadMesh;
            edgeQuadMesh.AverageNormals();
            List<Quad> edgeQuads = new(edgeQuadMesh.Quads.Length);
            EdgeModelGenerator.GenerateEdgeModel(edgeQuadMesh, edgeQuads.Add);
            EdgeModelGenerator.BuildEdgeMesh(edgeQuads, EdgeMesh);

            RunOnMainThread(() => EmitSignalEdgeModelGenerated(EdgeMesh));
        }

        RunOnMainThread(EmitSignalGenerationComplete);
        return quadMesh;
    }

    // Factory method for GDScript
    public static WorldMeshGenerator Create(ArrayMesh? mesh, ConcavePolygonShape3D? collision, Vector3I chunkPos, bool invert = true)
    {
        return new WorldMeshGenerator(mesh, collision, EditorState.Instance.World, chunkPos, invert);
    }

    private static void RunOnMainThread(Action action)
    {
        RunOnMainThread(Callable.From(action));
    }

    private static void RunOnMainThread(Callable callable, params Variant[] args)
    {
        if (Thread.CurrentThread.ManagedThreadId == 1)
        {
            callable.Call(args);
        }
        else
        {
            callable.CallDeferred(args);
        }
    }
}
