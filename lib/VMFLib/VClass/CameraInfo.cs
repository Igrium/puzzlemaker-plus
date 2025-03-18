using VMFLib.VClass;

public class CamerasHolder : BaseVClass
{
    public override string ClassHeader => "cameras";
    public override IDictionary<string, VProperty> Properties { get; } = new Dictionary<string, VProperty>();
}

public class Camera : BaseVClass
{
    public override string ClassHeader => "camera";
    public override IDictionary<string, VProperty> Properties { get; } = new Dictionary<string, VProperty>();
}