using Godot;
using Godot.Collections;
using System.Collections.Generic;

namespace PuzzlemakerPlus;

[Tool]
public partial class GreedyMeshTest : MeshInstance3D
{

	public ArrayMesh ArrayMesh { get; private set; } = new ArrayMesh();

	[Export]
	public bool Update { get; set; }

	private float _threshold;

	[Export]
	public float Threshold
	{
		get => _threshold;
		set
		{
			this.Threshold = value;
			Update = true;
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Update)
		{
			GD.Print("Updating!");
			UpdateMesh();
			Update = false;
		}
	}

	private void UpdateMesh()
	{
		PuzzlemakerWorld world = new PuzzlemakerWorld();
		var noise = new FastNoiseLite();
		noise.Seed = 1;
		noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular;

		for (int x = 0; x < 32; x++)
		{
			for (int y = 0; y < 32; y++)
			{
				for (int z = 0; z < 32; z++)
				{
					float val = noise.GetNoise3D(x, y, z);
					if (val > Threshold)
					{
						world.Set(x, y, z, new PuzzlemakerVoxel().WithOpen(true));
					}
				}
			}
		}

		GenMesh(world);
	}

    private List<Vector3> _vertices = new();
    private List<int> _indices = new();
    private List<Vector2> _uvs = new();
	private int _faceCount;

	private void AddUVs()
	{
		_uvs.Add(new Vector2(0, 0));
		_uvs.Add(new Vector2(1, 0));
		_uvs.Add(new Vector2(1, 1));
		_uvs.Add(new Vector2(0, 1));
	}

	private void AddTris()
	{
		_indices.Add(_faceCount * 4);
		_indices.Add(_faceCount * 4 + 1);
		_indices.Add(_faceCount * 4 + 2);

		_indices.Add(_faceCount * 4);
		_indices.Add(_faceCount * 4 + 2);
		_indices.Add(_faceCount * 4 + 3);

		_faceCount++;
	}

    private void GenMesh(PuzzlemakerWorld world)
	{
		ArrayMesh = new ArrayMesh();
		_vertices = new List<Vector3>();
		_indices = new List<int>();
		_uvs = new List<Vector2>();

		HashSet<Vector3I> visited = new();

		// Top
		foreach (var (pos, block) in world.IterateVoxels(true))
		{
			if (visited.Contains(pos) || !ShouldRenderFace(world, pos, Direction.Up)) continue;

			GreedyMeshHelper greedyMeshHelper = new GreedyMeshHelper(pos, Direction.Up);
			greedyMeshHelper.ExpandRight(vec => ShouldRenderFace(world, vec, Direction.Up));

			visited.UnionWith(greedyMeshHelper.GetVoxels());

            var (corner1, corner2) = greedyMeshHelper.GetCorners();

			_vertices.Add(new Vector3(Mathf.Min(corner1.X, corner2.X), corner1.Y, Mathf.Min(corner1.Z, corner2.Z)));
			_vertices.Add(new Vector3(Mathf.Max(corner1.X, corner2.X), corner1.Y, Mathf.Min(corner1.Z, corner2.Z)));
			_vertices.Add(new Vector3(Mathf.Max(corner1.X, corner2.X), corner1.Y, Mathf.Max(corner1.Z, corner2.Z)));
			_vertices.Add(new Vector3(Mathf.Min(corner1.X, corner2.X), corner1.Y, Mathf.Max(corner1.Z, corner2.Z)));

			AddTris();
			AddUVs();

			using (var arrays = new Godot.Collections.Array())
			{
				arrays.Resize((int)Mesh.ArrayType.Max);
				arrays[(int)Mesh.ArrayType.Vertex] = _vertices.ToArray();
				arrays[(int)Mesh.ArrayType.Index] = _indices.ToArray();
				arrays[(int)Mesh.ArrayType.TexUV] = _uvs.ToArray();
				ArrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
			}
			this.Mesh = ArrayMesh;
        }
	}

	private bool ShouldRenderFace(PuzzlemakerWorld world, Vector3I pos, Direction side)
	{
		return world.Get(pos).IsOpen && !world.Get(pos + side.GetNormal()).IsOpen;
	}
}
