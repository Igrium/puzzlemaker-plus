using Godot;

namespace PuzzlemakerPlus.Commands;

public class SetPortalabilityCommand : AbstractWorldCommand
{
    public Aabb Selection { get; }
    public bool Portalable { get; }

    public SetPortalabilityCommand(Aabb selection, bool portalable)
    {
        Selection = selection;
        Portalable = portalable;
    }

    protected override void Execute(IVoxelView<PuzzlemakerVoxel> world)
    {
        SelectionUtils.SetPortalable(Selection, world, Portalable);
    }
}
