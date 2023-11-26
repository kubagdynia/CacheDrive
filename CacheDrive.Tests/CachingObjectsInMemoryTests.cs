using CacheDrive.Configuration;
using CacheDrive.Services;
using CacheDrive.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace CacheDrive.Tests;

public class CachingObjectsInMemoryTests
{
    [Test]
    public async Task ObjectShouldBeCorrectlyCachedAndRestoreFromMemory()
    {
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            DateTime.Now,
            cacheEnabled: true,
            cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2,
            cacheType: CacheType.Memory);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        await cacheService.InitializeAsync();

        TestClass testClass = new TestClass
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Age = 50
        };
        
        await cacheService.SetAsync(testClass);

        var cachedTestClass = await cacheService.GetAsync<TestClass>(testClass.Id.ToString());
        
        await cacheService.FlushAsync();
        cachedTestClass.Should().BeEquivalentTo(testClass);
    }
    
    public class TestClass : ICacheable
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }
        
        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("age")]
        public int Age { get; set; }
        
        [JsonIgnore]
        public string CacheKey
            => GetCacheKey(Id.ToString());
    
        public static string GetCacheKey(string key)
            => $"{nameof(TestClass)}@{key}";
    }
}