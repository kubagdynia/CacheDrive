namespace CacheDrive;

public interface ICacheable
{
    string CacheKey { get; }

    static abstract string GetCacheKey(string key);
}