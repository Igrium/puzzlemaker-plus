using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

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
        SelectionUtils.SetPortalable(editor.Selection, editor.World, !portalable);

        editor.EmitOnChunksUpdated(editor.Selection);

    }
}