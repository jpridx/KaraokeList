using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public interface ILogCatalogLoader
{
    Task<LogCatalogSnapshot> LoadAsync();
    Task<VenueLoadResult> LoadVenuesAsync();
}

public sealed class LogCatalogLoader(IKaraokeApiClient api, ILogPerformanceLocalStore store) : ILogCatalogLoader
{
    public async Task<LogCatalogSnapshot> LoadAsync()
    {
        try
        {
            var songs = await api.GetSongsAsync();
            var artists = await api.GetArtistLookupsAsync();
            var venues = await api.GetVenuesAsync();
            var lists = await api.GetMyListsAsync();
            var repertoireList = lists.Succeeded
                ? lists.Lists.FirstOrDefault(l => l.Kind == SingerListKind.MyRepertoire)
                : null;
            var repertoire = repertoireList is not null
                ? await api.GetListSongsAsync(repertoireList.Id)
                : RepertoireResult.Fail("Could not load My repertoire list.");
            var repertoireIds = repertoire.Succeeded
                ? repertoire.Songs.Select(s => s.SongId).ToHashSet()
                : [];

            var workingUpList = lists.Succeeded
                ? lists.Lists.FirstOrDefault(l => l.Kind == SingerListKind.WorkingUp)
                : null;
            var workingUp = workingUpList is not null
                ? await api.GetListSongsAsync(workingUpList.Id)
                : RepertoireResult.Fail("Could not load Working up list.");
            var workingUpIds = workingUp.Succeeded
                ? workingUp.Songs.Select(s => s.SongId).ToHashSet()
                : [];

            var artistNames = artists.ToDictionary(a => a.Id, a => a.Name);
        var pickItems = CatalogSongMapper.ToPickItems(songs, artistNames, repertoireIds, workingUpIds);

            var cachedAt = DateTime.UtcNow;
            await store.SaveCachedCatalogAsync(new CachedLogCatalog(
                pickItems.Select(s => new CachedSongEntry(s.Id, s.Title, s.ArtistName)).ToList(),
                repertoireIds.ToList(),
                venues.Select(v => new CachedVenueEntry(v.Id, v.VenueName)).ToList(),
                cachedAt,
                workingUpIds.ToList()));

            return new LogCatalogSnapshot(pickItems, repertoireIds, workingUpIds, FromCache: false, HasCatalog: pickItems.Count > 0, cachedAt);
        }
        catch (Exception ex) when (IsOfflineFailure(ex))
        {
            return await LoadFromCacheAsync();
        }
    }

    public async Task<VenueLoadResult> LoadVenuesAsync()
    {
        try
        {
            var venues = await api.GetVenuesAsync();
            await PatchCachedVenuesAsync(venues);
            return new VenueLoadResult(venues, FromCache: false);
        }
        catch (Exception ex) when (IsOfflineFailure(ex))
        {
            var cached = await store.GetCachedCatalogAsync();
            if (cached?.Venues is { Count: > 0 } cachedVenues)
            {
                return new VenueLoadResult(MapVenues(cachedVenues), FromCache: true);
            }

            return new VenueLoadResult([], FromCache: true);
        }
    }

    private async Task<LogCatalogSnapshot> LoadFromCacheAsync()
    {
        var cached = await store.GetCachedCatalogAsync();
        if (cached is null || cached.Songs.Count == 0)
        {
            return new LogCatalogSnapshot([], [], [], FromCache: true, HasCatalog: false, cached?.CachedAtUtc);
        }

        var repertoireIds = cached.RepertoireSongIds.ToHashSet();
        var workingUpIds = (cached.WorkingUpSongIds ?? []).ToHashSet();
        var pickItems = cached.Songs
            .Select(s => new LogSongPickItem(
                s.Id,
                s.Title,
                s.ArtistName,
                repertoireIds.Contains(s.Id),
                workingUpIds.Contains(s.Id)))
            .OrderByDescending(s => s.InRepertoire)
            .ThenByDescending(s => s.InWorkingUp)
            .ThenBy(s => s.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new LogCatalogSnapshot(pickItems, repertoireIds, workingUpIds, FromCache: true, HasCatalog: true, cached.CachedAtUtc);
    }

    private async Task PatchCachedVenuesAsync(IReadOnlyList<VenueDto> venues)
    {
        var venueEntries = venues.Select(v => new CachedVenueEntry(v.Id, v.VenueName)).ToList();
        var cached = await store.GetCachedCatalogAsync();
        if (cached is null)
        {
            await store.SaveCachedCatalogAsync(new CachedLogCatalog([], [], venueEntries, DateTime.UtcNow, []));
            return;
        }

        await store.SaveCachedCatalogAsync(cached with
        {
            Venues = venueEntries,
            CachedAtUtc = DateTime.UtcNow
        });
    }

    private static List<VenueDto> MapVenues(IReadOnlyList<CachedVenueEntry> venues) =>
        venues.Select(v => new VenueDto { Id = v.Id, VenueName = v.VenueName }).ToList();

    private static bool IsOfflineFailure(Exception ex) =>
        ApiTransientFailure.IsTransient(ex) || ex is HttpRequestException;
}
