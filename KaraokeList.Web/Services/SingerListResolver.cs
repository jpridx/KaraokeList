using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public static class SingerListResolver
{
    public static SingerListDto? FindList(IReadOnlyList<SingerListDto> lists, SingerListKind kind) =>
        lists.FirstOrDefault(l => l.Kind == kind);

    public static async Task<(bool Succeeded, List<SingerListDto> Lists, string? ErrorMessage)> LoadListsAsync(
        IKaraokeApiClient api)
    {
        var result = await api.GetMyListsAsync();
        if (!result.Succeeded)
        {
            return (false, [], result.ErrorMessage);
        }

        return (true, result.Lists, null);
    }
}
