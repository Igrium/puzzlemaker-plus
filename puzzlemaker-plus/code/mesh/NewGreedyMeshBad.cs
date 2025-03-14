using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

public class NewGreedyMeshBad
{
    record struct VoxelFace
    {
        public bool Transparent;
        public bool Portalable;
        public byte Subdivision;
        public Direction Side;

        public static VoxelFace FromVoxel(PuzzlemakerVoxel voxel, Direction side)
        {
            return new VoxelFace()
            {
                Portalable = voxel.IsPortalable(side),
                Subdivision = voxel.Subdivision,
                Side = side,
            };
        }

        public static VoxelFace NewTransparent()
        {
            return new VoxelFace()
            {
                Transparent = true
            };
        }
    }

    public int ChunkSize { get; set; } = PuzzlemakerWorld.CHUNK_SIZE;

    public void Greedy(IVoxelView<PuzzlemakerVoxel> world, Action<Quad> faceConsumer)
    {

        VoxelFace getVoxelFace(int x, int y, int z, Direction side)
        {
            Vector3I pos = new Vector3I(x, y, z);
            PuzzlemakerVoxel voxel = world.GetVoxel(pos);
            if (!voxel.IsOpen)
                return VoxelFace.NewTransparent();

            PuzzlemakerVoxel compare = world.GetVoxel(pos + side.GetNormal());
            if (compare.IsOpen)
                return VoxelFace.NewTransparent();

            return VoxelFace.FromVoxel(voxel, side);
        }

        /*
         * These are just working variables for the algorithm - almost all taken 
         * directly from Mikola Lysenko's javascript implementation.
         */
        int i, j, k, l, w, h, u, v, n = 0;

        Direction side;

        int[] x = new int[] { 0, 0, 0 };
        int[] q = new int[] { 0, 0, 0 };
        int[] du = new int[] { 0, 0, 0 };
        int[] dv = new int[] { 0, 0, 0 };

        VoxelFace[] mask = new VoxelFace[ChunkSize * ChunkSize];

        VoxelFace voxelFace, voxelFace1;

        /**
         * We start with the lesser-spotted boolean for-loop (also known as the old flippy floppy). 
         * 
         * The variable backFace will be TRUE on the first iteration and FALSE on the second - this allows 
         * us to track which direction the indices should run during creation of the quad.
         * 
         * This loop runs twice, and the inner loop 3 times - totally 6 iterations - one for each 
         * voxel face.
         */
        for (bool backFace = true, b = false; b != backFace; backFace = backFace && b, b = !b)
        {
            // For each dimension
            for (int d = 0; d < 3; d++)
            {
                u = (d + 1) % 3;
                v = (d + 2) % 3;

                x[0] = 0;
                x[1] = 0;
                x[2] = 0;

                q[0] = 0;
                q[1] = 0;
                q[2] = 0;
                q[d] = 1;

                // Keep track of the side we're meshing.
                switch (d)
                {
                    case 0:
                        side = backFace ? Direction.Left : Direction.Right; 
                        break;
                    case 1:
                        side = backFace ? Direction.Down : Direction.Up;
                        break;
                    case 2:
                        side = backFace ? Direction.Forward : Direction.Back;
                        break;
                    default:
                        throw new Exception(); // Make the compiler happy; should not happen.
                }

                // We move through the dimension from front to back
                for (x[d] = -1; x[d] < ChunkSize;)
                {
                    // Compute the mask
                    n = 0;

                    for (x[v] = 0; x[v] < ChunkSize; x[v]++)
                    {
                        for (x[u] = 0; x[u] < ChunkSize; x[u]++)
                        {
                            voxelFace = getVoxelFace(x[0], x[1], x[2], side);
                            voxelFace1 = getVoxelFace(x[0] + q[0], x[1] + q[1], x[2] + q[2], side);

                            mask[n++] = voxelFace == voxelFace1 ? VoxelFace.NewTransparent() : (backFace ? voxelFace1 : voxelFace);

                            //mask[n++] = ((voxelFace.HasValue && voxelFace1.HasValue && voxelFace == voxelFace1))
                            //    ? null : backFace ? voxelFace1 : voxelFace;
                        }
                    }

                    x[d]++;


                    // Generate the mesh for the mask.
                    n = 0;
                    for (j = 0; j < ChunkSize; j++)
                    {
                        for (i = 0; i < ChunkSize; i++)
                        {
                            if (!mask[n].Transparent)
                            {
                                // Compute the width
                                for (w = 1; i + w < ChunkSize && mask[n + w] == mask[n]; w++) {}

                                // Compute the height;
                                bool done = false;
                                for (h = 1; j + h < ChunkSize; h++)
                                {
                                    for (k = 0; k < w; k++)
                                    {
                                        if (mask[n + k + h * ChunkSize] != mask[n])
                                        {
                                            done = true;
                                            break;
                                        }
                                    }

                                    if (done) break;
                                }

                                // Add quad
                                x[u] = i;
                                x[v] = j;

                                du[0] = 0;
                                du[1] = 0;
                                du[2] = 0;
                                du[u] = w;

                                dv[0] = 0;
                                dv[1] = 0;
                                dv[2] = 0;
                                dv[v] = h;

                                Quad quad = new Quad(
                                    new Vector3(x[0], x[1], x[2]),
                                    new Vector3(x[0] + du[0], x[1] + du[1], x[2] + du[2]),
                                    new Vector3(x[0] + du[0] + dv[0], x[1] + du[1] + dv[1], x[2] + du[2] + dv[2]),
                                    new Vector3(x[0] + dv[0], x[1] + dv[1], x[2] + dv[2]));

                                Vector3 normal = new Vector3();
                                normal[d] = 1;

                                quad.Normal1 = normal;
                                quad.Normal2 = normal;
                                quad.Normal3 = normal;
                                quad.Normal4 = normal;

                                if (backFace)
                                    quad = quad.Flipped();

                                faceConsumer(quad);

                                // Clear this part of the mask, so we don't add duplicate faces
                                for (l = 0; l < h; ++l)
                                {
                                    for (k = 0; k < w; ++k) { mask[n + k + l * ChunkSize] = VoxelFace.NewTransparent(); }
                                }

                                i += w;
                                n += w;
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
    }
}
