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
    private bool _prevPortalability;

    public void TogglePortalability()
    {
        EditorState editor = EditorState.Instance;
        SelectionUtils.SetPortalable(editor.Selection, editor.World, !_prevPortalability);
        editor.EmitOnChunksUpdated(editor.Selection);
        _prevPortalability = !_prevPortalability;
    }
}
