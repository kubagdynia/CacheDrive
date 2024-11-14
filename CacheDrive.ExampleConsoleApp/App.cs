using CacheDrive.Services;

namespace CacheDrive.ExampleConsoleApp;

public class App
{
    private readonly ICacheService _cacheService;

    public App(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }
    
    public async Task Run()
    {
        // SetAsync, GetAsync and TryGetValue
        string cacheKey1 = "testKey";
        await _cacheService.SetAsync(cacheKey1, "test text...");
        var cachedValue1 = await _cacheService.GetAsync<string>(cacheKey1);
        Console.WriteLine($"GetAsync - cached value: {cachedValue1}");
        
        Console.WriteLine(_cacheService.TryGetValue(cacheKey1, out string cachedValue2)
            ? $"TryGetValue OK - cached value: {cachedValue2}"
            : $"TryGetValue NOK - cached value: {cachedValue2}");
        
        Console.WriteLine();
        
        // Set, Get
        string cacheKey3 = "testKey2";
        _cacheService.Set(cacheKey3, 1234567);
        int cachedValue3 = _cacheService.Get<int>(cacheKey3);
        Console.WriteLine($"Get - cached value: {cachedValue3} ");
        
        // ************ with hashKey
        
        Console.WriteLine();
        Console.WriteLine("with hashKey");
        Console.WriteLine();
        
        // SetAsync, GetAsync and TryGetValue with hashKey
        string cacheKey2 = "testKey";
        await _cacheService.SetAsync(cacheKey2, "test text...", hashKey: true);
        var cachedValueWithHashKey1 = await _cacheService.GetAsync<string>(cacheKey2, hashKey: true);
        Console.WriteLine($"GetAsync with hashKey - cached value: {cachedValueWithHashKey1}");
        
        Console.WriteLine(_cacheService.TryGetValue(cacheKey2, hashKey: true, out string cachedValueWithHashKey2)
            ? $"TryGetValue with haskkey OK - cached value: {cachedValueWithHashKey2}"
            : $"TryGetValue with haskkey NOK - cached value: {cachedValueWithHashKey2}");
        
        // Set, Get with hashKey
        
        Console.WriteLine();
        
        string cacheKey4 = "testKey2";
        _cacheService.Set(cacheKey4, 1234567, hashKey: true);
        int cachedValueWithHashKey3 = _cacheService.Get<int>(cacheKey4, hashKey: true);
        Console.WriteLine($"Get with hashKey - cached value: {cachedValueWithHashKey3} ");
    }
}