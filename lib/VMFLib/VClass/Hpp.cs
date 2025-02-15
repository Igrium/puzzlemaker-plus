﻿//Hammer++ Classes

using VMFLib.Objects;

namespace VMFLib.VClass;

public class VerticesPlus : BaseVClass
{
    public override string ClassHeader => "vertices_plus";
    public override Dictionary<string, VProperty> Properties { get; set; } = new Dictionary<string, VProperty>();

    public List<Vec3> Vertices = new List<Vec3>();
}