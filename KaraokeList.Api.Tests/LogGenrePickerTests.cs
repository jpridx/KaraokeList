using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public class LogGenrePickerTests
{
    private static readonly GenreDto[] Genres =
    [
        new() { Id = 1, GenreName = "Rock" },
        new() { Id = 2, GenreName = "Pop" }
    ];

    [Fact]
    public void ResolveGenreId_matches_case_insensitive()
    {
        Assert.Equal(1, LogGenrePicker.ResolveGenreId("rock", Genres));
    }

    [Fact]
    public void ResolveGenreId_returns_null_for_blank_name()
    {
        Assert.Null(LogGenrePicker.ResolveGenreId("  ", Genres));
    }

    [Fact]
    public void NeedsNewGenre_true_when_name_not_in_catalog()
    {
        Assert.True(LogGenrePicker.NeedsNewGenre("Country", Genres));
    }

    [Fact]
    public void NeedsNewGenre_false_when_name_exists()
    {
        Assert.False(LogGenrePicker.NeedsNewGenre("Pop", Genres));
    }
}
