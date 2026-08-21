using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using KaraokeList.Web.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class QuickLogPerformanceTests : AuthPageTestContext
{
    private readonly Mock<ILogCatalogLoader> catalogLoader = new();
    private readonly LogPerformanceLocalStore logStore = new(new InMemoryLocalStorage());

    public QuickLogPerformanceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        Api.Setup(client => client.GetMySongSummaryAsync(It.IsAny<int>()))
            .ReturnsAsync(SongSummaryResult.Ok(new SongPerformanceSummaryDto { SongId = 42 }));
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        AddSyncfusionServices(services);
        services.AddSingleton(catalogLoader.Object);
        services.AddSingleton<ILogPerformanceLocalStore>(logStore);
    }

    [Fact]
    public async Task Applies_stored_venue_when_shared_venues_arrive_after_defaults_load()
    {
        await logStore.SaveFormDefaultsAsync(LogFormDefaults.ForToday(3));

        var cut = Render<QuickLogPerformance>(parameters => parameters
            .Add(p => p.SongId, 42)
            .Add(p => p.Title, "Jeopardy")
            .Add(p => p.ArtistName, "The Greg Kihn Band")
            .Add(p => p.SingerId, 1)
            .Add(p => p.SharedVenues, Array.Empty<VenueDto>())
            .Add(p => p.SharedSingers, Array.Empty<SingerDto>())
            .Add(p => p.ShowCoPerformersEditor, false)
            .Add(p => p.ShowHostMessagePanel, false));

        cut.Render(parameters => parameters
            .Add(p => p.SharedVenues, new List<VenueDto>
            {
                new() { Id = 3, VenueName = "Main Stage" }
            }));

        cut.WaitForAssertion(() => Assert.Contains("Main Stage", cut.Markup));
    }

    [Fact]
    public async Task Ignores_stored_venue_from_yesterday_when_shared_venues_arrive()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        await logStore.SaveFormDefaultsAsync(new LogFormDefaults(3, yesterday));

        var cut = Render<QuickLogPerformance>(parameters => parameters
            .Add(p => p.SongId, 42)
            .Add(p => p.Title, "Jeopardy")
            .Add(p => p.ArtistName, "The Greg Kihn Band")
            .Add(p => p.SingerId, 1)
            .Add(p => p.SharedVenues, Array.Empty<VenueDto>())
            .Add(p => p.SharedSingers, Array.Empty<SingerDto>())
            .Add(p => p.ShowCoPerformersEditor, false)
            .Add(p => p.ShowHostMessagePanel, false));

        cut.Render(parameters => parameters
            .Add(p => p.SharedVenues, new List<VenueDto>
            {
                new() { Id = 3, VenueName = "Main Stage" }
            }));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Where are you?", cut.Markup);
            Assert.DoesNotContain("value=\"Main Stage\"", cut.Markup);
        });
    }
}
