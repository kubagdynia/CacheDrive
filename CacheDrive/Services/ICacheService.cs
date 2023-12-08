using System.Threading.Tasks;

namespace CacheDrive.Services;

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