using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;

namespace KaraokeList.Web.Tests.Services;

public sealed class MyPerformancesLoaderTests
{
    [Fact]
    public async Task LoadAsync_when_online_saves_performances_to_cache()
    {
        var store = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        var api = new PerformancesApiStub();
        var loader = new MyPerformancesLoader(api, store);

        var result = await loader.LoadAsync();

        Assert.False(result.FromCache);
        Assert.True(result.HasCache);
        Assert.Equal(2, result.Performances.Count);
        Assert.Equal("Newer Song", result.Performances[0].Title);

        var cached = await store.GetCachedAsync();
        Assert.NotNull(cached);
        Assert.Equal(2, cached.Performances.Count);
    }

    [Fact]
    public async Task LoadAsync_when_offline_returns_cached_performances()
    {
        var store = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedAsync(new CachedMyPerformances(
            [new MyPerformanceEntryDto { Id = 5, SongId = 9, Title = "Cached Song", PerformedOn = DateTime.Today }],
            DateTime.UtcNow.AddHours(-2)));

        var loader = new MyPerformancesLoader(new PerformancesApiStub { ThrowOffline = true }, store);
        var result = await loader.LoadAsync();

        Assert.True(result.FromCache);
        Assert.True(result.HasCache);
        Assert.Single(result.Performances);
        Assert.Equal("Cached Song", result.Performances[0].Title);
    }

    [Fact]
    public async Task LoadAsync_when_offline_without_cache_returns_error()
    {
        var loader = new MyPerformancesLoader(
            new PerformancesApiStub { ThrowOffline = true },
            new MyPerformancesLocalStore(new InMemoryLocalStorage()));

        var result = await loader.LoadAsync();

        Assert.True(result.FromCache);
        Assert.False(result.HasCache);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task TryGetCachedAsync_returns_cached_performances()
    {
        var store = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedAsync(new CachedMyPerformances(
            [new MyPerformanceEntryDto { Id = 3, SongId = 7, Title = "Stored Song", PerformedOn = DateTime.Today }],
            DateTime.UtcNow));

        var loader = new MyPerformancesLoader(new PerformancesApiStub(), store);
        var result = await loader.TryGetCachedAsync();

        Assert.NotNull(result);
        Assert.True(result.FromCache);
        Assert.Single(result.Performances);
        Assert.Equal("Stored Song", result.Performances[0].Title);
    }

    private sealed class PerformancesApiStub : NotImplementedApiClient
    {
        public bool ThrowOffline { get; init; }

        public override Task<MyPerformancesResult> GetMyPerformancesAsync(int? venueId = null, string sortDir = "desc")
        {
            if (ThrowOffline)
            {
                throw new HttpRequestException("offline");
            }

            return Task.FromResult(MyPerformancesResult.Ok(
            [
                new MyPerformanceEntryDto
                {
                    Id = 2,
                    SongId = 20,
                    Title = "Newer Song",
                    PerformedOn = new DateTime(2024, 6, 1)
                },
                new MyPerformanceEntryDto
                {
                    Id = 1,
                    SongId = 10,
                    Title = "Older Song",
                    PerformedOn = new DateTime(2024, 1, 1)
                }
            ]));
        }
    }
}
