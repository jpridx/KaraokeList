using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using Microsoft.AspNetCore.Components;

namespace KaraokeList.Web.Pages;

public partial class MyPerformances
{
    [Inject] private IMyPerformancesLoader PerformancesLoader { get; set; } = default!;

    private SingerGatedPage? gatedPage;
    private bool isLoading = true;
    private string? loadError;
    private bool usingOfflinePerformances;
    private bool hasCachedPerformances;
    private DateTime? performancesCachedAt;
    private List<MyPerformanceEntryDto> allPerformances = [];
    private List<MyPerformanceEntryDto> performances = [];
    private List<MyPerformanceEntryDto> displayPerformances = [];
    private List<VenueFilterOption> venueFilters = [];
    private string searchText = string.Empty;
    private string sortDir = "desc";
    private int? filterVenueId;
    private int visibleLimit = PageSize;

    private const int PageSize = 40;

    private IReadOnlyList<MyPerformanceEntryDto> VisiblePerformances =>
        displayPerformances.Take(visibleLimit).ToList();

    private bool HasMorePerformances => visibleLimit < displayPerformances.Count;

    private IReadOnlyList<EditablePerformanceEntry> browseEntries =>
        VisiblePerformances.Select(EditablePerformanceEntry.FromBrowse).ToList();

    private IReadOnlyList<ChipFilterItem> venueChipItems =>
        ChipFilterBuilder.CreateAllPlusItems(
            filterVenueId,
            venueFilters,
            venue => venue.Id,
            venue => venue.Name,
            EventCallback.Factory,
            this,
            SetVenueFilterAsync);

    private async Task LoadPerformancesAsync(int singerId)
    {
        var cached = await PerformancesLoader.TryGetCachedAsync();
        if (cached is not null)
        {
            ApplyLoadResult(cached);
            usingOfflinePerformances = false;
            isLoading = false;
            StateHasChanged();
            _ = RefreshPerformancesInBackgroundAsync();
            return;
        }

        await ReloadPerformancesAsync();
    }

    private async Task ReloadPerformancesAsync()
    {
        isLoading = true;
        loadError = null;
        usingOfflinePerformances = false;
        hasCachedPerformances = false;
        performancesCachedAt = null;

        var loadTask = PerformancesLoader.LoadAsync();

        if (await Task.WhenAny(loadTask, Task.Delay(ApiSlowRequestNotifier.PageLoadTimeout)) != loadTask)
        {
            var cached = await PerformancesLoader.TryGetCachedAsync();
            if (cached is not null)
            {
                ApplyLoadResult(cached);
            }
            else
            {
                loadError = "Still connecting to the server\u2014your performances will appear shortly.";
            }

            isLoading = false;
            StateHasChanged();

            _ = loadTask.ContinueWith(async t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    await InvokeAsync(() =>
                    {
                        ApplyLoadResult(t.Result);
                        isLoading = false;
                        StateHasChanged();
                    });
                }
            });
            return;
        }

        var result = await loadTask;
        ApplyLoadResult(result);
        isLoading = false;
    }

    private void ApplyLoadResult(MyPerformancesLoadResult result)
    {
        usingOfflinePerformances = result.FromCache;
        hasCachedPerformances = result.HasCache;
        performancesCachedAt = result.CachedAtUtc;

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

        allPerformances = result.Performances.ToList();
        ApplyClientFilters();
        loadError = null;
    }

    private void ApplyClientFilters()
    {
        performances = MyPerformancesQuery.Apply(allPerformances, filterVenueId, sortDir);
        if (filterVenueId is null)
        {
            venueFilters = allPerformances
                .Where(p => p.VenueId is not null && !string.IsNullOrWhiteSpace(p.VenueName))
                .GroupBy(p => p.VenueId!.Value)
                .Select(g => new VenueFilterOption(g.Key, g.First().VenueName))
                .OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        RefreshDisplayList();
    }

    private async Task RefreshPerformancesInBackgroundAsync()
    {
        try
        {
            var refreshed = await PerformancesLoader.LoadAsync();
            ApplyLoadResult(refreshed);
            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            // Background refresh failures are silent.
        }
    }

    private void RefreshDisplayList()
    {
        displayPerformances = MyPerformancesSearch.Filter(performances, searchText).ToList();
        visibleLimit = PageSize;
    }

    private void LoadMore() => visibleLimit += PageSize;

    private Task ToggleSortDirAsync()
    {
        sortDir = sortDir == "desc" ? "asc" : "desc";
        ApplyClientFilters();
        return Task.CompletedTask;
    }

    private Task SetVenueFilterAsync(int? venueId)
    {
        filterVenueId = venueId;
        ApplyClientFilters();
        return Task.CompletedTask;
    }

    private sealed record VenueFilterOption(int Id, string Name);
}
