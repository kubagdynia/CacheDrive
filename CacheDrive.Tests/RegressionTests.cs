using CacheDrive.Configuration;
using CacheDrive.Extensions;
using CacheDrive.Services;
using CacheDrive.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CacheDrive.Tests;

public class RegressionTests
{
    private static readonly DateTime FixedNow = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly List<string> _folders = new();

    [TearDown]
    public void CleanUp()
    {
        foreach (string folder in _folders)
        {
            string path = Path.Combine(Environment.CurrentDirectory, folder);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        _folders.Clear();
    }

    private string NewFolderName()
    {
        string name = $"cache-regression-{Guid.NewGuid():N}";
        _folders.Add(name);
        return name;
    }

    private static ServiceProvider CreateFileProvider(string folderName, DateTime now)
    {
        ServiceCollection services = new ServiceCollection();
        services.RegisterCacheDrive(new CacheSettings
        {
            CacheType = CacheType.MemoryAndFile,
            CacheFolderName = folderName,
            CacheExpirationType = CacheExpirationType.Hours,
            CacheExpiration = 2,
            InitializeOnStartup = true,
            FlushOnExit = true
        });
        services.AddSingleton<IDateService>(_ => new TestDateService(now));
        return services.BuildServiceProvider();
    }

    [Test]
    public void ExpiredItemShouldBeRemovedFromStorageOnRead()
    {
        // Arrange
        using ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(FixedNow, cacheType: CacheType.Memory);
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        IDateService dateService = serviceProvider.GetRequiredService<IDateService>();

        cacheService.Set("key", "value");
        cacheService.CountCachedObjects().Should().Be(1);

        // Act
        dateService.SetUtcNow(FixedNow.AddHours(3));
        bool found = cacheService.TryGetValue("key", out string _);

        // Assert
        found.Should().BeFalse();
        cacheService.CountCachedObjects().Should().Be(0);
    }

    [Test]
    public void OverwritingAnItemShouldUseTheDateService()
    {
        // Arrange (cache expiration is 2 hours)
        using ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(FixedNow, cacheType: CacheType.Memory);
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        IDateService dateService = serviceProvider.GetRequiredService<IDateService>();

        cacheService.Set("key", "first");

        // Act: overwrite one hour later, the item should now live until FixedNow + 3h
        dateService.SetUtcNow(FixedNow.AddHours(1));
        cacheService.Set("key", "second");

        dateService.SetUtcNow(FixedNow.AddHours(2).AddMinutes(30));
        bool found = cacheService.TryGetValue("key", out string value);

        // Assert
        found.Should().BeTrue();
        value.Should().Be("second");
    }

    [Test]
    public void HasItemShouldReturnFalseWhenCacheIsDisabled()
    {
        using ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(FixedNow,
            cacheEnabled: false, cacheType: CacheType.Memory);
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();

        cacheService.Set("key", "value");

        cacheService.HasItem("key").Should().BeFalse();
    }

    [Test]
    public void GenericTypesWithDifferentArgumentsShouldNotShareCacheKeys()
    {
        using ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(FixedNow, cacheType: CacheType.Memory);
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();

        cacheService.Set("key", new List<int> { 1, 2 });
        cacheService.Set("key", new List<string> { "a" });

        cacheService.CountCachedObjects().Should().Be(2);
        cacheService.Get<List<int>>("key").Should().BeEquivalentTo(new List<int> { 1, 2 });
        cacheService.Get<List<string>>("key").Should().BeEquivalentTo(new List<string> { "a" });
    }

    [Test]
    public async Task CorruptedCacheFileShouldNotPreventLoadingOtherFiles()
    {
        // Arrange: write a valid item to disk, then surround it with broken files
        string folder = NewFolderName();

        using (ServiceProvider writer = CreateFileProvider(folder, FixedNow))
        {
            ICacheService writerCache = writer.GetRequiredService<ICacheService>();
            await writerCache.SetAsync("good", "value");
            await writerCache.FlushAsync();
        }

        string directory = Path.Combine(Environment.CurrentDirectory, folder);
        await File.WriteAllTextAsync(Path.Combine(directory, "0-corrupt.json"), "{ this is not json");
        await File.WriteAllTextAsync(Path.Combine(directory, "zz-empty.json"), string.Empty);

        // Act: the constructor loads the cache synchronously, InitializeAsync loads it again asynchronously
        using ServiceProvider reader = CreateFileProvider(folder, FixedNow);
        ICacheService readerCache = reader.GetRequiredService<ICacheService>();
        await readerCache.InitializeAsync();

        // Assert
        readerCache.TryGetValue("good", out string value).Should().BeTrue();
        value.Should().Be("value");
    }

    [Test]
    public async Task KeysThatDifferOnlyByInvalidFileNameCharactersShouldNotCollide()
    {
        // Arrange
        string folder = NewFolderName();

        using (ServiceProvider writer = CreateFileProvider(folder, FixedNow))
        {
            ICacheService writerCache = writer.GetRequiredService<ICacheService>();
            await writerCache.SetAsync("a/b", "with-slash");
            await writerCache.SetAsync("ab", "without-slash");
            await writerCache.FlushAsync();
        }

        // Act
        using ServiceProvider reader = CreateFileProvider(folder, FixedNow);
        ICacheService readerCache = reader.GetRequiredService<ICacheService>();

        // Assert
        readerCache.CountCachedObjects().Should().Be(2);
        readerCache.Get<string>("a/b").Should().Be("with-slash");
        readerCache.Get<string>("ab").Should().Be("without-slash");
    }

    [Test]
    public void DisposingTheProviderShouldFlushTheCacheToFiles()
    {
        // Arrange
        string folder = NewFolderName();

        ServiceProvider writer = CreateFileProvider(folder, FixedNow);
        writer.GetRequiredService<ICacheService>().Set("key", "value");

        // Act: FlushOnExit = true, so disposing must write the file synchronously and completely
        writer.Dispose();

        // Assert
        using ServiceProvider reader = CreateFileProvider(folder, FixedNow);
        ICacheService readerCache = reader.GetRequiredService<ICacheService>();

        readerCache.TryGetValue("key", out string value).Should().BeTrue();
        value.Should().Be("value");
    }
}
