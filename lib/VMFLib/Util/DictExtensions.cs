using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VMFLib.Util;

internal static class DictExtensions
{
    public static V? GetValueOrDefault<K, V>(this IDictionary<K, V> dict, K key)
    {
        if (dict.TryGetValue(key, out V? value))
            return value;
        else return default;
    }
}
