using KaraokeList.Shared;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using Moq;

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
    public async Task TryGetCachedAsync_returns_empty_snapshot_when_cache_is_empty()
    {
        var store = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedAsync(new CachedMyPerformances([], DateTime.UtcNow));

        var loader = new MyPerformancesLoader(new PerformancesApiStub(), store);
        var result = await loader.TryGetCachedAsync();

        Assert.NotNull(result);
        Assert.Empty(result!.Performances);
        Assert.True(result.FromCache);
    }

    [Fact]
    public async Task LoadAsync_returns_empty_stored_cache_when_invalidated_after_delete()
    {
        var store = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedAsync(new CachedMyPerformances(
        [
            new MyPerformanceEntryDto
            {
                Id = 3,
                SongId = 7,
                Title = "Stored Song",
                VenueName = "Old Venue",
                PerformedOn = DateTime.Today
            }
        ],
            DateTime.UtcNow));

        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetMyPerformancesAsync(null, "desc"))
            .ReturnsAsync(MyPerformancesResult.Ok(
            [
                new MyPerformanceEntryDto
                {
                    Id = 3,
                    SongId = 7,
                    Title = "Stored Song",
                    VenueName = "Old Venue",
                    PerformedOn = DateTime.Today
                }
            ]));

        var loader = new MyPerformancesLoader(api.Object, store);
        await loader.RemovePerformanceAsync(3);

        var apiResultStarted = new TaskCompletionSource<MyPerformancesResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        api.Setup(client => client.GetMyPerformancesAsync(null, "desc"))
            .Returns(apiResultStarted.Task);

        var loadTask = loader.LoadAsync();
        loader.InvalidateInFlightLoads();
        apiResultStarted.SetResult(MyPerformancesResult.Ok(
        [
            new MyPerformanceEntryDto
            {
                Id = 3,
                SongId = 7,
                Title = "Stored Song",
                VenueName = "Resurrected From Api",
                PerformedOn = DateTime.Today
            }
        ]));

        var result = await loadTask;

        Assert.Empty(result.Performances);
        Assert.True(result.FromCache);
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

    [Fact]
    public async Task PatchPerformanceAsync_updates_matching_entry_in_cache()
    {
        var store = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedAsync(new CachedMyPerformances(
        [
            new MyPerformanceEntryDto
            {
                Id = 3,
                SongId = 7,
                Title = "Stored Song",
                VenueName = "Old Venue",
                PerformedOn = DateTime.Today
            }
        ],
            DateTime.UtcNow));

        var loader = new MyPerformancesLoader(new PerformancesApiStub(), store);
        await loader.PatchPerformanceAsync(new MyPerformanceEntryDto
        {
            Id = 3,
            SongId = 7,
            Title = "Stored Song",
            VenueName = "New Venue",
            PerformedOn = DateTime.Today
        });

        var cached = await loader.TryGetCachedAsync();
        Assert.NotNull(cached);
        Assert.Equal("New Venue", cached.Performances[0].VenueName);
    }

    [Fact]
    public async Task LoadAsync_does_not_overwrite_cache_when_invalidated_before_persist()
    {
        var store = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedAsync(new CachedMyPerformances(
        [
            new MyPerformanceEntryDto
            {
                Id = 3,
                SongId = 7,
                Title = "Stored Song",
                VenueName = "New Venue",
                PerformedOn = DateTime.Today
            }
        ],
            DateTime.UtcNow));

        var apiResultStarted = new TaskCompletionSource<MyPerformancesResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetMyPerformancesAsync(null, "desc"))
            .Returns(apiResultStarted.Task);

        var loader = new MyPerformancesLoader(api.Object, store);
        var loadTask = loader.LoadAsync();
        loader.InvalidateInFlightLoads();
        apiResultStarted.SetResult(MyPerformancesResult.Ok(
        [
            new MyPerformanceEntryDto
            {
                Id = 3,
                SongId = 7,
                Title = "Stored Song",
                VenueName = "Old Venue",
                PerformedOn = DateTime.Today
            }
        ]));

        await loadTask;

        var cached = await store.GetCachedAsync();
        Assert.NotNull(cached);
        Assert.Equal("New Venue", cached.Performances[0].VenueName);
    }

    [Fact]
    public async Task PatchPerformanceAsync_invalidates_in_flight_load_before_writing()
    {
        var store = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedAsync(new CachedMyPerformances(
        [
            new MyPerformanceEntryDto
            {
                Id = 3,
                SongId = 7,
                Title = "Stored Song",
                VenueName = "Old Venue",
                PerformedOn = DateTime.Today
            }
        ],
            DateTime.UtcNow));

        var apiResultStarted = new TaskCompletionSource<MyPerformancesResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetMyPerformancesAsync(null, "desc"))
            .Returns(apiResultStarted.Task);

        var loader = new MyPerformancesLoader(api.Object, store);
        var loadTask = loader.LoadAsync();

        await loader.PatchPerformanceAsync(new MyPerformanceEntryDto
        {
            Id = 3,
            SongId = 7,
            Title = "Stored Song",
            VenueName = "New Venue",
            PerformedOn = DateTime.Today
        });

        apiResultStarted.SetResult(MyPerformancesResult.Ok(
        [
            new MyPerformanceEntryDto
            {
                Id = 3,
                SongId = 7,
                Title = "Stored Song",
                VenueName = "Old Venue",
                PerformedOn = DateTime.Today
            }
        ]));
        await loadTask;

        var cached = await store.GetCachedAsync();
        Assert.NotNull(cached);
        Assert.Equal("New Venue", cached.Performances[0].VenueName);
    }

    [Fact]
    public async Task Patch_waits_for_in_flight_load_save_then_keeps_patched_cache()
    {
        var inner = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        var gatedStore = new GatedMyPerformancesLocalStore(inner);
        await gatedStore.SaveCachedAsync(new CachedMyPerformances(
        [
            new MyPerformanceEntryDto
            {
                Id = 3,
                SongId = 7,
                Title = "Stored Song",
                VenueName = "Old Venue",
                PerformedOn = DateTime.Today
            }
        ],
            DateTime.UtcNow));

        var releaseLoadSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        gatedStore.PauseNextSaveUntil(releaseLoadSave);

        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetMyPerformancesAsync(null, "desc"))
            .ReturnsAsync(MyPerformancesResult.Ok(
            [
                new MyPerformanceEntryDto
                {
                    Id = 3,
                    SongId = 7,
                    Title = "Stored Song",
                    VenueName = "Stale From Api",
                    PerformedOn = DateTime.Today
                }
            ]));

        var loader = new MyPerformancesLoader(api.Object, gatedStore);
        var loadTask = loader.LoadAsync();

        var patchTask = loader.PatchPerformanceAsync(new MyPerformanceEntryDto
        {
            Id = 3,
            SongId = 7,
            Title = "Stored Song",
            VenueName = "New Venue",
            PerformedOn = DateTime.Today
        });

        await Task.Delay(50);
        releaseLoadSave.SetResult();
        await Task.WhenAll(loadTask, patchTask);

        var cached = await gatedStore.GetCachedAsync();
        Assert.NotNull(cached);
        Assert.Equal("New Venue", cached.Performances[0].VenueName);
    }

    [Fact]
    public async Task RemovePerformanceAsync_removes_matching_entry_from_cache()
    {
        var store = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedAsync(new CachedMyPerformances(
        [
            new MyPerformanceEntryDto { Id = 3, SongId = 7, Title = "Stored Song", PerformedOn = DateTime.Today },
            new MyPerformanceEntryDto { Id = 4, SongId = 8, Title = "Other Song", PerformedOn = DateTime.Today }
        ],
            DateTime.UtcNow));

        var loader = new MyPerformancesLoader(new PerformancesApiStub(), store);
        await loader.RemovePerformanceAsync(3);

        var cached = await loader.TryGetCachedAsync();
        Assert.NotNull(cached);
        Assert.Single(cached.Performances);
        Assert.Equal(4, cached.Performances[0].Id);
    }

    [Fact]
    public async Task AddPerformanceAsync_prepends_and_sorts_ahead_of_same_day_rows()
    {
        var store = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        var today = DateTime.Today;
        await store.SaveCachedAsync(new CachedMyPerformances(
        [
            new MyPerformanceEntryDto
            {
                Id = 8,
                SongId = 7,
                Title = "Earlier Tonight",
                VenueName = "Main Stage",
                PerformedOn = today
            }
        ],
            DateTime.UtcNow.AddMinutes(-30)));

        var loader = new MyPerformancesLoader(new PerformancesApiStub(), store);
        await loader.AddPerformanceAsync(new MyPerformanceEntryDto
        {
            SongId = 42,
            Title = "Jeopardy",
            ArtistName = "The Greg Kihn Band",
            ArtistDisplay = "The Greg Kihn Band",
            VenueName = "Main Stage",
            PerformedOn = today
        });

        var cached = await store.GetCachedAsync();
        Assert.NotNull(cached);
        Assert.Equal(2, cached.Performances.Count);

        var ordered = MyPerformancesQuery.Apply(cached.Performances, venueId: null, sortDir: "desc");
        Assert.Equal("Jeopardy", ordered[0].Title);
        Assert.Equal("Earlier Tonight", ordered[1].Title);
        Assert.True(ordered[0].Id > ordered[1].Id);
    }

    [Fact]
    public async Task AddPerformanceAsync_keeps_each_new_log_when_several_are_saved()
    {
        var store = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedAsync(new CachedMyPerformances([], DateTime.UtcNow.AddHours(-1)));

        var loader = new MyPerformancesLoader(new PerformancesApiStub(), store);
        await loader.AddPerformanceAsync(new MyPerformanceEntryDto
        {
            SongId = 1,
            Title = "First",
            PerformedOn = DateTime.Today
        });
        await loader.AddPerformanceAsync(new MyPerformanceEntryDto
        {
            SongId = 2,
            Title = "Second",
            PerformedOn = DateTime.Today
        });

        var cached = await store.GetCachedAsync();
        Assert.NotNull(cached);
        var ordered = MyPerformancesQuery.Apply(cached.Performances, venueId: null, sortDir: "desc");
        Assert.Equal(["Second", "First"], ordered.Select(p => p.Title));
        Assert.Equal(2, ordered.Select(p => p.Id).Distinct().Count());
    }

    [Fact]
    public async Task AddPerformanceAsync_does_nothing_when_performances_have_not_been_cached()
    {
        var store = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        var loader = new MyPerformancesLoader(new PerformancesApiStub(), store);

        await loader.AddPerformanceAsync(new MyPerformanceEntryDto
        {
            SongId = 42,
            Title = "Jeopardy",
            PerformedOn = DateTime.Today
        });

        Assert.Null(await store.GetCachedAsync());
    }

    [Fact]
    public async Task AddPerformanceAsync_invalidates_in_flight_load_before_writing()
    {
        var store = new MyPerformancesLocalStore(new InMemoryLocalStorage());
        await store.SaveCachedAsync(new CachedMyPerformances(
        [
            new MyPerformanceEntryDto
            {
                Id = 3,
                SongId = 7,
                Title = "Stored Song",
                PerformedOn = DateTime.Today
            }
        ],
            DateTime.UtcNow));

        var apiResultStarted = new TaskCompletionSource<MyPerformancesResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetMyPerformancesAsync(null, "desc"))
            .Returns(apiResultStarted.Task);

        var loader = new MyPerformancesLoader(api.Object, store);
        var loadTask = loader.LoadAsync();

        await loader.AddPerformanceAsync(new MyPerformanceEntryDto
        {
            SongId = 42,
            Title = "Jeopardy",
            PerformedOn = DateTime.Today
        });

        apiResultStarted.SetResult(MyPerformancesResult.Ok(
        [
            new MyPerformanceEntryDto
            {
                Id = 3,
                SongId = 7,
                Title = "Stored Song",
                PerformedOn = DateTime.Today
            }
        ]));
        await loadTask;

        var cached = await store.GetCachedAsync();
        Assert.NotNull(cached);
        Assert.Contains(cached.Performances, p => p.Title == "Jeopardy");
        Assert.Contains(cached.Performances, p => p.Title == "Stored Song");
    }

    [Fact]
    public async Task LoadAsync_keeps_pending_logs_that_are_not_on_the_server_yet()
    {
        var localStorage = new InMemoryLocalStorage();
        var logStore = new LogPerformanceLocalStore(localStorage);
        await logStore.EnqueuePendingPerformanceAsync(new PendingPerformanceEntry(
            Guid.NewGuid(),
            SingerId: 1,
            SongId: 30,
            VenueId: 2,
            PerformedOn: DateTime.Today,
            KeyChangeSemitones: null,
            Title: "Queued Song",
            ArtistName: "Artist",
            VenueName: "Main Stage",
            QueuedAt: DateTime.Now));

        var loader = new MyPerformancesLoader(
            new PerformancesApiStub(),
            new MyPerformancesLocalStore(localStorage),
            logStore);

        var result = await loader.LoadAsync();

        Assert.Contains(result.Performances, p => p.Title == "Queued Song");
        Assert.Contains(result.Performances, p => p.Title == "Newer Song");
        Assert.Equal(1, result.Performances.Count(p => p.Title == "Queued Song"));
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
