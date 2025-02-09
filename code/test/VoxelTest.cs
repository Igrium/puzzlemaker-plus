using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;

namespace PuzzlemakerPlus;

[Tool]
public partial class VoxelTest : MeshInstance3D
{
    [Export]
    public bool Update { get; set; }

    [Export]
    public int Size { get; set; } = 16;

    [Export]
    public float Threshold { get; set; } = 0;

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Update)
        {
            GenerateGreedyMesh();
            Update = false;
        }
    }

    private PuzzlemakerWorld _initWorld()
    {
        using (var noise = new FastNoiseLite())
        {
            noise.Seed = 1;
            noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular;

            PuzzlemakerWorld world = new PuzzlemakerWorld();
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        if (noise.GetNoise3D(x, y, z) < Threshold)
                            world.Set(x, y, z, new PuzzlemakerVoxel().WithOpen(true));
                        //if (z % 2 == 0)
                        //{
                        //    world.Set(x, y, z, new PuzzlemakerVoxel().WithOpen(true));
                        //}

                    }
                }
            }
            return world;
        }
    }

    private IEnumerable<Quad> CreateCubeMesh(PuzzlemakerWorld world, Vector3I pos)
    {
        if (!world.Get(pos).IsOpen)
            yield break;

        // Top
        if (!world.Get(pos + Vector3I.Up).IsOpen)
        {
            yield return new Quad(
                new Vector3(0, 1, 0),
                new Vector3(1, 1, 0),
                new Vector3(1, 1, 1),
                new Vector3(0, 1, 1))
                .FilledUVs().WithResetNormals() + pos;
        }

        // Right
        if (!world.Get(pos + Vector3I.Right).IsOpen)
        {
            yield return new Quad(
                new Vector3(1, 1, 1),
                new Vector3(1, 1, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 1))
                .FilledUVs().WithResetNormals() + pos;
        }


        // Back
        if (!world.Get(pos + Vector3I.Back).IsOpen)
        {
            yield return new Quad(
                new Vector3(0, 1, 1),
                new Vector3(1, 1, 1),
                new Vector3(1, 0, 1),
                new Vector3(0, 0, 1))
                .FilledUVs().WithResetNormals() + pos;
        }

        // Left
        if (!world.Get(pos + Vector3I.Left).IsOpen)
        {
            yield return new Quad(
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 1),
                new Vector3(0, 0, 1),
                new Vector3(0, 0, 0))
                .FilledUVs().WithResetNormals() + pos;
        }

        // Forward
        if (!world.Get(pos + Vector3I.Forward).IsOpen)
        {
            yield return new Quad(
                new Vector3(1, 1, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0))
                .FilledUVs().WithResetNormals() + pos;
        }

        // Bottom
        if (!world.Get(pos + Vector3I.Down).IsOpen)
        {
            yield return new Quad(
                new Vector3(0, 0, 1),
                new Vector3(1, 0, 1),
                new Vector3(1, 0, 0),
                new Vector3(0, 0, 0))
                .FilledUVs().WithResetNormals() + pos;
        }

    }

    public void GenerateMesh()
    {
        MeshBuilder builder = new MeshBuilder();
        PuzzlemakerWorld world = _initWorld();

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int z = 0; z < Size; z++)
                {
                    builder.AddQuads(CreateCubeMesh(world, new Vector3I(x, y, z)));
                }
            }
        }

        ArrayMesh aMesh = new ArrayMesh();
        builder.ToMesh(aMesh);
        this.Mesh = aMesh;
    }

    public void GenerateGreedyMesh()
    {
        GD.Print("Creating greedy mesh...");
        MeshBuilder builder = new MeshBuilder();
        PuzzlemakerWorld world = _initWorld();

        //foreach (var direction in Enum.GetValues<Direction>())
        //{
        //    builder.AddQuads(DoGreedyMesh(world, direction));
        //}
        builder.AddQuads(GreedyMesh(world, 32, 0, 0, 0));

        ArrayMesh aMesh = new ArrayMesh();
        builder.ToMesh(aMesh);
        this.Mesh = aMesh;
    }

    private IEnumerable<Quad> DoGreedyMesh(PuzzlemakerWorld world, Direction direction)
    {
        HashSet<Vector3I> visited = new();
        bool canRenderFace(Vector3I face)
        {
            return !visited.Contains(face) && world.Get(face).IsOpen && !world.Get(face + direction.GetNormal()).IsOpen;
        }

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int z = 0; z < Size; z++)
                {
                    Vector3I pos = new Vector3I(x, y, z);
                    if (!canRenderFace(pos)) continue;

                    GreedyMeshHelper greedy = new GreedyMeshHelper(pos, direction);
                    greedy.ExpandRight(canRenderFace);
                    visited.UnionWith(greedy.GetVoxels());

                    yield return greedy.GetQuad();
                }
            }
        }
    }

    public IEnumerable<Quad> GreedyMesh(PuzzlemakerWorld world, int chunkSize, int chunkPosX, int chunkPosY, int chunkPosZ)
    {
        bool IsBlockAt(int x, int y, int z)
        {
            return world.Get(x, y, z).IsOpen;
        }

        // Sweep over each axis (X, Y and Z)
        for (var d = 0; d < 3; ++d)
        {
            int i, j, k, l, w, h;
            int u = (d + 1) % 3;
            int v = (d + 2) % 3;
            var x = new int[3];
            var q = new int[3];

            var mask = new bool[chunkSize * chunkSize];
            q[d] = 1;

            // Check each slice of the chunk one at a time
            for (x[d] = -1; x[d] < chunkSize;)
            {
                // Compute the mask
                var n = 0;
                for (x[v] = 0; x[v] < chunkSize; ++x[v])
                {
                    for (x[u] = 0; x[u] < chunkSize; ++x[u])
                    {
                        // q determines the direction (X, Y or Z) that we are searching
                        // m.IsBlockAt(x,y,z) takes global map positions and returns true if a block exists there

                        bool blockCurrent = 0 <= x[d] ? IsBlockAt(x[0] + chunkPosX, x[1] + chunkPosY, x[2] + chunkPosZ) : true;
                        bool blockCompare = x[d] < chunkSize - 1 ? IsBlockAt(x[0] + q[0] + chunkPosX, x[1] + q[1] + chunkPosY, x[2] + q[2] + chunkPosZ) : true;

                        // The mask is set to true if there is a visible face between two blocks,
                        //   i.e. both aren't empty and both aren't blocks
                        mask[n++] = blockCurrent != blockCompare;
                    }
                }

                ++x[d];

                n = 0;

                // Generate a mesh from the mask using lexicographic ordering,      
                //   by looping over each block in this slice of the chunk
                for (j = 0; j < chunkSize; ++j)
                {
                    for (i = 0; i < chunkSize;)
                    {
                        if (mask[n])
                        {
                            // Compute the width of this quad and store it in w                        
                            //   This is done by searching along the current axis until mask[n + w] is false
                            for (w = 1; i + w < chunkSize && mask[n + w]; w++) { }

                            // Compute the height of this quad and store it in h                        
                            //   This is done by checking if every block next to this row (range 0 to w) is also part of the mask.
                            //   For example, if w is 5 we currently have a quad of dimensions 1 x 5. To reduce triangle count,
                            //   greedy meshing will attempt to expand this quad out to chunkSize x 5, but will stop if it reaches a hole in the mask

                            var done = false;
                            for (h = 1; j + h < chunkSize; h++)
                            {
                                // Check each block next to this quad
                                for (k = 0; k < w; ++k)
                                {
                                    // If there's a hole in the mask, exit
                                    if (!mask[n + k + h * chunkSize])
                                    {
                                        done = true;
                                        break;
                                    }
                                }

                                if (done)
                                    break;
                            }

                            x[u] = i;
                            x[v] = j;

                            // du and dv determine the size and orientation of this face
                            var du = new int[3];
                            du[u] = w;

                            var dv = new int[3];
                            dv[v] = h;

                            // Create a quad for this face. Colour, normal or textures are not stored in this block vertex format.
                            yield return new Quad(new Vector3I(x[0], x[1], x[2]),                 // Top-left vertice position
                                                   new Vector3I(x[0] + du[0], x[1] + du[1], x[2] + du[2]),         // Top right vertice position
                                                   new Vector3I(x[0] + du[0] + dv[0], x[1] + du[1] + dv[1], x[2] + du[2] + dv[2]),  // Bottom right vertice position
                                                   new Vector3I(x[0] + dv[0], x[1] + dv[1], x[2] + dv[2]),         // Bottom left vertice position
                                                   ).WithResetNormals();

                            // Clear this part of the mask, so we don't add duplicate faces
                            for (l = 0; l < h; ++l)
                                for (k = 0; k < w; ++k)
                                    mask[n + k + l * chunkSize] = false;

                            // Increment counters and continue
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
        yield break;
    }
}
