namespace CacheDrive.Configuration;

public class CacheSettings
{
    public bool CacheEnabled { get; set; } = true;
    
    public int CacheExpiration { get; set; } = 60;

    public string CacheFolderName { get; set; } = "cache";

    public CacheExpirationType CacheExpirationType { get; set; } = CacheExpirationType.Minutes;

    public CacheType CacheType { get; set; } = CacheType.Memory;
}