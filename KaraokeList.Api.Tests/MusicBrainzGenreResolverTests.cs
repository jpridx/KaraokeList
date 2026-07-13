using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public class MusicBrainzGenreResolverTests
{
    [Theory]
    [InlineData("classic rock", "Classic Rock")]
    [InlineData("Arena Rock", "Arena Rock")]
    [InlineData("synthpop", "Synth-Pop")]
    [InlineData("rhythm and blues", "R&B")]
    public void MapToCatalogGenre_maps_known_labels(string input, string expected) =>
        Assert.Equal(expected, MusicBrainzGenreResolver.MapToCatalogGenre(input));

    [Fact]
    public void ResolveBestGenre_prefers_specific_subgenre_over_generic_rock()
    {
        var candidates = new[]
        {
            ("rock", 14),
            ("classic rock", 7),
            ("pop", 3)
        };

        Assert.Equal("Classic Rock", MusicBrainzGenreResolver.ResolveBestGenre(candidates));
    }

    [Theory]
    [InlineData("1981-07-17", 1981)]
    [InlineData("1981", 1981)]
    [InlineData("", null)]
    [InlineData("nineteen eighty-one", null)]
    public void ParseReleaseYear_extracts_four_digit_year(string? input, int? expected) =>
        Assert.Equal(expected, MusicBrainzGenreResolver.ParseReleaseYear(input));
}
