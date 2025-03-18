namespace VMFLib.VClass;

public class VisGroups : BaseVClass
{
    public override string ClassHeader => "visgroups";
    public override IDictionary<string, VProperty> Properties { get; } = new Dictionary<string, VProperty>();
}
    
public class VisGroup : BaseVClass
{
    public override string ClassHeader => "visgroup";
    public override IDictionary<string, VProperty> Properties { get; } = new Dictionary<string, VProperty>();

    public string? Name => Properties["name"].Str();
}