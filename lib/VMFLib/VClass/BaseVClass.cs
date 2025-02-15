namespace VMFLib.VClass;

public abstract class BaseVClass
{
    /// <summary>
    /// The header a class uses in a vmf file, e.g "world" or "entity"
    /// </summary>
    public abstract string ClassHeader { get; }
    /// <summary>
    /// This classes properties
    /// </summary>
    public abstract Dictionary<string, VProperty> Properties { get; set; }
    
    /// <summary>
    /// Classes that are inside of this class
    /// </summary>
    public List<BaseVClass> SubClasses { get; set; }

    /// <summary>
    /// Add a new property to this class
    /// </summary>
    /// <param name="property"></param>
    public void AddProperty(VProperty property)
    {
        if (Properties.Keys.Contains(property.Name) || property.Name == null)
            return;
        Properties.Add(property.Name, property);
    }

    /// <summary>
    /// Set a property value in this class.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <param name="value">Stringified property value.</param>
    public void SetProperty(string name, string? value)
    {
        if (value == null)
            Properties.Remove(name);
        else
            Properties[name] = new VProperty(name, value);
    }

    public BaseVClass()
    {
        SubClasses = new List<BaseVClass>();
    }

    public override string ToString()
    {
        return ClassHeader;
    }
}

/// <summary>
/// A generic class, used when a class is detected but there is no native reading
/// </summary>
public class GenericVClass : BaseVClass
{
    public override string ClassHeader { get; }
    public override Dictionary<string, VProperty> Properties { get; set; } = new Dictionary<string, VProperty>();

    public GenericVClass(string classHeader)
    {
        ClassHeader = classHeader;
    }
}