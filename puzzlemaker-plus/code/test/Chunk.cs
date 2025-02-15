using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuzzlemakerPlus;

[Tool]
public partial class Chunk : MeshInstance3D
{
	[Export]
	public bool UpdateMesh { get; set; }

	[Export]
	public int ChunkSize { get; set; } = 16;

	[Export]
	public float Threshold { get; set; } = -.5f;

	private ArrayMesh _aMesh = new ArrayMesh();
	private List<Vector3> _vertices = new();
	private List<int> _indices = new();
	private List<Vector2> _uvs = new();

	private int _faceCount = 0;
	private float _texDiv = 0.25f;

	private PuzzlemakerWorld world = new PuzzlemakerWorld();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

    private void InitBlockWorld()
    {
        using (var noise = new FastNoiseLite())
        {
            noise.Seed = 1;
            noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular;

            world = new PuzzlemakerWorld();
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    for (int z = 0; z < ChunkSize; z++)
                    {
                        //world.Set(x, y, z, new PuzzlemakerVoxel().WithOpen(true));
                        //if (noise.GetNoise3D(x, y, z) < Threshold)
                            if (x % 2 == 0)
                            {
                                world.SetVoxel(x, y, z, new PuzzlemakerVoxel().WithOpen(true));
                            }
                    }
                }
            }
        }

        for (int z = 0; z < ChunkSize; z++)
        {
            GD.Print("LAYER:");
            for (int y = ChunkSize - 1; y >= 0; y--)
            {
                StringBuilder builder = new StringBuilder();
                for (int x = 0; x < ChunkSize; x++)
                {
                    builder.Append(world.GetVoxel(x, y, z).IsOpen ? 'X' : '0');
                }
                GD.Print(builder.ToString());
            }
        }
        
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if (UpdateMesh)
        {
            UpdateMesh = false;
            InitBlockWorld();
            GenChunk();
        }
	}

	private void AddUVs(float x, float y)
	{
		_uvs.Add(new Vector2(x * _texDiv, y * _texDiv));
        _uvs.Add(new Vector2(x * _texDiv + _texDiv, y * _texDiv));
        _uvs.Add(new Vector2(x * _texDiv + _texDiv, y * _texDiv + _texDiv));
        _uvs.Add(new Vector2(x * _texDiv, y * _texDiv + _texDiv));
    }

    private void AddTris()
    {
        int[] indices = new int[] 
		{
            _faceCount * 4 + 0, _faceCount * 4 + 1, _faceCount * 4 + 2,
            _faceCount * 4 + 0, _faceCount * 4 + 2, _faceCount * 4 + 3
        };

        _indices.AddRange(indices);
        _faceCount += 1;
    }

	private void GenCubeMesh(Vector3I pos)
	{
		// TOP
		if (BlockIsAir(pos + Vector3I.Up))
		{
			_vertices.Add(pos + new Vector3(0, 1, 0));
			_vertices.Add(pos + new Vector3(1, 1, 0));
			_vertices.Add(pos + new Vector3(1, 1, 1));
			_vertices.Add(pos + new Vector3(0, 1, 1));

			AddTris();
			AddUVs(0, 0);
		}

        // East
        if (BlockIsAir(pos + Vector3I.Up))
        {
            _vertices.Add(pos + new Vector3(1, 1, 1));
            _vertices.Add(pos + new Vector3(1, 1, 0));
            _vertices.Add(pos + new Vector3(1, 0, 0));
            _vertices.Add(pos + new Vector3(1, 0, 1));

            AddTris();
            AddUVs(3, 0);
        }

        // South
        if (BlockIsAir(pos + Vector3I.Up))
        {
            _vertices.Add(pos + new Vector3(0, 1, 1));
            _vertices.Add(pos + new Vector3(1, 1, 1));
            _vertices.Add(pos + new Vector3(1, 0, 1));
            _vertices.Add(pos + new Vector3(0, 0, 1));

            AddTris();
            AddUVs(0, 1);
        }

        // West
        if (BlockIsAir(pos + Vector3I.Up))
        {
            _vertices.Add(pos + new Vector3(0, 1, 0));
            _vertices.Add(pos + new Vector3(0, 1, 1));
            _vertices.Add(pos + new Vector3(0, 0, 1));
            _vertices.Add(pos + new Vector3(0, 0, 0));

            AddTris();
            AddUVs(1, 1);
        }

        // North
        if (BlockIsAir(pos + Vector3I.Up))
        {
            _vertices.Add(pos + new Vector3(1, 1, 0));
            _vertices.Add(pos + new Vector3(0, 1, 0));
            _vertices.Add(pos + new Vector3(0, 0, 0));
            _vertices.Add(pos + new Vector3(1, 0, 0));

            AddTris();
            AddUVs(2, 0);
        }

        // Bottom
        if (BlockIsAir(pos + Vector3I.Up))
        {
            _vertices.Add(pos + new Vector3(0, 0, 1));
            _vertices.Add(pos + new Vector3(1, 0, 1));
            _vertices.Add(pos + new Vector3(1, 0, 0));
            _vertices.Add(pos + new Vector3(0, 0, 0));

            AddTris();
            AddUVs(1, 0);
        }
    }

    private bool ShouldDrawFace(Vector3I pos, Vector3I dir)
    {
        return !BlockIsAir(pos) && BlockIsAir(pos + dir);
    }

    private void GenChunk()
    {
        _aMesh = new ArrayMesh();
        _vertices = new List<Vector3>();
        _indices = new List<int>();
        _uvs = new List<Vector2>();
        _faceCount = 0;

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    Vector3I pos = new Vector3I(x, y, z);
                    if (BlockIsAir(pos)) continue;
                    GenCubeMesh(pos);
                }
            }
        }

        using (var array = new Godot.Collections.Array())
        {
            array.Resize((int)Mesh.ArrayType.Max);
            array[(int)Mesh.ArrayType.Vertex] = _vertices.ToArray();
            array[(int)Mesh.ArrayType.Index] = _indices.ToArray();
            array[(int)Mesh.ArrayType.TexUV] = _uvs.ToArray();

            _aMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);
            this.Mesh = _aMesh;
        }
    }

    private bool BlockIsAir(Vector3I pos)
    {
        return !world.GetVoxel(pos).IsOpen;
  //      if (pos.X < 0 || pos.Y < 0 || pos.Z < 0 ||
  //          pos.X >= ChunkSize || pos.Y >= ChunkSize || pos.Z >= ChunkSize)
  //      {
  //          return true;
  //      }
		//else
		//{
		//	return !world.Get(pos).IsOpen;
		//}
    }
}
