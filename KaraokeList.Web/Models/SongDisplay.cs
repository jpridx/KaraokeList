using KaraokeList.Shared;

namespace KaraokeList.Web.Models;

public sealed class SongDisplay : SongDto
{
    public string ArtistName { get; set; } = string.Empty;
    public string GenreName { get; set; } = string.Empty;
    public string SecondaryArtistName { get; set; } = string.Empty;
}
