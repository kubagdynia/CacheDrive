using System;
using CacheDrive.Configuration;
using CacheDrive.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CacheDrive.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterCacheDrive(this IServiceCollection services, CacheSettings settings = null)
    {
        RegisterCacheDrive(services, null, null, settings);
    }

    public static void RegisterCacheDrive(this IServiceCollection services,
        IConfiguration configuration = null,
        string configurationSectionName = null,
        CacheSettings settings = null)
    {
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
                    opt.CacheFolderName = settings.CacheFolderName;
                    opt.CacheExpiration = settings.CacheExpiration;
                    opt.CacheExpirationType = settings.CacheExpirationType;
                    opt.CacheType = settings.CacheType;
                    opt.InitializeOnStartup = settings.InitializeOnStartup;
                    opt.FlushOnExit = settings.FlushOnExit;
                    opt.HashKeySalt = settings.HashKeySalt;
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
                    opt.CacheFolderName = settings.CacheFolderName;
                    opt.CacheExpiration = settings.CacheExpiration;
                    opt.CacheExpirationType = settings.CacheExpirationType;
                    opt.CacheType = settings.CacheType;
                    opt.InitializeOnStartup = settings.InitializeOnStartup;
                    opt.FlushOnExit = settings.FlushOnExit;
                    opt.HashKeySalt = settings.HashKeySalt;
                });
            }
            else
            {
                // Register default settings
                services.Configure<CacheSettings>(_ => { });
            }
        }
        
        services.AddSingleton<IDateService, DateService>();
        
        services.AddSingleton<ICacheService>(serviceProvider =>
        {
            return serviceProvider.GetService<IOptions<CacheSettings>>().Value.CacheType switch
            {
                CacheType.Memory => new MemoryCacheService(
                    serviceProvider.GetRequiredService<IOptions<CacheSettings>>(),
                    serviceProvider.GetRequiredService<IDateService>()),
                CacheType.MemoryAndFile => new MemoryCacheFileStorageService(
                    serviceProvider.GetRequiredService<IOptions<CacheSettings>>(),
                    serviceProvider.GetRequiredService<IDateService>()),
                _ => throw new ArgumentOutOfRangeException(paramName: serviceProvider.GetService<IOptions<CacheSettings>>().Value.CacheType.ToString())
            };
        });
    }
}