using System.Threading.Tasks;

namespace CacheDrive.Services;

public interface ICacheService
{
    bool HasItem(string key);

    bool TryGetValue<T>(string key, out T value);

    Task<T> GetAsync<T>(string key);

    void Set<T>(T item, int expirySeconds = 0) where T : ICacheable;

    Task SetAsync<T>(string key, T value);
    
    Task SetAsync<T>(T item, int expirySeconds = 0) where T : ICacheable;

    bool Delete(string key);
    
    Task<bool> DeleteAsync(string key);

    void DeletePrefix(string prefix);
    
    Task DeletePrefixAsync(string prefix);
    
    Task FlushAsync();
    
    Task InitializeAsync();
}