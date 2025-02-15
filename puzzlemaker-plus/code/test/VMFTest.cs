
using System.IO;
using Godot;
using VMFLib.Parsers;
using VMFLib.VClass;

namespace PuzzlemakerPlus;

public partial class VMFTest : Node
{
    public void TestLoadVMF(string path)
    {
        GD.Print("Loading test VMF data from " + path);
        SimpleVMF vmf = SimpleVMF.ReadFile(path);
        GD.Print(vmf);
    }
}
