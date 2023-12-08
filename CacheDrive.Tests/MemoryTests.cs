using CacheDrive.Configuration;
using CacheDrive.Services;
using CacheDrive.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CacheDrive.Tests;

public class MemoryTests
{
    [TestCase("key1", "one")]
    [TestCase("key2","Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.")]
    public async Task StringShouldBeCorrectlySavedAndReadFromTheCacheInMemory(string key, string text)
    {
        // Arrange
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            DateTime.Now,
            cacheEnabled: true,
            cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2,
            cacheType: CacheType.Memory);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        // Act
        await cacheService.SetAsync(key, text);
        
        string resultValue =  await cacheService.GetAsync<string>(key);
        
        // Assert
        resultValue.Should().Be(text);
    }

    [Test]
    public void ClearingTheCacheShouldRemoveAllObjectsFromTheCache()
    {
        // Arrange
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            DateTime.Now,
            cacheEnabled: true,
            cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2,
            cacheType: CacheType.Memory);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        // Act
        for (int i = 0; i < 100; i++)
        {
            cacheService.Set($"k-{i}", i);
        }
        
        // Assert
        cacheService.CountCachedObjects().Should().Be(100);
        cacheService.ClearCache();
        cacheService.CountCachedObjects().Should().Be(0);
    }
    
    [Test]
    public void ClearingTheCacheShouldRemoveAllObjectsFromTheCache2()
    {
        // Arrange
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            DateTime.Now,
            cacheEnabled: true,
            cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2,
            cacheType: CacheType.Memory);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        IDateService dateService = serviceProvider.GetRequiredService<IDateService>();
        
        // Act
        for (int i = 0; i < 50; i++)
        {
            cacheService.Set($"h0-{i}", i);
        }
        
        dateService.SetUtcNow(dateService.GetUtcNow().AddHours(1));
        
        for (int i = 0; i < 50; i++)
        {
            cacheService.Set($"h1-{i}", i);
        }
        
        dateService.SetUtcNow(dateService.GetUtcNow().AddHours(2));
        
        // Assert
        cacheService.CountCachedObjects().Should().Be(100);
        
        cacheService.ClearExpiredObjects();
        
        cacheService.CountCachedObjects().Should().Be(50);
    }
}