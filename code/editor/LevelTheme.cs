using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace PuzzlemakerPlus;

[GlobalClass]
public partial class LevelTheme : Resource
{
    [Export]
    public Material[] VoxelTextures { get; set; } = new Material[0];


    public Material? GetVoxelTexture(bool portalable, int subdiv)
    {
        int matCount = VoxelTextures.Length;
        int maxSubdiv = matCount / 2;
        if (subdiv > maxSubdiv)
            subdiv = maxSubdiv;

        int index = subdiv * 2 + (portalable ? 1 : 0);
        if (index >= matCount)
        {
            GD.PushError($"Unable to get voxel texture for portalability {portalable} with subdivision {subdiv}.");
            return null;
        }

        return VoxelTextures[index];
    }

}
