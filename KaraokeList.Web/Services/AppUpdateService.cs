using Microsoft.JSInterop;

namespace KaraokeList.Web.Services;

public sealed class AppUpdateService(IJSRuntime js) : IAppUpdateService
{
    public Task ApplyUpdateAsync() => js.InvokeVoidAsync("karaokeListAppUpdates.applyUpdate").AsTask();
}
