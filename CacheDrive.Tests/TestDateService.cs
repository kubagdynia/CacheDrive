using CacheDrive.Services;

namespace CacheDrive.Tests;

public class TestDateService(DateTime utcNow) : IDateService
{
    public DateTime GetUtcNow()
    {
        return utcNow;
    }
}