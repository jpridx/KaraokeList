using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class VenuesCrudTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetVenues_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/venues");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task Venues_CreateReadUpdateDelete_WorksWithJwt()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = factory.CreateClient();
        var email = $"venues-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var venueName = $"Test Venue {Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/api/venues", new VenueDto { VenueName = venueName });
        Assert.Equal(HttpStatusCode.NoContent, createResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/venues");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var venues = await listResponse.Content.ReadFromJsonAsync<List<VenueDto>>();
        Assert.NotNull(venues);
        var created = Assert.Single(venues, v => v.VenueName == venueName);
        Assert.True(created.Id > 0);

        var updatedName = $"{venueName} Updated";
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/venues/{created.Id}",
            new VenueDto { Id = created.Id, VenueName = updatedName });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        venues = await (await client.GetAsync("/api/venues")).Content.ReadFromJsonAsync<List<VenueDto>>();
        Assert.NotNull(venues);
        Assert.Contains(venues, v => v.Id == created.Id && v.VenueName == updatedName);

        var deleteResponse = await client.DeleteAsync($"/api/venues/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        venues = await (await client.GetAsync("/api/venues")).Content.ReadFromJsonAsync<List<VenueDto>>();
        Assert.NotNull(venues);
        Assert.DoesNotContain(venues, v => v.Id == created.Id);
    }
}
