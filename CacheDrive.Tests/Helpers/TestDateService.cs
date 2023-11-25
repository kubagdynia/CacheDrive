using CacheDrive.Services;

namespace CacheDrive.Tests.Helpers;

public class TestDateService(DateTime utcNow) : IDateService
{
    public DateTime GetUtcNow()
    {
        return utcNow;
    }
}