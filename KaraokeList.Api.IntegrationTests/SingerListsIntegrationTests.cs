using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class SingerListsIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task Register_CreatesThreeSystemLists()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await client.GetFromJsonAsync<List<SingerListDto>>("/api/singers/me/lists");

        Assert.NotNull(lists);
        Assert.Equal(3, lists.Count);
        Assert.Contains(lists, l => l.Kind == SingerListKind.MyRepertoire);
        Assert.Contains(lists, l => l.Kind == SingerListKind.WantToSing);
        Assert.Contains(lists, l => l.Kind == SingerListKind.WorkingUp);
    }

    [SkippableFact]
    public async Task CreatePerformance_AutoAddsSongToMyRepertoireList()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await GetListsAsync(client);
        var repertoireListId = lists.Single(l => l.Kind == SingerListKind.MyRepertoire).Id;

        var (songId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var songsBefore = await client.GetFromJsonAsync<List<RepertoireSongDto>>(
            $"/api/singers/me/lists/{repertoireListId}/songs");
        Assert.NotNull(songsBefore);
        Assert.DoesNotContain(songsBefore, s => s.SongId == songId);

        await PerformanceTestDataHelper.CreatePerformanceAsync(client, songId, venueId);

        var songsAfter = await client.GetFromJsonAsync<List<RepertoireSongDto>>(
            $"/api/singers/me/lists/{repertoireListId}/songs");
        Assert.NotNull(songsAfter);
        Assert.Contains(songsAfter, s => s.SongId == songId);
        Assert.Equal(1, Assert.Single(songsAfter, s => s.SongId == songId).PerformanceCount);
    }

    [SkippableFact]
    public async Task AddToWantToSing_RejectsPerformedSong()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await GetListsAsync(client);
        var wantListId = lists.Single(l => l.Kind == SingerListKind.WantToSing).Id;

        var (songId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        await PerformanceTestDataHelper.CreatePerformanceAsync(client, songId, venueId);

        var response = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{wantListId}/songs",
            new AddSingerListSongRequest { SongId = songId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("not performed", body, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task AddToWantToSing_RejectsSongOnMyRepertoire()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await GetListsAsync(client);
        var repertoireListId = lists.Single(l => l.Kind == SingerListKind.MyRepertoire).Id;
        var wantListId = lists.Single(l => l.Kind == SingerListKind.WantToSing).Id;

        var (songId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var addToRepertoire = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{repertoireListId}/songs",
            new AddSingerListSongRequest { SongId = songId });
        Assert.Equal(HttpStatusCode.NoContent, addToRepertoire.StatusCode);

        var response = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{wantListId}/songs",
            new AddSingerListSongRequest { SongId = songId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("My repertoire", body, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task AddToWantToSing_AllowsUnperformedSongNotOnRepertoire()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await GetListsAsync(client);
        var wantListId = lists.Single(l => l.Kind == SingerListKind.WantToSing).Id;

        var (songId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var response = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{wantListId}/songs",
            new AddSingerListSongRequest { SongId = songId });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var songs = await client.GetFromJsonAsync<List<RepertoireSongDto>>(
            $"/api/singers/me/lists/{wantListId}/songs");
        Assert.NotNull(songs);
        Assert.Contains(songs, s => s.SongId == songId);
        Assert.Equal(0, Assert.Single(songs, s => s.SongId == songId).PerformanceCount);
    }

    [SkippableFact]
    public async Task RemoveSong_FromList_ReturnsNoContent()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await GetListsAsync(client);
        var workingUpListId = lists.Single(l => l.Kind == SingerListKind.WorkingUp).Id;

        var (songId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var add = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{workingUpListId}/songs",
            new AddSingerListSongRequest { SongId = songId });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        var remove = await client.DeleteAsync($"/api/singers/me/lists/{workingUpListId}/songs/{songId}");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);

        var songs = await client.GetFromJsonAsync<List<RepertoireSongDto>>(
            $"/api/singers/me/lists/{workingUpListId}/songs");
        Assert.NotNull(songs);
        Assert.DoesNotContain(songs, s => s.SongId == songId);
    }

    [SkippableFact]
    public async Task GetSongListMembership_ReturnsListsContainingSong()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await GetListsAsync(client);
        var workingUpListId = lists.Single(l => l.Kind == SingerListKind.WorkingUp).Id;

        var (songId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var add = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{workingUpListId}/songs",
            new AddSingerListSongRequest { SongId = songId });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        var membership = await client.GetFromJsonAsync<SongListMembershipDto>(
            $"/api/singers/me/songs/{songId}/list-membership");
        Assert.NotNull(membership);
        Assert.Contains(SingerListKind.WorkingUp, membership.Lists);
    }

    [SkippableFact]
    public async Task CreatePerformance_RemovesSongFromWantToSing()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await GetListsAsync(client);
        var wantListId = lists.Single(l => l.Kind == SingerListKind.WantToSing).Id;
        var repertoireListId = lists.Single(l => l.Kind == SingerListKind.MyRepertoire).Id;

        var (songId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var add = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{wantListId}/songs",
            new AddSingerListSongRequest { SongId = songId });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);

        await PerformanceTestDataHelper.CreatePerformanceAsync(client, songId, venueId);

        var wantSongs = await client.GetFromJsonAsync<List<RepertoireSongDto>>(
            $"/api/singers/me/lists/{wantListId}/songs");
        Assert.NotNull(wantSongs);
        Assert.DoesNotContain(wantSongs, s => s.SongId == songId);

        var repertoireSongs = await client.GetFromJsonAsync<List<RepertoireSongDto>>(
            $"/api/singers/me/lists/{repertoireListId}/songs");
        Assert.NotNull(repertoireSongs);
        Assert.Contains(repertoireSongs, s => s.SongId == songId);
    }

    [SkippableFact]
    public async Task AddSong_BlocksTitleArtistDuplicateWithoutOverride()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await GetListsAsync(client);
        var workingUpListId = lists.Single(l => l.Kind == SingerListKind.WorkingUp).Id;

        var (firstSongId, secondSongId) = await CreateDuplicateTitleArtistSongsAsync(client);
        var addFirst = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{workingUpListId}/songs",
            new AddSingerListSongRequest { SongId = firstSongId });
        Assert.Equal(HttpStatusCode.NoContent, addFirst.StatusCode);

        var addSecond = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{workingUpListId}/songs",
            new AddSingerListSongRequest { SongId = secondSongId });
        Assert.Equal(HttpStatusCode.Conflict, addSecond.StatusCode);
    }

    [SkippableFact]
    public async Task GetTitleArtistCollision_ReturnsExistingSong()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await GetListsAsync(client);
        var workingUpListId = lists.Single(l => l.Kind == SingerListKind.WorkingUp).Id;

        var (firstSongId, secondSongId) = await CreateDuplicateTitleArtistSongsAsync(client);
        var addFirst = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{workingUpListId}/songs",
            new AddSingerListSongRequest { SongId = firstSongId });
        Assert.Equal(HttpStatusCode.NoContent, addFirst.StatusCode);

        var response = await client.GetAsync(
            $"/api/singers/me/lists/{workingUpListId}/songs/title-artist-collision?songId={secondSongId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var collision = await response.Content.ReadFromJsonAsync<TitleArtistCollisionDto>();
        Assert.NotNull(collision);
        Assert.Equal(firstSongId, collision!.ExistingSongId);
    }

    [SkippableFact]
    public async Task AddSong_AllowsTitleArtistDuplicateWhenOverrideSet()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await GetListsAsync(client);
        var workingUpListId = lists.Single(l => l.Kind == SingerListKind.WorkingUp).Id;

        var (firstSongId, secondSongId) = await CreateDuplicateTitleArtistSongsAsync(client);
        var addFirst = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{workingUpListId}/songs",
            new AddSingerListSongRequest { SongId = firstSongId });
        Assert.Equal(HttpStatusCode.NoContent, addFirst.StatusCode);

        var addSecond = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{workingUpListId}/songs",
            new AddSingerListSongRequest
            {
                SongId = secondSongId,
                AllowTitleArtistDuplicate = true
            });
        Assert.Equal(HttpStatusCode.NoContent, addSecond.StatusCode);

        var songs = await client.GetFromJsonAsync<List<RepertoireSongDto>>(
            $"/api/singers/me/lists/{workingUpListId}/songs");
        Assert.NotNull(songs);
        Assert.Contains(songs, s => s.SongId == firstSongId);
        Assert.Contains(songs, s => s.SongId == secondSongId);
    }

    [SkippableFact]
    public async Task ImportListSongs_SkipsTitleArtistDuplicate()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var (firstSongId, secondSongId) = await CreateDuplicateTitleArtistSongsAsync(client);

        var addFirst = await client.PostAsJsonAsync(
            "/api/singers/me/lists/import",
            new ImportSingerListSongsRequest
            {
                ListKind = SingerListKind.WorkingUp,
                SongIds = [firstSongId]
            });
        Assert.Equal(HttpStatusCode.OK, addFirst.StatusCode);

        var importSecond = await client.PostAsJsonAsync(
            "/api/singers/me/lists/import",
            new ImportSingerListSongsRequest
            {
                ListKind = SingerListKind.WorkingUp,
                SongIds = [secondSongId]
            });
        Assert.Equal(HttpStatusCode.OK, importSecond.StatusCode);
        var body = await importSecond.Content.ReadFromJsonAsync<ImportSingerListSongsResponse>();
        Assert.NotNull(body);
        Assert.Equal(0, body!.Added);
        Assert.Equal(1, body.Skipped);
    }

    [SkippableFact]
    public async Task GetListSongs_SortByLastPerformedDesc_PutsUnperformedSongsLast()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var client = await CreateAuthedClientAsync();
        var lists = await GetListsAsync(client);
        var repertoireListId = lists.Single(l => l.Kind == SingerListKind.MyRepertoire).Id;

        var (recentSongId, venueId) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var (olderSongId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);
        var (unperformedSongId, _) = await PerformanceTestDataHelper.CreateCatalogAsync(client);

        await PerformanceTestDataHelper.CreatePerformanceAsync(
            client, olderSongId, venueId, DateTime.Today.AddDays(-30));
        await PerformanceTestDataHelper.CreatePerformanceAsync(
            client, recentSongId, venueId, DateTime.Today.AddDays(-1));

        var addUnperformed = await client.PostAsJsonAsync(
            $"/api/singers/me/lists/{repertoireListId}/songs",
            new AddSingerListSongRequest { SongId = unperformedSongId });
        Assert.Equal(HttpStatusCode.NoContent, addUnperformed.StatusCode);

        var songs = await client.GetFromJsonAsync<List<RepertoireSongDto>>(
            $"/api/singers/me/lists/{repertoireListId}/songs?sortBy=lastPerformed&sortDir=desc");

        Assert.NotNull(songs);
        var orderedIds = songs.Select(s => s.SongId).ToList();
        Assert.Equal(recentSongId, orderedIds[0]);
        Assert.Equal(olderSongId, orderedIds[1]);
        Assert.Equal(unperformedSongId, orderedIds[^1]);
        Assert.Null(songs.Single(s => s.SongId == unperformedSongId).LastPerformedOn);
    }

    private static async Task<List<SingerListDto>> GetListsAsync(HttpClient client)
    {
        var lists = await client.GetFromJsonAsync<List<SingerListDto>>("/api/singers/me/lists");
        Assert.NotNull(lists);
        return lists;
    }

    private static async Task<(int FirstSongId, int SecondSongId)> CreateDuplicateTitleArtistSongsAsync(HttpClient client)
    {
        var artistName = $"Artist {Guid.NewGuid():N}";
        var createArtist = await client.PostAsJsonAsync("/api/artists", new ArtistDto { Name = artistName });
        Assert.Equal(HttpStatusCode.NoContent, createArtist.StatusCode);

        var artists = await client.GetFromJsonAsync<List<ArtistDto>>("/api/artists");
        Assert.NotNull(artists);
        var artistId = Assert.Single(artists, a => a.Name == artistName).Id;

        var songTitle = $"Song {Guid.NewGuid():N}";
        var credits = new List<SongArtistDto>
        {
            new() { ArtistId = artistId, DisplayOrder = 0, Name = artistName }
        };

        var createFirst = await client.PostAsJsonAsync("/api/songs", new SongDto
        {
            Title = songTitle,
            Artists = credits
        });
        Assert.Equal(HttpStatusCode.Created, createFirst.StatusCode);
        var firstSong = await createFirst.Content.ReadFromJsonAsync<SongDto>();
        Assert.NotNull(firstSong);

        var createSecond = await client.PostAsJsonAsync("/api/songs", new SongDto
        {
            Title = songTitle,
            Artists = credits
        });
        Assert.Equal(HttpStatusCode.Created, createSecond.StatusCode);
        var secondSong = await createSecond.Content.ReadFromJsonAsync<SongDto>();
        Assert.NotNull(secondSong);

        return (firstSong!.Id, secondSong!.Id);
    }

    private async Task<HttpClient> CreateAuthedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"lists-{Guid.NewGuid():N}@example.com";
        var token = await IntegrationAuthHelper.RegisterAndGetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
