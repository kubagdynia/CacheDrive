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
    private const string CacheFolderName = "cache";

    public MemoryCacheFileStorageService(IOptions<CacheSettings> settings, IDateService dateService)
        : base(settings, dateService)
    {
        CreateCacheDirectory();
    }
    
    public override async Task FlushAsync()
    {
        foreach ((string key, CachedItem item) in Storage)
        {
            if (!item.Dirty || item.Expired(DateService))
                continue;

            string cachedItem = JsonSerializer.Serialize(item, JsonSettings.JsonOptions);

            string safeFilename = SafeFilenameRegex().Replace(input: key, replacement: string.Empty);
            
            string path = CachePath(fileName: $"{safeFilename}.json");

            await using StreamWriter sw = File.CreateText(path);
            await sw.WriteLineAsync(cachedItem);
            item.Dirty = false;
        }
    }

    public override async Task InitializeAsync()
    {
        string[] dirs = Directory.GetFiles(path: GetCacheDirectory(), searchPattern: "*.json");
        
        foreach (string file in dirs)
        {
            string jsonString = await File.ReadAllTextAsync(file);

            CachedItem cachedItem = JsonSerializer.Deserialize<CachedItem>(jsonString, JsonSettings.JsonOptions);

            if (cachedItem is null) return;

            if (cachedItem.Expired(DateService))
            {
                File.Delete(file);
                continue;
            }

            await SetAsync(cachedItem);
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
    
    private string CachePath(string fileName)
        => Path.Combine(Environment.CurrentDirectory, CacheFolderName, fileName);

    private string GetCacheDirectory()
        => Path.Combine(Environment.CurrentDirectory, CacheFolderName);
    
    private void CreateCacheDirectory()
    {
        var folderName = GetCacheDirectory();
        if (!string.IsNullOrEmpty(folderName) && !Directory.Exists(folderName))
        {
            Directory.CreateDirectory(folderName);
        }
    }
}