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
        
        // SetAsync, GetAsync and TryGetValue with hashKey
        string cacheKey2 = "testKey";
        await cacheService.SetAsync(cacheKey2, "test text...", hashKey: true);
        var cachedValueWithHashKey1 = await cacheService.GetAsync<string>(cacheKey2, hashKey: true);
        Console.WriteLine($"GetAsync with hashKey - cached value: {cachedValueWithHashKey1}");
        
        Console.WriteLine(cacheService.TryGetValue(cacheKey2, hashKey: true, out string cachedValueWithHashKey2)
            ? $"TryGetValue with haskkey OK - cached value: {cachedValueWithHashKey2}"
            : $"TryGetValue with haskkey NOK - cached value: {cachedValueWithHashKey2}");

        Console.WriteLine();
        
        // Set, Get
        string cacheKey3 = "testKey2";
        cacheService.Set(cacheKey3, 1234567);
        int cachedValue3 = cacheService.Get<int>(cacheKey3);
        Console.WriteLine($"Get - cached value: {cachedValue3} ");
        
        // Set, Get with hashKey
        string cacheKey4 = "testKey2";
        cacheService.Set(cacheKey4, 1234567, hashKey: true);
        int cachedValueWithHashKey3 = cacheService.Get<int>(cacheKey4, hashKey: true);
        Console.WriteLine($"Get with hashKey - cached value: {cachedValueWithHashKey3} ");
    }
}