using CacheDrive.Configuration;
using CacheDrive.Services;
using CacheDrive.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CacheDrive.Tests;

public class MemoryAndFileHashKeyWithSaltTests
{
    [Test, Order(1)]
    public async Task CacheShouldBeSavedCorrectly()
    {
        // Arrange
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            dateNow: DateTime.Now, cacheEnabled: true, cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2, cacheType: CacheType.MemoryAndFile, hashSalt: "Secret123");
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        // Act
        string key = "name";

        await cacheService.SetAsync(key, "John", hashKey: true);
        
        // Assert
        if (cacheService.TryGetValue(key, hashKey: true, out string cachedValue))
        {
            cachedValue.Should().Be("John");
            await cacheService.FlushAsync();
        }
        else
        {
            await cacheService.FlushAsync();
            Assert.Fail();
        }
        
        await cacheService.FlushAsync();
    }
    
    [Test, Order(2)]
    public async Task CacheShouldBeLoadCorrectly()
    {
        // Arrange
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            dateNow: DateTime.Now, cacheEnabled: true, cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2, cacheType: CacheType.MemoryAndFile, hashSalt: "Secret123");
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        string key = "name";

        // Assert
        if (cacheService.TryGetValue(key, hashKey: true, out string cachedValue))
        {
            cachedValue.Should().Be("John");
            await cacheService.FlushAsync();
        }
        else
        {
            await cacheService.FlushAsync();
            Assert.Fail();
        }
    }
    
    [Test, Order(3)]
    public async Task CacheShouldExpired()
    {
        // Arrange
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            dateNow: DateTime.Now.AddHours(3), cacheEnabled: true, cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2, cacheType: CacheType.MemoryAndFile, hashSalt: "Secret123");
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        string key = "name";

        // Assert
        if (!cacheService.TryGetValue(key, hashKey: true, out string _))
        {
            await cacheService.FlushAsync();
            Assert.Pass();
        }
        else
        {
            await cacheService.FlushAsync();
            Assert.Fail();
        }
    }
}