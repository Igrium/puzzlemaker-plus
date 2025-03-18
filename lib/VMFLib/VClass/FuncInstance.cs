namespace VMFLib.VClass;

public class FuncInstance : Entity
{
    public FuncInstance()
    {
        ClassName = "func_instance";
        SetProperty("file", "");
    }

    public string VMFFile
    {
        get => Properties["file"].Str();
        set => SetProperty("file", value);
    }

    public IEnumerable<KeyValuePair<string, string>> GetReplaceProps()
    {
        foreach (var (key, val) in Properties)
        {
            if (key.StartsWith("replace"))
            {
                yield return ParseReplaceProp(val.Str());
            }
        }
    }

    public string? GetReplaceProp(string name)
    {
        foreach (var (key, val) in GetReplaceProps())
        {
            if (key == name)
                return val;
        }
        return null;
    }

    public void SetReplaceProps(IEnumerable<KeyValuePair<string, string>> props)
    {
        // Clear any other replace props.
        var toRemove = Properties.Where(pair => pair.Key.StartsWith("replace"))
            .Select(pair => pair.Key)
            .ToList();

        foreach (var key in toRemove)
        {
            Properties.Remove(key);
        }

        int i = 1;
        foreach (var (key, value) in props)
        {
            SetProperty("replace" + i.ToString("00"), WriteReplaceProp(key, value));
            i++;
        }
    }

    public void AddReplaceProps(IEnumerable<KeyValuePair<string, string>> props)
    {
        int i = GetReplaceProps().Count() + 1;
        foreach(var (key, value) in props)
        {
            SetProperty("replace" + i.ToString("00"), WriteReplaceProp(key, value));
            i++;
        }
    }

    public void AddReplaceProp(string key, string value)
    {
        int index = GetReplaceProps().Count() + 1;
        SetProperty("replace" + index.ToString("00"), WriteReplaceProp(key, value));
    }

    public static KeyValuePair<string, string> ParseReplaceProp(string propValue)
    {
        if (propValue.StartsWith('$'))
            propValue = propValue.Substring(1);

        string[] parts = propValue.Split(' ', 2);
        if (parts.Length == 0)
            return new("", "");

        return new(parts[0], parts.Length >= 2 ? parts[1] : "");
    }

    public static string WriteReplaceProp(string key, string value)
    {
        return "$" + key + " " + value;
    }
}