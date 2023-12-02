using System.Threading.Tasks;

namespace CacheDrive.Services;

public interface ICacheService
{
    /// <summary>
    /// Initializes the cache, such as loading data from a file, database, and so on.
    /// Should be run before the cache is used, usually at application startup.
    /// Can be used many times, each time adding or overwriting data if they have the same keys.
    /// </summary>
    /// <returns></returns>
    Task InitializeAsync();
    
    /// <summary>
    /// Dumps cached data into files, database, and so on.
    /// Usually it should be run before the application terminates.
    /// Can be used many times, each time saving data that are new or has been changed.
    /// </summary>
    /// <returns></returns>
    Task FlushAsync();
    
    bool HasItem(string key);

    bool TryGetValue<T>(string key, out T value);

    Task<T> GetAsync<T>(string key);

    void Set<T>(T item, int expirySeconds = 0) where T : ICacheable;

    Task SetAsync<T>(string key, T value, int expirySeconds = 0);
    
    Task SetAsync<T>(T item, int expirySeconds = 0) where T : ICacheable;

    bool Delete(string key);
    
    Task<bool> DeleteAsync(string key);

    void DeletePrefix(string prefix);
    
    Task DeletePrefixAsync(string prefix);

    int CountCacheItems();
}