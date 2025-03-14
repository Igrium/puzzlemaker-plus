using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

public class NewGreedyMesh
{

    private record struct FaceType()
    {
        public bool solid;
        public bool reverse;
        public bool portalable;
        public byte subdiv;

        public int GetMaterialIndex()
        {
            return subdiv * 2 + (portalable ? 1 : 0);
        }

        public static FaceType FromVoxel(PuzzlemakerVoxel voxel, Direction side, bool reverse)
        {
            return new FaceType()
            {
                solid = true,
                portalable = voxel.IsPortalable(side),
                subdiv = voxel.Subdivision,
                reverse = reverse
            };
        }
    }

    public static void DoGreedyMesh(IVoxelView<PuzzlemakerVoxel> world, Action<Quad> quadConsumer, int chunkSize = 16, float uvScale = 1, bool invert = false)
    {

        FaceType[] mask = new FaceType[chunkSize * chunkSize];

        // Sweep over each axis
        for (int axis = 0; axis < 3; axis++)
        {
            Array.Fill(mask, default);

            int uAxis = (axis + 1) % 3;
            int vAxis = (axis + 2) % 3;

            Direction dir = Directions.FromAxis(axis, false);
            Vector3I normal = Vector3I.Zero;
            normal[axis] = 1;

            // Check each slice of the chunk
            Vector3I pos = new();
            int b = 0;
            for (pos[axis] = -1; pos[axis] < chunkSize;)
            {
                // Compute mask
                int n = 0;
                for (pos[vAxis] = 0; pos[vAxis] < chunkSize; pos[vAxis]++)
                {
                    for (pos[uAxis] = 0; pos[uAxis] < chunkSize; pos[uAxis]++)
                    {
                        PuzzlemakerVoxel currentBlock = world.GetVoxel(pos);
                        PuzzlemakerVoxel compareBlock = world.GetVoxel(pos + normal);

                        // For positive-facing we're in the compare block looking at this one. Otherwise, we're in this one looking at compare.
                        // TODO: Is this true?

                        if (currentBlock.IsOpen && !compareBlock.IsOpen)
                        {
                            mask[n] = FaceType.FromVoxel(currentBlock, dir, false);
                        }
                        else if (compareBlock.IsOpen && !currentBlock.IsOpen)
                        {
                            mask[n] = FaceType.FromVoxel(compareBlock, dir.Opposite(), true);
                        }
                        
                        n++;
                    }
                }

                pos[axis]++;

                // Generate a mesh from the mask using lexicographic ordering,
                // by looping over each block in this slice of the chunk
                for (byte r = 0; r < 2; r++)
                {
                    bool reverse = r == 1;

                    n = 0;
                    for (int v = 0; v < chunkSize; v++)
                    {
                        //GD.Print(v);
                        for (int u = 0; u < chunkSize;)
                        {
                            //GD.Print(u);
                            FaceType face = mask[n];
                            if (face.solid)
                            {

                                // Compute the width of this quad and store it in quadWidth
                                // This is done by searching along the current axis until mask[n + quadWidth] is false
                                int quadWidth = 1;
                                while (u + quadWidth < PuzzlemakerWorld.CHUNK_SIZE && mask[n + quadWidth] == face)
                                {
                                    //GD.Print("widthLoop");
                                    quadWidth++;
                                }

                                // Compute the height of this quad and store it in quadHeight
                                // This is done by checking if every block next to this row (range 0 to quadWidth) is also part of the mask.
                                // For example, if quadWidth is 5 we currently have a quad of dimensions 1 Position 5. To reduce triangle count,
                                // greedy meshing will attempt to expand this quad out to chunkSize Position 5, but will stop if it reaches a hole in the mask
                                int quadHeight = 1;
                                bool done = false;
                                while (v + quadHeight < PuzzlemakerWorld.CHUNK_SIZE)
                                {

                                    for (int k = 0; k < quadWidth; k++)
                                    {
                                        //GD.Print("heightLoop");
                                        // Check each block next to this quad
                                        if (mask[n + k + quadHeight * chunkSize] != face)
                                        {
                                            done = true;
                                            break;
                                        }
                                    }
                                    if (done)
                                        break;
                                    quadHeight++;
                                }
                                pos[uAxis] = u;
                                pos[vAxis] = v;

                                // du and dv determine the size and orientation of this face
                                Vector3I du = new();
                                du[uAxis] = quadWidth;

                                Vector3I dv = new();
                                dv[vAxis] = quadHeight;

                                // CREATE QUAD
                                var quad = new Quad
                                    (
                                        new Vector3I(pos[0], pos[1], pos[2]),
                                        new Vector3I(pos[0] + du[0], pos[1] + du[1], pos[2] + du[2]),
                                        new Vector3I(pos[0] + du[0] + dv[0], pos[1] + du[1] + dv[1], pos[2] + du[2] + dv[2]),
                                        new Vector3I(pos[0] + dv[0], pos[1] + dv[1], pos[2] + dv[2])
                                    ).WithResetNormals();

                                // Compute UVs
                                quad.UV1 = new Vector2(pos[uAxis] * uvScale, 1 - pos[vAxis] * uvScale);
                                quad.UV2 = new Vector2((pos[uAxis] + du[uAxis]) * uvScale, 1 - pos[vAxis] * uvScale);
                                quad.UV3 = new Vector2((pos[uAxis] + du[uAxis]) * uvScale, 1 - (pos[vAxis] + dv[vAxis]) * uvScale);
                                quad.UV4 = new Vector2(pos[uAxis] * uvScale, 1 - (pos[vAxis] + dv[vAxis]) * uvScale);

                                if ((face.reverse && !invert) || (!face.reverse && invert))
                                {
                                    quad = quad.Flipped();
                                }

                                quad.MaterialIndex = face.GetMaterialIndex();
                                quadConsumer(quad);


                                // Clear this part of the mask, so we don't add duplicate faces
                                for (var l = 0; l < quadHeight; l++)
                                {
                                    for (var k = 0; k < quadWidth; k++)
                                    {
                                        mask[n + quadWidth + l * chunkSize] = default;
                                    }
                                }

                                u += quadWidth;
                                n += quadHeight;
                            }
                            else
                            {
                                u++;
                                n++;
                            }
                        }

                    }

                }
                GD.Print(pos[axis]);
            }

            GD.Print($"Looped {b} times");


            //for (int b = 0; b < 2; b++)
            //{
            //    bool negative = b == 1;
            //    Vector3I pos = Vector3I.Zero;

            //    Direction dir = Directions.FromAxis(axis, negative);


            //    // Move through the chunk from front to back.
            //    for (pos[axis] = -1; pos[axis] < ChunkSize;)
            //    {
            //        // Compute forwardMask

            //        int n = 0;
            //        for (int v = 0; v < ChunkSize; v++)
            //        {
            //            for (int u = 0; u < ChunkSize; u++)
            //            {
            //                pos[axisU] = u;
            //                pos[axisV] = v;

            //                forwardMask[n++] = getVoxelFace(in pos, dir);
            //            }
            //        }
            //    }
            //}
        }
    }
}
