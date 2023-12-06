using CacheDrive.Services;

namespace CacheDrive.ExampleConsoleApp;

public class App(ICacheService cacheService)
{
    public async Task Run()
    {
        // SetAsync, GetAsync and TryGetValue
        string cacheKey1 = "testKey";
        await cacheService.SetAsync(cacheKey1, "test text...");
        var cachedValue1 = await cacheService.GetAsync<string>(cacheKey1);
        Console.WriteLine($"GetAsync - cached value: {cachedValue1}");
        
        Console.WriteLine(cacheService.TryGetValue(cacheKey1, out string cachedValue2)
            ? $"TryGetValue OK - cached value: {cachedValue2}"
            : $"TryGetValue NOK - cached value: {cachedValue2}");

        // Set, Get
        string cacheKey2 = "testKey2";
        cacheService.Set(cacheKey2, 1234567);
        int cachedValue3 = cacheService.Get<int>(cacheKey2);
        Console.WriteLine($"Get - cached value: {cachedValue3} ");
    }
}