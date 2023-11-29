using System.Text.Json.Serialization;

namespace CacheDrive.Models;

public class ObjectItem<T> : ICacheable
{
    [JsonPropertyName("key")]
    public string Key { get; set; }
    
    [JsonPropertyName("value")]
    public T Value { get; set; }
    
    [JsonIgnore]
    public string CacheKey
        => GetCacheKey(Key);
    
    public static string GetCacheKey(string key)
        => $"{typeof(T).Name.ToLower()}@{key}";
    
    public static ObjectItem<T> Create(string key, T value)
        => new() { Key = key, Value = value };
}