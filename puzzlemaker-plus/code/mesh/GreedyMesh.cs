using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PuzzlemakerPlus;

public struct GreedyMesh
{
    // We need to keep track of both whether a world has been queried for an adjacent chunk AND whether it exists.
    private struct OptionalChunk
    {
        public OptionalChunk(VoxelChunk<PuzzlemakerVoxel>? value)
        {
            this.value = value;
        }

        public VoxelChunk<PuzzlemakerVoxel>? value;
    }
    private record struct FaceType
    {
        public FaceType(bool portalable, short subdivision)
        {
            this.Portalable = portalable;
            this.Subdivision = subdivision;
        }

        bool Portalable;
        short Subdivision;

        public int GetMaterialIndex()
        {
            return Subdivision * 2 + (Portalable ? 1 : 0);
        }

        public static FaceType FromVoxel(PuzzlemakerVoxel voxel, Direction direction)
        {
            return new FaceType(voxel.IsPortalable(direction), voxel.Subdivision);
        }
    }

    private static IEnumerable<FaceType> IterateFaceTypes()
    {
        for (short i = 0; i <= 2; i++)
        {
            yield return new FaceType(false, i);
            yield return new FaceType(true, i);
        }
    }


    private PuzzlemakerWorld _world;
    private Vector3I _chunkPos;
    private float _uvScale;
    private bool _invert;
    private bool[] _mask;
    private Vector3I _position;
    private Vector3I _direction;
    private Direction _dirEnum;
    private int _currentAxis;
    private int _axisU;
    private int _axisV;
    private int _dir;
    private FaceType _faceType;

    private VoxelChunk<PuzzlemakerVoxel> _chunk;

    public IEnumerable<Quad> DoGreedyMesh(PuzzlemakerWorld world, Vector3I chunkPos, float uvScale = 1, bool invert = false)
    {

        _world = world;
        _chunkPos = chunkPos;
        _uvScale = uvScale;
        _invert = invert;

        if (world.Chunks.TryGetValue(chunkPos, out var chunk))
        {
            _chunk = chunk;
        }
        else
        {
            yield break;
        }

        // Sweep over each axis (X, Y and Z)
        for (_currentAxis = 0; _currentAxis < 3; ++_currentAxis)
        {
            // 0 = negative, 1 = positive.
            for (_dir = 0; _dir <= 1; _dir++)
            {
                _dirEnum = Directions.FromAxis(_currentAxis, _dir == 0);
                foreach (var facetype in IterateFaceTypes())
                {
                    this._faceType = facetype;
                    InitializeAxisVariables();
                    _direction[_currentAxis] = 1;

                    

                    // Check each slice of the chunk one at a time
                    for (_position[_currentAxis] = -1; _position[_currentAxis] < PuzzlemakerWorld.CHUNK_SIZE;)
                    {
                        ComputeMask();
                        ++_position[_currentAxis];

                        foreach (var quad in GenerateMeshFromMask())
                        {
                            yield return quad;
                        }
                    }
                }
                
            }
        }
    }

    private PuzzlemakerVoxel GetVoxel(Vector3I pos)
    {
        Vector3I chunkPos = PuzzlemakerWorld.GetChunk(pos);
        Vector3I local = PuzzlemakerWorld.GetPosInChunk(pos);

        // Avoid hash lookup of the block is in this chunk.
        if (chunkPos == _chunkPos)
        {
            return _chunk.GetVoxel(local);
        }
        else
        {
            return _world.GetVoxel(pos);
        }
    }

    private void InitializeAxisVariables()
    {
        _axisU = (_currentAxis + 1) % 3;
        _axisV = (_currentAxis + 2) % 3;
        _position = new Vector3I();
        _direction = new Vector3I();
        _mask = new bool[PuzzlemakerWorld.CHUNK_SIZE * PuzzlemakerWorld.CHUNK_SIZE];
    }

    private void ComputeMask()
    {
        var n = 0;
        var offset = _chunkPos * PuzzlemakerWorld.CHUNK_SIZE;
        for (_position[_axisV] = 0; _position[_axisV] < PuzzlemakerWorld.CHUNK_SIZE; ++_position[_axisV])
        {
            for (_position[_axisU] = 0; _position[_axisU] < PuzzlemakerWorld.CHUNK_SIZE; ++_position[_axisU])
            {
                PuzzlemakerVoxel currentBlock = GetVoxel(_position + offset);
                PuzzlemakerVoxel compareBlock = GetVoxel(_position + _direction + offset);

                // If _dir is 1, we're in the compare block looking at this one. Otherwise, we're in this one looking at the compare.
                if (_dir == 1)
                {
                    FaceType f = FaceType.FromVoxel(currentBlock, _dirEnum);
                    _mask[n++] = currentBlock.IsOpen && !compareBlock.IsOpen && f == _faceType;
                }
                else
                {
                    FaceType f = FaceType.FromVoxel(compareBlock, _dirEnum);
                    _mask[n++] = compareBlock.IsOpen && !currentBlock.IsOpen && f == _faceType;
                }
            }
        }
    }

    private IEnumerable<Quad> GenerateMeshFromMask()
    {
        // Generate a mesh from the mask using lexicographic ordering,
        // by looping over each block in this slice of the chunk
        var n = 0;
        for (var v = 0; v < PuzzlemakerWorld.CHUNK_SIZE; ++v)
        {
            for (var u = 0; u < PuzzlemakerWorld.CHUNK_SIZE;)
            {
                if (_mask[n])
                {
                    // Compute the width of this quad and store it in quadWidth
                    // This is done by searching along the current axis until mask[n + quadWidth] is false
                    var quadWidth = ComputeQuadWidth(u, n);

                    // Compute the height of this quad and store it in quadHeight
                    // This is done by checking if every block next to this row (range 0 to quadWidth) is also part of the mask.
                    // For example, if quadWidth is 5 we currently have a quad of dimensions 1 Position 5. To reduce triangle count,
                    // greedy meshing will attempt to expand this quad out to chunkSize Position 5, but will stop if it reaches a hole in the mask
                    var quadHeight = ComputeQuadHeight(v, n, quadWidth);

                    _position[_axisU] = u;
                    _position[_axisV] = v;

                    // du and dv determine the size and orientation of this face
                    var du = new int[3];
                    du[_axisU] = quadWidth;

                    var dv = new int[3];
                    dv[_axisV] = quadHeight;

                    // Create a quad for this face. Colour, normal or textures are not stored in this block vertex format.
                    var quad = CreateQuad(du, dv);
                    yield return quad;
                    // Clear this part of the mask, so we don't add duplicate faces
                    ClearMask(n, quadWidth, quadHeight);
                    u += quadWidth;
                    n += quadWidth;
                }
                else
                {
                    u++;
                    n++;
                }
            }
        }
    }

    private int ComputeQuadWidth(int u, int n)
    {
        var quadWidth = 1;
        while (u + quadWidth < PuzzlemakerWorld.CHUNK_SIZE && _mask[n + quadWidth])
        {
            quadWidth++;
        }
        return quadWidth;
    }

    private int ComputeQuadHeight(int v, int n, int quadWidth)
    {
        var quadHeight = 1;
        var done = false;
        while (v + quadHeight < PuzzlemakerWorld.CHUNK_SIZE)
        {
            for (var k = 0; k < quadWidth; ++k)
            {
                // Check each block next to this quad
                if (!_mask[n + k + quadHeight * PuzzlemakerWorld.CHUNK_SIZE])
                {
                    done = true;
                    break;
                }
            }
            if (done)
                break;
            quadHeight++;
        }
        return quadHeight;
    }

    private Quad CreateQuad(int[] du, int[] dv)
    {
        var quad = new Quad(
            new Vector3I(_position[0], _position[1], _position[2]),
            new Vector3I(_position[0] + du[0], _position[1] + du[1], _position[2] + du[2]),
            new Vector3I(_position[0] + du[0] + dv[0], _position[1] + du[1] + dv[1], _position[2] + du[2] + dv[2]),
            new Vector3I(_position[0] + dv[0], _position[1] + dv[1], _position[2] + dv[2])
        ).WithResetNormals();

        // Compute UVs
        quad.UV1 = new Vector2(_position[_axisU] * _uvScale, 1 - _position[_axisV] * _uvScale);
        quad.UV2 = new Vector2((_position[_axisU] + du[_axisU]) * _uvScale, 1 - _position[_axisV] * _uvScale);
        quad.UV3 = new Vector2((_position[_axisU] + du[_axisU]) * _uvScale, 1 - (_position[_axisV] + dv[_axisV]) * _uvScale);
        quad.UV4 = new Vector2(_position[_axisU] * _uvScale, 1 - (_position[_axisV] + dv[_axisV]) * _uvScale);

        if ((_dir == 1 && !_invert) || (_dir == 0 && _invert))
        {
            quad = quad.Flipped();
        }

        quad.MaterialIndex = _faceType.GetMaterialIndex();

        return quad;
    }

    private void ClearMask(int n, int quadWidth, int quadHeight)
    {
        for (var l = 0; l < quadHeight; ++l)
        {
            for (var k = 0; k < quadWidth; ++k)
            {
                _mask[n + k + l * PuzzlemakerWorld.CHUNK_SIZE] = false;
            }
        }
    }
}