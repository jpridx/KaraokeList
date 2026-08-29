namespace KaraokeList.Shared;

public static class SongMatchKey
{
    public static string Make(string title, string artist) =>
        $"{title.Trim().ToLowerInvariant()}|{artist.Trim().ToLowerInvariant()}";
}
