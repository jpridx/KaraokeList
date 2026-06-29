using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public static class SingerListActions
{
    public static async Task<(bool Succeeded, string? ErrorMessage, string? SuccessMessage)> AddSongAsync(
        IKaraokeApiClient api,
        IReadOnlyList<SingerListDto> lists,
        SingerListKind kind,
        int songId)
    {
        var list = SingerListResolver.FindList(lists, kind);
        if (list is null)
        {
            return (false, $"{SingerListKindNames.DisplayName(kind)} list was not found.", null);
        }

        var result = await api.AddListSongAsync(list.Id, songId);
        if (!result.Succeeded)
        {
            return (false, result.ErrorMessage, null);
        }

        return (true, null, $"Added to {SingerListKindNames.DisplayName(kind).ToLowerInvariant()}.");
    }

    public static async Task<(bool Succeeded, string? ErrorMessage, string? SuccessMessage)> RemoveSongAsync(
        IKaraokeApiClient api,
        IReadOnlyList<SingerListDto> lists,
        SingerListKind kind,
        int songId)
    {
        var list = SingerListResolver.FindList(lists, kind);
        if (list is null)
        {
            return (false, $"{SingerListKindNames.DisplayName(kind)} list was not found.", null);
        }

        var result = await api.RemoveListSongAsync(list.Id, songId);
        if (!result.Succeeded)
        {
            return (false, result.ErrorMessage, null);
        }

        return (true, null, $"Removed from {SingerListKindNames.DisplayName(kind).ToLowerInvariant()}.");
    }
}
