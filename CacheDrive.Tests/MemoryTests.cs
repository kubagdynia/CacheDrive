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
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(DateTime.Now, true, CacheExpirationType.Hours, 2, CacheType.Memory);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        await cacheService.InitializeAsync();
        
        // Act
        await cacheService.SetAsync(key, text);
        
        string resultValue =  await cacheService.GetAsync<string>(key);
        
        await cacheService.FlushAsync();
        
        // Assert
        resultValue.Should().Be(text);
    }
}