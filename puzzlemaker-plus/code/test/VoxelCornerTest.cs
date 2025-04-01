using System;
using GdUnit4;
using Godot;

using static GdUnit4.Assertions;

namespace PuzzlemakerPlus.Test;

[TestSuite]
public class VoxelCornerTest
{
    [TestCase]
    public void TestVisibleEdges()
    {
        PuzzlemakerWorld world = new PuzzlemakerWorld();
        world.SetVoxel(new Vector3I(0, 0, 0), new PuzzlemakerVoxel() { IsOpen = true });
        world.SetVoxel(new Vector3I(0, 0, 0), new PuzzlemakerVoxel() { IsOpen = true });

        DirectionFlags flags = VoxelCornersOld.GetVisibleEdges(world, new Vector3I(1, 1, 1));
        AssertArray(flags.AsArray()).ContainsExactly(false, true, true, false, true, false);

        GD.Print(flags);
    }
}
