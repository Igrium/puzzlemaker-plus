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

    /// <summary>
    /// Called when a set of voxels have been updated.
    /// </summary>
    /// <param name="voxels">All the voxels which were updated. Godot has no PackedVector3IArray, so this has to do.</param>
    [Signal]
    public delegate void OnVoxelsUpdatedEventHandler(Vector3[] voxels);

    /// <summary>
    /// Called when a set of chunks have had the voxels in them updated.
    /// </summary>
    /// <param name="chunks">All chunks that were updated.</param>
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

    [Export]
    public LevelTheme Theme { get; set; } = new();

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
    }
    
    public void AddTestVoxels() 
    {
        // World.SetVoxel(0, 0, 0, new PuzzlemakerVoxel().WithOpen(true));
        // World.SetVoxel(0, 0, 1, new PuzzlemakerVoxel().WithOpen(true));
        World.Fill(new Vector3I(0, 0, 0), new Vector3I(7, 7, 7), new PuzzlemakerVoxel().WithOpen(true));
        World.SetVoxel(new Vector3I(0, 0, 0), new PuzzlemakerVoxel().WithOpen(false));

        //Vector3I portalable = new Vector3I(4, 0, 3);
        //World.SetVoxel(portalable, World.GetVoxel(portalable).WithPortalability(Direction.Down, true));
        World.UpdateVoxel(4, 0, 3, (block) => block.WithPortalability(Direction.Down, true));

        EmitOnChunksUpdated(new Vector3(0, 0, 0));
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
