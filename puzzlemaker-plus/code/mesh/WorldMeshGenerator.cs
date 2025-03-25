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
    /// <param name="mesh">The quad mesh with no caches computed.</param>
    /// <remarks>May continue to be updated as caches are computed!</remarks>
    [Signal]
    public delegate void QuadsComputedEventHandler(SimpleQuadMesh mesh);

    /// <summary>
    /// Called when the edge model has finished generating.
    /// </summary>
    /// <param name="mesh">The edge model.</param>
    [Signal]
    public delegate void EdgeModelCreatedEventHandler(SimpleQuadMesh mesh);

    /// <summary>
    /// Called after all processing has completed.
    /// </summary>
    /// <param name="mesh">The model.</param>
    [Signal]
    public delegate void ModelCompletedEventHandler(SimpleQuadMesh mesh);

    public ArrayMesh? Mesh { get; set; }
    public ConcavePolygonShape3D? Collision { get; set; }

    public bool GenerateEdgeModel = true;

    public ArrayMesh? EdgeMesh;

    /// <summary>
    /// Edge model generation requires that normals are smoothened. If set, do this operation on a temporary copy of the mesh.
    /// </summary>
    public bool DuplicateMeshForEdges { get; set; } = false;

    public PuzzlemakerWorld World { get; set; }
    public Vector3I ChunkPos { get; set; }
    public bool Invert { get; set; } = true;

    public Array<Material>? WallTextureOverride { get; set; }

    public WorldMeshGenerator(ArrayMesh? mesh, ConcavePolygonShape3D? collision, PuzzlemakerWorld world, Vector3I chunkPos, bool invert = true)
    {
        Mesh = mesh;
        Collision = collision;
        World = world;
        ChunkPos = chunkPos;
        Invert = invert;
    }

    public async void DoGreedyMeshThreaded()
    {
        await Task.Run(DoGreedyMesh);
    }

    /// <summary>
    /// Perform the greedy mesh on this thread.
    /// </summary>
    /// <returns>The generated mesh.</returns>
    public SimpleQuadMesh DoGreedyMesh()
    {
        // Initial greedymesh.
        List<Quad> quads = new List<Quad>();
        ChunkView<PuzzlemakerVoxel> view = new(World, ChunkPos);
        GreedyMesh.DoGreedyMesh(view, quads.Add, uvScale: .25f);

        SimpleQuadMesh quadMesh = new SimpleQuadMesh(quads.ToArray());
        
        // Collision
        PolygonShapeBuilder polygonBuilder = new PolygonShapeBuilder();
        foreach (var quad in quadMesh)
        {
            polygonBuilder.AddQuad(quad);
        }
        SimpleQuadMesh quadMesh2 = new SimpleQuadMesh(quadMesh); // Duplicate for thread safety
        RunOnMainThread(() => OnQuadsComputed(quadMesh2, polygonBuilder));

        // Compute caches
        quadMesh.ComputeVertexCache();
        quadMesh.ComputeEdgeCache();

        if (GenerateEdgeModel)
        {
            SimpleQuadMesh edgeQuadMesh = DuplicateMeshForEdges ? new SimpleQuadMesh(quadMesh) : quadMesh;
            edgeQuadMesh.AverageNormals();

            List<Quad> edgeQuads = new(edgeQuadMesh.Quads.Length);
            EdgeModelGenerator.CreateEdgeModel(edgeQuadMesh, edgeQuads.Add);

            SimpleQuadMesh edgeModel = new SimpleQuadMesh(edgeQuads);
            RunOnMainThread(() => OnEdgeModelCompleted(edgeModel));
        }
        RunOnMainThread(() => EmitSignalModelCompleted(quadMesh));
        return quadMesh;
    }

    private void OnQuadsComputed(SimpleQuadMesh quadMesh, PolygonShapeBuilder polygonBuilder)
    {
        if (Mesh != null)
        {
            Material[] textures =
                WallTextureOverride != null ? WallTextureOverride.ToArray() :
                EditorState.Instance.GetEditorTheme().WallTextures.ToArray();
            quadMesh.ToArrayMesh(Mesh, true, textures);
        }
        if (Collision != null)
        {
            polygonBuilder.ToShape(Collision);
        }
        EmitSignalQuadsComputed(quadMesh);
    }

    private void OnEdgeModelCompleted(SimpleQuadMesh edgeModel)
    {
        if (EdgeMesh != null)
        {
            edgeModel.ToArrayMesh(EdgeMesh);
        }
        EmitSignalEdgeModelCreated(edgeModel);
    }

    // Factory method for GDScript
    public static WorldMeshGenerator Create(ArrayMesh? mesh, ConcavePolygonShape3D? collision, Vector3I chunkPos, bool invert = true)
    {
        return new WorldMeshGenerator(mesh, collision, EditorState.Instance.World, chunkPos, invert);
    }

    private static void DebugDrawEdge(Edge edge, Color? color = null, float duration = 0)
    {
        RunOnMainThread(() => DebugDraw3D.DrawLine(edge.Vert1, edge.Vert2, color, duration));
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
