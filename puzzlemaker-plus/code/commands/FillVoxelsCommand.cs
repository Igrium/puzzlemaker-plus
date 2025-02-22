using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus.Commands;

internal class FillVoxelsCommand : AbstractWorldCommand
{

    private Vector3I _minPos;
    private Vector3I _maxPos;

    private bool _isOpen;

    public FillVoxelsCommand(Vector3I minPos, Vector3I maxPos, bool isOpen)
    {
        _minPos = minPos;
        _maxPos = maxPos;
        _isOpen = isOpen;
    }

    public FillVoxelsCommand(Aabb selection, bool isOpen)
    {
        _isOpen = isOpen;
        _minPos = selection.Position.FloorInt();
        _maxPos = selection.End.CeilInt() - new Vector3I(1, 1, 1);
    }

    protected override void Execute(IVoxelView<PuzzlemakerVoxel> world)
    {
        bool portabale = SelectionUtils.AveragePortalability(new Aabb(_minPos, (_maxPos - _minPos) + new Vector3(1, 1, 1)), world) > 0;
        PuzzlemakerVoxel voxel = new PuzzlemakerVoxel().WithOpen(_isOpen).WithPortalability(portabale);
        world.Fill(_minPos, _maxPos, voxel);
    }
}
