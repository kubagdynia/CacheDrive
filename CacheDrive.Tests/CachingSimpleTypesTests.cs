using CacheDrive.Configuration;
using CacheDrive.Services;
using CacheDrive.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CacheDrive.Tests;

public class CachingSimpleTypesTests
{
    [Test]
    public async Task SimpleTypesShouldBeProperlyCachedAndRestoredFromTheMemory()
    {
        // Arrange
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            DateTime.Now,
            cacheEnabled: true,
            cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2,
            cacheType: CacheType.Memory);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        await cacheService.InitializeAsync();
        
        // Act
        (string key, int value) intItem = ("int_test", 10);
        await cacheService.SetAsync(intItem.key, intItem.value);
        
        (string key, char value) charItem = ("char_test", 'p');
        await cacheService.SetAsync(charItem.key, charItem.value);
        
        (string key, float value) floatItem = ("float_test", 4.8f);
        await cacheService.SetAsync(floatItem.key, floatItem.value);
        
        (string key, double value) doubleItem = ("double_test", 6.8d);
        await cacheService.SetAsync(doubleItem.key, doubleItem.value);
        
        (string key, bool value) boolItem = ("bool_test", true);
        await cacheService.SetAsync(boolItem.key, boolItem.value);
        
        int cachedIntItem = await cacheService.GetAsync<int>(intItem.key);
        char cachedCharItem = await cacheService.GetAsync<char>(charItem.key);
        float cachedFloatItem = await cacheService.GetAsync<float>(floatItem.key);
        double cachedDoubleItem = await cacheService.GetAsync<double>(doubleItem.key);
        bool cachedBoolItem = await cacheService.GetAsync<bool>(boolItem.key);
        
        await cacheService.FlushAsync();
        
        // Assert
        cachedIntItem.Should().Be(intItem.value);
        cachedCharItem.Should().Be(charItem.value);
        cachedFloatItem.Should().Be(floatItem.value);
        cachedDoubleItem.Should().Be(doubleItem.value);
        cachedBoolItem.Should().Be(boolItem.value);
    }
}