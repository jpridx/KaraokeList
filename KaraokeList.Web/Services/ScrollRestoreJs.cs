using Microsoft.JSInterop;

namespace KaraokeList.Web.Services;

public interface IScrollRestoreJs
{
    Task<bool> ConsumeBackNavigationAsync();
    Task ScrollToSongWithRetryAsync(int songId, string listSelector, double itemSize, int index);
    Task NavigateBackAsync();
}

public sealed class ScrollRestoreJs(IJSRuntime js) : IScrollRestoreJs
{
    public Task<bool> ConsumeBackNavigationAsync() =>
        js.InvokeAsync<bool>("karaokeListScrollRestore.consumeBackNavigation").AsTask();

    public Task ScrollToSongWithRetryAsync(int songId, string listSelector, double itemSize, int index) =>
        js.InvokeVoidAsync(
            "karaokeListScrollRestore.scrollToSongWithRetry",
            songId,
            listSelector,
            itemSize,
            index).AsTask();

    public Task NavigateBackAsync() =>
        js.InvokeVoidAsync("karaokeListScrollRestore.navigateBack").AsTask();
}
