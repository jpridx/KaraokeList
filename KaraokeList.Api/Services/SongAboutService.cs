using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Api.Services;

public interface ISongAboutService
{
    Task<SongAboutDto?> GetAboutAsync(int songId, bool enrich, CancellationToken cancellationToken = default);
}

public sealed class SongAboutService(
    SongCatalogService songCatalogService,
    ApplicationDbContext db) : ISongAboutService
{
    public async Task<SongAboutDto?> GetAboutAsync(
        int songId,
        bool enrich,
        CancellationToken cancellationToken = default)
    {
        var song = await songCatalogService.GetSongDtoAsync(songId, cancellationToken);
        if (song is null)
        {
            return null;
        }

        // Phase 2 handles enrich=true.
        _ = enrich;

        var genreName = await ResolveGenreNameAsync(song.Genre, cancellationToken);
        return ToAboutDto(song, genreName);
    }

    internal static SongAboutDto ToAboutDto(SongDto song, string? genreName)
    {
        var artistNames = song.Artists
            .OrderBy(a => a.DisplayOrder)
            .Select(a => a.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        return new SongAboutDto
        {
            SongId = song.Id,
            Title = song.Title,
            ArtistDisplay = SongArtistFormatting.FormatDisplay(song.ArtistCreditDisplay, artistNames),
            ArtistNames = artistNames,
            Year = song.Year,
            GenreName = string.IsNullOrWhiteSpace(genreName) ? null : genreName.Trim()
        };
    }

    private async Task<string?> ResolveGenreNameAsync(int? genreId, CancellationToken cancellationToken)
    {
        if (genreId is not int id)
        {
            return null;
        }

        return await db.Genres.AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => g.GenreName)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
