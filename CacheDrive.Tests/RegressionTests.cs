using CacheDrive.Configuration;
using CacheDrive.Extensions;
using CacheDrive.Services;
using CacheDrive.Tests.Helpers;
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

    private static string[] JsonFiles(string folderName)
        => Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, folderName), "*.json");

    private static ServiceProvider CreateFileProvider(string folderName, DateTime now, bool initializeOnStartup = true)
    {
        ServiceCollection services = new ServiceCollection();
        services.RegisterCacheDrive(new CacheSettings
        {
            CacheType = CacheType.MemoryAndFile,
            CacheFolderName = folderName,
            CacheExpirationType = CacheExpirationType.Hours,
            CacheExpiration = 2,
            InitializeOnStartup = initializeOnStartup,
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
        Assert.That(cacheService.CountCachedObjects(), Is.EqualTo(1));

        // Act
        dateService.SetUtcNow(FixedNow.AddHours(3));
        bool found = cacheService.TryGetValue("key", out string _);

        // Assert
        Assert.That(found, Is.False);
        Assert.That(cacheService.CountCachedObjects(), Is.EqualTo(0));
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
        Assert.That(found, Is.True);
        Assert.That(value, Is.EqualTo("second"));
    }

    [Test]
    public void HasItemShouldReturnFalseWhenCacheIsDisabled()
    {
        using ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(FixedNow,
            cacheEnabled: false, cacheType: CacheType.Memory);
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();

        cacheService.Set("key", "value");

        Assert.That(cacheService.HasItem("key"), Is.False);
    }

    [Test]
    public void GenericTypesWithDifferentArgumentsShouldNotShareCacheKeys()
    {
        using ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(FixedNow, cacheType: CacheType.Memory);
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();

        cacheService.Set("key", new List<int> { 1, 2 });
        cacheService.Set("key", new List<string> { "a" });

        Assert.That(cacheService.CountCachedObjects(), Is.EqualTo(2));
        Assert.That(cacheService.Get<List<int>>("key"), Is.EqualTo(new List<int> { 1, 2 }));
        Assert.That(cacheService.Get<List<string>>("key"), Is.EqualTo(new List<string> { "a" }));
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
        Assert.That(readerCache.TryGetValue("good", out string value), Is.True);
        Assert.That(value, Is.EqualTo("value"));
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
        Assert.That(readerCache.CountCachedObjects(), Is.EqualTo(2));
        Assert.That(readerCache.Get<string>("a/b"), Is.EqualTo("with-slash"));
        Assert.That(readerCache.Get<string>("ab"), Is.EqualTo("without-slash"));
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

        Assert.That(readerCache.TryGetValue("key", out string value), Is.True);
        Assert.That(value, Is.EqualTo("value"));
    }

    [Test]
    public void HasItemShouldFindItemsStoredWithSet()
    {
        using ServiceProvider serviceProvider = TestHelper.CreateServiceProvider(FixedNow, cacheType: CacheType.Memory);
        ICacheService cacheService = serviceProvider.GetRequiredService<ICacheService>();
        IDateService dateService = serviceProvider.GetRequiredService<IDateService>();

        cacheService.Set("key", "value");
        cacheService.Set("hashed", "value", hashKey: true);

        Assert.That(cacheService.HasItem("key"), Is.True);
        Assert.That(cacheService.HasItem<string>("key"), Is.True);
        Assert.That(cacheService.HasItem<int>("key"), Is.False);
        Assert.That(cacheService.HasItem("missing"), Is.False);

        Assert.That(cacheService.HasItem("hashed", hashKey: true), Is.True);
        Assert.That(cacheService.HasItem<string>("hashed", hashKey: true), Is.True);
        Assert.That(cacheService.HasItem<string>("hashed"), Is.False);

        // Both overloads ignore expired items (cache expiration is 2 hours)
        dateService.SetUtcNow(FixedNow.AddHours(3));

        Assert.That(cacheService.HasItem("key"), Is.False);
        Assert.That(cacheService.HasItem<string>("key"), Is.False);
    }

    [Test]
    public async Task DeletedItemsShouldNotComeBackAfterRestart()
    {
        string folder = NewFolderName();

        using ServiceProvider writer = CreateFileProvider(folder, FixedNow);
        ICacheService writerCache = writer.GetRequiredService<ICacheService>();

        await writerCache.SetAsync("a", "1");
        await writerCache.SetAsync("b", "2");
        await writerCache.FlushAsync();
        Assert.That(JsonFiles(folder), Has.Length.EqualTo(2));

        Assert.That(writerCache.Delete<string>("a"), Is.True);
        Assert.That(JsonFiles(folder), Has.Length.EqualTo(1));

        using ServiceProvider reader = CreateFileProvider(folder, FixedNow);
        ICacheService readerCache = reader.GetRequiredService<ICacheService>();

        Assert.That(readerCache.CountCachedObjects(), Is.EqualTo(1));
        Assert.That(readerCache.HasItem<string>("b"), Is.True);
        Assert.That(readerCache.HasItem<string>("a"), Is.False);
    }

    [Test]
    public async Task DeleteShouldRemoveTheFileOfAnItemThatWasNotLoaded()
    {
        string folder = NewFolderName();

        using (ServiceProvider writer = CreateFileProvider(folder, FixedNow))
        {
            ICacheService writerCache = writer.GetRequiredService<ICacheService>();
            await writerCache.SetAsync("a", "1");
            await writerCache.FlushAsync();
        }

        Assert.That(JsonFiles(folder), Has.Length.EqualTo(1));

        // InitializeOnStartup = false: the item is on disk but not in memory
        using ServiceProvider reader = CreateFileProvider(folder, FixedNow, initializeOnStartup: false);
        ICacheService readerCache = reader.GetRequiredService<ICacheService>();
        Assert.That(readerCache.CountCachedObjects(), Is.EqualTo(0));

        readerCache.Delete<string>("a");

        Assert.That(JsonFiles(folder), Is.Empty);
    }

    [Test]
    public async Task ClearCacheShouldAlsoRemoveTheCacheFiles()
    {
        string folder = NewFolderName();

        using ServiceProvider provider = CreateFileProvider(folder, FixedNow);
        ICacheService cacheService = provider.GetRequiredService<ICacheService>();

        await cacheService.SetAsync("a", "1");
        await cacheService.SetAsync("b", "2");
        await cacheService.FlushAsync();
        Assert.That(JsonFiles(folder), Has.Length.EqualTo(2));

        cacheService.ClearCache();

        Assert.That(cacheService.CountCachedObjects(), Is.EqualTo(0));
        Assert.That(JsonFiles(folder), Is.Empty);
    }

    [Test]
    public async Task ReadingAnExpiredItemShouldAlsoRemoveItsFile()
    {
        string folder = NewFolderName();

        using ServiceProvider provider = CreateFileProvider(folder, FixedNow);
        ICacheService cacheService = provider.GetRequiredService<ICacheService>();
        IDateService dateService = provider.GetRequiredService<IDateService>();

        await cacheService.SetAsync("a", "1");
        await cacheService.FlushAsync();
        Assert.That(JsonFiles(folder), Has.Length.EqualTo(1));

        dateService.SetUtcNow(FixedNow.AddHours(3));
        Assert.That(cacheService.TryGetValue("a", out string _), Is.False);

        Assert.That(JsonFiles(folder), Is.Empty);
    }
}
