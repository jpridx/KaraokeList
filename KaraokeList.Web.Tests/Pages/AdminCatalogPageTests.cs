using KaraokeList.Shared;
using KaraokeList.Web.Pages;
using KaraokeList.Web.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Pages;

/// <summary>
/// Smoke tests for admin catalog pages — verify API load + page chrome without exercising Syncfusion row rendering.
/// </summary>
public sealed class AdminCatalogPageTests : AuthPageTestContext
{
    public AdminCatalogPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        AddSyncfusionServices(services);
    }

    [Fact]
    public void Genres_page_loads_from_api()
    {
        Api.Setup(client => client.GetGenresAsync()).ReturnsAsync([]);

        var cut = RenderComponent<Genres>();

        cut.WaitForAssertion(() => Api.Verify(client => client.GetGenresAsync(), Times.Once));
        Assert.Contains("Genres", cut.Markup);
    }

    [Fact]
    public void Singers_page_loads_from_api()
    {
        Api.Setup(client => client.GetSingersAsync()).ReturnsAsync([]);

        var cut = RenderComponent<Singers>();

        cut.WaitForAssertion(() => Api.Verify(client => client.GetSingersAsync(), Times.Once));
        Assert.Contains("Singers", cut.Markup);
    }

    [Fact]
    public void Venues_page_loads_from_api()
    {
        Api.Setup(client => client.GetVenuesAsync()).ReturnsAsync([]);

        var cut = RenderComponent<Venues>();

        cut.WaitForAssertion(() => Api.Verify(client => client.GetVenuesAsync(), Times.Once));
        Assert.Contains("Venues", cut.Markup);
    }

    [Fact]
    public void Artists_page_loads_from_api()
    {
        Api.Setup(client => client.GetGenresAsync()).ReturnsAsync([]);
        Api.Setup(client => client.GetArtistsAsync()).ReturnsAsync([]);

        var cut = RenderComponent<Artists>();

        cut.WaitForAssertion(() =>
        {
            Api.Verify(client => client.GetGenresAsync(), Times.Once);
            Api.Verify(client => client.GetArtistsAsync(), Times.Once);
        });
        Assert.Contains("Artists", cut.Markup);
    }

    [Fact]
    public void Songs_page_loads_from_api()
    {
        Api.Setup(client => client.GetArtistLookupsAsync()).ReturnsAsync([]);
        Api.Setup(client => client.GetGenresAsync()).ReturnsAsync([]);
        Api.Setup(client => client.GetSongsAsync()).ReturnsAsync([]);

        var cut = RenderComponent<Songs>();

        cut.WaitForAssertion(() =>
        {
            Api.Verify(client => client.GetSongsAsync(), Times.Once);
            Api.Verify(client => client.GetArtistLookupsAsync(), Times.Once);
        });
        Assert.Contains("Songs", cut.Markup);
    }

    [Fact]
    public void Performances_page_loads_when_lookups_exist()
    {
        Api.Setup(client => client.GetProfileAsync())
            .ReturnsAsync(new UserProfileDto { SingerId = 1 });
        Api.Setup(client => client.GetSingersAsync())
            .ReturnsAsync([new SingerDto { Id = 1, Name = "Alex" }]);
        Api.Setup(client => client.GetSongsAsync())
            .ReturnsAsync([new SongDto { Id = 2, Title = "Jeopardy" }]);
        Api.Setup(client => client.GetVenuesAsync())
            .ReturnsAsync([new VenueDto { Id = 3, VenueName = "Main Stage" }]);
        Api.Setup(client => client.GetPerformancesAsync(null))
            .ReturnsAsync([]);

        var cut = RenderComponent<Performances>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Performances", cut.Markup);
            Assert.Contains("About a song", cut.Markup);
        });
    }
}
