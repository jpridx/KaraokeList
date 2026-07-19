using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class SongArtistsEditorTests : BunitTestContext
{
    private readonly Mock<IKaraokeApiClient> api = new();
    private readonly Mock<ILogCatalogLoader> catalogLoader = new();

    public SongArtistsEditorTests()
    {
        AddSyncfusionServices(Services);
        Services.AddSingleton(api.Object);
        Services.AddSingleton(catalogLoader.Object);
    }

    [Fact]
    public void Shows_add_artist_button_when_name_not_in_catalog()
    {
        var artistNames = new List<string> { "Brand New Band" };

        var cut = Render<SongArtistsEditor>(parameters => parameters
            .Add(p => p.ArtistNames, artistNames)
            .Add(p => p.ArtistLookups, Array.Empty<ArtistLookupDto>())
            .Add(p => p.ArtistNamesChanged, EventCallback.Factory.Create<List<string>>(this, _ => { }))
            .Add(p => p.ArtistLookupsChanged, EventCallback.Factory.Create<IReadOnlyList<ArtistLookupDto>>(this, _ => { })));

        Assert.Contains("Add artist \"Brand New Band\"", cut.Markup);
    }

    [Fact]
    public void Add_another_artist_button_adds_second_row()
    {
        var artistNames = new List<string> { "Existing Band" };
        var lookups = new List<ArtistLookupDto> { new() { Id = 1, Name = "Existing Band" } };

        var cut = Render<SongArtistsEditor>(parameters => parameters
            .Add(p => p.ArtistNames, artistNames)
            .Add(p => p.ArtistLookups, lookups)
            .Add(p => p.ArtistNamesChanged, EventCallback.Factory.Create<List<string>>(this, names => artistNames = names))
            .Add(p => p.ArtistLookupsChanged, EventCallback.Factory.Create<IReadOnlyList<ArtistLookupDto>>(this, _ => { })));

        cut.Find("button.btn-outline-secondary").Click();

        Assert.Equal(2, artistNames.Count);
        Assert.Equal(string.Empty, artistNames[1]);
    }
}
