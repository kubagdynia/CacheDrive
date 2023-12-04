namespace CacheDrive.Configuration;

public enum CacheType
{
    /// <summary>
    /// Storing the cache only in memory.
    /// </summary>
    Memory,
    
    /// <summary>
    /// Storing the cache in memory while the application is running
    /// and in files after the application is closed.
    /// </summary>
    MemoryAndFile
}