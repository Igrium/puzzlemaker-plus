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

    private Aabb _preSelection;
    private Aabb _postSelection;

    /// <summary>
    /// If true, indicates that this action will update the editor selection when triggered, and it should be saved in the undo/redo state.
    /// </summary>
    public bool ModifiesSelection { get; protected set; }

    /// <summary>
    /// Actually run the command.
    /// </summary>
    /// <param name="world">World to run on. Automatically keeps track of changes for undo/redo.</param>
    protected abstract void Execute(IVoxelView<PuzzlemakerVoxel> world);

    public bool Execute()
    {
        EditorState editor = EditorState.Instance;
        ModifiedWorldLayer<PuzzlemakerVoxel> layer = new(editor.World);
        _preSelection = editor.Selection;

        Execute(layer);

        _postSelection = editor.Selection;
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
        return true;
    }

    public virtual void Undo()
    {
        EditorState editor = EditorState.Instance;
        foreach (var (pos, preChunk) in _preChunks)
        {
            preChunk.CopyTo(editor.World.GetOrCreateChunk(pos));
        }
        if (ModifiesSelection)
        {
            editor.SetSelection(_preSelection);
        }
        editor.EmitOnChunksUpdated(_preChunks.Keys);
    }

    public virtual void Redo()
    {
        EditorState editor = EditorState.Instance;
        foreach (var (pos, postChunk) in _postChunks)
        {
            postChunk.CopyTo(editor.World.GetOrCreateChunk(pos));
        }
        if (ModifiesSelection)
        {
            editor.SetSelection(_postSelection);
        }

        editor.EmitOnChunksUpdated(_postChunks.Keys);
    }

}
