using System;

namespace CacheDrive.Services;

public interface IDateService
{
    /// <summary>
    /// Returns the current utc date or the date that was set using the SetUtcNow method.
    /// </summary>
    DateTime GetUtcNow();

    /// <summary>
    /// Sets the given date as the current UTC date.
    /// From now on, the GetUtcNow method will return this date.
    /// </summary>
    void SetUtcNow(DateTime dateNow);

    /// <summary>
    /// Removes the date set by the SetUtcNow method and restores the current UTC date.
    /// </summary>
    void SetUtcNow();
}