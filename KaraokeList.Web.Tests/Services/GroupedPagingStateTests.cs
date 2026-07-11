using KaraokeList.Shared;
using KaraokeList.Web.Services;

namespace KaraokeList.Web.Tests.Services;

public sealed class GroupedPagingStateTests
{
    [Fact]
    public void BuildVisible_returns_all_songs_when_under_page_size()
    {
        var songs = CreateSongs(
            ("Alpha", "Rock"),
            ("Beta", "Rock"),
            ("Gamma", "Pop"));

        var paging = new GroupedPagingState();
        var view = paging.BuildVisible(songs);

        Assert.Equal(3, view.VisibleCount);
        Assert.False(view.HasMore);
        Assert.Equal(["Pop", "Rock"], view.Sections.Select(section => section.Key));
        Assert.Equal(["Gamma"], SectionSongs(view.Sections[0]));
        Assert.Equal(["Alpha", "Beta"], SectionSongs(view.Sections[1]));
    }

    [Fact]
    public void BuildVisible_uses_two_level_grouping_when_resolver_set()
    {
        var songs = CreateSongs(
            ("Alpha", "Classic Rock", 1),
            ("Beta", "Pop", 2));

        var groups = new List<GenreGroupDto>
        {
            new()
            {
                Id = 1,
                GroupName = "Rock",
                SortOrder = 1,
                Genres = [new GenreGroupMemberDto { GenreId = 1, GenreName = "Classic Rock", IsPrimary = true }]
            },
            new()
            {
                Id = 2,
                GroupName = "Pop",
                SortOrder = 2,
                Genres = [new GenreGroupMemberDto { GenreId = 2, GenreName = "Pop", IsPrimary = true }]
            }
        };

        var paging = new GroupedPagingState();
        paging.SetResolver(new GenreGroupResolver(groups));
        var view = paging.BuildVisible(songs);

        Assert.Equal(2, view.Sections.Count);
        Assert.Equal("Rock", view.Sections[0].Key);
        Assert.Equal("Classic Rock", view.Sections[0].SubSections[0].Key);
        Assert.Equal(["Alpha"], view.Sections[0].SubSections[0].Songs.Select(song => song.Title));
        Assert.Equal("Pop", view.Sections[1].Key);
        Assert.Equal(["Beta"], view.Sections[1].SubSections[0].Songs.Select(song => song.Title));
    }

    [Fact]
    public void BuildVisible_puts_unmapped_genres_in_other_section()
    {
        var songs = CreateSongs(("Novelty", "Comedy", 99));
        var groups = new List<GenreGroupDto>
        {
            new() { Id = 1, GroupName = "Rock", SortOrder = 1, Genres = [] }
        };

        var paging = new GroupedPagingState();
        paging.SetResolver(new GenreGroupResolver(groups));
        var view = paging.BuildVisible(songs);

        Assert.Equal(GenreGroupResolver.OtherGroupName, view.Sections[0].Key);
        Assert.Equal("Comedy", view.Sections[0].SubSections[0].Key);
    }

    [Fact]
    public void LoadMore_reveals_additional_grouped_songs()
    {
        var songs = CreateSongs(
            ("A1", "Rock"),
            ("A2", "Rock"),
            ("B1", "Pop"),
            ("B2", "Pop"));

        var paging = new GroupedPagingState();
        paging.Reset(pageSize: 2);

        var firstPage = paging.BuildVisible(songs);
        Assert.Equal(2, firstPage.VisibleCount);
        Assert.True(firstPage.HasMore);

        paging.LoadMore(pageSize: 2);
        var secondPage = paging.BuildVisible(songs);

        Assert.Equal(4, secondPage.VisibleCount);
        Assert.False(secondPage.HasMore);
    }

    [Fact]
    public void Reset_returns_to_first_page()
    {
        var songs = CreateSongs(
            ("A1", "Rock"),
            ("A2", "Rock"),
            ("B1", "Pop"),
            ("B2", "Pop"));

        var paging = new GroupedPagingState();
        paging.Reset(pageSize: 2);
        paging.LoadMore(pageSize: 2);
        paging.Reset(pageSize: 2);

        var view = paging.BuildVisible(songs);

        Assert.Equal(2, view.VisibleCount);
        Assert.True(view.HasMore);
    }

    [Fact]
    public void EnsureSongVisible_expands_limit_to_include_target_song()
    {
        var songs = CreateSongs(
            ("A1", "Rock"),
            ("A2", "Rock"),
            ("B1", "Pop"),
            ("B2", "Pop"),
            ("C1", "Soul"));

        var paging = new GroupedPagingState();
        paging.Reset(pageSize: 2);

        paging.EnsureSongVisible(songId: 5, songs, pageSize: 2);
        var view = paging.BuildVisible(songs);

        Assert.Equal(5, view.VisibleCount);
        Assert.False(view.HasMore);
        Assert.Contains(AllSongs(view), song => song.SongId == 5);
    }

    [Fact]
    public void EnsureSongVisible_does_not_shrink_existing_limit()
    {
        var songs = CreateSongs(
            ("A1", "Rock"),
            ("A2", "Rock"),
            ("B1", "Pop"),
            ("B2", "Pop"));

        var paging = new GroupedPagingState();
        paging.Reset(pageSize: 2);
        paging.LoadMore(pageSize: 2);

        paging.EnsureSongVisible(songId: 1, songs, pageSize: 2);
        var view = paging.BuildVisible(songs);

        Assert.Equal(4, view.VisibleCount);
    }

    [Fact]
    public void RestoreVisibleLimit_sets_limit_without_shrinking_below_default()
    {
        var paging = new GroupedPagingState();
        paging.LoadMore(pageSize: 40);

        paging.RestoreVisibleLimit(80);

        Assert.Equal(80, paging.VisibleLimit);
    }

    [Fact]
    public void RestoreVisibleLimit_never_sets_below_default_page_size()
    {
        var paging = new GroupedPagingState();

        paging.RestoreVisibleLimit(10);

        Assert.Equal(GroupedPagingState.DefaultPageSize, paging.VisibleLimit);
    }

    [Fact]
    public void EnsureSongVisible_expands_limit_for_nested_genre_groups()
    {
        var songs = CreateSongs(
            ("A1", "Classic Rock", 1),
            ("A2", "Classic Rock", 1),
            ("B1", "Pop", 2),
            ("B2", "Pop", 2),
            ("C1", "Soul", 3));

        var groups = new List<GenreGroupDto>
        {
            new()
            {
                Id = 1,
                GroupName = "Rock",
                SortOrder = 1,
                Genres = [new GenreGroupMemberDto { GenreId = 1, GenreName = "Classic Rock", IsPrimary = true }]
            },
            new()
            {
                Id = 2,
                GroupName = "Pop",
                SortOrder = 2,
                Genres = [new GenreGroupMemberDto { GenreId = 2, GenreName = "Pop", IsPrimary = true }]
            },
            new()
            {
                Id = 3,
                GroupName = "Soul",
                SortOrder = 3,
                Genres = [new GenreGroupMemberDto { GenreId = 3, GenreName = "Soul", IsPrimary = true }]
            }
        };

        var paging = new GroupedPagingState();
        paging.SetResolver(new GenreGroupResolver(groups));
        paging.Reset(pageSize: 2);

        paging.EnsureSongVisible(songId: 5, songs, pageSize: 2);
        var view = paging.BuildVisible(songs);

        Assert.Equal(5, view.VisibleCount);
        Assert.Contains(AllSongs(view), song => song.SongId == 5);
    }

    private static IEnumerable<string> SectionSongs(GroupedSongSection section) =>
        section.SubSections.SelectMany(sub => sub.Songs).Select(song => song.Title);

    private static IEnumerable<RepertoireSongDto> AllSongs(GroupedPagingView view) =>
        view.Sections.SelectMany(section => section.SubSections.SelectMany(sub => sub.Songs));

    private static List<RepertoireSongDto> CreateSongs(params (string Title, string Genre)[] entries) =>
        entries.Select((entry, index) => new RepertoireSongDto
        {
            SongId = index + 1,
            Title = entry.Title,
            ArtistName = "Artist",
            GenreName = entry.Genre
        }).ToList();

    private static List<RepertoireSongDto> CreateSongs(params (string Title, string Genre, int GenreId)[] entries) =>
        entries.Select((entry, index) => new RepertoireSongDto
        {
            SongId = index + 1,
            Title = entry.Title,
            ArtistName = "Artist",
            GenreId = entry.GenreId,
            GenreName = entry.Genre
        }).ToList();
}
