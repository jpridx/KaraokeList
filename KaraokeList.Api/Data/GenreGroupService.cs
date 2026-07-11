using KaraokeList.Shared;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Data;

public class GenreGroupService(ApplicationDbContext db)
{
    public async Task<List<GenreGroupDto>> GetAllAsync()
    {
        var groups = await db.GenreGroups
            .AsNoTracking()
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.GroupName)
            .ToListAsync();

        var memberships = await db.GenreGroupGenres
            .AsNoTracking()
            .Include(m => m.Genre)
            .ToListAsync();

        return groups.Select(group => new GenreGroupDto
        {
            Id = group.Id,
            GroupName = group.GroupName,
            SortOrder = group.SortOrder,
            Genres = memberships
                .Where(m => m.GenreGroupId == group.Id)
                .OrderBy(m => m.Genre!.GenreName)
                .Select(m => new GenreGroupMemberDto
                {
                    GenreId = m.GenreId,
                    GenreName = m.Genre!.GenreName,
                    IsPrimary = m.IsPrimary
                })
                .ToList()
        }).ToList();
    }

    public async Task<(bool Succeeded, string? Error)> ReplaceGroupGenresAsync(
        int groupId,
        IReadOnlyList<GenreGroupGenreAssignmentDto> assignments)
    {
        var group = await db.GenreGroups.FindAsync(groupId);
        if (group is null)
        {
            return (false, "Genre group not found.");
        }

        var genreIds = assignments.Select(a => a.GenreId).Distinct().ToList();
        if (genreIds.Count != assignments.Count)
        {
            return (false, "Duplicate genre assignments are not allowed.");
        }

        var existingGenreCount = await db.Genres.CountAsync(g => genreIds.Contains(g.Id));
        if (existingGenreCount != genreIds.Count)
        {
            return (false, "One or more genres were not found.");
        }

        foreach (var genreId in genreIds)
        {
            var primaryCount = await db.GenreGroupGenres.CountAsync(m =>
                m.GenreId == genreId && m.IsPrimary);

            var removingPrimary = await db.GenreGroupGenres.AnyAsync(m =>
                m.GenreGroupId == groupId && m.GenreId == genreId && m.IsPrimary);

            var addingPrimary = assignments.First(a => a.GenreId == genreId).IsPrimary;
            var newPrimaryCount = primaryCount - (removingPrimary ? 1 : 0) + (addingPrimary ? 1 : 0);

            if (newPrimaryCount > 1)
            {
                return (false, "Each genre may have only one primary group.");
            }
        }

        var currentMemberships = await db.GenreGroupGenres
            .Where(m => m.GenreGroupId == groupId)
            .ToListAsync();

        db.GenreGroupGenres.RemoveRange(currentMemberships);

        foreach (var assignment in assignments)
        {
            db.GenreGroupGenres.Add(new GenreGroupGenre
            {
                GenreGroupId = groupId,
                GenreId = assignment.GenreId,
                IsPrimary = assignment.IsPrimary
            });
        }

        await db.SaveChangesAsync();
        return (true, null);
    }
}
