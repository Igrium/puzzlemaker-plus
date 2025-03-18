using System.Collections;
using System.Diagnostics.CodeAnalysis;
using VMFLib.VClass;

namespace VMFLib.Util;

public class ParamReplaceDict : IDictionary<string, VProperty>
{
    public BaseVClass VClass { get; init; }

    public ParamReplaceDict(BaseVClass vClass)
    {
        this.VClass = vClass;
    }

    public VProperty this[string key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public ICollection<string> Keys => throw new NotImplementedException();

    public ICollection<VProperty> Values => throw new NotImplementedException();

    public int Count => throw new NotImplementedException();

    public bool IsReadOnly => throw new NotImplementedException();

    public void Add(string key, VProperty value)
    {
        throw new NotImplementedException();
    }

    public void Add(KeyValuePair<string, VProperty> item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(KeyValuePair<string, VProperty> item)
    {
        throw new NotImplementedException();
    }

    public bool ContainsKey(string key)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(KeyValuePair<string, VProperty>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<KeyValuePair<string, VProperty>> GetEnumerator()
    {
        foreach (var (name, val) in VClass.Properties)
        {
            if (name.StartsWith("replace"))
            {
                string valStr = val.Str();
                if (!valStr.StartsWith('$')) continue;

                string[] split = valStr.Split(' ', 2);
                if (split.Length != 2) continue;

                string key = split[0];
                string value = split[1];

                yield return new(key, new VProperty(key, value));
            }
        }
    }

    public bool Remove(string key)
    {
        throw new NotImplementedException();
    }

    public bool Remove(KeyValuePair<string, VProperty> item)
    {
        throw new NotImplementedException();
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out VProperty value)
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
