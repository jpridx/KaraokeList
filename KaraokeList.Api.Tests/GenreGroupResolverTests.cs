using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public sealed class GenreGroupResolverTests
{
    [Fact]
    public void ResolvePrimaryGroupName_uses_primary_membership()
    {
        var groups = new List<GenreGroupDto>
        {
            new()
            {
                GroupName = "Rock",
                SortOrder = 1,
                Genres = [new GenreGroupMemberDto { GenreId = 10, GenreName = "Country Rock", IsPrimary = true }]
            },
            new()
            {
                GroupName = "Country",
                SortOrder = 3,
                Genres = [new GenreGroupMemberDto { GenreId = 10, GenreName = "Country Rock", IsPrimary = false }]
            }
        };

        var resolver = new GenreGroupResolver(groups);
        var song = new RepertoireSongDto { GenreId = 10, GenreName = "Country Rock" };

        Assert.Equal("Rock", resolver.ResolvePrimaryGroupName(song));
    }

    [Fact]
    public void ResolvePrimaryGroupName_falls_back_to_first_group_when_no_primary_flag()
    {
        var groups = new List<GenreGroupDto>
        {
            new()
            {
                GroupName = "Country",
                SortOrder = 3,
                Genres = [new GenreGroupMemberDto { GenreId = 10, GenreName = "Country Rock", IsPrimary = false }]
            },
            new()
            {
                GroupName = "Rock",
                SortOrder = 1,
                Genres = [new GenreGroupMemberDto { GenreId = 10, GenreName = "Country Rock", IsPrimary = false }]
            }
        };

        var resolver = new GenreGroupResolver(groups);
        var song = new RepertoireSongDto { GenreId = 10, GenreName = "Country Rock" };

        Assert.Equal("Rock", resolver.ResolvePrimaryGroupName(song));
    }

    [Fact]
    public void ResolvePrimaryGroupName_returns_other_for_unmapped_genre()
    {
        var resolver = new GenreGroupResolver([]);
        var song = new RepertoireSongDto { GenreId = 99, GenreName = "Comedy" };

        Assert.Equal(GenreGroupResolver.OtherGroupName, resolver.ResolvePrimaryGroupName(song));
    }
}
