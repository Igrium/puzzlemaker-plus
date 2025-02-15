
using Godot;
using VMFParser;

namespace PuzzlemakerPlus;

public partial class VMFTest : Node
{
    public void TestLoadVMF(string path)
    {
        GD.Print("Loading test VMF data from " + path);
        var contents = FileAccess.GetFileAsString(path);

        VMF vmf = new VMF(new string[] { contents });

        GD.Print(vmf);
    }
}
