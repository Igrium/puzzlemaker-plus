using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus.Commands;

/// <summary>
/// A base class for commands that modify the voxel world.
/// </summary>
public abstract class AbstractWorldCommand : ICommand
{
    private readonly Dictionary<Vector3I, VoxelChunk<PuzzlemakerVoxel>> _preChunks = new();
    private readonly Dictionary<Vector3I, VoxelChunk<PuzzlemakerVoxel>> _postChunks = new();


    /// <summary>
    /// Actually run the command.
    /// </summary>
    /// <param name="world">World to run on. Automatically keeps track of changes for undo/redo.</param>
    protected abstract void Execute(IVoxelView<PuzzlemakerVoxel> world);

    public void Execute()
    {
        EditorState editor = EditorState.Instance;
        ModifiedWorldLayer<PuzzlemakerVoxel> layer = new(editor.World);
        Execute(layer);

        foreach (var (pos, postChunk) in layer.UpdatedChunks)
        {
            VoxelChunk<PuzzlemakerVoxel> preChunk = new();
            VoxelChunk<PuzzlemakerVoxel> worldChunk = layer.World.GetOrCreateChunk(pos);

            worldChunk.CopyTo(preChunk);
            _preChunks[pos] = preChunk;

            postChunk.CopyTo(worldChunk);
            _postChunks[pos] = postChunk;
        }

        editor.EmitOnChunksUpdated(layer.UpdatedChunks.Keys);
    }

    public void Redo()
    {
        EditorState editor = EditorState.Instance;
        foreach (var (pos, postChunk) in _postChunks)
        {
            postChunk.CopyTo(editor.World.GetOrCreateChunk(pos));
        }
        editor.EmitOnChunksUpdated(_postChunks.Keys);
    }

    public void Undo()
    {
        EditorState editor = EditorState.Instance;
        foreach (var (pos, preChunk) in _preChunks)
        {
            preChunk.CopyTo(editor.World.GetOrCreateChunk(pos));
        }
        editor.EmitOnChunksUpdated(_preChunks.Keys);
    }
}
