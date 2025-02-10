using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

    [Export]
    public Material? Material { get; set; }

    [Export]
    public bool Invert { get; set; }

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
                        //    world.Set(position, y, z, new PuzzlemakerVoxel().WithOpen(true));
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
        builder.ToMesh(aMesh, Material);
        this.Mesh = aMesh;
    }

    public void GenerateGreedyMesh()
    {
        GD.Print("Creating greedy mesh...");
        MeshBuilder builder = new MeshBuilder();
        PuzzlemakerWorld world = _initWorld();

        var watch = Stopwatch.StartNew();
        builder.AddQuads(GreedyMesh.DoGreedyMesh(world, 64, Vector3I.Zero, uvScale: .125f, invert: Invert));
        watch.Stop();
        GD.Print($"Generated greedy mesh in {watch.ElapsedMilliseconds}ms");

        ArrayMesh aMesh = new ArrayMesh();
        builder.ToMesh(aMesh, Material);
        this.Mesh = aMesh;
    }

}
