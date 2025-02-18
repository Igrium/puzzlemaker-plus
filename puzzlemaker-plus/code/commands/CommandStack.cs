using System;
using System.Collections.Generic;
using Godot;

namespace PuzzlemakerPlus.Commands;

/// <summary>
/// Executes commands and keeps track of undo/redo history.
/// </summary>
public class CommandStack
{
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();

    /// <summary>
    /// Execute a command and add it to the undo stack. Clears the redo stack.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    public void ExecuteCommand(ICommand command)
    {
        GD.Print("Executing command " + command);
        _redoStack.Clear();
        try
        {
            command.Execute();
            _undoStack.Push(command);
        }
        catch (Exception ex)
        {
            GD.PrintErr("Error executing command:", ex);
            _undoStack.Clear(); // possible invalid state
        }
    }

    /// <summary>
    /// Attempt to undo the most-recent command.
    /// </summary>
    /// <returns>If there was a command to undo.</returns>
    public bool Undo()
    {
        if (_undoStack.TryPop(out var command))
        {
            try
            {
                command.Undo();
                _redoStack.Push(command);
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr("Error undoing command:", ex);
                _undoStack.Clear();
                _redoStack.Clear();
            }

        }
        return false;
    }

    /// <summary>
    /// Attempt to redo the most recently undone command.
    /// </summary>
    /// <returns>If there was a command to redo.</returns>
    public bool Redo()
    {
        if (_redoStack.TryPop(out var command))
        {
            try
            {
                command.Redo();
                _undoStack.Push(command);
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr("Error redoing command:", ex);
                _undoStack.Clear();
                _redoStack.Clear();
            }
        }
        return false;
    }
}
