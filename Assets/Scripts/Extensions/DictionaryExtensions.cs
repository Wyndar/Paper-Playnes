using System.Collections.Generic;
using System.Linq;

public static class DictionaryExtensions
{
    public static TKey GetKeyByValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue value)
    {
        return dictionary.FirstOrDefault(pair => EqualityComparer<TValue>.Default.Equals(pair.Value, value)).Key;
    }
}
