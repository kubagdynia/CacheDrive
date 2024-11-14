using CacheDrive.Services;

namespace CacheDrive.Tests.Helpers;

public class TestDateService : IDateService
{
    private DateTime? _utcNow;

    public TestDateService(DateTime utcNow)
    {
        _utcNow = utcNow;
    }

    public DateTime GetUtcNow()
        => _utcNow ?? DateTime.UtcNow;

    public void SetUtcNow(DateTime dateNow)
        => _utcNow = dateNow;

    public void SetUtcNow()
        => _utcNow = null;
}