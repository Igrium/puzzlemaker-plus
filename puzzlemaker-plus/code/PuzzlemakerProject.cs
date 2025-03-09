using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using PuzzlemakerPlus.Commands;

namespace PuzzlemakerPlus;

/// <summary>
/// A puzzlemaker project loaded in memory.
/// </summary>
[GlobalClass]
public partial class PuzzlemakerProject : RefCounted
{
    public PuzzlemakerWorld World { get; }

    public CommandStack CommandStack { get; } = new();

    /// <summary>
    /// The file path at which this project is saved. Null if it's a new project that hasn't been saved yet.
    /// </summary>
    public string? FileName { get; set; }


    public PuzzlemakerProject()
    {
        World = new PuzzlemakerWorld();
    }

    private PuzzlemakerProject(PuzzlemakerProjectJson json)
    {
        GD.Print("Loaded project from json: " + json);
        World = json.World;
    }

    private PuzzlemakerProjectJson WriteJson()
    {
        PuzzlemakerProjectJson json = new PuzzlemakerProjectJson();
        json.World = World;
        return json;
    }

    /// <summary>
    /// Get an array of all the chunks in the world that contain voxels. Utility to call from GDScript.
    /// </summary>
    /// <returns>PackedVector3Array of occupied chunks.</returns>
    public Vector3[] GetOccupiedChunks()
    {
        return World.Chunks.Keys.Select(vec => new Vector3(vec.X, vec.Y, vec.Z)).ToArray();
    }

    public void WriteFile(Stream stream)
    {
        var json = WriteJson();
        JsonSerializer.Serialize(stream, json);
    }

    public static PuzzlemakerProject ReadFile(Stream stream)
    {
        var json = JsonSerializer.Deserialize<PuzzlemakerProjectJson>(stream) ?? throw new Exception("Unable to read json");
        return new PuzzlemakerProject(json);
    }

    private record class PuzzlemakerProjectJson
    {
        public PuzzlemakerWorld World { get; set; } = new PuzzlemakerWorld();
    }
}
