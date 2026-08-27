using KaraokeList.Shared;
using KaraokeList.Web.Services;
using Moq;

namespace KaraokeList.Web.Tests.Services;

public sealed class SingerListResolverTests
{
    [Fact]
    public void FindList_returns_matching_list()
    {
        var lists = new List<SingerListDto>
        {
            new() { Id = 1, Kind = SingerListKind.MyRepertoire, DisplayName = "My repertoire" },
            new() { Id = 2, Kind = SingerListKind.WorkingUp, DisplayName = "Working up" }
        };

        var workingUp = SingerListResolver.FindList(lists, SingerListKind.WorkingUp);

        Assert.NotNull(workingUp);
        Assert.Equal(2, workingUp.Id);
    }

    [Fact]
    public async Task LoadListsAsync_returns_error_when_api_fails()
    {
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetMyListsAsync())
            .ReturnsAsync(SingerListsResult.Fail("offline"));

        var result = await SingerListResolver.LoadListsAsync(api.Object);

        Assert.False(result.Succeeded);
        Assert.Equal("offline", result.ErrorMessage);
    }
}

public sealed class SingerListActionsTests
{
    [Fact]
    public async Task AddSongAsync_returns_error_when_list_missing()
    {
        var api = new Mock<IKaraokeApiClient>();

        var result = await SingerListActions.AddSongAsync(
            api.Object,
            [],
            SingerListKind.WorkingUp,
            songId: 5);

        Assert.False(result.Succeeded);
        Assert.Contains("Working up", result.ErrorMessage);
        api.Verify(client => client.AddListSongAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task AddSongAsync_calls_api_when_list_found()
    {
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.AddListSongAsync(3, 5, It.IsAny<bool>()))
            .ReturnsAsync(ListSongActionResult.Ok());

        var lists = new List<SingerListDto>
        {
            new() { Id = 3, Kind = SingerListKind.WorkingUp, DisplayName = "Working up" }
        };

        var result = await SingerListActions.AddSongAsync(
            api.Object,
            lists,
            SingerListKind.WorkingUp,
            songId: 5);

        Assert.True(result.Succeeded);
        Assert.Contains("working up", result.SuccessMessage);
    }

    [Fact]
    public async Task CheckTitleArtistCollisionsAsync_returns_collision_when_api_finds_match()
    {
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetTitleArtistCollisionAsync(3, 5))
            .ReturnsAsync(TitleArtistCollisionResult.Found(new TitleArtistCollisionDto
            {
                ExistingSongId = 9,
                Title = "Jeopardy",
                ArtistName = "The Greg Kihn Band"
            }));

        var lists = new List<SingerListDto>
        {
            new() { Id = 3, Kind = SingerListKind.WorkingUp, DisplayName = "Working up" }
        };

        var result = await SingerListActions.CheckTitleArtistCollisionsAsync(
            api.Object,
            lists,
            songId: 5,
            [SingerListKind.WorkingUp]);

        Assert.True(result.Succeeded);
        Assert.True(result.HasCollisions);
        Assert.Equal(SingerListKind.WorkingUp, result.Collisions[0].Kind);
        Assert.Equal("Jeopardy", result.Collisions[0].Collision.Title);
    }
}
