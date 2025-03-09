using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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

    [Export]
    public LevelTheme Theme { get; set; } = new();

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
        GD.Print("Editor state is ready!");
        var theme = LevelTheme.LoadTheme("res://assets/themes/clean.json");
        if (theme != null)
            Theme = theme;
    }

    public PuzzlemakerProject NewProject()
    {
        GD.Print("Creating new project...");
        PuzzlemakerProject project = new PuzzlemakerProject();
        OpenProject(project);
        return project;
    }

    public void OpenProject(PuzzlemakerProject project)
    {
        if (project == Project) return;
        Project = project;
        EmitSignal(SignalName.OnOpenProject, project);
        EmitSignal(SignalName.OnChangeProjectName, project.FileName!);
        UnSaved = false;
    }

    public void OpenProjectFile(string filepath)
    {
        PuzzlemakerProject? project;
        using (var stream = new FileAccessStream(filepath))
        {
            project = PuzzlemakerProject.ReadFile(stream);
        }
        if (project == null)
            throw new Exception("Json deserializer returned null.");
        project.FileName = filepath;
        OpenProject(project);
    }

    public void SaveProjectAs(string filepath)
    {
        using (var stream = new FileAccessStream(filepath, Godot.FileAccess.ModeFlags.Write))
        {
            Project.WriteFile(stream);
        }
        ProjectName = filepath;
        UnSaved = false;
        GD.Print("Saved to " + filepath);
    }

    public void SaveProject()
    {
        if (!HasProjectName)
            throw new InvalidOperationException("Project name has not been set. Please use SaveProjectAs()");

        SaveProjectAs(ProjectName!);
    }

    public bool Undo()
    {
        return CommandStack.Undo();
    }

    public bool Redo()
    {
        return CommandStack.Redo();
    }
    
    public void AddTestVoxels() 
    {
        // World.SetVoxel(0, 0, 0, new PuzzlemakerVoxel().WithOpen(true));
        // World.SetVoxel(0, 0, 1, new PuzzlemakerVoxel().WithOpen(true));
        World.Fill(new Vector3I(-16, 0, -32), new Vector3I(31, 7, 31), new PuzzlemakerVoxel().WithOpen(true));
        World.SetVoxel(new Vector3I(0, 0, 0), new PuzzlemakerVoxel().WithOpen(false));

        //Vector3I portalable = new Vector3I(4, 0, 3);
        //World.SetVoxel(portalable, World.GetVoxel(portalable).WithPortalability(Direction.Down, true));
        World.UpdateVoxel(4, 0, 3, (block) => block.WithPortalability(Direction.Down, true));

        UpdateAllChunks();
        //EmitOnChunksUpdated(new Aabb(new Vector3(-4, 0, 4), new Vector3(8, 0, 8)));
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
        Vector3I minChunk = (bounds.Position / 16f).FloorInt();
        Vector3I maxChunk = (bounds.End / 16f).FloorInt();

        if (minChunk == maxChunk)
        {
            EmitOnChunksUpdated(minChunk);
        }
        else
        {
            List<Vector3> chunks = new List<Vector3>();
            for (int x = minChunk.X; x <= maxChunk.X; x++)
            {
                for (int y = minChunk.Y; x <= maxChunk.Y; y++)
                {
                    for (int z = minChunk.Z; x <= maxChunk.Z; z++)
                    {
                        chunks.Add(new Vector3(x, y, z));
                    }
                }
            }

            EmitOnChunksUpdated(chunks.ToArray());
        }
    }

}
