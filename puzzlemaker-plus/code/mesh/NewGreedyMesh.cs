using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

    // Code ported from https://0fps.net/2012/06/30/meshing-in-a-minecraft-game/

    // Note this implementation does not support different block types or block normals
    // The original author describes how to do this here: https://0fps.net/2012/07/07/meshing-minecraft-part-2/



    public static void DoGreedyMesh(IVoxelView<PuzzlemakerVoxel> world, Action<Quad> faceConsumer, int chunkSize = 16, float uvScale = 1, bool invert = false)
    {

        // Sweep over each axis (X, Y and Z)
        for (var axis = 0; axis < 3; ++axis)
        {
            int u, v, k, l, w, h;
            int uAxis = (axis + 1) % 3;
            int vAxis = (axis + 2) % 3;
            var pos = new Vector3I();
            var normal = new Vector3I();

            var mask = new FaceType[chunkSize * chunkSize];
            normal[axis] = 1;

            // Check each slice of the chunk one at a time
            for (pos[axis] = -1; pos[axis] < chunkSize;)
            {
                Direction direction = Directions.FromAxis(axis, false);
                // Compute the mask
                var n = 0;
                for (pos[vAxis] = 0; pos[vAxis] < chunkSize; ++pos[vAxis])
                {
                    for (pos[uAxis] = 0; pos[uAxis] < chunkSize; ++pos[uAxis])
                    {
                        PuzzlemakerVoxel currentBlock = world.GetVoxel(pos);
                        PuzzlemakerVoxel compareBlock = world.GetVoxel(pos + normal);

                        // Reverse normal: compare block looking at this one. Forward: this looking at compare.
                        if (currentBlock.IsOpen && !compareBlock.IsOpen)
                        {
                            mask[n++] = FaceType.FromVoxel(currentBlock, direction, false);
                        }
                        else if (!currentBlock.IsOpen && compareBlock.IsOpen)
                        {
                            mask[n++] = FaceType.FromVoxel(compareBlock, direction.Opposite(), true);
                        }
                        else
                        {
                            mask[n++] = default;
                        }



                        // normal determines the normal (X, Y or Z) that we are searching
                        // m.IsBlockAt(pos,y,z) takes global map positions and returns true if a block exists there

                        //bool blockCurrent = 0 <= pos[axis] ? IsBlockAt(pos[0] + chunkPosX, pos[1] + chunkPosY, pos[2] + chunkPosZ) : true;
                        //bool blockCompare = pos[axis] < chunkSize - 1 ? IsBlockAt(pos[0] + normal[0] + chunkPosX, pos[1] + normal[1] + chunkPosY, pos[2] + normal[2] + chunkPosZ) : true;

                        //// The mask is set to true if there is a visible face between two blocks,
                        ////   u.e. both aren't empty and both aren't blocks
                        //mask[n++] = blockCurrent != blockCompare;
                    }
                }

                ++pos[axis];

                n = 0;

                // Generate a mesh from the mask using lexicographic ordering,      
                //   by looping over each block in this slice of the chunk
                for (v = 0; v < chunkSize; ++v)
                {
                    for (u = 0; u < chunkSize;)
                    {
                        FaceType face = mask[n];
                        if (face.solid)
                        {
                            // Compute the width of this quad and store it in w                        
                            //   This is done by searching along the current axis until mask[n + w] is false
                            for (w = 1; u + w < chunkSize && mask[n + w] == face; w++) { }

                            // Compute the height of this quad and store it in h                        
                            //   This is done by checking if every block next to this row (range 0 to w) is also part of the mask.
                            //   For example, if w is 5 we currently have a quad of dimensions 1 pos 5. To reduce triangle count,
                            //   greedy meshing will attempt to expand this quad out to chunkSize pos 5, but will stop if it reaches a hole in the mask

                            var done = false;
                            for (h = 1; v + h < chunkSize; h++)
                            {
                                // Check each block next to this quad
                                for (k = 0; k < w; ++k)
                                {
                                    // If there's a hole in the mask, exit
                                    if (mask[n + k + h * chunkSize] != face)
                                    {
                                        done = true;
                                        break;
                                    }
                                }

                                if (done)
                                    break;
                            }

                            pos[uAxis] = u;
                            pos[vAxis] = v;

                            // du and dv determine the size and orientation of this face
                            var du = new Vector3I();
                            du[uAxis] = w;

                            var dv = new Vector3I();
                            dv[vAxis] = h;

                            // Create quad for this face.
                            Quad quad = new Quad(pos, pos + du, pos + du + dv, pos + dv);
                            quad.ResetNormals();

                            quad.UV1 = new Vector2(pos[uAxis] * uvScale, 1 - pos[vAxis] * uvScale);
                            quad.UV2 = new Vector2((pos[uAxis] + du[uAxis]) * uvScale, 1 - pos[vAxis] * uvScale);
                            quad.UV3 = new Vector2((pos[uAxis] + du[uAxis]) * uvScale, 1 - (pos[vAxis] + dv[vAxis]) * uvScale);
                            quad.UV4 = new Vector2(pos[uAxis] * uvScale, 1 - (pos[vAxis] + dv[vAxis]) * uvScale);

                            if (face.reverse != invert) // face.reverse && !invert || !face.reverse && invert
                            {
                                quad = quad.Flipped();
                            }

                            quad.MaterialIndex = face.GetMaterialIndex();

                            faceConsumer(quad);

                            // Create a quad for this face. Colour, normal or textures are not stored in this block vertex format.
                            //BlockVertex.AppendQuad(new Int3(pos[0], pos[1], pos[2]),                 // Top-left vertice position
                            //                       new Int3(pos[0] + du[0], pos[1] + du[1], pos[2] + du[2]),         // Top right vertice position
                            //                       new Int3(pos[0] + dv[0], pos[1] + dv[1], pos[2] + dv[2]),         // Bottom left vertice position
                            //                       new Int3(pos[0] + du[0] + dv[0], pos[1] + du[1] + dv[1], pos[2] + du[2] + dv[2])  // Bottom right vertice position
                            //                       );

                            // Clear this part of the mask, so we don't add duplicate faces
                            for (l = 0; l < h; ++l)
                                for (k = 0; k < w; ++k)
                                    mask[n + k + l * chunkSize] = default;

                            // Increment counters and continue
                            u += w;
                            n += w;
                        }
                        else
                        {
                            u++;
                            n++;
                        }
                    }
                }
            }
        }
    }

}
