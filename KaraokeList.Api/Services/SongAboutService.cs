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
    ApplicationDbContext db,
    IMusicBrainzService musicBrainzService) : ISongAboutService
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

        var genreName = await ResolveGenreNameAsync(song.Genre, cancellationToken);
        var about = ToAboutDto(song, genreName);

        if (enrich)
        {
            about.Enrichment = await ResolveEnrichmentAsync(song, cancellationToken);
        }

        return about;
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

    private async Task<SongAboutEnrichmentDto?> ResolveEnrichmentAsync(
        SongDto song,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(song.RecordingMbid))
        {
            return await musicBrainzService.GetRecordingEnrichmentAsync(song.RecordingMbid, cancellationToken);
        }

        var primaryArtist = SongArtistFormatting.PrimaryArtistName(song.Artists);
        if (string.IsNullOrWhiteSpace(primaryArtist))
        {
            return null;
        }

        var lookup = await musicBrainzService.LookupAsync(song.Title, primaryArtist, cancellationToken);
        var recordingMbid = lookup.Match.RecordingMbid;
        if (string.IsNullOrWhiteSpace(recordingMbid))
        {
            return null;
        }

        return await musicBrainzService.GetRecordingEnrichmentAsync(recordingMbid, cancellationToken);
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
