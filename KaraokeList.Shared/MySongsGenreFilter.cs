namespace KaraokeList.Shared;

public static class MySongsGenreFilter
{
    public static IReadOnlyList<string> BuildFilterGroups(
        IReadOnlyList<RepertoireSongDto> songs,
        IReadOnlyList<GenreGroupDto> genreGroups)
    {
        if (songs.Count == 0)
        {
            return [];
        }

        if (genreGroups.Count == 0)
        {
            return [];
        }

        var resolver = new GenreGroupResolver(genreGroups);
        return songs
            .GroupBy(resolver.ResolvePrimaryGroupName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Key)
            .OrderBy(name => resolver.GetGroupSortOrder(name))
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<RepertoireSongDto> ApplyGroupFilter(
        IReadOnlyList<RepertoireSongDto> songs,
        string? groupName,
        IReadOnlyList<GenreGroupDto> genreGroups)
    {
        if (string.IsNullOrWhiteSpace(groupName) || genreGroups.Count == 0)
        {
            return songs.ToList();
        }

        var resolver = new GenreGroupResolver(genreGroups);
        return songs
            .Where(s => string.Equals(resolver.ResolvePrimaryGroupName(s), groupName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static List<GenreDto> BuildFilterGenres(
        IReadOnlyList<RepertoireSongDto> songs,
        IReadOnlyList<GenreGroupDto> genreGroups,
        string? scopedGroupName = null)
    {
        var source = songs;

        if (!string.IsNullOrWhiteSpace(scopedGroupName) && genreGroups.Count > 0)
        {
            source = ApplyGroupFilter(songs, scopedGroupName, genreGroups);
        }

        return source
            .Where(s => s.GenreId is int)
            .GroupBy(s => s.GenreId!.Value)
            .Select(g => new GenreDto { Id = g.Key, GenreName = g.First().GenreName })
            .OrderBy(g => g.GenreName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
