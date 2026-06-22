namespace KaraokeList.Shared;

public static class LogArtistPicker
{
    public static int? ResolveArtistId(string? artistName, IEnumerable<ArtistLookupDto> lookups)
    {
        if (string.IsNullOrWhiteSpace(artistName))
        {
            return null;
        }

        var trimmed = artistName.Trim();
        return lookups.FirstOrDefault(a =>
            a.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    public static bool NeedsNewArtist(string? artistName, IEnumerable<ArtistLookupDto> lookups) =>
        !string.IsNullOrWhiteSpace(artistName) && ResolveArtistId(artistName, lookups) is null;

    public static T? FindCreatedSong<T>(
        IEnumerable<T> songs,
        string title,
        string artistName,
        Func<T, string> getTitle,
        Func<T, string> getArtistName) =>
        songs.FirstOrDefault(s =>
            getTitle(s).Equals(title.Trim(), StringComparison.OrdinalIgnoreCase)
            && getArtistName(s).Equals(artistName.Trim(), StringComparison.OrdinalIgnoreCase));
}
