using KaraokeList.Shared;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Data;

public sealed class TicklerExclusionService(ApplicationDbContext db, CatalogIntegrityService integrity)
{
    public async Task<SongTicklerExclusionDto> GetExclusionAsync(int singerId, int songId)
    {
        var exclusion = await db.SingerSongTicklerExclusions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.SingerId == singerId && e.SongId == songId);

        return exclusion is null
            ? new SongTicklerExclusionDto { Excluded = false }
            : new SongTicklerExclusionDto { Excluded = true, Reason = exclusion.Reason };
    }

    public async Task<(bool Succeeded, string? Error)> SetExclusionAsync(
        int singerId,
        int songId,
        string? reason)
    {
        if (!await integrity.SongExistsAsync(songId))
        {
            return (false, "Song was not found.");
        }

        var validationError = TicklerExclusionValidation.ValidateReason(reason);
        if (validationError is not null)
        {
            return (false, validationError);
        }

        var normalizedReason = TicklerExclusionValidation.NormalizeReason(reason);
        var existing = await db.SingerSongTicklerExclusions
            .FirstOrDefaultAsync(e => e.SingerId == singerId && e.SongId == songId);

        if (existing is null)
        {
            db.SingerSongTicklerExclusions.Add(new SingerSongTicklerExclusion
            {
                SingerId = singerId,
                SongId = songId,
                Reason = normalizedReason,
                CreatedUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.Reason = normalizedReason;
        }

        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> RemoveExclusionAsync(int singerId, int songId)
    {
        var existing = await db.SingerSongTicklerExclusions
            .FirstOrDefaultAsync(e => e.SingerId == singerId && e.SongId == songId);
        if (existing is null)
        {
            return false;
        }

        db.SingerSongTicklerExclusions.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<List<int>> GetExcludedSongIdsAsync(int singerId) =>
        await db.SingerSongTicklerExclusions
            .AsNoTracking()
            .Where(e => e.SingerId == singerId)
            .Select(e => e.SongId)
            .ToListAsync();
}
