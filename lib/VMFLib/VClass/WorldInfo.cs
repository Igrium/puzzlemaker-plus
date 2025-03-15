using VMFLib.Objects;

namespace VMFLib.VClass;

public class VersionInfo : BaseVClass
{
    public override string ClassHeader => "versioninfo";
    public override Dictionary<string, VProperty> Properties { get; set; } = new Dictionary<string, VProperty>();

    public bool? Prefab => Properties["prefab"].Bool();
    public int? EditorVersion => Properties["editorversion"].Int();
    public int? EditorBuild => Properties["editorbuild"].Int();
    public int? MapVersion => Properties["mapversion"].Int();
    public int? FormatVersion => Properties["formatversion"].Int();
}

public class World : BaseVClass
{
    public World()
    {
        Id = 1;
        MapVersion = 1;
        ClassName = "worldspawn";
    }

    public override string ClassHeader => "world";
    public override Dictionary<string, VProperty> Properties { get; set; } = new Dictionary<string, VProperty>();
    public List<Solid> Solids { get; set; } = new List<Solid>();
    public List<Group> Groups { get; set; } = new List<Group>();
    public List<Hidden> HiddenClasses { get; set; } = new List<Hidden>();

    public int Id
    {
        get => Properties["id"].Int();
        set => Properties["id"] = new VProperty("id", value);
    }
    public int MapVersion
    {
        get => Properties["mapversion"].Int();
        set => Properties["mapversion"] = new VProperty("mapversion", value);
    }
    public string ClassName
    {
        get => Properties["classname"].Str();
        set => Properties["classname"] = new VProperty("classname", value);
    }
    public string? SkyName
    {
        //get => Properties["skyname"].Str();
        get => Properties.GetValueOrDefault("skyname")?.Str();
        set
        {
            if (value != null)
                Properties["skyname"] = new VProperty("skyname", value);
            else
                Properties.Remove("skyname");
        }
    }
}

public class Solid : BaseVClass
{
    public override string ClassHeader => "solid";
    public override Dictionary<string, VProperty> Properties { get; set; } = new Dictionary<string, VProperty>();
    
    public int Id
    {
        get => Properties["id"].Int();
        set => SetProperty("id", value.ToString());
    }

    public List<Side> Sides = new List<Side>();
    public Editor? Editor;
}

public class Side : BaseVClass
{
    public Side()
    {
        Id = 0;
    }

    public override string ClassHeader => "side";
    public override Dictionary<string, VProperty> Properties { get; set; } = new Dictionary<string, VProperty>();

    //public int? Id => Properties["id"].Int();
    public int Id
    {
        get => Properties["id"].Int();
        set => Properties["id"] = new VProperty("id", value);
    }

    //public Plane? Plane => Properties["plane"].Plane();
    public Plane? Plane
    {
        get => Properties.GetValueOrDefault("plane")?.Plane();
        set => SetProperty("plane", value?.ToString());
    }

    public Displacement? DisplacementInfo;
    //public string? Material => Properties["material"].Str();
    public string? Material
    {
        get => Properties.GetValueOrDefault("material")?.Str();
        set => SetProperty("material", value);
    }

    //public UVAxis? UAxis => Properties["uaxis"].UvAxis();
    public UVAxis? UAxis
    {
        get => Properties.GetValueOrDefault("uaxis")?.UvAxis();
        set => SetProperty("uaxis", value?.ToString());
    }
    //public UVAxis? VAxis => Properties["vaxis"].UvAxis();
    public UVAxis? VAxis
    {
        get => Properties.GetValueOrDefault("vaxis")?.UvAxis();
        set => SetProperty("vaxis", value?.ToString());
    }

    //public double? Rotation => Properties["rotation"].Dec();
    public double? Rotation
    {
        get => Properties.GetValueOrDefault("rotation")?.Dec();
        set => SetProperty("rotation", value?.ToString());
    }

    //public int? LightmapScale => Properties["lightmapscale"].Int();
    public int? LightmapScale
    {
        get => Properties.GetValueOrDefault("lightmapscale")?.Int();
        set => SetProperty("lightmapscale", value?.ToString());
    }
    public int? SmoothingGroups => Properties["smoothing_groups"].Int();
    
    //TODO: Flag helpers
    public int? Contents => Properties["contents"].Int();
    public int? Flags => Properties["flags"].Int();
}

public class Displacement : BaseVClass
{
    public override string ClassHeader => "dispinfo";
    public override Dictionary<string, VProperty> Properties { get; set; } = new Dictionary<string, VProperty>();

    public int? Power => Properties["power"].Int();
    public Vec3? StartPosition => Properties["startposition"].Vec3();
    public float? Elevation => Properties["elevation"].Float();
    public bool? IsSubdivided => Properties["subdiv"].Bool();
    public DispRows Rows;

    public Displacement()
    {
        Rows = new DispRows();
    }
}