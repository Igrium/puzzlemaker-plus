using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using PuzzlemakerPlus.Commands;

namespace PuzzlemakerPlus;

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
        editor.CommandStack.Execute(new SetPortalabilityCommand(editor.Selection, !portalable));
        //SelectionUtils.SetPortalable(editor.Selection, editor.World, !portalable);

        //editor.EmitOnChunksUpdated(editor.Selection);
    }

    public void FillSelection()
    {
        EditorState editor = EditorState.Instance;
        editor.CommandStack.Execute(new FillVoxelsCommand(editor.Selection, false));
    }

    public void EmptySelection()
    {
        EditorState editor = EditorState.Instance;
        editor.CommandStack.Execute(new FillVoxelsCommand(editor.Selection, true));
    }

    public void Undo()
    {
        EditorState.Instance.CommandStack.Undo();
    }

    public void Redo()
    {
        EditorState.Instance.CommandStack.Redo();
    }
}