using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus.Commands;

public class SetPortalabilityCommand : AbstractVoxelCommand
{
    private readonly Aabb _selection;
    private readonly bool _portalable;

    public SetPortalabilityCommand(in Aabb selection, bool portalable)
    {
        this._selection = selection;
        this._portalable = portalable;
    }

    protected override void Execute(PuzzlemakerWorld world)
    {
        SelectionUtils.SetPortalable(_selection, world, _portalable);
    }

    protected override IEnumerable<Vector3I> GetModifiedChunks()
    {
        return SelectionUtils.GetSelectedChunks(in _selection, !_selection.HasVolume());
    }
}