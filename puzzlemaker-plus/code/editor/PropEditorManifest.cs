using System;
using Godot;
using Godot.Collections;

namespace PuzzlemakerPlus.code.editor;

/// <summary>
/// Proxy names for property editor scenes
/// </summary>
[GlobalClass]
public partial class PropEditorManifest : Resource
{
    private static PropEditorManifest? _instance;

    public static PropEditorManifest Instance
    {
        get
        {
            if (_instance == null)
                _instance = ResourceLoader.Load<PropEditorManifest>("res://prop_editors.tres") ??
                            throw new InvalidOperationException("No PropEditorManifest found");

            return _instance;
        }
    }
    
    [Export]
    public Dictionary<string, string> Editors { get; set; } = new();
    
    
}