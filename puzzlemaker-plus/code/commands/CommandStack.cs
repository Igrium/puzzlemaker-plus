using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus.Commands;

/// <summary>
/// Keeps track of the undo/redo stack.
/// </summary>
public class CommandStack
{
    public delegate void ErrorHandler(string message, Exception? ex);

    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();

    /// <summary>
    /// Called when a command throws an uncaught exception.
    /// </summary>
    public ErrorHandler OnError { get; set; } = (message, ex) => GD.PushError(ex);

    /// <summary>
    /// Execute a command and add it to the undo stack.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    /// <returns>If the command sucessfully ran without throwing an exception.</returns>
    public virtual bool Execute(ICommand command)
    {
        _redoStack.Clear();
        command.Execute();
        _undoStack.Push(command);
        EditorState.Instance.UnSaved = true;
        return true;
    }

    /// <summary>
    /// Undo the last command.
    /// </summary>
    /// <returns>If there was a command to undo and it undid without an exception.</returns>
    public virtual bool Undo()
    {
        if (_undoStack.TryPop(out var command))
        {
            try
            {
                command.Undo();
                _redoStack.Push(command);
            }
            catch (Exception ex)
            {
                OnError("Error undoing command.", ex);
                _redoStack.Clear(); // We likely have an invalid state now
                _undoStack.Clear();
                return false;
            }
            EditorState.Instance.UnSaved = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Redo the last undone command.
    /// </summary>
    /// <returns>If there was a command to redo and it redid without an exception.</returns>
    public virtual bool Redo()
    {
        if (_redoStack.TryPop(out var command))
        {
            try
            {
                command.Redo();
                _undoStack.Push(command);
            }
            catch (Exception ex)
            {
                OnError("Error redoing command.", ex);
                _undoStack.Clear(); // We likely have an invalid state now.
                _redoStack.Clear();
                return false;
            }
            EditorState.Instance.UnSaved = true;
            return true;
        }
        return false;
    }
}
