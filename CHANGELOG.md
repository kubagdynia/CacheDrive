# Changelog

## 0.5.0

### Added
- `HasItem<T>(key)` and `HasItem<T>(key, hashKey)`: check whether an unexpired item of type `T` exists. These are new members of `ICacheService`, so custom implementations or mocks of the interface need to add them.

### Fixed
- `HasItem(key)` and `HasItem(key, hashKey)` compared the key with the internal storage key (which includes the `type@` prefix), so they never found items stored with `Set`. They now find an unexpired item with that key regardless of its type.
- With `CacheType.MemoryAndFile`, items removed with `Delete`/`DeleteAsync`, items removed because they expired (on read or in `ClearExpiredObjects`) and items removed by `ClearCache` now also have their cache files removed. Previously they were loaded back from disk on the next start. `Delete` removes the file even if the item was not loaded into memory (for example with `InitializeOnStartup = false`).

### Notes
- `ClearCache` removes the files of the items that are in memory. Files that were never loaded stay on disk, and the same goes for other files in the cache folder.
- `HasItem(key)` without a type now scans the cache (O(n)); prefer `HasItem<T>(key)`, which is a direct lookup.
- The cache is not synchronized with file operations: a `FlushAsync` running at the same moment as a `Delete` can write the file of the item that is being deleted.
- If a cache file cannot be deleted (locked, no permission), the `IOException` or `UnauthorizedAccessException` is thrown instead of silently bringing the item back later.

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
