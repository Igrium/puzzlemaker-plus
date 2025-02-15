
using System.IO;
using Godot;
using PuzzlemakerPlus.VMF;
using VMFLib.Objects;
using VMFLib.Parsers;
using VMFLib.VClass;

namespace PuzzlemakerPlus;

public partial class VMFTest : Node
{
    public void TestLoadVMF(string path)
    {
        
        VMFBuilder builder = new VMFBuilder();
        VMFBuilder.SolidBuilder solid = new VMFBuilder.SolidBuilder(builder);
        solid.AddBox(new Vec3(0, 0, 0), new Vec3(128, 128, 256));
        builder.AddSolid(solid);

        GD.Print("Saving test VMF to " + path);
        builder.WriteVMF(path);
    }
}
