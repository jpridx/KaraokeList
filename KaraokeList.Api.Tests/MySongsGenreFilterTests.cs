using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public sealed class MySongsGenreFilterTests
{
    private static readonly IReadOnlyList<GenreGroupDto> SampleGroups =
    [
        new()
        {
            GroupName = "Rock",
            SortOrder = 1,
            Genres =
            [
                new GenreGroupMemberDto { GenreId = 10, GenreName = "Classic Rock", IsPrimary = true },
                new GenreGroupMemberDto { GenreId = 11, GenreName = "Hair Metal", IsPrimary = true }
            ]
        },
        new()
        {
            GroupName = "Pop",
            SortOrder = 2,
            Genres = [new GenreGroupMemberDto { GenreId = 20, GenreName = "Pop Rock", IsPrimary = true }]
        }
    ];

    [Fact]
    public void BuildFilterGroups_returns_only_groups_with_songs_in_sort_order()
    {
        var songs = new List<RepertoireSongDto>
        {
            new() { SongId = 1, GenreId = 10, GenreName = "Classic Rock" },
            new() { SongId = 2, GenreId = 20, GenreName = "Pop Rock" }
        };

        var groups = MySongsGenreFilter.BuildFilterGroups(songs, SampleGroups);

        Assert.Equal(["Rock", "Pop"], groups);
    }

    [Fact]
    public void BuildFilterGroups_includes_other_for_unmapped_genre()
    {
        var songs = new List<RepertoireSongDto>
        {
            new() { SongId = 1, GenreId = 99, GenreName = "Comedy" }
        };

        var groups = MySongsGenreFilter.BuildFilterGroups(songs, SampleGroups);

        Assert.Single(groups);
        Assert.Equal(GenreGroupResolver.OtherGroupName, groups[0]);
    }

    [Fact]
    public void BuildFilterGroups_returns_empty_when_no_genre_groups()
    {
        var songs = new List<RepertoireSongDto>
        {
            new() { SongId = 1, GenreId = 10, GenreName = "Classic Rock" }
        };

        Assert.Empty(MySongsGenreFilter.BuildFilterGroups(songs, []));
    }

    [Fact]
    public void ApplyGroupFilter_uses_primary_group_assignment()
    {
        var songs = new List<RepertoireSongDto>
        {
            new() { SongId = 1, GenreId = 10, GenreName = "Classic Rock" },
            new() { SongId = 2, GenreId = 20, GenreName = "Pop Rock" }
        };

        var filtered = MySongsGenreFilter.ApplyGroupFilter(songs, "Rock", SampleGroups);

        Assert.Single(filtered);
        Assert.Equal(1, filtered[0].SongId);
    }

    [Fact]
    public void BuildFilterGenres_scopes_to_active_group()
    {
        var songs = new List<RepertoireSongDto>
        {
            new() { SongId = 1, GenreId = 10, GenreName = "Classic Rock" },
            new() { SongId = 2, GenreId = 11, GenreName = "Hair Metal" },
            new() { SongId = 3, GenreId = 20, GenreName = "Pop Rock" }
        };

        var genres = MySongsGenreFilter.BuildFilterGenres(songs, SampleGroups, scopedGroupName: "Rock");

        Assert.Equal(2, genres.Count);
        Assert.Contains(genres, g => g.GenreName == "Classic Rock");
        Assert.Contains(genres, g => g.GenreName == "Hair Metal");
        Assert.DoesNotContain(genres, g => g.GenreName == "Pop Rock");
    }
}
