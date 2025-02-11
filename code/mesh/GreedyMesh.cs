
using Godot;
using System.Collections.Generic;

namespace PuzzlemakerPlus;

public static class GreedyMesh
{
    public static IEnumerable<Quad> DoGreedyMesh(PuzzlemakerWorld world, int chunkSize, Vector3I chunkPos, float uvScale = 1, bool invert = false)
    {
        int chunkPosX = chunkPos.X;
        int chunkPosY = chunkPos.Y;
        int chunkPosZ = chunkPos.Z;

        bool IsBlockAt(int x, int y, int z)
        {
            return world.GetVoxel(x, y, z).IsOpen;
        }

        // Sweep over each axis (X, Y and Z)
        for (var currentAxis = 0; currentAxis < 3; ++currentAxis)
        {
            // 0 = negative, 1 = positive.
            for (var dir = 0; dir <= 1; dir++)
            {
                int i, j, k, l, quadWidth, quadHeight;
                int axisU = (currentAxis + 1) % 3;
                int axisV = (currentAxis + 2) % 3;
                var position = new int[3];
                var direction = new int[3];

                var mask = new bool[chunkSize * chunkSize];
                direction[currentAxis] = 1;

                // Check each slice of the chunk one at a time
                for (position[currentAxis] = -1; position[currentAxis] < chunkSize;)
                {
                    // Compute the mask
                    var n = 0;
                    for (position[axisV] = 0; position[axisV] < chunkSize; ++position[axisV])
                    {
                        for (position[axisU] = 0; position[axisU] < chunkSize; ++position[axisU])
                        {
                            // Direction determines the Direction (X, Y or Z) that we are searching
                            // m.IsBlockAt(Position,y,z) takes global map positions and returns true if a block exists there

                            bool blockCurrent = IsBlockAt(position[0] + chunkPosX, position[1] + chunkPosY, position[2] + chunkPosZ);
                            bool blockCompare = IsBlockAt(position[0] + direction[0] + chunkPosX, position[1] + direction[1] + chunkPosY, position[2] + direction[2] + chunkPosZ);

                            if (dir == 1)
                            {
                                mask[n++] = blockCurrent && !blockCompare;
                            }
                            else
                            {
                                mask[n++] = blockCompare && !blockCurrent;
                            }

                            // The mask is set to true if there is a visible face between two blocks,
                            //   i.e. both aren't empty and both aren't blocks
                            // mask[n++] = blockCurrent != blockCompare;
                        }
                    }

                    ++position[currentAxis];

                    n = 0;

                    // Generate a mesh from the mask using lexicographic ordering,      
                    //   by looping over each block in this slice of the chunk
                    for (j = 0; j < chunkSize; ++j)
                    {
                        for (i = 0; i < chunkSize;)
                        {
                            if (mask[n])
                            {
                                // Compute the width of this quad and store it in quadWidth                        
                                //   This is done by searching along the current axis until mask[n + quadWidth] is false
                                for (quadWidth = 1; i + quadWidth < chunkSize && mask[n + quadWidth]; quadWidth++) { }

                                // Compute the height of this quad and store it in quadHeight                        
                                //   This is done by checking if every block next to this row (range 0 to quadWidth) is also part of the mask.
                                //   For example, if quadWidth is 5 we currently have a quad of dimensions 1 Position 5. To reduce triangle count,
                                //   greedy meshing will attempt to expand this quad out to chunkSize Position 5, but will stop if it reaches a hole in the mask

                                var done = false;
                                for (quadHeight = 1; j + quadHeight < chunkSize; quadHeight++)
                                {
                                    // Check each block next to this quad
                                    for (k = 0; k < quadWidth; ++k)
                                    {
                                        // If there's a hole in the mask, exit
                                        if (!mask[n + k + quadHeight * chunkSize])
                                        {
                                            done = true;
                                            break;
                                        }
                                    }

                                    if (done)
                                        break;
                                }

                                position[axisU] = i;
                                position[axisV] = j;

                                // du and dv determine the size and orientation of this face
                                var du = new int[3];
                                du[axisU] = quadWidth;

                                var dv = new int[3];
                                dv[axisV] = quadHeight;

                                // Create a quad for this face. Colour, normal or textures are not stored in this block vertex format.
                                Quad quad = new Quad(new Vector3I(position[0], position[1], position[2]),                 // Top-left vertice Position
                                                       new Vector3I(position[0] + du[0], position[1] + du[1], position[2] + du[2]),         // Top right vertice Position
                                                       new Vector3I(position[0] + du[0] + dv[0], position[1] + du[1] + dv[1], position[2] + du[2] + dv[2]),  // Bottom right vertice Position
                                                       new Vector3I(position[0] + dv[0], position[1] + dv[1], position[2] + dv[2])         // Bottom left vertice Position
                                                       ).WithResetNormals();

                                // Compute UVs
                                quad.UV1 = new Vector2(position[axisU] * uvScale, 1 - position[axisV] * uvScale);
                                quad.UV2 = new Vector2((position[axisU] + quadWidth) * uvScale, 1 - position[axisV] * uvScale);
                                quad.UV3 = new Vector2((position[axisU] + quadWidth) * uvScale, 1 - (position[axisV] + quadHeight) * uvScale);
                                quad.UV4 = new Vector2(position[axisU] * uvScale, 1 - (position[axisV] + quadHeight) * uvScale);


                                if ((dir == 1 && !invert) || (dir == 0 && invert))
                                {
                                    quad = quad.Flipped();
                                }

                                yield return quad;

                                // Clear this part of the mask, so we don't add duplicate faces
                                for (l = 0; l < quadHeight; ++l)
                                    for (k = 0; k < quadWidth; ++k)
                                        mask[n + k + l * chunkSize] = false;

                                // Increment counters and continue
                                i += quadWidth;
                                n += quadWidth;
                            }
                            else
                            {
                                i++;
                                n++;
                            }
                        }
                    }
                }
            }
        }
        yield break;
    }
}