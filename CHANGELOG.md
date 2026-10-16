# Changelog

## 0.4.0

### Changed
- Target frameworks are now `net8.0` and `net10.0` (.NET 7 is no longer supported by Microsoft, .NET 9 reaches end of support together with .NET 8).
- Generic types now include their type arguments in the cache key (`List<int>` and `List<string>` no longer share a key). Cache entries of generic types written by earlier versions are not found any more and simply expire.
- Cache file names are now built by percent-encoding characters that are not allowed in file names instead of dropping them, so different keys never end up in the same file.

### Upgrade notes
- Files written by earlier versions for keys containing characters that are invalid in file names (for example `a/b`) stay on disk under their old name; they are still loaded, but new writes use the new percent-encoded name, so you may want to clear the cache folder once.
- The finalizer was removed. Dispose the service provider (or the cache service) at shutdown, as shown in the README, to flush the cache to files; an application that never disposes it will not flush on exit.

### Fixed
- Expired items read through `TryGetValue` are now removed from the cache (the type prefix was applied twice).
- Overwriting an existing key now uses `IDateService` instead of `DateTime.UtcNow`.
- A corrupted or empty cache file no longer stops loading of the remaining files.
- Cache files are written to a temporary file first and then moved into place; the synchronous flush on dispose no longer fires an un-awaited write.
- Flushing no longer happens from a finalizer; `Dispose` and the new `DisposeAsync` flush when `FlushOnExit` is enabled.
- `HasItem` returns `false` when the cache is disabled.
- The cache directory is resolved once at startup and no longer follows later changes of the current directory.
