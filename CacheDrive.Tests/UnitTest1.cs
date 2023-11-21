using CacheDrive.Configuration;
using CacheDrive.Extensions;
using CacheDrive.Models;
using CacheDrive.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CacheDrive.Tests;

public class Tests
{
    private ServiceCollection _services;
    private ServiceProvider _serviceProvider;
    
    [OneTimeSetUp]
    public void Init()
    {
        _services = new ServiceCollection();
        _services.RegisterCacheDrive(new CacheSettings
        {
            CacheEnabled = true,
            CacheExpirationType = CacheExpirationType.Hours,
            CacheExpiration = 2,
            CacheType = CacheType.MemoryAndFile
        });
        
        _serviceProvider = _services.BuildServiceProvider();
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        _serviceProvider.Dispose();
    }

    [Test, Order(1)]
    public async Task CacheShouldBeSavedCorrectly()
    {
        ICacheService cacheService = _serviceProvider.GetRequiredService<ICacheService>();

        await cacheService.InitializeAsync();
        
        string cacheKey = "name";

        await cacheService.SetAsync(SpecificField.Create(cacheKey, "John"));

        if (cacheService.TryGetValue(SpecificField.GetCacheKey(cacheKey), out SpecificField cachedSpecificField))
        {
             cachedSpecificField.Value.Should().Be("John");
        }
        else
        {
            Assert.Fail();
        }
        
        await cacheService.FlushAsync();
    }
    
    [Test, Order(2)]
    public async Task CacheShouldBeloadCorrectly()
    {
        ICacheService cacheService = _serviceProvider.GetRequiredService<ICacheService>();
        await cacheService.InitializeAsync();
        
        string cacheKey = "name";

        if (cacheService.TryGetValue(SpecificField.GetCacheKey(cacheKey), out SpecificField cachedSpecificField))
        {
            cachedSpecificField.Value.Should().Be("John");
        }
        else
        {
            Assert.Fail();
        }
        
        await cacheService.FlushAsync();
    }
}