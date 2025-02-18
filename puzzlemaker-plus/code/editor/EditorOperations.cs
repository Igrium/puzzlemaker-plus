using Godot;
using PuzzlemakerPlus.Commands;

namespace PuzzlemakerPlus.Editor;

/// <summary>
/// Implementation of general editor operations.
/// </summary>
[GlobalClass]
public partial class EditorOperations : Node
{
    public void TogglePortalability()
    {
        EditorState editor = EditorState.Instance;

        bool portalable = SelectionUtils.AveragePortalability(editor.Selection, editor.World) > 0;
        editor.ExecuteCommand(new SetPortalabilityCommand(editor.Selection, portalable));

    }
}