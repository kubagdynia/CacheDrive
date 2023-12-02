namespace CacheDrive;

internal interface ICacheable
{
    string CacheKey { get; }

    static abstract string GetCacheKey(string key);
}