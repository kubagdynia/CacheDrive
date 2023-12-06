using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using CacheDrive.Configuration;
using CacheDrive.Models;
using Microsoft.Extensions.Options;

namespace CacheDrive.Services;

internal class MemoryCacheService : ICacheService
{
    private readonly IDateService _dateService;
    internal readonly ConcurrentDictionary<string, CachedItem> Storage = new();
    
    protected readonly CacheSettings CacheSettings;

    private int _cacheExpirationInSeconds;

    public MemoryCacheService(IOptions<CacheSettings> settings, IDateService dateService)
    {
        _dateService = dateService;
        CacheSettings = settings?.Value;
        CalculateCacheExpiration();
    }

    protected IDateService DateService => _dateService;

    public virtual Task InitializeAsync()
        => Task.CompletedTask;
    
    public virtual Task FlushAsync()
        => Task.CompletedTask;
    
    public bool HasItem(string key)
    {
        if (Storage.TryGetValue(key, out CachedItem cachedItem))
        {
            return !cachedItem.Expired(_dateService);
        }

        return false;
    }
    
    public bool TryGetValue<T>(string key, out T value)
    {
        if (TryGetCacheableValue(key, out CacheableItem<T> result))
        {
            if (result == null)
            {
                value = default;
                return true;
            }
            
            if (result.Value is { } item)
            {
                value = item;
                return true;
            }
        }
        
        value = default;
        return false;
    }

    private bool TryGetCacheableValue<T>(string key, out T value) where T : ICacheable
    {
        string cacheKey = T.GetCacheKey(key);
        
        if (!Storage.TryGetValue(cacheKey, out CachedItem cachedItem))
        {
            value = default;
            return false;
        }
        
        if (cachedItem.Expired(_dateService))
        {
            Delete<T>(cachedItem.Key);
            value = default;
            return false;
        }
        
        value = cachedItem.Unwrap<T>();
        return true;
    }
    
    public T Get<T>(string key)
    {
        CacheableItem<T> cachedItem = GetCacheable<CacheableItem<T>>(key);

        if (cachedItem is null)
        {
            return default;
        }
        
        return cachedItem.Value;
    }
    
    public async Task<T> GetAsync<T>(string key)
    {
        CacheableItem<T> cachedItem = await GetCacheableAsync<CacheableItem<T>>(key);

        if (cachedItem is null)
        {
            return await Task.FromResult<T>(default);
        }
        
        return await Task.FromResult(cachedItem.Value);
    }

    private async Task<T> GetCacheableAsync<T>(string key) where T : ICacheable
    {
        string cacheKey = T.GetCacheKey(key);
        
        CachedItem cachedItem = Get(cacheKey);

        if (cachedItem is null)
            return await Task.FromResult<T>(default);
        
        if (cachedItem.Expired(_dateService))
        {
            await DeleteAsync(cachedItem);
            return await Task.FromResult<T>(default);
        }

        return cachedItem.Unwrap<T>();
    }
    
    private T GetCacheable<T>(string key) where T : ICacheable
    {
        string cacheKey = T.GetCacheKey(key);
        
        CachedItem cachedItem = Get(cacheKey);

        if (cachedItem is null)
            return default;
        
        if (cachedItem.Expired(_dateService))
        {
            Delete(cachedItem);
            return default;
        }

        return cachedItem.Unwrap<T>();
    }
    
    public void Set<T>(string key, T value, int expirySeconds = 0)
    {
        SetCacheableItem(CacheableItem<T>.Create(key, value), expirySeconds);
    }
    
    public Task SetAsync<T>(string key, T value, int expirySeconds = 0)
    {
        return SetAsync(CacheableItem<T>.Create(key, value), expirySeconds);
    }
    
    private void SetCacheableItem<T>(T item, int expirySeconds = 0) where T : ICacheable
    {
        TimeSpan length = GetExpirationLength(expirySeconds);
        
        if (Storage.TryGetValue(item.CacheKey, out CachedItem cached))
        {
            cached.Contents = JsonSerializer.SerializeToElement(item, JsonSettings.JsonOptions);
            cached.Cached = DateTime.UtcNow;
            cached.Expires = cached.Cached.Add(length);
            cached.Invalidate();
            return;
        }
        
        CachedItem cachedItem = CachedItem.FromCacheable(item, expiry: length, _dateService, dirty: true);
        Set(cachedItem);
    }
    
    private Task SetAsync<T>(T item, int expirySeconds = 0) where T : ICacheable
    {
        CachedItem cached = Get(item);

        TimeSpan length = GetExpirationLength(expirySeconds);

        if (cached != null)
        {
            cached.Contents = JsonSerializer.SerializeToElement(item, JsonSettings.JsonOptions);
            cached.Cached = DateTime.UtcNow;
            cached.Expires = cached.Cached.Add(length);
            cached.Invalidate();
            return Task.CompletedTask;
        }

        CachedItem cachedItem = CachedItem.FromCacheable(item, expiry: length, _dateService, dirty: true);

        return SetAsync(cachedItem);
    }
    
    internal void Set(CachedItem cachedItem)
    {
        Storage.AddOrUpdate(cachedItem.Key, _ => cachedItem, (_, _) => cachedItem);
    }

    internal Task SetAsync(CachedItem cachedItem)
    {
        Storage.AddOrUpdate(cachedItem.Key, _ => cachedItem, (_, _) => cachedItem);
        return Task.CompletedTask;
    }

    public bool Delete<T>(string key)
        => key is not null && Storage.TryRemove(CacheableItem<T>.GetCacheKey(key), out _);
    
    public Task<bool> DeleteAsync<T>(string key)
        => key is null ? Task.FromResult(false) : DeleteAsync(CacheableItem<T>.GetCacheKey(key));
    
    private Task<bool> DeleteAsync(string key)
        => key is null ? Task.FromResult(false) : Task.FromResult(Storage.TryRemove(key, out _));
    
    private bool Delete(string key)
        => key is not null && Storage.TryRemove(key, out _);
    
    private Task<bool> DeleteAsync(CachedItem item)
        => DeleteAsync(item.Key);
    
    private bool Delete(CachedItem item)
        => Delete(item.Key);

    public int CountCachedItems()
        => Storage.Count;

    private CachedItem Get(ICacheable item)
        => Get(item.CacheKey);

    private CachedItem Get(string key)
        => Storage.TryGetValue(key, out CachedItem cachedItem) ? cachedItem : null;
    
    private void CalculateCacheExpiration()
    {
        _cacheExpirationInSeconds = CacheSettings.CacheExpirationType switch
        {
            CacheExpirationType.Seconds => CacheSettings.CacheExpiration,
            CacheExpirationType.Minutes => CacheSettings.CacheExpiration * 60,
            CacheExpirationType.Hours => CacheSettings.CacheExpiration * 60 * 60,
            CacheExpirationType.Days => CacheSettings.CacheExpiration * 60 * 60 * 24,
            CacheExpirationType.Never => -1,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    private TimeSpan GetExpirationLength(int expirySeconds = 0)
    {
        if (expirySeconds == 0)
        {
            return CacheSettings.CacheExpirationType == CacheExpirationType.Never
                ? TimeSpan.FromDays(365 * 1000)
                : TimeSpan.FromSeconds(_cacheExpirationInSeconds);
        }

        return TimeSpan.FromSeconds(expirySeconds);
    }
}