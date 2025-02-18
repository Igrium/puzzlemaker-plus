using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PuzzlemakerPlus.Commands;

/// <summary>
/// A command that can update the voxel world.
/// </summary>
public abstract class AbstractVoxelCommand : ICommand
{

    private readonly Dictionary<Vector3I, VoxelChunk<PuzzlemakerVoxel>> _beforeChunks = new();
    private readonly Dictionary<Vector3I, VoxelChunk<PuzzlemakerVoxel>> _afterChunks = new();

    private bool _savePostState;

    protected PuzzlemakerWorld World => EditorState.Instance.World;

    /// <summary>
    /// Base constructor.
    /// </summary>
    /// <param name="savePostState">If set, the world state will be saved after the command is run. Otherwise, redo will execute the command again.</param>
    public AbstractVoxelCommand(bool savePostState = true)
    {
        _savePostState = savePostState;
    }

    public virtual void Execute()
    {
        Vector3I[] modifiedChunks = GetModifiedChunks().ToArray();

        foreach (var pos in modifiedChunks)
        {
            VoxelChunk<PuzzlemakerVoxel>? chunk;
            if (World.Chunks.TryGetValue(pos, out chunk))
            {
                _beforeChunks[pos] = chunk.Copy();
            }
        }
        Execute(World);
        if (_savePostState)
        {
            foreach (var pos in modifiedChunks)
            {
                VoxelChunk<PuzzlemakerVoxel>? chunk;
                if (World.Chunks.TryGetValue(pos, out chunk))
                {
                    _afterChunks[pos] = chunk.Copy();
                }
            }
        }
        
        EditorState.Instance.EmitOnChunksUpdated(modifiedChunks);
    }

    /// <summary>
    /// Get an enumerable of all the chunks that this command will modify.
    /// </summary>
    /// <returns>All modified chunks.</returns>
    protected abstract IEnumerable<Vector3I> GetModifiedChunks();

    /// <summary>
    /// Execute this command.
    /// </summary>
    /// <param name="world">World to execute on.</param>
    protected abstract void Execute(PuzzlemakerWorld world);

    public virtual void Undo()
    {
        foreach (var (pos, chunk) in _beforeChunks)
        {
            World.GetOrCreateChunk(pos).CopyFrom(chunk);
        }
        EditorState.Instance.EmitOnChunksUpdated(_beforeChunks.Keys.ToArray());
    }

    public virtual void Redo()
    {
        if (_savePostState)
        {
            foreach (var (pos, chunk) in _afterChunks)
            {
                World.GetOrCreateChunk(pos).CopyFrom(chunk);
            }
        }
        else
        {
            Execute(World);
        }
        EditorState.Instance.EmitOnChunksUpdated(_beforeChunks.Keys.ToArray());
    }
}
