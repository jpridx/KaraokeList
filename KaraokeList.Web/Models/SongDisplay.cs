using KaraokeList.Shared;

namespace KaraokeList.Web.Models;

public sealed class SongDisplay : SongDto
{
    public string GenreName { get; set; } = string.Empty;

    public string ArtistDisplay =>
        SongArtistFormatting.FormatDisplay(
            ArtistCreditDisplay,
            Artists.OrderBy(a => a.DisplayOrder).Select(a => a.Name));

    public string PrimaryArtistName =>
        SongArtistFormatting.PrimaryArtistName(Artists);
}
