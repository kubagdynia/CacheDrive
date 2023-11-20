using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CacheDrive.Models;

public class CachedItem
{
    public string Key { get; set; }
    
    public DateTime Cached { get; set; }
    
    public DateTime Expires { get; set; }
    
    public JsonElement Contents { get; set; }
    
    [JsonIgnore]
    public bool Dirty { get; set; }
    
    private object _unwrapped;
    
    internal static CachedItem FromCacheable<T>(T item, TimeSpan expiry, bool dirty = false) where T : ICacheable
    {
        return new CachedItem
        {
            Cached = DateTime.UtcNow,
            Expires = DateTime.UtcNow + expiry,
            Key = item.CacheKey,
            Contents = JsonSerializer.SerializeToElement(item, JsonSettings.JsonOptions),
            Dirty = dirty
        };
    }
    
    internal bool Expired()
        => Expires < DateTime.UtcNow;

    internal void Invalidate()
    {
        _unwrapped = null;
        Dirty = true;
    }
    
    internal T Unwrap<T>() where T : ICacheable
    {
        _unwrapped ??= Contents.Deserialize<T>(JsonSettings.JsonOptions);
        return (T)_unwrapped;
    }
}