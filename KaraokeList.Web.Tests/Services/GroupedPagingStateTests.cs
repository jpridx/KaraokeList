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
        Assert.Equal(["Gamma"], view.Sections[0].Songs.Select(song => song.Title));
        Assert.Equal(["Alpha", "Beta"], view.Sections[1].Songs.Select(song => song.Title));
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
        Assert.Contains(view.Sections.SelectMany(section => section.Songs), song => song.SongId == 5);
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

    private static List<RepertoireSongDto> CreateSongs(params (string Title, string Genre)[] entries) =>
        entries.Select((entry, index) => new RepertoireSongDto
        {
            SongId = index + 1,
            Title = entry.Title,
            ArtistName = "Artist",
            GenreName = entry.Genre
        }).ToList();
}
