
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
        WorldExporter exporter = new WorldExporter(EditorState.Instance.Theme);
        exporter.ExportWorld(builder, EditorState.Instance.World);

        GD.Print("Saving test VMF to " + path);
        builder.WriteVMF(path);
    }
}
