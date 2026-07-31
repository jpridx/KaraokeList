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
        showDetailedGenreFilters = await MySongsStore.GetShowDetailedGenreFiltersAsync();
        listKind = await MySongsStore.GetListKindAsync();
        sortBy = await MySongsStore.GetSortByAsync();
        sortDir = await MySongsStore.GetSortDirAsync();
        searchText = await MySongsStore.GetSearchTextAsync();
        filterGenreId = await MySongsStore.GetFilterGenreIdAsync();
        filterGroupName = await MySongsStore.GetFilterGroupNameAsync();
        groupByGenre = await MySongsStore.GetGroupByGenreAsync();
        if (groupByGenre)
        {
            showGenreFilters = true;
        }
    }

    private async Task LoadListsAsync(int singerId)
    {
        var cached = await MySongsLoader.TryGetCachedAsync(listKind, sortBy, sortDir, filterGenreId, filterGroupName);

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

        var loadTask = MySongsLoader.LoadAsync(listKind, sortBy, sortDir, filterGenreId, filterGroupName,
            step => { loadingStep = step; StateHasChanged(); });

        if (await Task.WhenAny(loadTask, Task.Delay(ApiSlowRequestNotifier.PageLoadTimeout)) != loadTask)
        {
            // API is slow (DB waking up) — stop blocking the UI.
            // Show whatever is in the cache (may be null on a first visit).
            var cached = await MySongsLoader.TryGetCachedAsync(listKind, sortBy, sortDir, filterGenreId, filterGroupName);
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
        genreGroups = result.GenreGroups.ToList();

        filterGenres = result.FilterGenres.ToList();
        filterGroups = result.FilterGroups.ToList();

        loadError = null;

        if (ClearStaleFiltersIfNeeded(result))
        {
            _ = InvokeAsync(async () =>
            {
                await PersistFilterStateAsync();
                await ReloadListsAsync();
            });
            return;
        }

        RefreshDisplayList();
    }

    private bool ClearStaleFiltersIfNeeded(MySongsLoadResult result)
    {
        var cleared = false;

        if (filterGenreId is int genreId && !result.FilterGenres.Any(g => g.Id == genreId))
        {
            filterGenreId = null;
            cleared = true;
        }

        if (!string.IsNullOrWhiteSpace(filterGroupName) &&
            !result.FilterGroups.Any(g => string.Equals(g, filterGroupName, StringComparison.OrdinalIgnoreCase)))
        {
            filterGroupName = null;
            cleared = true;
        }

        return cleared;
    }

    private async Task RefreshListsInBackgroundAsync()
    {
        try
        {
            if (!await MySongsLoader.NeedsRefreshAsync()) return;

            var refreshed = await MySongsLoader.LoadAsync(listKind, sortBy, sortDir, filterGenreId, filterGroupName);
            ApplyLoadResult(refreshed);
            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            // Background refresh failures are silent.
        }
    }

    private async Task ReapplyFiltersAsync()
    {
        var cached = await MySongsLoader.TryGetCachedAsync(
            listKind, sortBy, sortDir, filterGenreId, filterGroupName);
        if (cached is not null)
        {
            ApplyLoadResult(cached);
            isLoading = false;
            loadingStep = null;
            await InvokeAsync(StateHasChanged);
            return;
        }

        await ReloadListsAsync();
    }

    #endregion

    #region Query / filter state

    private string searchText = string.Empty;
    private string sortBy = "lastPerformed";
    private string sortDir = "desc";
    private int? filterGenreId;
    private string? filterGroupName;
    private List<GenreDto> filterGenres = [];
    private List<string> filterGroups = [];
    private List<GenreGroupDto> genreGroups = [];
    private bool groupByGenre;
    private bool showGenreFilters;
    private bool showDetailedGenreFilters;

    private IEnumerable<RepertoireSongDto> FilteredSongs =>
        RepertoireSearch.Filter(songs, searchText);

    private void RefreshDisplayList()
    {
        displaySongs = FilteredSongs.ToList();
        groupedPaging.SetResolver(genreGroups.Count > 0 ? new GenreGroupResolver(genreGroups) : null);
        groupedPaging.Reset();
        _ = PersistFilterStateAsync();
    }

    private async Task ToggleGenreFiltersAsync()
    {
        showGenreFilters = !showGenreFilters;
        await MySongsStore.SetShowGenreFiltersAsync(showGenreFilters);
    }

    private async Task ToggleDetailedGenreFiltersAsync()
    {
        showDetailedGenreFilters = !showDetailedGenreFilters;
        await MySongsStore.SetShowDetailedGenreFiltersAsync(showDetailedGenreFilters);

        if (!showDetailedGenreFilters)
        {
            if (filterGenreId is int genreId && genreGroups.Count > 0)
            {
                var resolver = new GenreGroupResolver(genreGroups);
                var song = songs.FirstOrDefault(s => s.GenreId == genreId);
                filterGroupName = song is not null
                    ? resolver.ResolvePrimaryGroupName(song)
                    : null;
            }

            filterGenreId = null;
            await PersistFilterStateAsync();
            await ReapplyFiltersAsync();
        }
        else
        {
            await PersistFilterStateAsync();
        }
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

    private Task PersistFilterStateAsync() =>
        MySongsStore.SetFilterStateAsync(searchText, filterGenreId, filterGroupName, groupByGenre);

    private async Task SetGenreFilterAsync(int? genreId)
    {
        filterGenreId = genreId;
        await PersistFilterStateAsync();
        await ReapplyFiltersAsync();
    }

    private async Task SetGroupFilterAsync(string? groupName)
    {
        filterGroupName = groupName;
        filterGenreId = null;
        await PersistFilterStateAsync();
        await ReapplyFiltersAsync();
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
    private int? deferredScrollSongId;

    private GroupedPagingView groupedPagingView => groupedPaging.BuildVisible(displaySongs);

    private bool UseNestedGenreHeadings => genreGroups.Count > 0;

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
        showDetailedGenreFilters || filterGroups.Count == 0
            ? ChipFilterBuilder.CreateAllPlusItems(
                filterGenreId,
                filterGenres,
                genre => genre.Id,
                genre => genre.GenreName,
                EventCallback.Factory,
                this,
                SetGenreFilterAsync)
            : ChipFilterBuilder.CreateAllPlusItemsByString(
                filterGroupName,
                filterGroups,
                group => group,
                group => group,
                EventCallback.Factory,
                this,
                SetGroupFilterAsync);

    private string GenreChipBarLabel =>
        showDetailedGenreFilters || filterGroups.Count == 0 ? "Genre" : "Genre group";

    private void LoadMoreGroupedSongs() => groupedPaging.LoadMore();

    private async Task OnGroupByGenreChangedAsync()
    {
        groupedPaging.Reset();
        await PersistFilterStateAsync();
    }

    private void GoToHistoryAsync(RepertoireSongDto song)
    {
        ScrollRestoreState.SetPending(
            song.SongId,
            groupByGenre,
            groupByGenre ? groupedPaging.VisibleLimit : null);
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
        if (!string.IsNullOrEmpty(loadError) || isLoading || displaySongs.Count == 0)
        {
            return;
        }

        if (deferredScrollSongId is int deferredSongId)
        {
            deferredScrollSongId = null;
            await ScrollRestoreJs.ScrollToSongWithRetryAsync(deferredSongId, ListSelector, -1, -1);
            return;
        }

        if (scrollRestoreChecked)
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

        if (displaySongs.All(s => s.SongId != pending.SongId))
        {
            return;
        }

        if (pending.GroupByGenre)
        {
            groupByGenre = true;
            showGenreFilters = true;
            groupedPaging.RestoreVisibleLimit(pending.GroupedVisibleLimit ?? GroupedPagingState.DefaultPageSize);
            groupedPaging.EnsureSongVisible(pending.SongId, displaySongs);
            deferredScrollSongId = pending.SongId;
            _ = PersistFilterStateAsync();
            StateHasChanged();
            return;
        }

        var index = displaySongs.FindIndex(s => s.SongId == pending.SongId);
        await ScrollRestoreJs.ScrollToSongWithRetryAsync(
            pending.SongId,
            ListSelector,
            VirtualizedItemSize,
            index);
    }

    #endregion
}
