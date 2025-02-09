using System;
using System.Collections.Generic;
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
        MeshBuilder builder = new MeshBuilder();
        PuzzlemakerWorld world = _initWorld();
        
        foreach (var direction in Enum.GetValues<Direction>())
        {
            builder.AddQuads(DoGreedyMesh(world, 32));
        }

        ArrayMesh aMesh = new ArrayMesh();
        builder.ToMesh(aMesh);
        this.Mesh = aMesh;
    }

    private IEnumerable<Quad> DoGreedyMesh(PuzzlemakerWorld world, int chunkSize)
    {
        // Sweep over each axis
        for (var axis = 0; axis < 3; ++axis)
        {
            int i, j, k, l, quadWidth, quadHeight;
            int axisU = (axis + 1) % 3;
            int axisV = (axis + 2) % 3;

            var position = new Vector3I();
            var direction = new Vector3I();

            var mask = new bool[chunkSize * chunkSize];
            direction[axis] = 1;

            // Check each slice of the chunk one at a time
            for (position[axis] = -1; position[axis] < chunkSize;)
            {
                // Compute the mask
                var maskIndex = 0;
                for (position[axisV] = 0; position[axisV] < chunkSize; ++position[axisV])
                {
                    for (position[axisU] = 0; position[axisU] < chunkSize; ++position[axisU])
                    {
                        // direction determines the direction (X, Y or Z) that we are searching
                        // IsBlockAt(x,y,z) takes global map positions and returns true if a block exists there

                        bool blockCurrent = 0 <= position[axis] ? world.Get(position).IsOpen : true;
                        bool blockCompare = position[axis] < chunkSize - 1 ? world.Get(position + direction).IsOpen : true;

                        mask[maskIndex++] = blockCurrent != blockCompare;
                    }
                }

                ++position[axis];
                maskIndex = 0;

                // Generate a mesh from the mask using lexicographic ordering,      
                // by looping over each block in this slice of the chunk
                for (j = 0; j < chunkSize; ++j)
                {
                    for (i = 0; i < chunkSize;)
                    {
                        if (mask[maskIndex])
                        {
                            // Compute the width of this quad and store it in quadWidth                        
                            // This is done by searching along the current axis until mask[maskIndex + quadWidth] is false
                            for (quadWidth = 1; i + quadWidth < chunkSize && mask[maskIndex + quadWidth]; quadWidth++) { };

                            // Compute the height of this quad and store it in quadHeight                        
                            // This is done by checking if every block next to this row (range 0 to quadWidth) is also part of the mask.
                            // For example, if quadWidth is 5 we currently have a quad of dimensions 1 x 5. To reduce triangle count,
                            // greedy meshing will attempt to expand this quad out to CHUNK_SIZE x 5, but will stop if it reaches a hole in the mask

                            var done = false;
                            for (quadHeight = 1; j + quadHeight < chunkSize; quadHeight++)
                            {
                                // Check each block next to this quad
                                for (k = 0; k < quadWidth; ++k)
                                {
                                    // If there's a hole in the mask, exit
                                    if (!mask[maskIndex + k + quadHeight * chunkSize])
                                    {
                                        done = true;
                                        break;
                                    }
                                }

                                if (done) break;
                            }

                            position[axisU] = i;
                            position[axisV] = j;

                            // du and dv determine the size and orientation of this face
                            var du = new Vector3I();
                            du[axisU] = quadWidth;

                            var dv = new Vector3I();
                            dv[axisV] = quadHeight;

                            // Create a quad for this face. Colour, normal or textures are not stored in this block vertex format.
                            yield return new Quad(new Vector3(position[0], position[1], position[2]),                 // Top-left vertice position
                                                  new Vector3(position[0] + du[0], position[1] + du[1], position[2] + du[2]),         // Top right vertice position
                                                  new Vector3(position[0] + dv[0], position[1] + dv[1], position[2] + dv[2]),         // Bottom left vertice position
                                                  new Vector3(position[0] + du[0] + dv[0], position[1] + du[1] + dv[1], position[2] + du[2] + dv[2])  // Bottom right vertice position
                                                  ).FilledUVs().WithResetNormals();

                            // Clear this part of the mask so we don't add duplicate faces.
                            for (l = 0; l < quadHeight; ++l)
                            {
                                for (k = 0; k < quadWidth; ++k)
                                {
                                    mask[maskIndex + k + l * chunkSize] = false;
                                }
                            }

                            // Increment counters and continue.
                            i += quadWidth;
                            maskIndex = quadWidth;
                        }
                        else
                        {
                            i++;
                            maskIndex++;
                        }
                    }
                }
            }
        }
        //HashSet<Vector3I> visited = new();
        //bool canRenderFace(Vector3I face)
        //{
        //    return !visited.Contains(face) && world.Get(face).IsOpen && !world.Get(face + direction.GetNormal()).IsOpen;
        //}

        //for (int x = 0; x < Size; x++)
        //{
        //    for (int y = 0; y < Size; y++)
        //    {
        //        for (int z = 0; z < Size; z++)
        //        {
        //            Vector3I pos = new Vector3I(x, y, z);
        //            if (!canRenderFace(pos)) continue;

        //            GreedyMeshHelper greedy = new GreedyMeshHelper(pos, direction);
        //            greedy.ExpandRight(canRenderFace);
        //            visited.UnionWith(greedy.GetVoxels());

        //            yield return greedy.GetQuad();
        //        }
        //    }
        //}
    }
}
