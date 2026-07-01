using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class SingerStatsIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetMyStats_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/performances/my-stats");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetMyStats_ReturnsAggregatesForSinger()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (songIdA, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var (songIdB, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        // Use fixed dates with an explicit asOfDate so the test is immune to the calendar.
        // asOf = Mar 15; two songIdA performances in March (different dates); songIdB in Jan (old).
        var asOf = new DateTime(2025, 3, 15);
        await PerformanceTestDataHelper.CreatePerformanceAsync(client, songIdA, venueId, new DateTime(2025, 3, 5));
        await PerformanceTestDataHelper.CreatePerformanceAsync(client, songIdA, venueId, asOf);
        await PerformanceTestDataHelper.CreatePerformanceAsync(client, songIdB, venueId, new DateTime(2025, 1, 1));

        var stats = await client.GetFromJsonAsync<SingerStatsDto>(
            $"/api/performances/my-stats?topSongs=5&topArtists=5&newRepertoireDays=30&topVenues=3&asOfDate={asOf:yyyy-MM-dd}");

        Assert.NotNull(stats);
        Assert.Equal(3, stats.TotalPerformances);
        Assert.Equal(2, stats.UniqueSongs);
        Assert.Equal(2, stats.PerformancesThisMonth);
        Assert.Equal(3, stats.PerformancesThisYear);
        Assert.NotNull(stats.LastPerformedOn);
        Assert.Equal(0, stats.DaysSinceLastPerformance);
        Assert.NotEmpty(stats.TopVenues);
        Assert.Equal(3, stats.TopVenues[0].PerformanceCount);
        Assert.NotEmpty(stats.TopSongs);
        Assert.Equal(songIdA, stats.TopSongs[0].SongId);
        Assert.Equal(2, stats.TopSongs[0].PerformanceCount);
        Assert.NotEmpty(stats.TopArtists);
        Assert.Single(stats.NewRepertoireSongs);
    }

    [SkippableFact]
    public async Task GetMyStats_WithInvalidTopSongs_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var response = await client.GetAsync("/api/performances/my-stats?topSongs=99");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetMyStats_WithNoPerformances_ReturnsZeros()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var stats = await client.GetFromJsonAsync<SingerStatsDto>("/api/performances/my-stats");

        Assert.NotNull(stats);
        Assert.Equal(0, stats.TotalPerformances);
        Assert.Equal(0, stats.UniqueSongs);
        Assert.Empty(stats.TopVenues);
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"stats-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
