namespace KaraokeList.Data;

public sealed class SongGenreService(SongService songService, CatalogIntegrityService integrity)
{
    public async Task<(bool Succeeded, string? Error)> UpdateGenreAsync(int songId, int? genreId)
    {
        if (!await integrity.SongExistsAsync(songId))
        {
            return (false, "Song was not found.");
        }

        if (genreId is int id && !await integrity.GenreExistsAsync(id))
        {
            return (false, "Genre was not found.");
        }

        var updated = await songService.UpdateSongGenreAsync(songId, genreId);
        return updated
            ? (true, null)
            : (false, "Song was not found.");
    }
}
