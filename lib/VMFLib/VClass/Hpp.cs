//Hammer++ Classes

using VMFLib.Objects;

namespace VMFLib.VClass;

public class VerticesPlus : BaseVClass
{
    public override string ClassHeader => "vertices_plus";
    public override IDictionary<string, VProperty> Properties { get; } = new Dictionary<string, VProperty>();

    public List<Vec3> Vertices = new List<Vec3>();
}