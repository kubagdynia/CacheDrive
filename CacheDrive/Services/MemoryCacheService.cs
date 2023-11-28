using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    
    private readonly CacheSettings _cacheSettings;

    private int _cacheExpirationInSeconds;

    public MemoryCacheService(IOptions<CacheSettings> settings, IDateService dateService)
    {
        _dateService = dateService;
        _cacheSettings = settings?.Value;
        CalculateCacheExpiration();
    }

    protected IDateService DateService => _dateService;
    
    public bool HasItem(string key)
    {
        if (Storage.TryGetValue(key, out var cachedItem))
        {
            return !cachedItem.Expired(_dateService);
        }

        return false;
    }
    
    public bool TryGetValue<T>(string key, out T value)
    {
        if (TryGetCacheableValue(key, out ObjectItem<T> result))
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
        if (!Storage.TryGetValue(ObjectItem<T>.GetCacheKey(key), out CachedItem cachedItem))
        {
            value = default;
            return false;
        }
        
        if (cachedItem.Expired(_dateService))
        {
            DeleteAsync(cachedItem);
            value = default;
            return false;
        }
        
        value = cachedItem.Unwrap<T>();
        return true;
    }
    
    public async Task<T> GetAsync<T>(string key)
    {
        var cachedItem = await GetCacheableAsync<ObjectItem<T>>(key);

        if (cachedItem is null)
        {
            return await Task.FromResult<T>(default);
        }
        
        return await Task.FromResult(cachedItem.Value);
    }

    private async Task<T> GetCacheableAsync<T>(string key) where T : ICacheable
    {
        var cacheKey = T.GetCacheKey(key);
        
        var cachedItem = Get(cacheKey);

        if (cachedItem is null)
            return await Task.FromResult<T>(default);
        
        if (cachedItem.Expired(_dateService))
        {
            await DeleteAsync(cachedItem);
            return await Task.FromResult<T>(default);
        }

        return cachedItem.Unwrap<T>();
    }

    public void Set<T>(T item, int expirySeconds = 0) where T : ICacheable
    {
        var length = GetExpirationLength(expirySeconds);
        
        if (Storage.TryGetValue(item.CacheKey, out var cached))
        {
            cached.Contents = JsonSerializer.SerializeToElement(item, JsonSettings.JsonOptions);
            cached.Cached = DateTime.UtcNow;
            cached.Expires = cached.Cached.Add(length);
            cached.Invalidate();
            return;
        }
        
        var cachedItem = CachedItem.FromCacheable(item, length, _dateService, true);
        Set(cachedItem);
    }
    
    public Task SetAsync<T>(string key, T value)
    {
        return SetAsync(ObjectItem<T>.Create(key, value));
    }
    
    public Task SetAsync<T>(T item, int expirySeconds = 0) where T : ICacheable
    {
        var cached = Get(item);

        TimeSpan length = GetExpirationLength(expirySeconds);

        if (cached != null)
        {
            cached.Contents = JsonSerializer.SerializeToElement(item, JsonSettings.JsonOptions);
            cached.Cached = DateTime.UtcNow;
            cached.Expires = cached.Cached.Add(length);
            cached.Invalidate();
            return Task.CompletedTask;
        }

        var cachedItem = CachedItem.FromCacheable(item, length, _dateService, true);

        return SetAsync(cachedItem);
    }
    
    private void Set(CachedItem cachedItem)
    {
        Storage.AddOrUpdate(cachedItem.Key, _ => cachedItem, (_, _) => cachedItem);
    }

    internal Task SetAsync(CachedItem cachedItem)
    {
        Storage.AddOrUpdate(cachedItem.Key, _ => cachedItem, (_, _) => cachedItem);
        return Task.CompletedTask;
    }

    public bool Delete(string key)
        => key is not null && Storage.TryRemove(key, out _);
    
    public Task<bool> DeleteAsync(string key)
        => key is null ? Task.FromResult(false) : Task.FromResult(Storage.TryRemove(key, out _));
    
    private bool Delete(CachedItem item)
        => Delete(item.Key);
    
    private Task<bool> DeleteAsync(CachedItem item)
        => DeleteAsync(item.Key);

    public void DeletePrefix(string prefix)
    {
        var toDeleted = new List<string>();
        
        foreach (var pair in Storage)
        {
            if (pair.Key.StartsWith(prefix))
            {
                toDeleted.Add(pair.Key);
            }
        }

        foreach (var key in toDeleted)
        {
            Delete(key);
        }
    }
    
    public Task DeletePrefixAsync(string prefix)
    {
        var toDeleted = new List<string>();
        
        foreach (var pair in Storage)
        {
            if (pair.Key.StartsWith(prefix))
            {
                toDeleted.Add(pair.Key);
            }
        }

        foreach (var key in toDeleted)
        {
            DeleteAsync(key);
        }

        return Task.CompletedTask;
    }

    public virtual Task FlushAsync()
        => Task.CompletedTask;

    public virtual Task InitializeAsync()
        => Task.CompletedTask;

    private CachedItem Get(ICacheable item)
        => Get(item.CacheKey);

    private CachedItem Get(string key)
        => Storage.TryGetValue(key, out var cachedItem) ? cachedItem : null;
    
    private void CalculateCacheExpiration()
    {
        _cacheExpirationInSeconds = _cacheSettings.CacheExpirationType switch
        {
            CacheExpirationType.Seconds => _cacheSettings.CacheExpiration,
            CacheExpirationType.Minutes => _cacheSettings.CacheExpiration * 60,
            CacheExpirationType.Hours => _cacheSettings.CacheExpiration * 60 * 60,
            CacheExpirationType.Days => _cacheSettings.CacheExpiration * 60 * 60 * 24,
            CacheExpirationType.Never => -1,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    private TimeSpan GetExpirationLength(int expirySeconds = 0)
    {
        if (expirySeconds == 0)
        {
            return _cacheSettings.CacheExpirationType == CacheExpirationType.Never
                ? TimeSpan.FromDays(365 * 1000)
                : TimeSpan.FromSeconds(_cacheExpirationInSeconds);
        }

        return TimeSpan.FromSeconds(expirySeconds);
    }
}