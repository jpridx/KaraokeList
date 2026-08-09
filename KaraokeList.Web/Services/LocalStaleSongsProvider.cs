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
    private sealed record RepertoireSource(
        List<RepertoireSongDto> Songs,
        DateTime? CachedAtUtc,
        bool FromLogCache);

    public async Task<LocalStaleSongsResult> ComputeAsync(DateTime? asOfDate = null, Random? random = null)
    {
        var today = PerformanceRelativeDate.ResolveAsOfDate(asOfDate);
        var logSource = await TryGetLogRepertoireAsync();
        var mySongsSource = await TryGetMySongsRepertoireAsync();

        if (logSource.Songs.Count == 0 && mySongsSource.Songs.Count == 0)
        {
            return new LocalStaleSongsResult(null, false, null, false);
        }

        var settings = TicklerSettingsNormalizer.Normalize(await settingsStore.GetAsync());
        var exclusions = await exclusionsStore.GetExcludedSongIdsAsync();

        var primary = PickPrimarySource(logSource, mySongsSource);
        var alternate = primary.FromLogCache ? mySongsSource : logSource;

        var response = ComputeFromSource(primary, exclusions, settings, today, random);
        if (response.Songs.Count == 0 && alternate.Songs.Count > 0)
        {
            var alternateResponse = ComputeFromSource(alternate, exclusions, settings, today, random);
            if (alternateResponse.Songs.Count > 0)
            {
                return new LocalStaleSongsResult(
                    alternateResponse,
                    true,
                    alternate.CachedAtUtc,
                    alternate.FromLogCache);
            }
        }

        return new LocalStaleSongsResult(response, true, primary.CachedAtUtc, primary.FromLogCache);
    }

    private static RepertoireSource PickPrimarySource(RepertoireSource logSource, RepertoireSource mySongsSource)
    {
        if (logSource.Songs.Count == 0)
        {
            return mySongsSource;
        }

        if (mySongsSource.Songs.Count == 0)
        {
            return logSource;
        }

        var logCachedAt = logSource.CachedAtUtc ?? DateTime.MinValue;
        var mySongsCachedAt = mySongsSource.CachedAtUtc ?? DateTime.MinValue;
        return mySongsCachedAt > logCachedAt ? mySongsSource : logSource;
    }

    private static StaleSongsResponseDto ComputeFromSource(
        RepertoireSource source,
        IReadOnlySet<int> exclusions,
        TicklerSettingsDto settings,
        DateTime today,
        Random? random) =>
        StaleSongsComputer.Compute(source.Songs, exclusions, settings, today, random);

    private async Task<RepertoireSource> TryGetLogRepertoireAsync()
    {
        var logCache = await logStore.GetCachedCatalogAsync();
        if (logCache?.RepertoireStats is { Count: > 0 } stats)
        {
            return new RepertoireSource(MapStatsToRepertoire(stats), logCache.CachedAtUtc, true);
        }

        return new RepertoireSource([], null, true);
    }

    private async Task<RepertoireSource> TryGetMySongsRepertoireAsync()
    {
        var mySongsCache = await mySongsStore.GetCachedListsAsync();
        if (mySongsCache is not null)
        {
            var repertoireEntry = mySongsCache.ListsSongs
                .FirstOrDefault(entry => entry.Kind == SingerListKind.MyRepertoire);
            if (repertoireEntry is not null && repertoireEntry.Songs.Count > 0)
            {
                return new RepertoireSource(
                    repertoireEntry.Songs.ToList(),
                    mySongsCache.CachedAtUtc,
                    false);
            }
        }

        return new RepertoireSource([], null, false);
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
