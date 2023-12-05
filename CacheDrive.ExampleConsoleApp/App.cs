using CacheDrive.Services;

namespace CacheDrive.ExampleConsoleApp;

public class App(ICacheService cacheService)
{
    public async Task Run()
    {
        string cacheKey = "testKey";
        
        await cacheService.SetAsync(cacheKey, "test text...");

        Console.WriteLine(cacheService.TryGetValue(cacheKey, out string cachedValue)
            ? $"OK: cached value - {cachedValue}"
            : $"NOK: cached value - {cachedValue}");
    }
}