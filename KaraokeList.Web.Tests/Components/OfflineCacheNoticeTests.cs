using Bunit;
using KaraokeList.Web.Components;

namespace KaraokeList.Web.Tests.Components;

public sealed class OfflineCacheNoticeTests : BunitTestContext
{
    [Fact]
    public void Renders_nothing_when_online()
    {
        var cut = RenderComponent<OfflineCacheNotice>(parameters => parameters
            .Add(p => p.UsingOffline, false)
            .Add(p => p.HasCachedData, true)
            .Add(p => p.CachedAt, DateTime.UtcNow));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Shows_cached_timestamp_when_offline_with_cache()
    {
        var cachedAt = new DateTime(2026, 6, 1, 14, 30, 0, DateTimeKind.Utc);
        var cut = RenderComponent<OfflineCacheNotice>(parameters => parameters
            .Add(p => p.UsingOffline, true)
            .Add(p => p.HasCachedData, true)
            .Add(p => p.CachedAt, cachedAt)
            .Add(p => p.ResourceName, "songs"));

        Assert.Contains("Using cached songs", cut.Markup);
        Assert.Contains(cachedAt.ToLocalTime().ToString("g"), cut.Markup);
    }

    [Fact]
    public void Shows_unavailable_content_when_offline_without_cache()
    {
        var cut = RenderComponent<OfflineCacheNotice>(parameters => parameters
            .Add(p => p.UsingOffline, true)
            .Add(p => p.HasCachedData, false)
            .Add(p => p.UnavailableContent, builder => builder.AddMarkupContent(0, "No cache yet.")));

        Assert.Contains("No cache yet.", cut.Markup);
    }
}
