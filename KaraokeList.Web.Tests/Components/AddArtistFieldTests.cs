using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class AddArtistFieldTests : BunitTestContext
{
    private readonly Mock<IKaraokeApiClient> api = new();

    public AddArtistFieldTests()
    {
        AddSyncfusionServices(Services);
        Services.AddSingleton(api.Object);
    }

    [Fact]
    public void Shows_hint_when_artist_field_empty()
    {
        var cut = RenderComponent<AddArtistField>(parameters => parameters
            .Add(p => p.ArtistLookups, Array.Empty<ArtistLookupDto>())
            .Add(p => p.ArtistName, string.Empty)
            .Add(p => p.ArtistNameChanged, EventCallback.Factory.Create<string>(this, _ => { }))
            .Add(p => p.ArtistLookupsChanged, EventCallback.Factory.Create<IReadOnlyList<ArtistLookupDto>>(this, _ => { }))
            .Add(p => p.OnArtistConfirmed, EventCallback.Factory.Create<ArtistConfirmedEventArgs>(this, _ => { })));

        Assert.Contains("Type the artist name", cut.Markup);
        Assert.Contains("Add artist", cut.Markup);
    }

    [Fact]
    public async Task AddArtistAsync_fires_OnArtistConfirmed_with_catalog_id()
    {
        ArtistConfirmedEventArgs? confirmed = null;
        var refreshedLookups = new List<ArtistLookupDto>
        {
            new() { Id = 42, Name = "New Band" }
        };

        api.Setup(client => client.CreateArtistAsync(It.IsAny<ArtistDto>()))
            .Returns(Task.CompletedTask);
        api.Setup(client => client.GetArtistLookupsAsync())
            .ReturnsAsync(refreshedLookups);

        var cut = RenderComponent<AddArtistField>(parameters => parameters
            .Add(p => p.ArtistLookups, Array.Empty<ArtistLookupDto>())
            .Add(p => p.ArtistName, "New Band")
            .Add(p => p.ArtistNameChanged, EventCallback.Factory.Create<string>(this, _ => { }))
            .Add(p => p.ArtistLookupsChanged, EventCallback.Factory.Create<IReadOnlyList<ArtistLookupDto>>(this, _ => { }))
            .Add(p => p.OnArtistConfirmed, EventCallback.Factory.Create<ArtistConfirmedEventArgs>(this, args => confirmed = args)));

        cut.Find("button.btn-outline-primary").Click();

        await cut.InvokeAsync(() => Task.CompletedTask);
        cut.WaitForAssertion(() => Assert.NotNull(confirmed));
        Assert.Equal(42, confirmed!.Id);
        Assert.Equal("New Band", confirmed.Name);
    }
}
