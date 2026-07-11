using System.Net;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class GenreGroupsIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task GetGenreGroups_WithoutToken_ReturnsUnauthorized()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var response = await factory.CreateClient().GetAsync("/api/genre-groups");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetGenreGroups_ReturnsSeededGroups()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var member = await IntegrationAuthHelper.CreateMemberClientAsync(factory);
        var groups = await member.GetFromJsonAsync<List<GenreGroupDto>>("/api/genre-groups");

        Assert.NotNull(groups);
        Assert.True(groups.Count >= 6);
        Assert.Contains(groups, g => g.GroupName == "Rock");
        Assert.Contains(groups, g => g.GroupName == "Christian");
    }

    [SkippableFact]
    public async Task ReplaceGenreGroupGenres_AsAdmin_AssignsGenreMembership()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var (admin, _) = await IntegrationAuthHelper.CreateAdminClientAsync(factory);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var genreId = await PerformanceTestDataHelper.CreateGenreAsync(admin, $"Arena Rock {suffix}");

        var groups = await admin.GetFromJsonAsync<List<GenreGroupDto>>("/api/genre-groups");
        Assert.NotNull(groups);
        var rockGroup = Assert.Single(groups, g => g.GroupName == "Rock");

        var update = await admin.PutAsJsonAsync(
            $"/api/genre-groups/{rockGroup.Id}/genres",
            new UpdateGenreGroupGenresRequest
            {
                Genres =
                [
                    new GenreGroupGenreAssignmentDto { GenreId = genreId, IsPrimary = true }
                ]
            });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var refreshed = await admin.GetFromJsonAsync<List<GenreGroupDto>>("/api/genre-groups");
        Assert.NotNull(refreshed);
        var refreshedRock = Assert.Single(refreshed, g => g.Id == rockGroup.Id);
        var member = Assert.Single(refreshedRock.Genres, g => g.GenreId == genreId);
        Assert.True(member.IsPrimary);
    }

    [SkippableFact]
    public async Task ReplaceGenreGroupGenres_AsMember_ReturnsForbidden()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var member = await IntegrationAuthHelper.CreateMemberClientAsync(factory);
        var response = await member.PutAsJsonAsync(
            "/api/genre-groups/1/genres",
            new UpdateGenreGroupGenresRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
