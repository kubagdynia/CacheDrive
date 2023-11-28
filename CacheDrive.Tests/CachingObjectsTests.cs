using CacheDrive.Configuration;
using CacheDrive.Services;
using CacheDrive.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace CacheDrive.Tests;

public class CachingObjectsTests
{
    private TestClass _testClass;
    
    [SetUp]
    public void SetUp()
    {
        _testClass = new TestClass
        {
            Id = Guid.Parse("5b8e8e5c-6ad5-43c0-94d8-ef68e96eaef8"),
            FirstName = "John",
            LastName = "Doe",
            Age = 50
        };
    }
    
    [Test]
    public async Task ObjectShouldBeProperlyCachedAndRestoredFromTheMemory()
    {
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            DateTime.Now,
            cacheEnabled: true,
            cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2,
            cacheType: CacheType.Memory);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        await cacheService.InitializeAsync();
        
        await cacheService.SetAsync(_testClass.Id.ToString(), _testClass);

        var cachedTestClass = await cacheService.GetAsync<TestClass>(_testClass.Id.ToString());
        
        await cacheService.FlushAsync();
        cachedTestClass.Should().BeEquivalentTo(_testClass);
    }
    
    [Test, Order(1)]
    public async Task ObjectShoulBeProperlyCachedInMemoryAndPersistedToTheFile()
    {
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            DateTime.Now,
            cacheEnabled: true,
            cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2,
            cacheType: CacheType.MemoryAndFile);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        await cacheService.InitializeAsync();
        
        await cacheService.SetAsync(_testClass.Id.ToString(), _testClass);

        var cachedTestClass = await cacheService.GetAsync<TestClass>(_testClass.Id.ToString());
        
        await cacheService.FlushAsync();
        cachedTestClass.Should().BeEquivalentTo(_testClass);
    }
    
    [Test, Order(2)]
    public async Task CacheShouldBeProperlyLoadedFromTheFile_Then_ObjectShouldBeProperlyRestoredFromTheMemory()
    {
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            DateTime.Now,
            cacheEnabled: true,
            cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2,
            cacheType: CacheType.MemoryAndFile);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        await cacheService.InitializeAsync();

        var cachedTestClass = await cacheService.GetAsync<TestClass>(_testClass.Id.ToString());
        
        await cacheService.FlushAsync();
        cachedTestClass.Should().BeEquivalentTo(_testClass);
    }
    
    [Test, Order(3)]
    public async Task CacheShouldExpiredDuringLoadedFromTheFile_Then_ObjectShouldNotBeProperlyRestoredFromTheMemory()
    {
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            DateTime.Now.AddHours(3),
            cacheEnabled: true,
            cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2,
            cacheType: CacheType.MemoryAndFile);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        await cacheService.InitializeAsync();

        var cachedTestClass = await cacheService.GetAsync<TestClass>(_testClass.Id.ToString());
        
        await cacheService.FlushAsync();
        cachedTestClass.Should().BeNull();
    }

    private class TestClass : ICacheable
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