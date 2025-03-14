using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus.VMF;

/// <summary>
/// A simple class exposing some VMF export functions.
/// </summary>
[GlobalClass]
public partial class VMFExporter : RefCounted
{
    public void ExportVMF(string path)
    {
        VMFBuilder builder = new VMFBuilder();
        LevelTheme? theme = EditorState.Instance.Theme;
        if (theme == null)
        {
            GD.PrintErr("Unable to export VMF: no editor theme loaded.");
            return;
        }
        WorldExporter exporter = new WorldExporter(theme);
        exporter.ExportWorld(builder, EditorState.Instance.World);

        GD.Print("Saving VMF to " + path);
        builder.WriteVMF(path);
    }

}
