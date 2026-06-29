using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class AddSongToListPanelTests : AuthPageTestContext
{
    public AddSongToListPanelTests()
    {
        AddSyncfusionServices(Services);
        Api.Setup(client => client.GetArtistLookupsAsync()).ReturnsAsync([]);
        Api.Setup(client => client.GetGenresAsync()).ReturnsAsync([]);
    }

    [Fact]
    public void Choose_mode_shows_catalog_and_new_song_options()
    {
        var cut = RenderComponent<AddSongToListPanel>(parameters => parameters
            .Add(p => p.ListId, 2)
            .Add(p => p.ListDisplayName, "Working up"));

        Assert.Contains("+ Add from catalog", cut.Markup);
        Assert.Contains("+ New song", cut.Markup);
        Assert.DoesNotContain("Loading catalog", cut.Markup);
    }

    [Fact]
    public void OpenNewSong_shows_add_song_panel()
    {
        var cut = RenderComponent<AddSongToListPanel>(parameters => parameters
            .Add(p => p.ListId, 2)
            .Add(p => p.ListDisplayName, "Want to sing")
            .Add(p => p.OnSongAdded, EventCallback.Factory.Create<SongAddedToListEventArgs>(this, _ => { })));

        var newSongButton = cut.FindAll("button.btn-outline-primary")
            .First(button => button.TextContent?.Contains("New song", StringComparison.Ordinal) == true);
        newSongButton.Click();

        cut.WaitForAssertion(() => Assert.Contains("Title", cut.Markup));
    }
}
