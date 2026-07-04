using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.TestDoubles;

/// <summary>
/// Test double that always returns null for the cache tag (simulates offline / unavailable version endpoint).
/// </summary>
public sealed class NullCatalogVersionService : ICatalogVersionService
{
    public Task<string?> GetCacheTagAsync(bool forceRefresh = false) => Task.FromResult<string?>(null);
    public void Invalidate() { }
}
