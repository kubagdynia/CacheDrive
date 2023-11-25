using CacheDrive.Configuration;
using CacheDrive.Extensions;
using CacheDrive.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CacheDrive.Tests.Helpers;

public static class TestHelper
{
    public static ServiceProvider CreateServiceProvider(DateTime dateNow,
        bool cacheEnabled = true,
        CacheExpirationType cacheExpirationType = CacheExpirationType.Hours,
        int cacheExpiration = 2,
        CacheType cacheType = CacheType.MemoryAndFile)
    {
        ServiceCollection services = new ServiceCollection();
        services.RegisterCacheDrive(new CacheSettings
        {
            CacheEnabled = cacheEnabled,
            CacheExpirationType = cacheExpirationType,
            CacheExpiration = cacheExpiration,
            CacheType = cacheType
        });
        services.AddSingleton<IDateService>( x => new TestDateService(dateNow));
        
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        return serviceProvider;
    }
}