using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class CatalogSortIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetGenres_ReturnsAlphabeticalOrder()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await client.PostAsJsonAsync("/api/genres", new GenreDto { GenreName = $"Zulu-{suffix}" });
        await client.PostAsJsonAsync("/api/genres", new GenreDto { GenreName = $"Alpha-{suffix}" });

        var genres = await client.GetFromJsonAsync<List<GenreDto>>("/api/genres");
        Assert.NotNull(genres);

        var names = genres.Select(g => g.GenreName).ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase), names);
    }

    [SkippableFact]
    public async Task GetVenues_ReturnsAlphabeticalOrder()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        await client.PostAsJsonAsync("/api/venues", new VenueDto { VenueName = $"Zulu Bar-{suffix}" });
        await client.PostAsJsonAsync("/api/venues", new VenueDto { VenueName = $"Alpha Lounge-{suffix}" });

        var venues = await client.GetFromJsonAsync<List<VenueDto>>("/api/venues");
        Assert.NotNull(venues);

        var names = venues.Select(v => v.VenueName).ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase), names);
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"sort-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
