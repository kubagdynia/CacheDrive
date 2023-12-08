using CacheDrive.Configuration;
using CacheDrive.Services;
using CacheDrive.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CacheDrive.Tests;

public class CachingSimpleTypesTests
{
    [Test]
    public async Task SimpleTypesShouldBeProperlyCachedAndRestoredFromTheMemoryAsync()
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
        (string key, int value) intItem = ("int_test_async", 10);
        await cacheService.SetAsync(intItem.key, intItem.value);
        
        (string key, char value) charItem = ("char_test_async", 'p');
        await cacheService.SetAsync(charItem.key, charItem.value);
        
        (string key, float value) floatItem = ("float_test_async", 4.8f);
        await cacheService.SetAsync(floatItem.key, floatItem.value);
        
        (string key, double value) doubleItem = ("double_test_async", 6.8d);
        await cacheService.SetAsync(doubleItem.key, doubleItem.value);
        
        (string key, bool value) boolItem = ("bool_test_async", true);
        await cacheService.SetAsync(boolItem.key, boolItem.value);
        
        int cachedIntItem = await cacheService.GetAsync<int>(intItem.key);
        char cachedCharItem = await cacheService.GetAsync<char>(charItem.key);
        float cachedFloatItem = await cacheService.GetAsync<float>(floatItem.key);
        double cachedDoubleItem = await cacheService.GetAsync<double>(doubleItem.key);
        bool cachedBoolItem = await cacheService.GetAsync<bool>(boolItem.key);
        
        // Assert
        cachedIntItem.Should().Be(intItem.value);
        cachedCharItem.Should().Be(charItem.value);
        cachedFloatItem.Should().Be(floatItem.value);
        cachedDoubleItem.Should().Be(doubleItem.value);
        cachedBoolItem.Should().Be(boolItem.value);
    }
    
    [Test]
    public void SimpleTypesShouldBeProperlyCachedAndRestoredFromTheMemory()
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
        (string key, int value) intItem = ("int_test", 10);
        cacheService.Set(intItem.key, intItem.value);
        
        (string key, char value) charItem = ("char_test", 'p');
        cacheService.Set(charItem.key, charItem.value);
        
        (string key, float value) floatItem = ("float_test", 4.8f);
        cacheService.Set(floatItem.key, floatItem.value);
        
        (string key, double value) doubleItem = ("double_test", 6.8d);
        cacheService.Set(doubleItem.key, doubleItem.value);
        
        (string key, bool value) boolItem = ("bool_test", true);
        cacheService.Set(boolItem.key, boolItem.value);
        
        int cachedIntItem = cacheService.Get<int>(intItem.key);
        char cachedCharItem = cacheService.Get<char>(charItem.key);
        float cachedFloatItem = cacheService.Get<float>(floatItem.key);
        double cachedDoubleItem = cacheService.Get<double>(doubleItem.key);
        bool cachedBoolItem = cacheService.Get<bool>(boolItem.key);
        
        // Assert
        cachedIntItem.Should().Be(intItem.value);
        cachedCharItem.Should().Be(charItem.value);
        cachedFloatItem.Should().Be(floatItem.value);
        cachedDoubleItem.Should().Be(doubleItem.value);
        cachedBoolItem.Should().Be(boolItem.value);
    }

    [Test]
    public async Task SimpleTypesShouldBeProperlyDeletedFromTheMemoryCacheAsync()
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
        
        // Add 50 items
        for (int i = 0; i < 50; i++)
        {
            await cacheService.SetAsync($"test_{i}_async", i);
        }

        // Delete every second element
        for (int i = 0; i < 50; i++)
        {
            if ((i + 1) % 2 == 0)
            {
                await cacheService.DeleteAsync<int>($"test_{i}_async");
            }
        }

        // // Assert
        int countCachedItems = cacheService.CountCachedObjects();
        countCachedItems.Should().Be(25);
    }
    
    [Test]
    public void SimpleTypesShouldBeProperlyDeletedFromTheMemoryCache()
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
        
        // Add 50 items
        for (int i = 0; i < 50; i++)
        {
            cacheService.Set($"test_{i}", i);
        }

        // Delete every second element
        for (int i = 0; i < 50; i++)
        {
            if ((i + 1) % 2 == 0)
            {
                cacheService.Delete<int>($"test_{i}");
            }
        }

        // // Assert
        int countCachedItems = cacheService.CountCachedObjects();
        countCachedItems.Should().Be(25);
    }
}