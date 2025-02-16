using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// A collection of basic operators that involve selections.
/// </summary>
public static class SelectionUtils
{

    public record struct FaceReference(PuzzlemakerVoxel voxel, Direction face);


    /// <summary>
    /// Set the portalability of all faces in a selection.
    /// </summary>
    /// <param name="selection">Selection box.</param>
    /// <param name="world">World to apply to.</param>
    public static void SetPortalable(Aabb selection, VoxelWorld<PuzzlemakerVoxel> world, bool portalable)
    {
        // Remember it's min-inclusive, max-exclusive.
        Vector3I min = selection.Position.RoundInt();
        Vector3I max = selection.End.RoundInt();

        if (selection.HasVolume())
        {
            foreach (var pos in EnumerateBox(min, max))
            {
                world.UpdateVoxel(pos, block => block.WithPortalability(portalable));
            }
        }
        else
        {
            int axis;
            if (min.X == max.X)
                axis = 0;
            else if (min.Y == max.Y)
                axis = 1;
            else if (min.Z == max.Z)
                axis = 2;
            else
                throw new Exception("Igrium fucked up his math and we tried a 2D portalability update on a selection with volume.");

            Vector3I normal = new Vector3I();
            normal[axis] = 1;

            Direction positive = Directions.FromAxis(axis, false);
            Direction negative = Directions.FromAxis(axis, true);

            // Add 1 to 2d axis because this is exclusive.
            foreach (var pos in EnumerateBox(min, max + normal))
            {
                world.UpdateVoxel(pos, block => block.WithPortalability(negative, portalable));
                world.UpdateVoxel(pos - normal, block => block.WithPortalability(positive, portalable));
            }
        }
    }


    private static IEnumerable<Vector3I> EnumerateBox(Vector3I min, Vector3I max, bool inclusive = false)
    {
        if (inclusive)
            max += new Vector3I(1, 1, 1);

        for (int x = min.X; x < max.X; x++)
        {
            for (int y = min.Y; y < max.Y; y++)
            {
                for (int z = min.Z; z < max.Z; z++)
                {
                    yield return new Vector3I(x, y, z);
                }
            }
        }
    }

}
