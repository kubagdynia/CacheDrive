using CacheDrive.Services;
using CacheDrive.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CacheDrive.Tests;

public class MemoryAndFileTests
{
    [Test, Order(1)]
    public async Task CacheShouldBeSavedCorrectly()
    {
        // Arrange
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(DateTime.Now);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();

        await cacheService.InitializeAsync();
        
        // Act
        string key = "name";

        await cacheService.SetAsync(key, "John");
        
        // Assert
        if (cacheService.TryGetValue(key, out string cachedValue))
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
    
    [Test, Order(2)]
    public async Task CacheShouldBeLoadCorrectly()
    {
        // Arrange
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(DateTime.Now);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        await cacheService.InitializeAsync();
        
        string key = "name";

        // Assert
        if (cacheService.TryGetValue(key, out string cachedValue))
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
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(DateTime.Now.AddHours(3));
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        await cacheService.InitializeAsync();
        
        string key = "name";

        // Assert
        if (!cacheService.TryGetValue(key, out string _))
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