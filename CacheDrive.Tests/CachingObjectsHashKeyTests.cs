using System.Text.Json.Serialization;
using CacheDrive.Configuration;
using CacheDrive.Services;
using CacheDrive.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace CacheDrive.Tests;

public class CachingObjectsHashKeyTests
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
        // Arrange
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            DateTime.Now,
            cacheEnabled: true,
            cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2,
            cacheType: CacheType.Memory);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        // Act
        await cacheService.SetAsync(_testClass.Id.ToString(), _testClass, hashKey: true);

        var cachedTestClass = await cacheService.GetAsync<TestClass>(_testClass.Id.ToString(), hashKey: true);
        
        await cacheService.FlushAsync();
        
        // Assert
        AssertSameAs(_testClass, cachedTestClass);
    }
    
    [Test, Order(1)]
    public async Task ObjectShoulBeProperlyCachedInMemoryAndPersistedToTheFile()
    {
        // Arrange
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            DateTime.Now,
            cacheEnabled: true,
            cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2,
            cacheType: CacheType.MemoryAndFile);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        // Act
        await cacheService.SetAsync(_testClass.Id.ToString(), _testClass, hashKey: true);

        var cachedTestClass = await cacheService.GetAsync<TestClass>(_testClass.Id.ToString(), hashKey: true);
        
        await cacheService.FlushAsync();
        
        // Assert
        AssertSameAs(_testClass, cachedTestClass);
    }
    
    [Test, Order(2)]
    public async Task CacheShouldBeProperlyLoadedFromTheFile_Then_ObjectShouldBeProperlyRestoredFromTheMemory()
    {
        // Arrange
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            DateTime.Now,
            cacheEnabled: true,
            cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2,
            cacheType: CacheType.MemoryAndFile);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        // Act
        var cachedTestClass = await cacheService.GetAsync<TestClass>(_testClass.Id.ToString(), hashKey: true);
        
        await cacheService.FlushAsync();
        
        // Assert
        AssertSameAs(_testClass, cachedTestClass);
    }
    
    [Test, Order(3)]
    public async Task CacheShouldExpiredDuringLoadedFromTheFile_Then_ObjectShouldNotBeProperlyRestoredFromTheMemory()
    {
        // Arrange
        ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(
            DateTime.Now.AddHours(3),
            cacheEnabled: true,
            cacheExpirationType: CacheExpirationType.Hours,
            cacheExpiration: 2,
            cacheType: CacheType.MemoryAndFile);
        
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();

        // Act
        var cachedTestClass = await cacheService.GetAsync<TestClass>(_testClass.Id.ToString(), hashKey: true);
        
        await cacheService.FlushAsync();
        
        // Assert
        Assert.That(cachedTestClass, Is.Null);
    }

    private static void AssertSameAs(TestClass expected, TestClass actual)
    {
        Assert.That(actual, Is.Not.Null);
        Assert.That(actual.Id, Is.EqualTo(expected.Id));
        Assert.That(actual.FirstName, Is.EqualTo(expected.FirstName));
        Assert.That(actual.LastName, Is.EqualTo(expected.LastName));
        Assert.That(actual.Age, Is.EqualTo(expected.Age));
    }

    private class TestClass
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }
        
        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("age")]
        public int Age { get; set; }
    }
}