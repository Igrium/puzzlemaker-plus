using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PuzzlemakerPlus.Commands;

namespace PuzzlemakerPlus;

/// <summary>
/// A global state kept for the entirety of the editor lifecycle.
/// </summary>
[GlobalClass]
public sealed partial class EditorState : Node
{
    private static EditorState? _instance;
    public static EditorState Instance => _instance ?? throw new InvalidOperationException("Editor has not finished initializing!");

    /// <summary>
    /// Called when a project has been opened.
    /// </summary>
    /// <param name="project">The project
    [Signal]
    public delegate void OnOpenProjectEventHandler(PuzzlemakerProject project);

    [Signal]
    public delegate void OnChangeProjectNameEventHandler(string? newValue);

    [Signal]
    public delegate void OnSetUnsavedEventHandler(bool unsaved);

    /// <summary>
    /// Called when a set of voxels have been updated.
    /// </summary>
    /// <param name="voxels">All the voxels which were updated. Godot has no PackedVector3IArray, so this has to do.</param>
    [Signal]
    public delegate void OnVoxelsUpdatedEventHandler(Vector3[] voxels);

    /// <summary>
    /// Called when a set of _chunks have had the voxels in them updated.
    /// </summary>
    /// <param name="chunks">All _chunks that were updated.</param>
    [Signal]
    public delegate void OnChunksUpdatedEventHandler(Vector3[] chunks);


    /// <summary>
    /// The current project loaded into the editor.
    /// An empty project by default.
    /// </summary>
    public PuzzlemakerProject Project { get; private set; } = new PuzzlemakerProject();

    /// <summary>
    /// The global voxel world. Null if there's no project loaded.
    /// Shortcut for Project?.World
    /// </summary>
    public PuzzlemakerWorld World => Project.World;

    public LevelTheme? Theme { get; private set; }
    public string ThemeName { get; private set; } = "res://assets/editor_themes/clean.json";

    /// <summary>
    /// Load a level theme from a file path.
    /// </summary>
    /// <param name="filepath">Theme's file path.</param>
    /// <returns>If the theme was able to load.</returns>
    public bool LoadTheme(string filepath)
    {
        GD.Print("Loading theme " + filepath);
        LevelTheme? newTheme = LevelTheme.Load(filepath);
        if (newTheme == null)
            return false;

        Theme = newTheme;
        ThemeName = System.IO.Path.GetFileNameWithoutExtension(filepath);
        return true;
    }

    public string EditorThemeName => Theme?.EditorTheme ?? EditorTheme.DEFAULT;

    /// <summary>
    /// Load the current editor theme from the level theme and return it.
    /// </summary>
    /// <returns>The editor theme.</returns>
    public EditorTheme GetEditorTheme()
    {
        return LoadEditorTheme(EditorThemeName);
    }

    private static EditorTheme LoadEditorTheme(string filepath)
    {
        Resource res = ResourceLoader.Load(filepath, "EditorTheme");
        if (filepath == EditorTheme.DEFAULT)
        {
            return (EditorTheme)res;
        }
        else
        {
            return res is EditorTheme theme ? theme : LoadEditorTheme(EditorTheme.DEFAULT);
        }
    }

    public CommandStack CommandStack => Project.CommandStack;

    /// <summary>
    /// The name of the currently open project. Null if the project has not been saved.
    /// </summary>
    public string? ProjectName
    {
        get => Project.FileName;
        set
        {
            if (Project.FileName == value) return;
            Project.FileName = value;
            EmitSignal(SignalName.OnChangeProjectName, value!);
        }
    }

    public bool HasProjectName => !string.IsNullOrWhiteSpace(ProjectName);

    private bool _unsaved;

    /// <summary>
    /// Whether the currently open project has unsaved changes.
    /// </summary>
    public bool UnSaved
    {
        get => _unsaved;
        set
        {
            if (_unsaved == value) return;
            _unsaved = value;
            EmitSignal(SignalName.OnSetUnsaved, value);
        }
    }

    public EditorState()
    {
        if (_instance != null)
            GD.PushWarning("Tried to initialize EditorState twice!");
        _instance = this;

    }

    public override void _Ready()
    {
        base._Ready();
        LoadTheme(ThemeName);
    }

    public void EmitOnChunksUpdated(params Vector3[] chunks)
    {
        EmitSignal(SignalName.OnChunksUpdated, chunks);
    }

    public void EmitOnChunksUpdated(IEnumerable<Vector3> chunks)
    {
        EmitOnChunksUpdated(chunks.ToArray());
    }

    public void EmitOnChunksUpdated(IEnumerable<Vector3I> chunks)
    {
        EmitOnChunksUpdated(chunks.Select(vec => (Vector3)vec).ToArray());
    }

    public void UpdateAllChunks()
    {
        EmitOnChunksUpdated(World.Chunks.Keys);
    }

    public void EmitOnChunksUpdated(Aabb bounds)
    {
        Vector3I minChunk = bounds.Position.RoundInt().GetChunk();
        Vector3I maxChunk = bounds.End.RoundInt().GetChunk();
        EmitOnChunksUpdated(minChunk, maxChunk);
    }

    public void EmitOnChunksUpdated(Vector3I min, Vector3I max)
    {
        Vector3I size = (max - min).Abs();
        Vector3[] chunks = new Vector3[(size.X + 1) * (size.Y + 1) * (size.Z + 1)];

        int i = 0;
        for (int x = min.X; x <= max.X; x++)
        {
            for (int y = min.Y; y <= max.Y; y++)
            {
                for (int z = min.Z; z <= max.Z; z++)
                {
                    chunks[i++] = new Vector3(x, y, z);
                }
            }
        }
        EmitOnChunksUpdated(chunks);
    }
}
