using System.Text.Json.Serialization;

namespace CacheDrive.Models;

public class SpecificField : ICacheable
{
    [JsonPropertyName("key")]
    public string Key { get; set; }
    
    [JsonPropertyName("value")]
    public string Value { get; set; }
    
    [JsonIgnore]
    public string CacheKey
        => GetCacheKey(Key);
    
    public static string GetCacheKey(string key)
        => $"specific-field@{key}";

    public static SpecificField Create(string key, string value)
        => new() { Key = key, Value = value };
}