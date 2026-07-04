using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;

namespace KaraokeList.Web.Pages;

public partial class Log
{
    [Inject] private IKaraokeApiClient Api { get; set; } = default!;
    [Inject] private ILogPerformanceLocalStore LogStore { get; set; } = default!;
    [Inject] private ILogCatalogLoader CatalogLoader { get; set; } = default!;

    #region Data loader / orchestration

    private readonly LogCatalogState catalogState = new();
    private bool isLoading = true;
    private string? loadingStep;
    private List<SingerListDto> singerLists = [];
    private IReadOnlyList<RecentLoggedPerformance> recentLogs = [];

    protected override async Task OnParametersSetAsync()
    {
        if (SongId is int id && !isLoading && selectedSongId != id)
        {
            selectedSongId = id;
            await OnSongChangedAsync(id);
        }
    }

    private async Task LoadCatalogAsync(int singerId)
    {
        var cached = await CatalogLoader.TryGetCachedAsync();

        if (cached is not null)
        {
            // Fast path: render immediately from cache, then refresh in the background.
            // MarkOnline() suppresses the offline banner — cached data is shown for
            // performance, not because the API is unreachable.
            catalogState.Apply(cached);
            catalogState.MarkOnline();

            recentLogs = await LogStore.GetRecentLogsAsync();
            isLoading = false;

            if (SongId is int querySongId)
            {
                selectedSongId = querySongId;
                await OnSongChangedAsync(querySongId);
            }

            StateHasChanged();
            _ = RefreshCatalogInBackgroundAsync();
            return;
        }

        // No cache — full foreground load.
        await FullLoadCatalogAsync();

        if (SongId is int querySongId2)
        {
            selectedSongId = querySongId2;
            await OnSongChangedAsync(querySongId2);
        }
    }

    private async Task FullLoadCatalogAsync()
    {
        isLoading = true;
        loadingStep = null;
        saveError = null;

        catalogState.Apply(await CatalogLoader.LoadAsync(step => { loadingStep = step; StateHasChanged(); }));

        if (!catalogState.UsingOfflineCatalog)
        {
            var listsResult = await SingerListResolver.LoadListsAsync(Api);
            if (listsResult.Succeeded) singerLists = listsResult.Lists;
        }

        recentLogs = await LogStore.GetRecentLogsAsync();
        loadingStep = null;
        isLoading = false;
    }

    private async Task RefreshCatalogInBackgroundAsync()
    {
        try
        {
            if (await CatalogLoader.NeedsRefreshAsync())
            {
                var refreshed = await CatalogLoader.LoadAsync();

                // Don't replace the picker if the user has already selected a song.
                if (selectedSongId is null)
                {
                    catalogState.Apply(refreshed);
                }
            }

            // Singer lists were skipped in the fast-path initial render.
            // Always load them here so the add-to-list dropdown populates.
            var listsResult = await SingerListResolver.LoadListsAsync(Api);
            if (listsResult.Succeeded) singerLists = listsResult.Lists;

            recentLogs = await LogStore.GetRecentLogsAsync();
            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            // Background refresh failures are silent.
        }
    }

    private async Task ApplyCatalogSnapshotAsync()
    {
        catalogState.Apply(await CatalogLoader.LoadAsync());
    }

    #endregion

    #region Song selection state

    [SupplyParameterFromQuery]
    public int? SongId { get; set; }

    private int? selectedSongId;
    private string? songHint;

    private LogSongPickItem? SelectedSong
    {
        get
        {
            if (selectedSongId is not int id)
            {
                return null;
            }

            var fromPicker = catalogState.SongPickerItems.FirstOrDefault(s => s.Id == id);
            if (fromPicker is not null)
            {
                return fromPicker;
            }

            var fromRecent = recentLogs.FirstOrDefault(r => r.SongId == id);
            return fromRecent is null
                ? null
                : new LogSongPickItem(
                    fromRecent.SongId,
                    fromRecent.Title,
                    fromRecent.ArtistName,
                    catalogState.RepertoireSongIds.Contains(id),
                    catalogState.WorkingUpSongIds.Contains(id));
        }
    }

    private async Task OnSongSelectedAsync() => await OnSongChangedAsync(selectedSongId);

    private async Task OnSongChangedAsync(int? songId)
    {
        songHint = null;
        if (songId is null)
        {
            return;
        }

        if (catalogState.UsingOfflineCatalog)
        {
            songHint = catalogState.RepertoireSongIds.Contains(songId.Value)
                ? "Ready to log (offline — using cached catalog)."
                : "Ready to log.";
            return;
        }

        songHint = await SongSummaryHints.LoadForSongAsync(Api, songId.Value);
    }

    private async Task SelectSongAsync(int songId)
    {
        selectedSongId = songId;
        await OnSongChangedAsync(songId);
    }

    #endregion

    #region Rendering helpers

    private string? saveMessage;
    private string? saveError;

    private async Task OnPerformanceSavedAsync(string? message)
    {
        selectedSongId = null;
        songHint = null;
        saveMessage = message;
        saveError = null;
        recentLogs = await LogStore.GetRecentLogsAsync();

        if (!catalogState.UsingOfflineCatalog)
        {
            await ApplyCatalogSnapshotAsync();
        }
    }

    private Task OnAddedToWorkingUpAsync()
    {
        if (selectedSongId is int songId)
        {
            catalogState.WorkingUpSongIds.Add(songId);
        }

        return Task.CompletedTask;
    }

    private async Task OnNewSongAddedAsync(SongAddedEventArgs args)
    {
        saveError = null;
        await ApplyCatalogSnapshotAsync();

        var created = CatalogSongMapper.FindCreatedPickItem(
            catalogState.SongPickerItems,
            args.Title,
            args.ArtistName);

        if (created is not null)
        {
            selectedSongId = created.Id;
            await OnSongChangedAsync(created.Id);
        }

        saveMessage = "Song added.";
    }

    #endregion
}
