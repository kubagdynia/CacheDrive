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
<PackageReference Include="CacheDrive" Version="0.1.1" />
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
    "FlushOnExit": true
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
    FlushOnExit = true
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
        await _cacheService.SetAsync(cacheKey1, "test text...");
        var cachedValue1 = await _cacheService.GetAsync<string>(cacheKey1);
        Console.WriteLine($"GetAsync - cached value: {cachedValue1}");
        
        Console.WriteLine(_cacheService.TryGetValue(cacheKey1, out string cachedValue2)
            ? $"TryGetValue OK - cached value: {cachedValue2}"
            : $"TryGetValue NOK - cached value: {cachedValue2}");

        // Set, Get
        string cacheKey2 = "testKey2";
        _cacheService.Set(cacheKey2, 1234567);
        int cachedValue3 = _cacheService.Get<int>(cacheKey2);
        Console.WriteLine($"Get - cached value: {cachedValue3} ");
    }
}
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
    /// <returns>The value contains the object from the cache with the specified
    /// key or the default value of T, if the operation failed.
    /// </returns>
    Task<T> GetAsync<T>(string key);

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
    /// <param name="expirySeconds">After how many seconds a given value will expire in the cache. Optional parameter.
    /// By default, the value is taken from the configuration.</param>
    Task SetAsync<T>(string key, T value, int expirySeconds = 0);

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
    /// <returns>true if an object was removed successfully; otherwise, false.</returns>
    Task<bool> DeleteAsync<T>(string key);

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


### Technologies
List of technologies, frameworks and libraries used for implementation:
- [.NET 7.0 or .NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (platform)
- [NUnit](https://nunit.org/) (testing framework)

### License
This project is licensed under the [MIT License](https://opensource.org/licenses/MIT).
