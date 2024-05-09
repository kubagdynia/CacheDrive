namespace CacheDrive.Configuration;

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
    /// 
    /// </summary>
    public string HashKeySalt { get; set; } = "";
}