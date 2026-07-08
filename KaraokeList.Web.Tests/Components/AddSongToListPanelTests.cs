using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class AddSongToListPanelTests : AuthPageTestContext
{
    private readonly Mock<ILogCatalogLoader> catalogLoader = new();

    public AddSongToListPanelTests()
    {
        AddSyncfusionServices(Services);
        catalogLoader.Setup(loader => loader.TryGetCachedAsync()).ReturnsAsync((LogCatalogSnapshot?)null);
        catalogLoader.Setup(loader => loader.LoadAsync(It.IsAny<Action<string>?>()))
            .ReturnsAsync(new LogCatalogSnapshot([], [], [], false, false, null));
        catalogLoader.Setup(loader => loader.NeedsRefreshAsync()).ReturnsAsync(false);
        catalogLoader.Setup(loader => loader.TryGetCachedLookupsAsync()).ReturnsAsync((LookupsLoadResult?)null);
        catalogLoader.Setup(loader => loader.LoadLookupsAsync())
            .ReturnsAsync(new LookupsLoadResult([], [], false));
        Services.AddSingleton(catalogLoader.Object);
        Api.Setup(client => client.GetArtistLookupsAsync()).ReturnsAsync([]);
        Api.Setup(client => client.GetGenresAsync()).ReturnsAsync([]);
    }

    [Fact]
    public void Choose_mode_shows_catalog_and_new_song_options()
    {
        var cut = Render<AddSongToListPanel>(parameters => parameters
            .Add(p => p.ListId, 2)
            .Add(p => p.ListDisplayName, "Working up"));

        Assert.Contains("+ Add from catalog", cut.Markup);
        Assert.Contains("+ New song", cut.Markup);
        Assert.DoesNotContain("Loading catalog", cut.Markup);
    }

    [Fact]
    public void OpenNewSong_shows_add_song_panel()
    {
        var cut = Render<AddSongToListPanel>(parameters => parameters
            .Add(p => p.ListId, 2)
            .Add(p => p.ListDisplayName, "Want to sing")
            .Add(p => p.OnSongAdded, EventCallback.Factory.Create<SongAddedToListEventArgs>(this, _ => { })));

        var newSongButton = cut.FindAll("button.btn-outline-primary")
            .First(button => button.TextContent?.Contains("New song", StringComparison.Ordinal) == true);
        newSongButton.Click();

        cut.WaitForAssertion(() => Assert.Contains("Title", cut.Markup));
    }

    [Fact]
    public async Task OnNewSongAddedAsync_bypasses_stale_cache_and_adds_to_list()
    {
        var staleSong = new LogSongPickItem(1, "Old Song", "Old Artist", false, false);
        var newSong = new LogSongPickItem(99, "Brand New", "New Artist", false, false);
        var staleSnapshot = new LogCatalogSnapshot([staleSong], [], [], true, true, DateTime.UtcNow);
        var freshSnapshot = new LogCatalogSnapshot([staleSong, newSong], [], [], false, true, DateTime.UtcNow);

        catalogLoader.Setup(loader => loader.TryGetCachedAsync()).ReturnsAsync(staleSnapshot);
        catalogLoader.Setup(loader => loader.LoadAsync(It.IsAny<Action<string>?>()))
            .ReturnsAsync(freshSnapshot);
        Api.Setup(client => client.AddListSongAsync(2, 99))
            .ReturnsAsync(ListSongActionResult.Ok());

        SongAddedToListEventArgs? added = null;
        var cut = Render<AddSongToListPanel>(parameters => parameters
            .Add(p => p.ListId, 2)
            .Add(p => p.ListDisplayName, "Working up")
            .Add(p => p.OnSongAdded, EventCallback.Factory.Create<SongAddedToListEventArgs>(this, args => added = args)));

        cut.Instance.OpenNewSong();
        cut.Render();

        var addSongPanel = cut.FindComponent<AddSongPanel>();
        await addSongPanel.InvokeAsync(() =>
            addSongPanel.Instance.OnSongAdded.InvokeAsync(new SongAddedEventArgs("Brand New", "New Artist")));

        cut.WaitForAssertion(() =>
        {
            catalogLoader.Verify(loader => loader.LoadAsync(It.IsAny<Action<string>?>()), Times.Once);
            Api.Verify(client => client.AddListSongAsync(2, 99), Times.Once);
            Assert.NotNull(added);
            Assert.Contains("Brand New", added!.Message);
            Assert.Contains("Working up", added.Message);
        });
    }
}
