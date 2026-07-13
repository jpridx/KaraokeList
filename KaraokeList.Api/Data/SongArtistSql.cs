namespace KaraokeList.Data;

public static class SongArtistSql
{
    public const string PrimaryArtistJoin = """
        LEFT JOIN SongArtists sa ON sa.SongId = s.Id AND sa.DisplayOrder = 0
        LEFT JOIN Artists a ON a.Id = sa.ArtistId
        """;

    public const string PrimaryArtistName = "ISNULL(a.Name, N'')";

    public const string ArtistDisplay = "COALESCE(NULLIF(s.ArtistCreditDisplay, N''), ISNULL(a.Name, N''))";
}
