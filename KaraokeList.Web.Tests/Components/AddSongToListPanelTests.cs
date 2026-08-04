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

    private static readonly List<SingerListDto> SingerLists =
    [
        new() { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" },
        new() { Id = 2, Kind = SingerListKind.WantToSing, DisplayName = "Want to sing" },
        new() { Id = 3, Kind = SingerListKind.WorkingUp, DisplayName = "Working up" }
    ];

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
    public void Collapsed_shows_add_song_button()
    {
        var cut = RenderPanel();

        Assert.Contains("+ Add song", cut.Markup);
        Assert.DoesNotContain("Add to selected lists", cut.Markup);
    }

    [Fact]
    public void Expand_shows_catalog_picker_and_list_legend()
    {
        var cut = RenderPanel(catalogItems:
        [
            new LogSongPickItem(42, "Jeopardy", "The Greg Kihn Band", true, false)
        ]);

        cut.Instance.Expand();
        cut.Render();

        cut.FindComponent<CatalogSongPicker>();
        Assert.Contains("1 songs", cut.Markup);
        Assert.Contains("★ = on My repertoire", cut.Markup);
        Assert.Contains("+ New song", cut.Markup);
    }

    [Fact]
    public void Expand_and_new_song_shows_add_song_panel()
    {
        var cut = RenderPanel(onSongAdded: _ => { });

        cut.Instance.Expand();
        cut.Render();

        var newSongButton = cut.FindAll("button.btn-link")
            .First(button => button.TextContent?.Contains("New song", StringComparison.Ordinal) == true);
        newSongButton.Click();

        cut.WaitForAssertion(() => Assert.Contains("Title", cut.Markup));
    }

    [Fact]
    public async Task Confirm_adds_to_multiple_selected_lists()
    {
        var catalogItems = new List<LogSongPickItem>
        {
            new(99, "Brand New", "New Artist", false, false)
        };

        Api.Setup(client => client.GetSongListMembershipAsync(99))
            .ReturnsAsync(SongListMembershipResult.Ok([]));
        Api.Setup(client => client.AddListSongAsync(1, 99))
            .ReturnsAsync(ListSongActionResult.Ok());
        Api.Setup(client => client.AddListSongAsync(3, 99))
            .ReturnsAsync(ListSongActionResult.Ok());

        SongAddedToListEventArgs? added = null;
        var cut = RenderPanel(
            catalogItems: catalogItems,
            defaultListKind: SingerListKind.MyRepertoire,
            onSongAdded: args => added = args);

        cut.Instance.Expand();
        cut.Render();

        await cut.InvokeAsync(() => cut.FindComponent<CatalogSongPicker>().Instance
            .SelectedSongIdChanged.InvokeAsync(99));
        cut.Render();

        cut.WaitForAssertion(() => Assert.Contains("Add to lists", cut.Markup));

        var workingUpCheckbox = cut.Find("#add-song-list-WorkingUp");
        workingUpCheckbox.Change(true);

        var confirmButton = cut.FindAll("button.btn-primary")
            .First(button => button.TextContent?.Contains("Add to selected lists", StringComparison.Ordinal) == true);
        confirmButton.Click();

        cut.WaitForAssertion(() =>
        {
            Api.Verify(client => client.AddListSongAsync(1, 99), Times.Once);
            Api.Verify(client => client.AddListSongAsync(3, 99), Times.Once);
            Assert.NotNull(added);
            Assert.Contains("Brand New", added!.Message);
            Assert.Contains("My repertoire", added.Message);
            Assert.Contains("Working up", added.Message);
            Assert.Equal(99, added.SongId);
            Assert.Contains(SingerListKind.MyRepertoire, added.AddedLists);
            Assert.Contains(SingerListKind.WorkingUp, added.AddedLists);
        });
    }

    [Fact]
    public void Offline_catalog_disables_add_song_button()
    {
        var cut = RenderPanel(usingOfflineCatalog: true);

        var button = cut.Find("button.btn-outline-primary");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Want_to_sing_checkbox_disabled_when_membership_includes_repertoire()
    {
        var catalogItems = new List<LogSongPickItem>
        {
            new(42, "Jeopardy", "The Greg Kihn Band", false, false)
        };

        Api.Setup(client => client.GetSongListMembershipAsync(42))
            .ReturnsAsync(SongListMembershipResult.Ok([SingerListKind.MyRepertoire]));

        var cut = RenderPanel(
            catalogItems: catalogItems,
            defaultListKind: SingerListKind.WantToSing);

        cut.Instance.Expand();
        cut.Render();

        await cut.InvokeAsync(() => cut.FindComponent<CatalogSongPicker>().Instance
            .SelectedSongIdChanged.InvokeAsync(42));
        cut.Render();

        cut.WaitForAssertion(() =>
        {
            var wantCheckbox = cut.Find("#add-song-list-WantToSing");
            Assert.True(wantCheckbox.HasAttribute("disabled"));
        });
    }

    [Fact]
    public async Task OnNewSongCreated_selects_song_and_pre_checks_default_list()
    {
        var patchedSnapshot = new LogCatalogSnapshot(
            [new LogSongPickItem(99, "Brand New", "New Artist", false, false)],
            [],
            [],
            false,
            true,
            DateTime.UtcNow);

        Api.Setup(client => client.GetSongListMembershipAsync(99))
            .ReturnsAsync(SongListMembershipResult.Ok([]));
        Api.Setup(client => client.AddListSongAsync(3, 99))
            .ReturnsAsync(ListSongActionResult.Ok());

        SongAddedToListEventArgs? added = null;
        var cut = RenderPanel(
            defaultListKind: SingerListKind.WorkingUp,
            onSongAdded: args => added = args);

        cut.Instance.Expand();
        cut.Render();

        var newSongButton = cut.FindAll("button.btn-link")
            .First(button => button.TextContent?.Contains("New song", StringComparison.Ordinal) == true);
        newSongButton.Click();
        cut.Render();

        var addSongPanel = cut.FindComponent<AddSongPanel>();
        await addSongPanel.InvokeAsync(() =>
            addSongPanel.Instance.OnSongAdded.InvokeAsync(
                new SongAddedEventArgs(99, "Brand New", "New Artist", patchedSnapshot)));

        cut.Render();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Add to lists", cut.Markup);
            var workingUpCheckbox = cut.Find("#add-song-list-WorkingUp");
            Assert.True(workingUpCheckbox.HasAttribute("checked"));
        });

        var confirmButton = cut.FindAll("button.btn-primary")
            .First(button => button.TextContent?.Contains("Add to selected lists", StringComparison.Ordinal) == true);
        confirmButton.Click();

        cut.WaitForAssertion(() =>
        {
            Api.Verify(client => client.AddListSongAsync(3, 99), Times.Once);
            Assert.NotNull(added);
            Assert.Contains("Working up", added!.Message);
        });
    }

    private IRenderedComponent<AddSongToListPanel> RenderPanel(
        IReadOnlyList<LogSongPickItem>? catalogItems = null,
        SingerListKind defaultListKind = SingerListKind.WorkingUp,
        bool usingOfflineCatalog = false,
        Action<SongAddedToListEventArgs>? onSongAdded = null)
    {
        return Render<AddSongToListPanel>(parameters => parameters
            .Add(p => p.CatalogItems, catalogItems ?? [])
            .Add(p => p.SingerLists, SingerLists)
            .Add(p => p.DefaultListKind, defaultListKind)
            .Add(p => p.UsingOfflineCatalog, usingOfflineCatalog)
            .Add(p => p.OnSongAdded, EventCallback.Factory.Create(this, onSongAdded ?? (_ => { }))));
    }
}
