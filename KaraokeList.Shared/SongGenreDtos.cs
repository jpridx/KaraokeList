namespace KaraokeList.Shared;

public class UpdateSongGenreRequest
{
    public int? GenreId { get; set; }
}

public sealed class SongGenreUpdateResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }

    public static SongGenreUpdateResult Ok() => new() { Succeeded = true };

    public static SongGenreUpdateResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
