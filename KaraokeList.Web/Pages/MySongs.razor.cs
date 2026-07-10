using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;

namespace KaraokeList.Web.Pages;

public partial class MySongs
{
    private const string ListSelector = "#my-songs-list";
    private const double VirtualizedItemSize = 88;

    [Inject] private IMySongsLocalStore MySongsStore { get; set; } = default!;
    [Inject] private IMySongsLoader MySongsLoader { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private MySongsScrollRestoreState ScrollRestoreState { get; set; } = default!;
    [Inject] private IScrollRestoreJs ScrollRestoreJs { get; set; } = default!;

    #region Data loader / orchestration

    private SingerGatedPage? gatedPage;
    private bool isLoading = true;
    private string? loadingStep;
    private bool usingOfflineLists;
    private bool hasCachedLists;
    private DateTime? listsCachedAt;
    private string? loadError;
    private List<RepertoireSongDto> songs = [];
    private List<SingerListDto> singerLists = [];
    private SingerListKind listKind = SingerListKind.MyRepertoire;

    protected override async Task OnInitializedAsync()
    {
        showGenreFilters = await MySongsStore.GetShowGenreFiltersAsync();
        listKind = await MySongsStore.GetListKindAsync();
        sortBy = await MySongsStore.GetSortByAsync();
        sortDir = await MySongsStore.GetSortDirAsync();
    }

    private async Task LoadListsAsync(int singerId)
    {
        var cached = await MySongsLoader.TryGetCachedAsync(listKind, sortBy, sortDir, filterGenreId);

        if (cached is not null)
        {
            // Fast path: render immediately from cache, then refresh in the background.
            // Clear usingOfflineLists so the offline banner doesn't show — cached data
            // is shown for performance, not because the API is unreachable.
            ApplyLoadResult(cached);
            usingOfflineLists = false;
            isLoading = false;
            StateHasChanged();
            _ = RefreshListsInBackgroundAsync();
            return;
        }

        // No cache — full foreground load.
        await ReloadListsAsync();
    }

    private async Task ReloadListsAsync()
    {
        isLoading = true;
        loadingStep = null;
        loadError = null;
        usingOfflineLists = false;
        hasCachedLists = false;
        listsCachedAt = null;

        var loadTask = MySongsLoader.LoadAsync(listKind, sortBy, sortDir, filterGenreId,
            step => { loadingStep = step; StateHasChanged(); });

        if (await Task.WhenAny(loadTask, Task.Delay(ApiSlowRequestNotifier.PageLoadTimeout)) != loadTask)
        {
            // API is slow (DB waking up) — stop blocking the UI.
            // Show whatever is in the cache (may be null on a first visit).
            var cached = await MySongsLoader.TryGetCachedAsync(listKind, sortBy, sortDir, filterGenreId);
            if (cached is not null)
            {
                ApplyLoadResult(cached);
            }
            else
            {
                loadError = "Still connecting to the server\u2014your songs will appear shortly.";
            }

            loadingStep = null;
            isLoading = false;
            StateHasChanged();

            // Auto-update when the DB eventually wakes and the load completes.
            _ = loadTask.ContinueWith(async t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    await InvokeAsync(() =>
                    {
                        ApplyLoadResult(t.Result);
                        loadingStep = null;
                        isLoading = false;
                        StateHasChanged();
                    });
                }
            });
            return;
        }

        var result = await loadTask;
        ApplyLoadResult(result);
        loadingStep = null;
        isLoading = false;
    }

    private void ApplyLoadResult(MySongsLoadResult result)
    {
        usingOfflineLists = result.FromCache;
        hasCachedLists = result.HasCache;
        listsCachedAt = result.CachedAtUtc;

        if (result.NeedsSingerLink)
        {
            loadError = result.ErrorMessage;
            gatedPage?.RequireLinkIfNotLinked(result.ErrorMessage);
            return;
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            loadError = result.ErrorMessage;
            return;
        }

        singerLists = result.Lists.ToList();
        var selectedList = singerLists.FirstOrDefault(l => l.Kind == listKind)
            ?? singerLists.FirstOrDefault(l => l.Kind == SingerListKind.MyRepertoire);
        if (selectedList is null)
        {
            loadError = "Could not load your singer lists.";
            return;
        }

        listKind = selectedList.Kind;
        songs = result.Songs.ToList();

        if (filterGenreId is null)
        {
            filterGenres = result.FilterGenres.ToList();
        }

        loadError = null;
        RefreshDisplayList();
    }

    private async Task RefreshListsInBackgroundAsync()
    {
        try
        {
            if (!await MySongsLoader.NeedsRefreshAsync()) return;

            var refreshed = await MySongsLoader.LoadAsync(listKind, sortBy, sortDir, filterGenreId);
            ApplyLoadResult(refreshed);
            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            // Background refresh failures are silent.
        }
    }

    #endregion

    #region Query / filter state

    private string searchText = string.Empty;
    private string sortBy = "lastPerformed";
    private string sortDir = "desc";
    private int? filterGenreId;
    private List<GenreDto> filterGenres = [];
    private bool groupByGenre;
    private bool showGenreFilters;

    private IEnumerable<RepertoireSongDto> FilteredSongs =>
        RepertoireSearch.Filter(songs, searchText);

    private void RefreshDisplayList()
    {
        displaySongs = FilteredSongs.ToList();
        groupedPaging.Reset();
    }

    private async Task ToggleGenreFiltersAsync()
    {
        showGenreFilters = !showGenreFilters;
        await MySongsStore.SetShowGenreFiltersAsync(showGenreFilters);
    }

    private async Task ToggleSortDirAsync()
    {
        sortDir = sortDir == "desc" ? "asc" : "desc";
        await PersistSortPreferenceAsync();
        await ReloadListsAsync();
    }

    private async Task OnSortFieldChangedAsync()
    {
        await PersistSortPreferenceAsync();
        await ReloadListsAsync();
    }

    private Task PersistSortPreferenceAsync() =>
        MySongsStore.SetSortPreferenceAsync(sortBy, sortDir);

    private async Task SetGenreFilterAsync(int? genreId)
    {
        filterGenreId = genreId;
        await ReloadListsAsync();
    }

    private async Task SetListKindAsync(SingerListKind newKind)
    {
        if (listKind == newKind)
        {
            return;
        }

        listKind = newKind;
        addSongMessage = null;
        await MySongsStore.SetListKindAsync(newKind);
        await ReloadListsAsync();
    }

    #endregion

    #region Rendering helpers

    private List<RepertoireSongDto> displaySongs = [];
    private readonly GroupedPagingState groupedPaging = new();
    private AddSongToListPanel? addSongPanel;
    private string? addSongMessage;
    private bool scrollRestoreChecked;

    private GroupedPagingView groupedPagingView => groupedPaging.BuildVisible(displaySongs);

    private bool SupportsCatalogAdd =>
        listKind is SingerListKind.WantToSing or SingerListKind.WorkingUp;

    private SingerListDto? CurrentList =>
        singerLists.FirstOrDefault(l => l.Kind == listKind);

    private string EmptyListMessage => listKind switch
    {
        SingerListKind.WantToSing => "No songs on your want-to-sing list yet.",
        SingerListKind.WorkingUp => "No songs on your working-up list yet.",
        _ => "No songs on your repertoire yet."
    };

    private IReadOnlyList<ChipFilterItem> listChipItems =>
        singerLists.Select(list => new ChipFilterItem
        {
            Label = list.DisplayName,
            IsActive = listKind == list.Kind,
            OnClick = EventCallback.Factory.Create(this, () => SetListKindAsync(list.Kind))
        }).ToList();

    private IReadOnlyList<ChipFilterItem> genreChipItems =>
        ChipFilterBuilder.CreateAllPlusItems(
            filterGenreId,
            filterGenres,
            genre => genre.Id,
            genre => genre.GenreName,
            EventCallback.Factory,
            this,
            SetGenreFilterAsync);

    private void LoadMoreGroupedSongs() => groupedPaging.LoadMore();

    private void ResetGroupedPaging() => groupedPaging.Reset();

    private void GoToHistoryAsync(RepertoireSongDto song)
    {
        if (!groupByGenre)
        {
            ScrollRestoreState.SetPending(song.SongId);
        }

        Navigation.NavigateTo($"my-songs/{song.SongId}");
    }

    private void GoToLogAsync(RepertoireSongDto song) =>
        Navigation.NavigateTo($"log?songId={song.SongId}");

    private Task OpenAddFromCatalogAsync() =>
        addSongPanel?.OpenCatalogAsync() ?? Task.CompletedTask;

    private void OpenAddNewSongAsync() => addSongPanel?.OpenNewSong();

    private async Task OnSongAddedToListAsync(SongAddedToListEventArgs args)
    {
        addSongMessage = args.Message;
        await ReloadListsAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (scrollRestoreChecked || isLoading || groupByGenre || displaySongs.Count == 0 || !string.IsNullOrEmpty(loadError))
        {
            return;
        }

        scrollRestoreChecked = true;

        var arrivedViaBack = await ScrollRestoreJs.ConsumeBackNavigationAsync();
        var pending = ScrollRestoreState.TryConsume(arrivedViaBack);
        if (pending is null)
        {
            return;
        }

        var index = displaySongs.FindIndex(s => s.SongId == pending.SongId);
        if (index < 0)
        {
            return;
        }

        await ScrollRestoreJs.ScrollToSongWithRetryAsync(
            pending.SongId,
            ListSelector,
            VirtualizedItemSize,
            index);
    }

    #endregion
}
