using System.Net;
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
    public async Task Member_CanCreateAndListVenues_ButCannotUpdateOrDelete()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await IntegrationAuthHelper.CreateMemberClientAsync(factory);

        var venueName = $"Test Venue {Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/api/venues", new VenueDto { VenueName = venueName });
        Assert.Equal(HttpStatusCode.NoContent, createResponse.StatusCode);

        var venues = await client.GetFromJsonAsync<List<VenueDto>>("/api/venues");
        Assert.NotNull(venues);
        var created = Assert.Single(venues, v => v.VenueName == venueName);
        Assert.True(created.Id > 0);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/venues/{created.Id}",
            new VenueDto { Id = created.Id, VenueName = $"{venueName} Updated" });
        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/venues/{created.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [SkippableFact]
    public async Task Admin_CanUpdateAndDeleteVenues()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var member = await IntegrationAuthHelper.CreateMemberClientAsync(factory);
        var venueName = $"Admin Venue {Guid.NewGuid():N}";
        var createResponse = await member.PostAsJsonAsync("/api/venues", new VenueDto { VenueName = venueName });
        Assert.Equal(HttpStatusCode.NoContent, createResponse.StatusCode);

        var venues = await member.GetFromJsonAsync<List<VenueDto>>("/api/venues");
        Assert.NotNull(venues);
        var created = Assert.Single(venues, v => v.VenueName == venueName);

        var (admin, _) = await IntegrationAuthHelper.CreateAdminClientAsync(factory);
        var updatedName = $"{venueName} Updated";
        var updateResponse = await admin.PutAsJsonAsync(
            $"/api/venues/{created.Id}",
            new VenueDto { Id = created.Id, VenueName = updatedName });
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        venues = await admin.GetFromJsonAsync<List<VenueDto>>("/api/venues");
        Assert.NotNull(venues);
        Assert.Contains(venues, v => v.Id == created.Id && v.VenueName == updatedName);

        var deleteResponse = await admin.DeleteAsync($"/api/venues/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        venues = await admin.GetFromJsonAsync<List<VenueDto>>("/api/venues");
        Assert.NotNull(venues);
        Assert.DoesNotContain(venues, v => v.Id == created.Id);
    }
}
