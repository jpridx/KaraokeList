using KaraokeList.Data;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Api.Services;

public sealed class CatalogMergeService(ApplicationDbContext db)
{
    /// <summary>
    /// Merges <paramref name="sourceId"/> into <paramref name="targetId"/>:
    /// updates all SingerListSongs, Performances, and SingerSongTicklerExclusions
    /// that reference the source song, then deletes the source song.
    /// Where the target already exists in the same list/singer, the source row is removed instead.
    /// </summary>
    public async Task<(bool Succeeded, string? Error)> MergeAsync(int sourceId, int targetId)
    {
        if (sourceId == targetId)
            return (false, "Cannot merge a song into itself.");

        var source = await db.Songs.FindAsync(sourceId);
        if (source is null)
            return (false, $"Source song (id {sourceId}) was not found.");

        var target = await db.Songs.FindAsync(targetId);
        if (target is null)
            return (false, $"Target song (id {targetId}) was not found.");

        await MergeSingerListSongsAsync(sourceId, targetId);
        await MergeTicklerExclusionsAsync(sourceId, targetId);
        await MergePerformancesAsync(sourceId, targetId);
        await MergeSongArtistsAsync(sourceId, targetId);

        db.Songs.Remove(source);
        await db.SaveChangesAsync();

        return (true, null);
    }

    private async Task MergeSingerListSongsAsync(int sourceId, int targetId)
    {
        var sourceRows = await db.SingerListSongs
            .Where(s => s.SongId == sourceId)
            .ToListAsync();

        if (sourceRows.Count == 0)
            return;

        var targetListIds = (await db.SingerListSongs
                .Where(s => s.SongId == targetId)
                .Select(s => s.ListId)
                .ToListAsync())
            .ToHashSet();

        foreach (var row in sourceRows)
        {
            if (targetListIds.Contains(row.ListId))
                db.SingerListSongs.Remove(row);
            else
                row.SongId = targetId;
        }

        await db.SaveChangesAsync();
    }

    private async Task MergeTicklerExclusionsAsync(int sourceId, int targetId)
    {
        var sourceRows = await db.SingerSongTicklerExclusions
            .Where(e => e.SongId == sourceId)
            .ToListAsync();

        if (sourceRows.Count == 0)
            return;

        var targetSingerIds = (await db.SingerSongTicklerExclusions
                .Where(e => e.SongId == targetId)
                .Select(e => e.SingerId)
                .ToListAsync())
            .ToHashSet();

        foreach (var row in sourceRows)
        {
            if (targetSingerIds.Contains(row.SingerId))
                db.SingerSongTicklerExclusions.Remove(row);
            else
                row.SongId = targetId;
        }

        await db.SaveChangesAsync();
    }

    private async Task MergeSongArtistsAsync(int sourceId, int targetId)
    {
        var sourceRows = await db.SongArtists
            .Where(sa => sa.SongId == sourceId)
            .ToListAsync();

        if (sourceRows.Count == 0)
        {
            return;
        }

        var targetArtistIds = (await db.SongArtists
                .Where(sa => sa.SongId == targetId)
                .Select(sa => sa.ArtistId)
                .ToListAsync())
            .ToHashSet();

        var nextOrder = await db.SongArtists
            .Where(sa => sa.SongId == targetId)
            .Select(sa => (int?)sa.DisplayOrder)
            .MaxAsync() ?? -1;

        foreach (var row in sourceRows.OrderBy(r => r.DisplayOrder))
        {
            if (targetArtistIds.Contains(row.ArtistId))
            {
                continue;
            }

            nextOrder++;
            db.SongArtists.Add(new SongArtist
            {
                SongId = targetId,
                ArtistId = row.ArtistId,
                DisplayOrder = nextOrder
            });
            targetArtistIds.Add(row.ArtistId);
        }

        await db.SaveChangesAsync();
    }

    private async Task MergePerformancesAsync(int sourceId, int targetId)
    {
        await db.Performances
            .Where(p => p.Song == sourceId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Song, targetId));
    }
}
