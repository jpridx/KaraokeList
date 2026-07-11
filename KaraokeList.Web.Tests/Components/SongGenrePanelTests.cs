using System.Reflection;
using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class SongGenrePanelTests : BunitTestContext
{
    private readonly Mock<IKaraokeApiClient> api = new();
    private readonly Mock<ILogCatalogLoader> catalogLoader = new();
    private readonly Mock<IMySongsLoader> mySongsLoader = new();

    public SongGenrePanelTests()
    {
        AddSyncfusionServices(Services);
        Services.AddSingleton(api.Object);
        Services.AddSingleton(catalogLoader.Object);
        Services.AddSingleton(mySongsLoader.Object);

        catalogLoader.Setup(loader => loader.TryGetCachedLookupsAsync())
            .ReturnsAsync(new LookupsLoadResult(
                [],
                [
                    new GenreDto { Id = 10, GenreName = "Classic Rock" },
                    new GenreDto { Id = 20, GenreName = "Pop Rock" }
                ],
                true));
    }

    [Fact]
    public void Shows_current_genre_and_change_button()
    {
        var cut = Render<SongGenrePanel>(parameters => parameters
            .Add(p => p.SongId, 42)
            .Add(p => p.Title, "Bohemian Rhapsody")
            .Add(p => p.ArtistName, "Queen")
            .Add(p => p.GenreId, 10)
            .Add(p => p.GenreName, "Classic Rock")
            .Add(p => p.OnGenreChanged, EventCallback.Factory.Create<SongGenreChangedEventArgs>(this, _ => { })));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Genre", cut.Markup);
            Assert.Contains("Classic Rock", cut.Markup);
            Assert.Contains("Change genre", cut.Markup);
        });
    }

    [Fact]
    public async Task Save_updates_genre_and_fires_callback()
    {
        SongGenreChangedEventArgs? changed = null;
        api.Setup(client => client.UpdateSongGenreAsync(7, It.IsAny<UpdateSongGenreRequest>()))
            .ReturnsAsync(SongGenreUpdateResult.Ok());
        mySongsLoader.Setup(loader => loader.PatchCachedSongGenreAsync(7, 20, "Pop Rock"))
            .Returns(Task.CompletedTask);

        var cut = Render<SongGenrePanel>(parameters => parameters
            .Add(p => p.SongId, 7)
            .Add(p => p.Title, "Zebra")
            .Add(p => p.ArtistName, "Artist Z")
            .Add(p => p.GenreId, 10)
            .Add(p => p.GenreName, "Classic Rock")
            .Add(p => p.OnGenreChanged, EventCallback.Factory.Create<SongGenreChangedEventArgs>(this, args => changed = args)));

        cut.WaitForAssertion(() => Assert.Contains("Change genre", cut.Markup));
        cut.Find("button.btn-outline-secondary").Click();

        cut.WaitForAssertion(() => Assert.Contains("Save genre", cut.Markup));

        SetPrivateField(cut.Instance, "editGenreName", "Pop Rock");
        SetPrivateField(cut.Instance, "confirmedGenreId", 20);
        cut.Render();

        cut.FindAll("button.btn-outline-primary")
            .First(button => button.TextContent?.Contains("Save genre") == true)
            .Click();

        await cut.InvokeAsync(() => Task.CompletedTask);

        api.Verify(client => client.UpdateSongGenreAsync(
            7,
            It.Is<UpdateSongGenreRequest>(request => request.GenreId == 20)), Times.Once);
        mySongsLoader.Verify(loader => loader.PatchCachedSongGenreAsync(7, 20, "Pop Rock"), Times.Once);
        Assert.NotNull(changed);
        Assert.Equal(20, changed.GenreId);
        Assert.Equal("Pop Rock", changed.GenreName);
    }

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(target, value);
    }
}
