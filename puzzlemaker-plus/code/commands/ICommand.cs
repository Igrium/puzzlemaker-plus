
namespace PuzzlemakerPlus;

/// <summary>
/// A command that can be performed in the editor and undone.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Execute this command for the first time.
    /// </summary>
    public void Execute();

    /// <summary>
    /// Undo the command. Expects the project state is exactly as this command left it.
    /// </summary>
    public void Undo();

    /// <summary>
    /// Redo the command after it has been undone. Expects the project state is exactly as it was the first time the command was called.
    /// </summary>
    public void Redo()
    {
        Execute();
    }
}
