using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record LocalStaleSongsResult(
    StaleSongsResponseDto? Response,
    bool HasSourceData,
    DateTime? SourceCachedAtUtc,
    bool FromLogCache);

public interface ILocalStaleSongsProvider
{
    Task<LocalStaleSongsResult> ComputeAsync(DateTime? asOfDate = null, Random? random = null);
}

public sealed class LocalStaleSongsProvider(
    ILogPerformanceLocalStore logStore,
    IMySongsLocalStore mySongsStore,
    ITicklerSettingsLocalStore settingsStore,
    ITicklerExclusionsLocalStore exclusionsStore) : ILocalStaleSongsProvider
{
    public async Task<LocalStaleSongsResult> ComputeAsync(DateTime? asOfDate = null, Random? random = null)
    {
        var today = PerformanceRelativeDate.ResolveAsOfDate(asOfDate);
        var repertoire = await TryGetRepertoireAsync();
        if (repertoire.Songs.Count == 0)
        {
            return new LocalStaleSongsResult(null, false, repertoire.CachedAtUtc, repertoire.FromLogCache);
        }

        var settings = await settingsStore.GetAsync();
        var exclusions = await exclusionsStore.GetExcludedSongIdsAsync();
        var response = StaleSongsComputer.Compute(repertoire.Songs, exclusions, settings, today, random);
        return new LocalStaleSongsResult(response, true, repertoire.CachedAtUtc, repertoire.FromLogCache);
    }

    private async Task<(List<RepertoireSongDto> Songs, DateTime? CachedAtUtc, bool FromLogCache)> TryGetRepertoireAsync()
    {
        var logCache = await logStore.GetCachedCatalogAsync();
        if (logCache?.RepertoireStats is { Count: > 0 } stats)
        {
            return (MapStatsToRepertoire(stats), logCache.CachedAtUtc, true);
        }

        var mySongsCache = await mySongsStore.GetCachedListsAsync();
        if (mySongsCache is not null)
        {
            var repertoireEntry = mySongsCache.ListsSongs
                .FirstOrDefault(entry => entry.Kind == SingerListKind.MyRepertoire);
            if (repertoireEntry is not null && repertoireEntry.Songs.Count > 0)
            {
                return (repertoireEntry.Songs.ToList(), mySongsCache.CachedAtUtc, false);
            }
        }

        return ([], null, false);
    }

    private static List<RepertoireSongDto> MapStatsToRepertoire(IReadOnlyList<CachedRepertoireStatsEntry> stats) =>
        stats.Select(s => new RepertoireSongDto
        {
            SongId = s.SongId,
            Title = s.Title,
            ArtistName = s.ArtistName,
            ArtistDisplay = s.ArtistDisplay,
            LastPerformedOn = s.LastPerformedOn,
            PerformanceCount = s.PerformanceCount
        }).ToList();
}
