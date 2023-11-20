using CacheDrive.Configuration;
using CacheDrive.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CacheDrive.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterCacheDrive(this IServiceCollection services, CacheSettings settings = null)
    {
        RegisterIpGeolocation(services, null, null, settings);
    }

    public static void RegisterIpGeolocation(this IServiceCollection services,
        IConfiguration configuration = null,
        string configurationSectionName = null,
        CacheSettings settings = null)
    {
        services.AddSingleton<ICacheService, MemoryCacheFileStorageService>();

        if (configuration is not null)
        {
            if (string.IsNullOrEmpty(configurationSectionName))
            {
                configurationSectionName = "CacheSettings";
            }

            if (settings is not null)
            {
                services.Configure<CacheSettings>(opt =>
                {
                    opt.CacheEnabled = settings.CacheEnabled;
                    opt.CacheExpiration = settings.CacheExpiration;
                    opt.CacheExpirationType = settings.CacheExpirationType;
                    opt.CacheType = settings.CacheType;
                });
            }
            else
            {
                services.Configure<CacheSettings>(configuration.GetSection(configurationSectionName));
            }
        }
        else
        {
            if (settings is not null)
            {
                services.Configure<CacheSettings>(opt =>
                {
                    opt.CacheEnabled = settings.CacheEnabled;
                    opt.CacheExpiration = settings.CacheExpiration;
                    opt.CacheExpirationType = settings.CacheExpirationType;
                    opt.CacheType = settings.CacheType;
                });
            }
            else
            {
                // Register default settings
                services.Configure<CacheSettings>(_ => { });
            }
        }
        
    }
}