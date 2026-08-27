using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public static class SingerListActions
{
    public static async Task<(bool Succeeded, string? ErrorMessage, string? SuccessMessage)> AddSongAsync(
        IKaraokeApiClient api,
        IReadOnlyList<SingerListDto> lists,
        SingerListKind kind,
        int songId,
        bool allowTitleArtistDuplicate = false)
    {
        var list = SingerListResolver.FindList(lists, kind);
        if (list is null)
        {
            return (false, $"{SingerListKindNames.DisplayName(kind)} list was not found.", null);
        }

        var result = await api.AddListSongAsync(list.Id, songId, allowTitleArtistDuplicate);
        if (!result.Succeeded)
        {
            return (false, result.ErrorMessage, null);
        }

        return (true, null, $"Added to {SingerListKindNames.DisplayName(kind).ToLowerInvariant()}.");
    }

    public static async Task<ListTitleArtistCollision> CheckTitleArtistCollisionsAsync(
        IKaraokeApiClient api,
        IReadOnlyList<SingerListDto> lists,
        int songId,
        IEnumerable<SingerListKind> kinds)
    {
        var collisions = new List<(SingerListKind Kind, TitleArtistCollisionDto Collision)>();

        foreach (var kind in kinds)
        {
            var list = SingerListResolver.FindList(lists, kind);
            if (list is null)
            {
                continue;
            }

            var result = await api.GetTitleArtistCollisionAsync(list.Id, songId);
            if (!result.Succeeded)
            {
                return ListTitleArtistCollision.Fail(result.ErrorMessage ?? "Could not check for duplicate songs.");
            }

            if (result.Collision is not null)
            {
                collisions.Add((kind, result.Collision));
            }
        }

        return ListTitleArtistCollision.Ok(collisions);
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

public sealed class ListTitleArtistCollision
{
    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public List<(SingerListKind Kind, TitleArtistCollisionDto Collision)> Collisions { get; init; } = [];

    public bool HasCollisions => Collisions.Count > 0;

    public static ListTitleArtistCollision Ok(List<(SingerListKind Kind, TitleArtistCollisionDto Collision)> collisions) =>
        new() { Succeeded = true, Collisions = collisions };

    public static ListTitleArtistCollision Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
