using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using PuzzlemakerPlus.Commands;

namespace PuzzlemakerPlus;

/// <summary>
/// A puzzlemaker project loaded in memory.
/// </summary>
[GlobalClass]
public partial class PuzzlemakerProject : RefCounted
{
    public PuzzlemakerWorld World { get; } = new PuzzlemakerWorld();

    public CommandStack CommandStack { get; } = new();
}
