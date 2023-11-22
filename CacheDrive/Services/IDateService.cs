using System;

namespace CacheDrive.Services;

public interface IDateService
{
    DateTime GetUtcNow();
}