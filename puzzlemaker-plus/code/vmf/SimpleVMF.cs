using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using VMFLib.Parsers;
using VMFLib.VClass;

namespace PuzzlemakerPlus.VMF;

public class SimpleVMF
{
    public VersionInfo Version { get; set; } = new VersionInfo();
    public World World { get; set; } = new World();
    public IList<Entity> Entities { get; } = new List<Entity>();

    public override string ToString()
    {
        return $"[VMF with {Entities.Count} entities]";
    }

    public void ToFile(VClassWriter writer)
    {
        writer.WriteClass(Version);
        writer.WriteClass(World);
        foreach (var ent in Entities)
        {
            writer.WriteClass(ent);
        }
    }

    public static SimpleVMF ReadFile(VClassReader reader)
    {
        SimpleVMF vmf = new SimpleVMF();

        BaseVClass? currentClass = reader.ReadClass();
        while (currentClass != null)
        {
            GD.Print(currentClass);
            if (currentClass is VersionInfo vinfo)
            {
                vmf.Version = vinfo;
            }
            else if (currentClass is World world)
            {
                vmf.World = world;
            }
            else if (currentClass is Entity entity)
            {
                vmf.Entities.Add(entity);
            }
            currentClass = reader.ReadClass();
        }

        return vmf;
    }

    public static SimpleVMF ReadFile(string path)
    {
        using (var reader = new VClassReader(new StreamReader(new FileAccessStream(path))))
        {
            return ReadFile(reader);
        }
    }
}

