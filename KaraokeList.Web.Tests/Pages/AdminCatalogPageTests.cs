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
    public void CatalogMaintenance_page_shows_tools_and_tables()
    {
        var cut = Render<CatalogMaintenance>();

        Assert.Contains("Catalog maintenance", cut.Markup);
        Assert.Contains("Catalog tools", cut.Markup);
        Assert.Contains("href=\"admin/import-songs\"", cut.Markup);
        Assert.Contains("href=\"songs\"", cut.Markup);
        Assert.Contains("href=\"artists\"", cut.Markup);
    }

    [Fact]
    public void Genres_page_loads_from_api()
    {
        Api.Setup(client => client.GetGenresAsync()).ReturnsAsync([]);

        var cut = Render<Genres>();

        cut.WaitForAssertion(() => Api.Verify(client => client.GetGenresAsync(), Times.Once));
        Assert.Contains("Genres", cut.Markup);
        AssertCatalogMaintenanceBackLink(cut.Markup);
    }

    [Fact]
    public void GenreGroups_page_loads_from_api()
    {
        Api.Setup(client => client.GetGenreGroupsAsync()).ReturnsAsync([]);
        Api.Setup(client => client.GetGenresAsync()).ReturnsAsync([]);

        var cut = Render<GenreGroups>();

        cut.WaitForAssertion(() =>
        {
            Api.Verify(client => client.GetGenreGroupsAsync(), Times.Once);
            Api.Verify(client => client.GetGenresAsync(), Times.Once);
        });
        Assert.Contains("Genre Groups", cut.Markup);
        AssertCatalogMaintenanceBackLink(cut.Markup);
    }

    [Fact]
    public void Singers_page_loads_from_api()
    {
        Api.Setup(client => client.GetSingersAsync()).ReturnsAsync([]);

        var cut = Render<Singers>();

        cut.WaitForAssertion(() => Api.Verify(client => client.GetSingersAsync(), Times.Once));
        Assert.Contains("Singers", cut.Markup);
        AssertCatalogMaintenanceBackLink(cut.Markup);
    }

    [Fact]
    public void Venues_page_loads_from_api()
    {
        Api.Setup(client => client.GetVenuesAsync()).ReturnsAsync([]);

        var cut = Render<Venues>();

        cut.WaitForAssertion(() => Api.Verify(client => client.GetVenuesAsync(), Times.Once));
        Assert.Contains("Venues", cut.Markup);
        AssertCatalogMaintenanceBackLink(cut.Markup);
    }

    [Fact]
    public void Artists_page_loads_from_api()
    {
        Api.Setup(client => client.GetGenresAsync()).ReturnsAsync([]);
        Api.Setup(client => client.GetArtistsAsync()).ReturnsAsync([]);

        var cut = Render<Artists>();

        cut.WaitForAssertion(() =>
        {
            Api.Verify(client => client.GetGenresAsync(), Times.Once);
            Api.Verify(client => client.GetArtistsAsync(), Times.Once);
        });
        Assert.Contains("Artists", cut.Markup);
        AssertCatalogMaintenanceBackLink(cut.Markup);
    }

    [Fact]
    public void Songs_page_loads_from_api()
    {
        Api.Setup(client => client.GetArtistLookupsAsync()).ReturnsAsync([]);
        Api.Setup(client => client.GetGenresAsync()).ReturnsAsync([]);
        Api.Setup(client => client.GetSongsAsync()).ReturnsAsync([]);

        var cut = Render<Songs>();

        cut.WaitForAssertion(() =>
        {
            Api.Verify(client => client.GetSongsAsync(), Times.Once);
            Api.Verify(client => client.GetArtistLookupsAsync(), Times.Once);
        });
        Assert.Contains("Songs", cut.Markup);
        AssertCatalogMaintenanceBackLink(cut.Markup);
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

        var cut = Render<Performances>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Performances", cut.Markup);
            Assert.Contains("About a song", cut.Markup);
        });
        AssertCatalogMaintenanceBackLink(cut.Markup);
    }

    private static void AssertCatalogMaintenanceBackLink(string markup)
    {
        Assert.Contains("href=\"/admin/catalog\"", markup);
        Assert.Contains("← Catalog maintenance", markup);
    }
}
