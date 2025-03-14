using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

[GlobalClass]
public partial class AsyncMeshGenerator : RefCounted
{
    [Signal]
    public delegate void GreedyMeshFinishedEventHandler(long milliseconds);

    private readonly PuzzlemakerWorld _world;
    private readonly Vector3I _offset;
    private readonly bool _invert;

    private readonly ArrayMesh? _mesh; 
    private readonly ConcavePolygonShape3D? _collision;

    public AsyncMeshGenerator(ArrayMesh? mesh, ConcavePolygonShape3D? collision, PuzzlemakerWorld world, Vector3I chunkPos, bool invert)
    {
        _mesh = mesh;
        _collision = collision;
        _world = world;
        _offset = chunkPos;
        _invert = invert;
    }

    public void DoGreedyMeshAsync()
    {
        Task.Run(() =>
        {
            try
            {
                DoGreedyMeshSync();
            }
            catch (Exception e)
            {
                GD.PushError(e);
            }
        });
    }

    private long DoGreedyMeshSync()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        //Quad[] quads = new GreedyMesh().DoGreedyMesh(_world, _offset, uvScale: .25f, invert: _invert).ToArray();
        List<Quad> quads = new List<Quad>();
        ChunkView<PuzzlemakerVoxel> view = new(_world, _offset);
        try
        {
            NewGreedyMesh.DoGreedyMesh(view, quads.Add);
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        GD.Print("done greedy mesh");

        if (_mesh != null)
        {
            MultiMeshBuilder builder = new();
            for (int i = 0; i < quads.Count; i++)
            {
                builder.AddQuad(quads[i]);
            }
            builder.ToMesh(_mesh, EditorState.Instance.Theme.EditorMaterials);
        }

        if (_collision != null)
        {
            PolygonShapeBuilder builder = new PolygonShapeBuilder();
            for (int i = 0; i < quads.Count; i++)
            {
                builder.AddQuad(quads[i]);
            }
            builder.ToShape(_collision);
        }
        stopwatch.Stop();

        long time = stopwatch.ElapsedMilliseconds;
        Callable.From(() => EmitSignal(SignalName.GreedyMeshFinished, time)).CallDeferred();

        return time;
    }

    public static AsyncMeshGenerator Create(ArrayMesh? mesh, ConcavePolygonShape3D? collision, PuzzlemakerWorld world, Vector3I chunkPos, bool invert = true)
    {
        return new AsyncMeshGenerator(mesh, collision, world, chunkPos, invert);
    }
}
