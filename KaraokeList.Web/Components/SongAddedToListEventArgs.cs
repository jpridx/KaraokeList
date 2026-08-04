using KaraokeList.Shared;

namespace KaraokeList.Web.Components;

public sealed record SongAddedToListEventArgs(
    string Message,
    int SongId,
    IReadOnlyList<SingerListKind> AddedLists);
