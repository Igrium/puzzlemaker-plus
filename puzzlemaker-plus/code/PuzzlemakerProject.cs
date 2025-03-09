using System;
using System.IO;
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
        World = json.World;
    }

    private PuzzlemakerProjectJson WriteJson()
    {
        PuzzlemakerProjectJson json = new PuzzlemakerProjectJson();
        json.World = World;
        return json;
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

    private class PuzzlemakerProjectJson
    {
        public PuzzlemakerWorld World { get; set; } = new PuzzlemakerWorld();
    }
}
