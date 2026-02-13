using System;
using System.Collections.Generic;
using Godot;
using PuzzlemakerPlus.Items;

namespace PuzzlemakerPlus.Commands;

public class MoveItemCommand(Item item, Vector3 newPos, Vector3 newRot) : ICommand
{
    private Vector3 _oldPos;
    private Vector3 _oldRot;

    public bool Execute()
    {
        _oldPos = item.Position;
        _oldRot = item.Rotation;
        item.Position = newPos;
        item.Rotation = newRot;
        return true;
    }

    public void Redo()
    {
        item.Position = newPos;
        item.Rotation = newRot;
    }

    public void Undo()
    {
        item.Position = _oldPos;
        item.Rotation = _oldRot;
    }
}
