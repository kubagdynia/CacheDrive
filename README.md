# CacheDrive

[![CI](https://img.shields.io/github/actions/workflow/status/kubagdynia/CacheDrive/dotnet.yml?branch=main)](https://github.com/kubagdynia/CacheDrive/actions?query=branch%3Amain) [![NuGet Version](https://img.shields.io/nuget/v/CacheDrive.svg?style=flat)](https://www.nuget.org/packages/CacheDrive/)

Simple in-memory caching provider with the ability to store objects in files.

### Project structure

- CacheDrive - CacheDrive library
- CacheDrive.ExampleConsoleApp - console application with example use of the Cache Drive library
- CacheDrive.Tests - unit tests



### Installation

Use NuGet Package Manager
```
Install-Package CacheDrive
```
or .NET CLI
```
dotnet add package CacheDrive
```

or just copy into the project file to reference the package
```
<PackageReference Include="CacheDrive" Version="0.3.0" />
```



### How to use

- Register a CacheDrive library, e.g.
```csharp
var services = new ServiceCollection();

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

services.RegisterCacheDrive(configuration, configurationSectionName: "CacheSettings");

var serviceProvider = services.BuildServiceProvider();

// rest of the app

serviceProvider.Dispose();
```
- Optionally add the configuration in appsettings.json
```json
{
  "CacheSettings": {
    "CacheEnabled": true,
    "CacheFolderName": "cache",
    "CacheExpirationType": "Minutes",
    "CacheExpiration": 60,
    "CacheType": "MemoryAndFile",
    "InitializeOnStartup": true,
    "FlushOnExit": true,
    "HashKeySalt": "Secret123Secret"
  }
}
```
- You can also specify configurations when registering the cacheDrive library
```csharp
services.RegisterCacheDrive(settings: new CacheSettings
{
    CacheEnabled = true,
    CacheFolderName = "cache",
    CacheExpirationType = CacheExpirationType.Minutes,
    CacheExpiration = 60,
    CacheType = CacheType.MemoryAndFile,
    InitializeOnStartup = true,
    FlushOnExit = true,
    HashKeySalt = "Secret123Secret"
});
```
- Then by injecting ICacheService you can start using write and read from the cache
```csharp
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
        await cacheService.SetAsync(cacheKey1, "test text...");
        var cachedValue1 = await cacheService.GetAsync<string>(cacheKey1);
        Console.WriteLine($"GetAsync - cached value: {cachedValue1}");
        
        Console.WriteLine(cacheService.TryGetValue(cacheKey1, out string cachedValue2)
            ? $"TryGetValue OK - cached value: {cachedValue2}"
            : $"TryGetValue NOK - cached value: {cachedValue2}");
        
        Console.WriteLine();
        
        // Set, Get
        string cacheKey3 = "testKey2";
        cacheService.Set(cacheKey3, 1234567);
        int cachedValue3 = cacheService.Get<int>(cacheKey3);
        Console.WriteLine($"Get - cached value: {cachedValue3} ");
        
        // ************ with hashKey
        
        Console.WriteLine();
        Console.WriteLine("with hashKey");
        Console.WriteLine();
        
        // SetAsync, GetAsync and TryGetValue with hashKey
        string cacheKey2 = "testKey";
        await cacheService.SetAsync(cacheKey2, "test text...", hashKey: true);
        var cachedValueWithHashKey1 = await cacheService.GetAsync<string>(cacheKey2, hashKey: true);
        Console.WriteLine($"GetAsync with hashKey - cached value: {cachedValueWithHashKey1}");
        
        Console.WriteLine(cacheService.TryGetValue(cacheKey2, hashKey: true, out string cachedValueWithHashKey2)
            ? $"TryGetValue with haskkey OK - cached value: {cachedValueWithHashKey2}"
            : $"TryGetValue with haskkey NOK - cached value: {cachedValueWithHashKey2}");
        
        // Set, Get with hashKey
        
        Console.WriteLine();
        
        string cacheKey4 = "testKey2";
        cacheService.Set(cacheKey4, 1234567, hashKey: true);
        int cachedValueWithHashKey3 = cacheService.Get<int>(cacheKey4, hashKey: true);
        Console.WriteLine($"Get with hashKey - cached value: {cachedValueWithHashKey3} ");
    }
}
```

If the **CacheType** is set to **MemoryAndFile** in the configuration, all cache keys that have not expired will be written to files when the application terminates (the name of the directory where the files will be saved can be set in the configuration). When the application starts, the cache will be loaded from the files. If the **CacheType** is set to **Memory**, the cache will be stored only in memory.

A file with a string value might look like this:

string@testKey.json
```
{"key":"string@testKey","cached":"2024-09-04T09:39:08.5120076Z","expires":"2024-09-04T10:39:08.512008Z","contents":{"key":"testKey","value":"test text..."}}
```

If HashKeySalt is configured, the file may look like this:

string@6B86A0C7C5CEAC4414D7D3BD15DFDDA0616C7626B66F80360F438145B8CF7B9C.json
```
{"key":"string@6B86A0C7C5CEAC4414D7D3BD15DFDDA0616C7626B66F80360F438145B8CF7B9C","cached":"2024-09-04T09:39:08.5222015Z","expires":"2024-09-04T10:39:08.5222015Z","contents":{"key":"6B86A0C7C5CEAC4414D7D3BD15DFDDA0616C7626B66F80360F438145B8CF7B9C","value":"test text..."}}
```

### ICacheService API Explanation

```csharp
public interface ICacheService
{
    /// <summary>
    /// Initializes the cache, such as loading data from a file, database, and so on.
    /// Should be run before the cache is used, usually at application startup.
    /// Can be used many times, each time adding or overwriting data if they have the same keys.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Dumps cached data into files, database, and so on.
    /// Usually it should be run before the application terminates.
    /// Can be used many times, each time saving data that are new or has been changed.
    /// </summary>
    Task FlushAsync();
    
    /// <summary>
    /// Returns whether an object with the specified key exists in the cache.
    /// </summary>
    /// <param name="key">The key of the value to check.</param>
    /// <returns>true if the key was found in the cache, otherwise, false.</returns>
    bool HasItem(string key);

    /// <summary>
    /// Returns whether an object with the specified key exists in the cache.
    /// </summary>
    /// <param name="key">The key of the value to check.</param>
    /// <param name="hashKey">Whether the key should be hashed. Can be used for long keys.</param>
    /// <returns>true if the key was found in the cache, otherwise, false.</returns>
    bool HasItem(string key, bool hashKey);

    /// <summary>
    /// Attempts to get the value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">
    /// When this method returns, value contains the object from
    /// the cache with the specified key or the default value of
    /// <typeparamref name="T"/>, if the operation failed.
    /// </param>
    /// <returns>true if the key was found in the cache, otherwise, false.</returns>
    bool TryGetValue<T>(string key, out T value);

    /// <summary>
    /// Attempts to get the value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="hashKey">Whether the key should be hashed. Can be used for long keys.</param>
    /// <param name="value">
    /// When this method returns, value contains the object from
    /// the cache with the specified key or the default value of
    /// <typeparamref name="T"/>, if the operation failed.
    /// </param>
    /// <returns>true if the key was found in the cache, otherwise, false.</returns>
    bool TryGetValue<T>(string key, bool hashKey, out T value);

    /// <summary>
    /// Get the value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <returns>The value contains the object from the cache with the specified
    /// key or the default value of T, if the operation failed.
    /// </returns>
    T Get<T>(string key);

    /// <summary>
    /// Get the value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="hashKey">Whether the key should be hashed. Can be used for long keys.</param>
    /// <returns>The value contains the object from the cache with the specified
    /// key or the default value of T, if the operation failed.
    /// </returns>
    T Get<T>(string key, bool hashKey);
    
    /// <summary>
    /// Get the value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <returns>The value contains the object from the cache with the specified
    /// key or the default value of T, if the operation failed.
    /// </returns>
    Task<T> GetAsync<T>(string key);

    /// <summary>
    /// Get the value associated with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="hashKey">Whether the key should be hashed. Can be used for long keys.</param>
    /// <returns>The value contains the object from the cache with the specified
    /// key or the default value of T, if the operation failed.
    /// </returns>
    Task<T> GetAsync<T>(string key, bool hashKey);

    /// <summary>
    /// Adds a value to the cache if the key does not already exist, or updates if the key already exists.
    /// </summary>
    /// <param name="key">The key to be added or whose value should be updated.</param>
    /// <param name="value">The value to add or update.</param>
    /// <param name="expirySeconds">After how many seconds a given value will expire in the cache. Optional parameter.
    /// By default, the value is taken from the configuration.</param>
    void Set<T>(string key, T value, int expirySeconds = 0);

    /// <summary>
    /// Adds a value to the cache if the key does not already exist, or updates if the key already exists.
    /// </summary>
    /// <param name="key">The key to be added or whose value should be updated.</param>
    /// <param name="value">The value to add or update.</param>
    /// <param name="hashKey">Whether the key should be hashed. Can be used for long keys.</param>
    /// <param name="expirySeconds">After how many seconds a given value will expire in the cache. Optional parameter.
    /// By default, the value is taken from the configuration.</param>
    void Set<T>(string key, T value, bool hashKey, int expirySeconds = 0);
    
    /// <summary>
    /// Adds a value to the cache if the key does not already exist, or updates if the key already exists.
    /// </summary>
    /// <param name="key">The key to be added or whose value should be updated.</param>
    /// <param name="value">The value to add or update.</param>
    /// <param name="expirySeconds">After how many seconds a given value will expire in the cache. Optional parameter.
    /// By default, the value is taken from the configuration.</param>
    Task SetAsync<T>(string key, T value, int expirySeconds = 0);

    /// <summary>
    /// Adds a value to the cache if the key does not already exist, or updates if the key already exists.
    /// </summary>
    /// <param name="key">The key to be added or whose value should be updated.</param>
    /// <param name="value">The value to add or update.</param>
    /// <param name="hashKey">Whether the key should be hashed. Can be used for long keys.</param>
    /// <param name="expirySeconds">After how many seconds a given value will expire in the cache. Optional parameter.
    /// By default, the value is taken from the configuration.</param>
    Task SetAsync<T>(string key, T value, bool hashKey, int expirySeconds = 0);

    /// <summary>
    /// Deletes the object associated with the given key.
    /// </summary>
    /// <param name="key">The key of the element to be remove.</param>
    /// <returns>true if an object was removed successfully; otherwise, false.</returns>
    bool Delete<T>(string key);

    /// <summary>
    /// Deletes the object associated with the given key.
    /// </summary>
    /// <param name="key">The key of the element to be remove.</param>
    /// <param name="hashKey">Whether the key should be hashed. Can be used for long keys.</param>
    /// <returns>true if an object was removed successfully; otherwise, false.</returns>
    bool Delete<T>(string key, bool hashKey);
    
    /// <summary>
    /// Deletes the object associated with the given key.
    /// </summary>
    /// <param name="key">The key of the element to be remove.</param>
    /// <returns>true if an object was removed successfully; otherwise, false.</returns>
    Task<bool> DeleteAsync<T>(string key);

    /// <summary>
    /// Deletes the object associated with the given key.
    /// </summary>
    /// <param name="key">The key of the element to be remove.</param>
    /// <param name="hashKey">Whether the key should be hashed. Can be used for long keys.</param>
    /// <returns>true if an object was removed successfully; otherwise, false.</returns>
    Task<bool> DeleteAsync<T>(string key, bool hashKey);

    /// <summary>
    /// Returns the number of objects in the cache.
    /// </summary>
    /// <returns>The number of objects in the cache.</returns>
    int CountCachedObjects();

    /// <summary>
    /// Removes all keys and values from the cache
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Removes all expired keys and values from the cache
    /// </summary>
    void ClearExpiredObjects();
}
```



### Configuration

```csharp
public class CacheSettings
{
    /// <summary>
    /// Determines whether cache is enabled.
    /// Default value is true.
    /// </summary>
    public bool CacheEnabled { get; set; } = true;

    /// <summary>
    /// Cache folder name.
    /// Default value is "cache".
    /// </summary>
    public string CacheFolderName { get; set; } = "cache";

    /// <summary>
    /// In what units do we specify cache expiration.
    /// Default value is Minutes.
    /// </summary>
    public CacheExpirationType CacheExpirationType { get; set; } = CacheExpirationType.Minutes;
    
    /// <summary>
    /// After what time the objects in the cache will expire. Based on CacheExpirationType.
    /// Default value is 60.
    /// </summary>
    public int CacheExpiration { get; set; } = 60;

    /// <summary>
    /// Method of storing the cache.
    /// Memory - only in memory.
    /// MemoryAndFile - In memory while the application is running and in files after the application is closed.
    /// Default value is Memory.
    /// </summary>
    public CacheType CacheType { get; set; } = CacheType.Memory;

    /// <summary>
    /// Initialize cache automatically on startup.
    /// Default value is true.
    /// </summary>
    public bool InitializeOnStartup { get; set; } = true;
    
    /// <summary>
    /// Before exit, flush the cache data to a files if necessary.
    /// Default value is true.
    /// </summary>
    public bool FlushOnExit { get; set; } = true;

    /// <summary>
    /// Salt, which will be added to the key hash.
    /// Default value is an empty string, which means that adding salt is disabled.
    /// </summary>
    public string HashKeySalt { get; set; } = "";
}
```



### Code Examples

- CacheDrive.ExampleConsoleApp
  https://github.com/kubagdynia/CacheDrive/tree/main/CacheDrive.ExampleConsoleApp
- IpGeolocation
https://github.com/kubagdynia/IpGeolocation/blob/main/IpGeolocation/Services/IpGeolocationService.cs




### Technologies
List of technologies, frameworks and libraries used for implementation:
- [.NET 7.0 or .NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (platform)
- [NUnit](https://nunit.org/) (testing framework)

### License
This project is licensed under the [MIT License](https://opensource.org/licenses/MIT).
