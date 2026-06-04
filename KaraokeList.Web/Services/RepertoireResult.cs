using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed class RepertoireResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public List<RepertoireSongDto> Songs { get; init; } = [];

    public static RepertoireResult Ok(List<RepertoireSongDto> songs) =>
        new() { Succeeded = true, Songs = songs };

    public static RepertoireResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}

public sealed class RepertoireGenresResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public List<GenreDto> Genres { get; init; } = [];

    public static RepertoireGenresResult Ok(List<GenreDto> genres) =>
        new() { Succeeded = true, Genres = genres };

    public static RepertoireGenresResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
