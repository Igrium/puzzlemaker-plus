
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

        solid.Material = "dev/DEV_MEASUREWALL01A";
        solid.Material2 = "tools/toolsnodraw";

        solid.AddThickenedQuad(new Vec3(0, 0, 0), new Vec3(0, 0, 128), new Vec3(128, 64, 128), new Vec3(128, 64, 0), 32, inset: 16);

        //solid.AddBox(new Vec3(0, 0, 0), new Vec3(128, 128, 256));
        //solid.AddSide(new VMFLib.Objects.Plane(0, 0, 0, 128, 128, 0, 128, 128, 128));

        builder.AddSolid(solid);

        GD.Print("Saving test VMF to " + path);
        builder.WriteVMF(path);
    }
}
