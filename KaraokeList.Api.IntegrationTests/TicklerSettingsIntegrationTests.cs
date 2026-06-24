using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class TicklerSettingsIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetTicklerSettings_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/auth/tickler-settings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetTicklerSettings_ReturnsDefaultsForNewUser()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var settings = await client.GetFromJsonAsync<TicklerSettingsDto>("/api/auth/tickler-settings");

        Assert.NotNull(settings);
        Assert.Equal(TicklerSettingsLimits.DefaultStaleAfterDays, settings.StaleAfterDays);
        Assert.Equal(TicklerSettingsLimits.DefaultSongLimit, settings.SongLimit);
    }

    [SkippableFact]
    public async Task UpdateTicklerSettings_PersistsAndAffectsStaleSongsQuery()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var customDays = 60;
        var customLimit = 3;

        var updateResponse = await client.PutAsJsonAsync(
            "/api/auth/tickler-settings",
            new UpdateTicklerSettingsRequest
            {
                StaleAfterDays = customDays,
                SongLimit = customLimit
            });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var settings = await client.GetFromJsonAsync<TicklerSettingsDto>("/api/auth/tickler-settings");
        Assert.NotNull(settings);
        Assert.Equal(customDays, settings.StaleAfterDays);
        Assert.Equal(customLimit, settings.SongLimit);

        var (staleSongId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        await PerformanceTestDataHelper.CreatePerformanceAsync(
            client, staleSongId, venueId, DateTime.Today.AddDays(-75));

        var withoutQuery = await client.GetFromJsonAsync<StaleSongsResponseDto>(
            "/api/performances/my-stale-songs");
        Assert.NotNull(withoutQuery);
        Assert.Equal(customDays, withoutQuery.StaleAfterDays);
        Assert.Contains(withoutQuery.Songs, s => s.SongId == staleSongId);

        var withQuery = await client.GetFromJsonAsync<StaleSongsResponseDto>(
            "/api/performances/my-stale-songs?days=90");
        Assert.NotNull(withQuery);
        Assert.Equal(90, withQuery.StaleAfterDays);
        Assert.DoesNotContain(withQuery.Songs, s => s.SongId == staleSongId);
    }

    [SkippableFact]
    public async Task UpdateTicklerSettings_InvalidValues_ReturnsBadRequest()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var response = await client.PutAsJsonAsync(
            "/api/auth/tickler-settings",
            new UpdateTicklerSettingsRequest
            {
                StaleAfterDays = 3,
                SongLimit = 5
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"tickler-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
