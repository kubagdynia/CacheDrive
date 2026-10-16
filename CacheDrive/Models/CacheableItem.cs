using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace CacheDrive.Models;

public class CacheableItem<T> : ICacheable
{
    // Computed once per closed generic type.
    private static readonly string TypeName = BuildTypeName(typeof(T));

    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("value")]
    public T Value { get; set; }

    [JsonIgnore]
    public string CacheKey
        => GetCacheKey(Key);

    public static string GetCacheKey(string key)
        => $"{TypeName}@{key}";

    public static CacheableItem<T> Create(string key, T value)
        => new() { Key = key, Value = value };

    /// <summary>
    /// Non-generic types keep their lower-cased simple name (so existing cache keys stay valid).
    /// Generic types include their type arguments, so e.g. List&lt;int&gt; and List&lt;string&gt;
    /// no longer share the same cache key.
    /// </summary>
    private static string BuildTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name.ToLower();
        }

        string name = type.Name;
        int backtickIndex = name.IndexOf('`');
        if (backtickIndex >= 0)
        {
            name = name.Substring(0, backtickIndex);
        }

        string arguments = string.Join(",", type.GetGenericArguments().Select(BuildTypeName));
        return $"{name.ToLower()}<{arguments}>";
    }
}
