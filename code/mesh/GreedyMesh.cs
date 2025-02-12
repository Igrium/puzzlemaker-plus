using Godot;
using System.Collections.Generic;

namespace PuzzlemakerPlus;

public struct GreedyMesh
{
    private PuzzlemakerWorld _world;
    private int _chunkSize;
    private Vector3I _chunkPos;
    private float _uvScale;
    private bool _invert;
    private bool[] _mask;
    private Vector3I _position;
    private Vector3I _direction;
    private int _currentAxis;
    private int _axisU;
    private int _axisV;
    private int _dir;

    public IEnumerable<Quad> DoGreedyMesh(PuzzlemakerWorld world, int chunkSize, Vector3I chunkPos, float uvScale = 1, bool invert = false)
    {
        _world = world;
        _chunkSize = chunkSize;
        _chunkPos = chunkPos;
        _uvScale = uvScale;
        _invert = invert;

        // Sweep over each axis (X, Y and Z)
        for (_currentAxis = 0; _currentAxis < 3; ++_currentAxis)
        {
            // 0 = negative, 1 = positive.
            for (_dir = 0; _dir <= 1; _dir++)
            {
                InitializeAxisVariables();
                _direction[_currentAxis] = 1;

                // Check each slice of the chunk one at a time
                for (_position[_currentAxis] = -1; _position[_currentAxis] < _chunkSize;)
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

    private void InitializeAxisVariables()
    {
        _axisU = (_currentAxis + 1) % 3;
        _axisV = (_currentAxis + 2) % 3;
        _position = new Vector3I();
        _direction = new Vector3I();
        _mask = new bool[_chunkSize * _chunkSize];
    }

    private void ComputeMask()
    {
        var n = 0;
        for (_position[_axisV] = 0; _position[_axisV] < _chunkSize; ++_position[_axisV])
        {
            for (_position[_axisU] = 0; _position[_axisU] < _chunkSize; ++_position[_axisU])
            {
                bool blockCurrent = _world.GetVoxel(_position + _chunkPos).IsOpen;
                bool blockCompare = _world.GetVoxel(_position + _direction + _chunkPos).IsOpen;

                // If _dir is 1, we're in the compare block looking at this one. Otherwise, we're in this one looking at the compare.
                if (_dir == 1)
                {
                    _mask[n++] = blockCurrent && !blockCompare;
                }
                else
                {
                    _mask[n++] = blockCompare && !blockCurrent;
                }

                // The mask is set to true if there is a visible face between two blocks,
                // i.e. both aren't empty and both aren't blocks
                // mask[n++] = blockCurrent != blockCompare;
            }
        }
    }

    private IEnumerable<Quad> GenerateMeshFromMask()
    {
        // Generate a mesh from the mask using lexicographic ordering,
        // by looping over each block in this slice of the chunk
        var n = 0;
        for (var j = 0; j < _chunkSize; ++j)
        {
            for (var i = 0; i < _chunkSize;)
            {
                if (_mask[n])
                {
                    // Compute the width of this quad and store it in quadWidth
                    // This is done by searching along the current axis until mask[n + quadWidth] is false
                    var quadWidth = ComputeQuadWidth(i, n);

                    // Compute the height of this quad and store it in quadHeight
                    // This is done by checking if every block next to this row (range 0 to quadWidth) is also part of the mask.
                    // For example, if quadWidth is 5 we currently have a quad of dimensions 1 Position 5. To reduce triangle count,
                    // greedy meshing will attempt to expand this quad out to chunkSize Position 5, but will stop if it reaches a hole in the mask
                    var quadHeight = ComputeQuadHeight(j, n, quadWidth);

                    _position[_axisU] = i;
                    _position[_axisV] = j;

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

    private int ComputeQuadWidth(int i, int n)
    {
        var quadWidth = 1;
        while (i + quadWidth < _chunkSize && _mask[n + quadWidth])
        {
            quadWidth++;
        }
        return quadWidth;
    }

    private int ComputeQuadHeight(int j, int n, int quadWidth)
    {
        var quadHeight = 1;
        var done = false;
        while (j + quadHeight < _chunkSize)
        {
            for (var k = 0; k < quadWidth; ++k)
            {
                // Check each block next to this quad
                if (!_mask[n + k + quadHeight * _chunkSize])
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

        return quad;
    }

    private void ClearMask(int n, int quadWidth, int quadHeight)
    {
        for (var l = 0; l < quadHeight; ++l)
        {
            for (var k = 0; k < quadWidth; ++k)
            {
                _mask[n + k + l * _chunkSize] = false;
            }
        }
    }
}