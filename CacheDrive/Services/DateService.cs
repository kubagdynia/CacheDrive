using System;

namespace CacheDrive.Services;

internal class DateService : IDateService
{
    public DateTime GetUtcNow()
    {
        return DateTime.UtcNow;
    }
}