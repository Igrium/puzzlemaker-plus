
using Godot;
using PuzzlemakerPlus.VMF;
using VMFLib.Parsers;

namespace PuzzlemakerPlus;

public class WorldExporter
{
    public LevelTheme Theme { get; set; }

    public string BackfaceTexture { get; set; } = "tools/toolsnodraw";

    public WorldExporter(LevelTheme theme)
    {
        this.Theme = theme;
    }

    public void ExportWorld(VMFBuilder builder, PuzzlemakerWorld world)
    {
        foreach (var pos in world.Chunks.Keys)
        {
            Vector3I blockPos = pos * 16;
            foreach (var quad in new GreedyMesh().DoGreedyMesh(world, 16, blockPos, invert: true))
            {
                Quad transformed = quad + blockPos;
                // TODO: Is there anything we can do to make the output hammer geo cleaner?
                VMFBuilder.SolidBuilder solid = new VMFBuilder.SolidBuilder(builder);
                solid.Material = GetTexture(quad.MaterialIndex);
                solid.Material2 = BackfaceTexture;
                solid.AddThickenedQuad(transformed.ToSourceQuad(), 8, 8);
                builder.AddSolid(solid);
            }
        }
    }

    private string? GetTexture(int index)
    {
        string[] tex = Theme.VoxelTextures;
        if (tex.Length == 0)
            return null;

        return index < tex.Length ? tex[index] : tex[tex.Length - 1];
    }
}
