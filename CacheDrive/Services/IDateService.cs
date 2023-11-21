using System;

namespace CacheDrive.Services;

internal interface IDateService
{
    DateTime GetUtcNow();
}