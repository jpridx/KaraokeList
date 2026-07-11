using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record GroupedSongSubSection(string Key, IReadOnlyList<RepertoireSongDto> Songs);

public sealed record GroupedSongSection(
    string Key,
    IReadOnlyList<GroupedSongSubSection> SubSections);

public sealed class GroupedPagingState
{
    public const int DefaultPageSize = 40;

    private int visibleLimit = DefaultPageSize;
    private GenreGroupResolver? resolver;

    public int VisibleLimit => visibleLimit;

    public void SetResolver(GenreGroupResolver? genreGroupResolver) => resolver = genreGroupResolver;

    public void Reset(int pageSize = DefaultPageSize) => visibleLimit = pageSize;

    public void LoadMore(int pageSize = DefaultPageSize) => visibleLimit += pageSize;

    public void RestoreVisibleLimit(int limit) =>
        visibleLimit = Math.Max(limit, DefaultPageSize);

    public void EnsureSongVisible(int songId, IReadOnlyList<RepertoireSongDto> songs, int pageSize = DefaultPageSize)
    {
        var indexInGroupedOrder = GetGroupedSongIndex(songId, songs);
        if (indexInGroupedOrder < 0)
        {
            return;
        }

        var needed = indexInGroupedOrder + 1;
        if (visibleLimit >= needed)
        {
            return;
        }

        visibleLimit = ((needed + pageSize - 1) / pageSize) * pageSize;
    }

    public GroupedPagingView BuildVisible(IReadOnlyList<RepertoireSongDto> songs)
    {
        var sections = new List<GroupedSongSection>();
        var remaining = visibleLimit;
        var totalCount = songs.Count;

        foreach (var group in GroupSongs(songs))
        {
            if (remaining <= 0)
            {
                break;
            }

            var subSections = new List<GroupedSongSubSection>();
            foreach (var subGroup in group)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var subGroupSongs = subGroup.Take(remaining).ToList();
                if (subGroupSongs.Count == 0)
                {
                    continue;
                }

                subSections.Add(new GroupedSongSubSection(subGroup.Key, subGroupSongs));
                remaining -= subGroupSongs.Count;
            }

            if (subSections.Count > 0)
            {
                sections.Add(new GroupedSongSection(group.Key, subSections));
            }
        }

        var visibleCount = sections.Sum(section => section.SubSections.Sum(sub => sub.Songs.Count));
        return new GroupedPagingView(sections, visibleCount, visibleCount < totalCount);
    }

    private IEnumerable<IGrouping<string, IGrouping<string, RepertoireSongDto>>> GroupSongs(
        IEnumerable<RepertoireSongDto> songs)
    {
        if (resolver is null)
        {
            return songs
                .GroupBy(s => string.IsNullOrWhiteSpace(s.GenreName) ? GenreGroupResolver.NoGenreLabel : s.GenreName)
                .OrderBy(g => g.Key)
                .Select(g => new GroupingAdapter<string, IGrouping<string, RepertoireSongDto>>(g.Key, [g]));
        }

        return songs
            .GroupBy(resolver.ResolvePrimaryGroupName)
            .OrderBy(g => resolver.GetGroupSortOrder(g.Key))
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new GroupingAdapter<string, IGrouping<string, RepertoireSongDto>>(
                group.Key,
                group
                    .GroupBy(resolver.ResolveGenreLabel)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)));
    }

    private int GetGroupedSongIndex(int songId, IReadOnlyList<RepertoireSongDto> songs)
    {
        var index = 0;
        foreach (var group in GroupSongs(songs))
        {
            foreach (var subGroup in group)
            {
                foreach (var song in subGroup)
                {
                    if (song.SongId == songId)
                    {
                        return index;
                    }

                    index++;
                }
            }
        }

        return -1;
    }

    private sealed class GroupingAdapter<TKey, TElement>(
        TKey key,
        IEnumerable<TElement> elements) : IGrouping<TKey, TElement>
    {
        public TKey Key { get; } = key;

        public IEnumerator<TElement> GetEnumerator() => elements.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

public sealed record GroupedPagingView(
    IReadOnlyList<GroupedSongSection> Sections,
    int VisibleCount,
    bool HasMore);
