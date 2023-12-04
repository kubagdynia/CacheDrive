using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CacheDrive.Configuration;
using CacheDrive.Models;
using Microsoft.Extensions.Options;

namespace CacheDrive.Services;

internal class MemoryCacheFileStorageService : MemoryCacheService
{
    public MemoryCacheFileStorageService(IOptions<CacheSettings> settings, IDateService dateService)
        : base(settings, dateService)
    {
        CreateCacheDirectory();
        Initialize();
    }

    ~MemoryCacheFileStorageService()
    {
        Flush();
    }
    
    public override async Task FlushAsync()
    {
        foreach ((string key, CachedItem item) in Storage)
        {
            if (!item.Dirty || item.Expired(DateService))
                continue;

            string cachedItem = JsonSerializer.Serialize(item, JsonSettings.JsonOptions);

            string path = GetFilePath(key);

            await using StreamWriter sw = File.CreateText(path);
            await sw.WriteLineAsync(cachedItem);
            item.Dirty = false;
        }
    }
    
    private void Flush()
    {
        foreach ((string key, CachedItem item) in Storage)
        {
            if (!item.Dirty || item.Expired(DateService))
                continue;

            string cachedItem = JsonSerializer.Serialize(item, JsonSettings.JsonOptions);
            
            string path = GetFilePath(key);

            using StreamWriter sw = File.CreateText(path);
            sw.WriteLineAsync(cachedItem);
            item.Dirty = false;
        }
    }
    
    public override async Task InitializeAsync()
    {
        string[] files = GetCacheFiles();
        
        foreach (string file in files)
        {
            string jsonString = await File.ReadAllTextAsync(file);
            CachedItem cachedItem = DeserializeCachedItem(jsonString);

            if (cachedItem is null) return;

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
        string[] files = GetCacheFiles();
        
        foreach (string file in files)
        {
            string jsonString = File.ReadAllText(file);
            CachedItem cachedItem = DeserializeCachedItem(jsonString);
    
            if (cachedItem is null) return;
    
            if (cachedItem.Expired(DateService))
            {
                File.Delete(file);
                continue;
            }
    
            Set(cachedItem);
        }
    }
    
    private Regex _safeFilenameRegex;
    
    private Regex SafeFilenameRegex()
    {
        if (_safeFilenameRegex is null)
        {
            string regSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            _safeFilenameRegex = new Regex(pattern: $"[{Regex.Escape(regSearch)}]");
        }

        return _safeFilenameRegex;
    }
    
    private string GetFilePath(string key)
    {
        string safeFilename = SafeFilenameRegex().Replace(input: key, replacement: string.Empty);
        string path = CachePath(fileName: $"{safeFilename}.json");
        return path;
    }
    
    private string[] GetCacheFiles()
        => Directory.GetFiles(path: GetCacheDirectory(), searchPattern: "*.json");
    
    private string CachePath(string fileName)
        => Path.Combine(Environment.CurrentDirectory, CacheSettings.CacheFolderName, fileName);

    private string GetCacheDirectory()
        => Path.Combine(Environment.CurrentDirectory, CacheSettings.CacheFolderName);

    private CachedItem DeserializeCachedItem(string jsonString)
        => JsonSerializer.Deserialize<CachedItem>(jsonString, JsonSettings.JsonOptions);
    
    private void CreateCacheDirectory()
    {
        var folderName = GetCacheDirectory();
        if (!string.IsNullOrEmpty(folderName) && !Directory.Exists(folderName))
        {
            Directory.CreateDirectory(folderName);
        }
    }
}