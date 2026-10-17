using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CacheDrive.Configuration;
using CacheDrive.Models;
using Microsoft.Extensions.Options;

namespace CacheDrive.Services;

internal class MemoryCacheFileStorageService : MemoryCacheService, IDisposable, IAsyncDisposable
{
    private static readonly HashSet<char> UnsafeFileNameChars = CreateUnsafeFileNameChars();

    // Resolved once, so the cache location does not move if the current directory changes at runtime.
    private readonly string _cacheDirectory;

    private bool _disposed;

    public MemoryCacheFileStorageService(IOptions<CacheSettings> settings, IDateService dateService)
        : base(settings, dateService)
    {
        if (settings.Value.CacheEnabled)
        {
            _cacheDirectory = Path.Combine(Environment.CurrentDirectory, settings.Value.CacheFolderName);

            CreateCacheDirectory();

            if (settings.Value.InitializeOnStartup)
            {
                Initialize();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (CacheSettings.CacheEnabled && CacheSettings.FlushOnExit)
        {
            Flush();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (CacheSettings.CacheEnabled && CacheSettings.FlushOnExit)
        {
            await FlushAsync();
        }
    }

    public override async Task FlushAsync()
    {
        if (!CacheSettings.CacheEnabled)
        {
            return;
        }

        foreach ((string key, CachedItem item) in Storage)
        {
            if (!item.Dirty || item.Expired(DateService))
                continue;

            string json = JsonSerializer.Serialize(item, JsonSettings.JsonOptions);
            string path = GetFilePath(key);
            string tempPath = path + ".tmp";

            // Write to a temporary file first, so a crash never leaves a half-written cache file behind.
            await File.WriteAllTextAsync(tempPath, json + Environment.NewLine);
            File.Move(tempPath, path, overwrite: true);
            item.Dirty = false;
        }
    }

    private void Flush()
    {
        foreach ((string key, CachedItem item) in Storage)
        {
            if (!item.Dirty || item.Expired(DateService))
                continue;

            string json = JsonSerializer.Serialize(item, JsonSettings.JsonOptions);
            string path = GetFilePath(key);
            string tempPath = path + ".tmp";

            File.WriteAllText(tempPath, json + Environment.NewLine);
            File.Move(tempPath, path, overwrite: true);
            item.Dirty = false;
        }
    }

    protected override void OnItemRemoved(string storageKey)
    {
        if (_cacheDirectory is null)
        {
            return;
        }

        // Without this the item would be loaded back from its file on the next start.
        // I/O errors are not swallowed: a file that stays behind would silently bring the item back.
        string path = GetFilePath(storageKey);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public override async Task InitializeAsync()
    {
        if (!CacheSettings.CacheEnabled)
        {
            return;
        }

        foreach (string file in GetCacheFiles())
        {
            string jsonString = await File.ReadAllTextAsync(file);

            if (!TryDeserializeCachedItem(jsonString, out CachedItem cachedItem))
            {
                // Skip empty or corrupted files instead of aborting the whole load.
                continue;
            }

            if (cachedItem.Expired(DateService))
            {
                File.Delete(file);
                continue;
            }

            await SetAsync(cachedItem);
        }
    }

    private void Initialize()
    {
        foreach (string file in GetCacheFiles())
        {
            string jsonString = File.ReadAllText(file);

            if (!TryDeserializeCachedItem(jsonString, out CachedItem cachedItem))
            {
                continue;
            }

            if (cachedItem.Expired(DateService))
            {
                File.Delete(file);
                continue;
            }

            Set(cachedItem);
        }
    }

    /// <summary>
    /// Builds an injective, file-system-safe file name: every character that is not allowed
    /// in a file name (and '%' itself) is percent-encoded instead of dropped,
    /// so different keys can never end up in the same file.
    /// </summary>
    private static string ToSafeFileName(string key)
    {
        var builder = new StringBuilder(key.Length);

        foreach (char c in key)
        {
            if (UnsafeFileNameChars.Contains(c))
            {
                builder.Append('%').Append(((int)c).ToString("X2"));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static HashSet<char> CreateUnsafeFileNameChars()
    {
        var chars = new HashSet<char>(Path.GetInvalidFileNameChars());
        chars.UnionWith(Path.GetInvalidPathChars());
        chars.Add('%');
        return chars;
    }

    private string GetFilePath(string key)
        => Path.Combine(_cacheDirectory, $"{ToSafeFileName(key)}.json");

    private string[] GetCacheFiles()
        => Directory.GetFiles(path: _cacheDirectory, searchPattern: "*.json");

    private static bool TryDeserializeCachedItem(string jsonString, out CachedItem cachedItem)
    {
        try
        {
            cachedItem = JsonSerializer.Deserialize<CachedItem>(jsonString, JsonSettings.JsonOptions);
            return cachedItem is not null && !string.IsNullOrEmpty(cachedItem.Key);
        }
        catch (JsonException)
        {
            cachedItem = null;
            return false;
        }
    }

    private void CreateCacheDirectory()
    {
        if (!string.IsNullOrEmpty(_cacheDirectory) && !Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }
}
