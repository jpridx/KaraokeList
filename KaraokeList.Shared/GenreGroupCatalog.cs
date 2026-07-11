namespace KaraokeList.Shared;

public class GenreGroupDto
{
    public int Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<GenreGroupMemberDto> Genres { get; set; } = [];
}

public class GenreGroupMemberDto
{
    public int GenreId { get; set; }
    public string GenreName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class UpdateGenreGroupGenresRequest
{
    public List<GenreGroupGenreAssignmentDto> Genres { get; set; } = [];
}

public class GenreGroupGenreAssignmentDto
{
    public int GenreId { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class GenreGroupResolver
{
    public const string OtherGroupName = "Other";
    public const string NoGenreLabel = "(No genre)";

    private readonly Dictionary<int, string> _primaryGroupByGenreId;
    private readonly Dictionary<string, int> _sortOrderByGroupName;

    public GenreGroupResolver(IReadOnlyList<GenreGroupDto> groups)
    {
        _primaryGroupByGenreId = new Dictionary<int, string>();
        _sortOrderByGroupName = groups.ToDictionary(g => g.GroupName, g => g.SortOrder, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups.OrderBy(g => g.SortOrder))
        {
            foreach (var member in group.Genres)
            {
                if (member.IsPrimary)
                {
                    _primaryGroupByGenreId[member.GenreId] = group.GroupName;
                }
            }
        }

        foreach (var group in groups.OrderBy(g => g.SortOrder))
        {
            foreach (var member in group.Genres)
            {
                _primaryGroupByGenreId.TryAdd(member.GenreId, group.GroupName);
            }
        }
    }

    public string ResolvePrimaryGroupName(RepertoireSongDto song)
    {
        if (song.GenreId is not int genreId)
        {
            return string.IsNullOrWhiteSpace(song.GenreName) ? OtherGroupName : OtherGroupName;
        }

        if (_primaryGroupByGenreId.TryGetValue(genreId, out var groupName))
        {
            return groupName;
        }

        return OtherGroupName;
    }

    public string ResolveGenreLabel(RepertoireSongDto song) =>
        string.IsNullOrWhiteSpace(song.GenreName) ? NoGenreLabel : song.GenreName;

    public int GetGroupSortOrder(string groupName)
    {
        if (_sortOrderByGroupName.TryGetValue(groupName, out var sortOrder))
        {
            return sortOrder;
        }

        return int.MaxValue;
    }
}
