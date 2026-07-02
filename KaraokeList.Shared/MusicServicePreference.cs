using System.ComponentModel.DataAnnotations;

namespace KaraokeList.Shared;

public enum MusicService
{
    None = 0,
    Spotify = 1,
    AppleMusic = 2,
    YouTubeMusic = 3,
    AmazonMusic = 4,
    Tidal = 5,
    Deezer = 6
}

public class MusicServicePreferenceDto
{
    public MusicService Service { get; set; } = MusicService.None;
}

public class UpdateMusicServicePreferenceRequest
{
    [EnumDataType(typeof(MusicService))]
    public MusicService Service { get; set; } = MusicService.None;
}

public static class MusicServiceCatalog
{
    public static IReadOnlyList<MusicService> SelectableServices { get; } =
    [
        MusicService.Spotify,
        MusicService.AppleMusic,
        MusicService.YouTubeMusic,
        MusicService.AmazonMusic,
        MusicService.Tidal,
        MusicService.Deezer
    ];

    public static string GetDisplayName(MusicService service) => service switch
    {
        MusicService.Spotify => "Spotify",
        MusicService.AppleMusic => "Apple Music",
        MusicService.YouTubeMusic => "YouTube Music",
        MusicService.AmazonMusic => "Amazon Music",
        MusicService.Tidal => "Tidal",
        MusicService.Deezer => "Deezer",
        _ => "None"
    };
}

public static class MusicServiceLinks
{
    public static string BuildSearchQuery(string artistName, string title)
    {
        var artist = artistName?.Trim() ?? string.Empty;
        var songTitle = title?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(artist))
        {
            return songTitle;
        }

        if (string.IsNullOrEmpty(songTitle))
        {
            return artist;
        }

        return $"{artist} {songTitle}";
    }

    public static string? BuildSearchUrl(MusicService service, string artistName, string title)
    {
        var query = BuildSearchQuery(artistName, title);
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var encoded = Uri.EscapeDataString(query);

        return service switch
        {
            MusicService.Spotify => $"https://open.spotify.com/search/{encoded}",
            MusicService.AppleMusic => $"https://music.apple.com/search?term={encoded}",
            MusicService.YouTubeMusic => $"https://music.youtube.com/search?q={encoded}",
            MusicService.AmazonMusic => $"https://music.amazon.com/search/{encoded}",
            MusicService.Tidal => $"https://listen.tidal.com/search?q={encoded}",
            MusicService.Deezer => $"https://www.deezer.com/search/{encoded}",
            _ => null
        };
    }
}
