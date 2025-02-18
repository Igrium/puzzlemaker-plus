using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using VMFLib.VClass;

namespace PuzzlemakerPlus.Commands;

public class AddTextVoxelsCommand : AbstractWorldCommand
{
    protected override void Execute(IVoxelView<PuzzlemakerVoxel> world)
    {
        world.Fill(new Vector3I(-16, 0, -32), new Vector3I(32, 7, 32), new PuzzlemakerVoxel().WithOpen(true));
        world.SetVoxel(new Vector3I(0, 0, 0), new PuzzlemakerVoxel().WithOpen(false));

        //Vector3I portalable = new Vector3I(4, 0, 3);
        //World.SetVoxel(portalable, World.GetVoxel(portalable).WithPortalability(Direction.Down, true));
        world.UpdateVoxel(4, 0, 3, (block) => block.WithPortalability(Direction.Down, true));
    }
}
