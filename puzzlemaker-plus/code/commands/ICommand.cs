using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzlemakerPlus.Commands;

/// <summary>
/// An undoable operation the user can perform in the editor.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Execute the command for the first time.
    /// </summary>
    public void Execute();

    /// <summary>
    /// Undo the command. Expects the project state is exactly as Execute() left it.
    /// </summary>
    public void Undo();

    /// <summary>
    /// Redo the command after it has been undone. 
    /// Expects the project state is exactly as it was the first time Execute() was called.
    /// </summary>
    public void Redo();
}
