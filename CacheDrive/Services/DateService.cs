using System;

namespace CacheDrive.Services;

internal class DateService : IDateService
{
    private DateTime? _utcNow;
    
    public DateTime GetUtcNow()
        => _utcNow ?? DateTime.UtcNow;

    public void SetUtcNow(DateTime dateNow)
        => _utcNow = dateNow;

    public void SetUtcNow()
        => _utcNow = null; 
}