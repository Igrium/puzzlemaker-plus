using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace PuzzlemakerPlus;

[GlobalClass]
public partial class WorldMeshGenerator : RefCounted
{
    [Signal]
    public delegate void GreedyMeshFinishedEventHandler(SimpleQuadMesh mesh);

    public ArrayMesh? Mesh { get; set; }
    public ConcavePolygonShape3D? Collision { get; set; }
    public PuzzlemakerWorld World { get; set; }
    public Vector3I ChunkPos { get; set; }
    public bool Invert { get; set; }

    public bool ComputeVertexCache { get; set; } = true;
    public bool ComputeEdgeCache { get; set; } = true;

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
    public async void DoGreedyMeshAsync()
    {
        var quadMesh = await Task.Run(DoGreedyMesh);
        EmitSignalGreedyMeshFinished(quadMesh);
    }

    /// <summary>
    /// Perform the greedy mesh process synchronously.
    /// </summary>
    /// <returns></returns>
    public SimpleQuadMesh DoGreedyMesh()
    {
        List<Quad> quads = new List<Quad>();
        ChunkView<PuzzlemakerVoxel> view = new(World, ChunkPos);
        GreedyMesh.DoGreedyMesh(view, quads.Add, uvScale: .25f);

        SimpleQuadMesh quadMesh = new SimpleQuadMesh(quads);

        if (ComputeVertexCache)
            quadMesh.ComputeVertexCache();
        if (ComputeEdgeCache)
            quadMesh.ComputeEdgeCache();

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

        if (Collision != null)
        {
            PolygonShapeBuilder builder = new PolygonShapeBuilder();
            foreach (var quad in quadMesh)
            {
                builder.AddQuad(quad);
            }
            builder.ToShape(Collision);
        }

        return quadMesh;
    }

    // Factory method for GDScript
    public static WorldMeshGenerator Create(ArrayMesh? mesh, ConcavePolygonShape3D? collision, Vector3I chunkPos, bool invert = true)
    {
        return new WorldMeshGenerator(mesh, collision, EditorState.Instance.World, chunkPos, invert);
    }
}
