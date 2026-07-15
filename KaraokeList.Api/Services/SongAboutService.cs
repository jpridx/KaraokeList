using KaraokeList.Data;
using KaraokeList.Shared;

namespace KaraokeList.Api.Services;

public interface ISongAboutService
{
    Task<SongAboutDto?> GetAboutAsync(int songId, bool enrich, CancellationToken cancellationToken = default);
}

public sealed class SongAboutService(SongCatalogService songCatalogService) : ISongAboutService
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

        // Scaffold: title only. Phase 1 fills catalog fields; phase 2 handles enrich=true.
        _ = enrich;

        return new SongAboutDto
        {
            SongId = song.Id,
            Title = song.Title
        };
    }
}
