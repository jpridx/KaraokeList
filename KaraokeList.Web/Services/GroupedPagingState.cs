using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed record GroupedSongSection(string Key, IReadOnlyList<RepertoireSongDto> Songs);

public sealed class GroupedPagingState
{
    public const int DefaultPageSize = 40;

    private int visibleLimit = DefaultPageSize;

    public void Reset(int pageSize = DefaultPageSize) => visibleLimit = pageSize;

    public void LoadMore(int pageSize = DefaultPageSize) => visibleLimit += pageSize;

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

            var groupSongs = group.Take(remaining).ToList();
            if (groupSongs.Count == 0)
            {
                continue;
            }

            sections.Add(new GroupedSongSection(group.Key, groupSongs));
            remaining -= groupSongs.Count;
        }

        var visibleCount = sections.Sum(section => section.Songs.Count);
        return new GroupedPagingView(sections, visibleCount, visibleCount < totalCount);
    }

    private static IEnumerable<IGrouping<string, RepertoireSongDto>> GroupSongs(IEnumerable<RepertoireSongDto> songs) =>
        songs.GroupBy(s => string.IsNullOrWhiteSpace(s.GenreName) ? "(No genre)" : s.GenreName)
            .OrderBy(g => g.Key);

    private static int GetGroupedSongIndex(int songId, IReadOnlyList<RepertoireSongDto> songs)
    {
        var index = 0;
        foreach (var group in GroupSongs(songs))
        {
            foreach (var song in group)
            {
                if (song.SongId == songId)
                {
                    return index;
                }

                index++;
            }
        }

        return -1;
    }
}

public sealed record GroupedPagingView(
    IReadOnlyList<GroupedSongSection> Sections,
    int VisibleCount,
    bool HasMore);
