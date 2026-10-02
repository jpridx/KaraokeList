using System.Reflection;
using Blazored.LocalStorage;
using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using KaraokeList.Web.Tests.TestDoubles;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Syncfusion.Blazor;

namespace KaraokeList.Web.Tests.Components;

public sealed class MyPerformancesRefreshTests : AuthPageTestContext
{
    private readonly InMemoryLocalStorage performancesLocalStorage = new();

    public MyPerformancesRefreshTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSyncfusionBlazor();
        Api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 1 });
        Api.Setup(client => client.GetVenuesAsync())
            .ReturnsAsync([]);
        Api.Setup(client => client.GetSingersAsync())
            .ReturnsAsync([]);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        var store = new MyPerformancesLocalStore(performancesLocalStorage);
        services.AddSingleton<IMyPerformancesLocalStore>(store);
        services.AddSingleton<IMyPerformancesLoader>(new ControllableMyPerformancesLoader(store));
    }

    [Fact]
    public async Task Stale_background_load_does_not_revert_patched_venue_after_edit()
    {
        await performancesLocalStorage.SetItemAsync("karaoke.myPerformances.cached", new CachedMyPerformances(
        [
            new MyPerformanceEntryDto
            {
                Id = 7,
                SongId = 10,
                Title = "Race Song",
                ArtistName = "Artist",
                VenueId = 1,
                VenueName = "Old Venue",
                PerformedOn = DateTime.Today
            }
        ],
            DateTime.UtcNow));

        var loader = (ControllableMyPerformancesLoader)Services.GetRequiredService<IMyPerformancesLoader>();
        var store = Services.GetRequiredService<IMyPerformancesLocalStore>();
        var staleLoadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        loader.EnqueueLoad(async () =>
        {
            await staleLoadStarted.Task;
            return new MyPerformancesLoadResult(
            [
                new MyPerformanceEntryDto
                {
                    Id = 7,
                    SongId = 10,
                    Title = "Race Song",
                    ArtistName = "Artist",
                    VenueId = 1,
                    VenueName = "Old Venue",
                    PerformedOn = DateTime.Today
                }
            ],
                FromCache: false,
                HasCache: true,
                DateTime.UtcNow,
                null,
                false);
        });
        loader.EnqueueLoad(() => Task.FromResult(new MyPerformancesLoadResult(
        [
            new MyPerformanceEntryDto
            {
                Id = 7,
                SongId = 10,
                Title = "Race Song",
                ArtistName = "Artist",
                VenueId = 2,
                VenueName = "New Venue",
                PerformedOn = DateTime.Today
            }
        ],
            FromCache: false,
            HasCache: true,
            DateTime.UtcNow,
            null,
            false)));

        var cut = Render<MyPerformances>();
        cut.WaitForAssertion(() => Assert.Contains("Old Venue", cut.Markup));

        await store.SaveCachedAsync(new CachedMyPerformances(
        [
            new MyPerformanceEntryDto
            {
                Id = 7,
                SongId = 10,
                Title = "Race Song",
                ArtistName = "Artist",
                VenueId = 2,
                VenueName = "New Venue",
                PerformedOn = DateTime.Today
            }
        ],
            DateTime.UtcNow));

        var refreshMethod = typeof(MyPerformances).GetMethod(
            "RefreshPerformancesAfterEditAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(refreshMethod);
        await cut.InvokeAsync(async () => await (Task)refreshMethod.Invoke(cut.Instance, null)!);

        cut.WaitForAssertion(() => Assert.Contains("New Venue", cut.Markup));

        staleLoadStarted.SetResult();
        await Task.Delay(100);
        cut.Render();

        Assert.Contains("New Venue", cut.Markup);
        Assert.DoesNotContain("Old Venue", cut.Markup);
    }
}
