namespace KaraokeList.Web.Services;

public interface IAppUpdateService
{
    Task ApplyUpdateAsync();
    Task ClearCacheAndReloadAsync();
}
