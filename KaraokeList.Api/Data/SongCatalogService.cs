using KaraokeList.Api.Mapping;
using KaraokeList.Shared;
using Microsoft.EntityFrameworkCore;

namespace KaraokeList.Data;

public sealed class SongCatalogService(ApplicationDbContext db, CatalogIntegrityService integrity)
{
    public async Task<List<SongDto>> GetSongsAsync(CancellationToken cancellationToken = default)
    {
        var songs = await db.Songs
            .AsNoTracking()
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

        var songIds = songs.Select(s => s.Id).ToList();
        var credits = await LoadArtistCreditsAsync(songIds, cancellationToken);
        return songs.Select(s => ToDto(s, credits.GetValueOrDefault(s.Id, []))).ToList();
    }

    public async Task<SongDto> AddSongAsync(SongDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Song
        {
            Title = dto.Title.Trim(),
            Genre = dto.Genre,
            Year = dto.Year,
            RecordingMbid = dto.RecordingMbid,
            ArtistCreditDisplay = dto.ArtistCreditDisplay?.Trim()
        };

        db.Songs.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        await ReplaceArtistCreditsAsync(entity.Id, dto.Artists, cancellationToken);
        return (await GetSongDtoAsync(entity.Id, cancellationToken))!;
    }

    public async Task UpdateSongAsync(SongDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await db.Songs.FindAsync([dto.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Song {dto.Id} was not found.");

        entity.Title = dto.Title.Trim();
        entity.Genre = dto.Genre;
        entity.Year = dto.Year;
        entity.RecordingMbid = dto.RecordingMbid;
        entity.ArtistCreditDisplay = dto.ArtistCreditDisplay?.Trim();

        await ReplaceArtistCreditsAsync(entity.Id, dto.Artists, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSongAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Songs.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return;
        }

        db.Songs.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SongDto?> GetSongDtoAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Songs.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var credits = await LoadArtistCreditsAsync([id], cancellationToken);
        return ToDto(entity, credits.GetValueOrDefault(id, []));
    }

    public async Task ReplaceArtistCreditsAsync(
        int songId,
        IReadOnlyList<SongArtistDto> artists,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.SongArtists.Where(sa => sa.SongId == songId).ToListAsync(cancellationToken);
        db.SongArtists.RemoveRange(existing);

        var ordered = artists
            .OrderBy(a => a.DisplayOrder)
            .Select((artist, index) => new { artist.ArtistId, DisplayOrder = index })
            .ToList();

        foreach (var credit in ordered)
        {
            db.SongArtists.Add(new SongArtist
            {
                SongId = songId,
                ArtistId = credit.ArtistId,
                DisplayOrder = credit.DisplayOrder
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Dictionary<int, List<SongArtistDto>>> LoadArtistCreditsAsync(
        IReadOnlyCollection<int> songIds,
        CancellationToken cancellationToken = default)
    {
        if (songIds.Count == 0)
        {
            return [];
        }

        var rows = await (
            from sa in db.SongArtists.AsNoTracking()
            join a in db.Artists.AsNoTracking() on sa.ArtistId equals a.Id
            where songIds.Contains(sa.SongId)
            orderby sa.SongId, sa.DisplayOrder
            select new
            {
                sa.SongId,
                sa.ArtistId,
                sa.DisplayOrder,
                a.Name
            }).ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.SongId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new SongArtistDto
                {
                    ArtistId = r.ArtistId,
                    DisplayOrder = r.DisplayOrder,
                    Name = r.Name
                }).ToList());
    }

    public async Task<List<SongDto>> SearchSongsAsync(
        string? q,
        int? artistId,
        int? genreId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = db.Songs.AsNoTracking().AsQueryable();
    
        if (artistId is int aid)
            query = query.Where(s => db.SongArtists.Any(sa => sa.SongId == s.Id && sa.ArtistId == aid));
        if (genreId is int gid)
            query = query.Where(s => s.Genre == gid);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s =>
                s.Title.Contains(term) ||
                (s.ArtistCreditDisplay != null && s.ArtistCreditDisplay.Contains(term)) ||
                db.SongArtists.Any(sa => sa.SongId == s.Id &&
                    db.Artists.Any(a => a.Id == sa.ArtistId && a.Name.Contains(term))));
        }

        var songs = await query
            .OrderBy(s => s.Title)
            .Take(take)
            .ToListAsync(cancellationToken);
        var songIds = songs.Select(s => s.Id).ToList();
        var credits = await LoadArtistCreditsAsync(songIds, cancellationToken);
        return songs.Select(s => ToDto(s, credits.GetValueOrDefault(s.Id, []))).ToList();
    }

    public static SongDto ToDto(Song entity, IReadOnlyList<SongArtistDto> artists) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Genre = entity.Genre,
        Year = entity.Year,
        RecordingMbid = entity.RecordingMbid,
        ArtistCreditDisplay = entity.ArtistCreditDisplay,
        Artists = artists.OrderBy(a => a.DisplayOrder).ToList()
    };

    public string? ValidateArtists(IReadOnlyList<SongArtistDto> artists)
    {
        if (artists.Count == 0)
        {
            return "At least one artist is required.";
        }

        var seen = new HashSet<int>();
        foreach (var artist in artists.OrderBy(a => a.DisplayOrder))
        {
            if (artist.ArtistId <= 0)
            {
                return "Each artist credit must reference a valid artist.";
            }

            if (!seen.Add(artist.ArtistId))
            {
                return "Duplicate artists are not allowed on the same song.";
            }
        }

        return null;
    }

    public async Task<string?> ValidateArtistReferencesAsync(
        IReadOnlyList<SongArtistDto> artists,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateArtists(artists);
        if (validation is not null)
        {
            return validation;
        }

        foreach (var artist in artists)
        {
            if (!await integrity.ArtistExistsAsync(artist.ArtistId))
            {
                return "One or more artists were not found.";
            }
        }

        return null;
    }
}
