using KaraokeList.Api.Mapping;
using KaraokeList.Api.Services;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PerformancesController(
    PerformanceService performanceService,
    CatalogIntegrityService integrity,
    ICurrentUserSingerResolver currentUserSinger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PerformanceDto>>> GetAll([FromQuery] int? songId)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var performances = await performanceService.GetPerformancesAsync(singerId.Value!.Value, songId);
        return Ok(performances.Select(p => p.ToDto()).ToList());
    }

    [HttpGet("my-history")]
    public async Task<ActionResult<List<MyPerformanceEntryDto>>> GetMyHistory(
        [FromQuery] int? venueId = null,
        [FromQuery] string sortDir = "desc")
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        if (!IsValidSortDir(sortDir))
        {
            return BadRequest(new ApiErrorResponse { Message = "Invalid sortDir. Use asc or desc." });
        }

        var performances = await performanceService.GetMyPerformancesAsync(
            singerId.Value!.Value, venueId, sortDir);
        return Ok(performances.Select(p => p.ToDto()).ToList());
    }

    [HttpGet("my-song-summary")]
    public async Task<ActionResult<SongPerformanceSummaryDto>> GetMySongSummary([FromQuery] int songId)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var summary = await performanceService.GetSongPerformanceSummaryAsync(singerId.Value!.Value, songId);
        return Ok(summary?.ToDto() ?? new SongPerformanceSummaryDto { SongId = songId });
    }

    [HttpGet("my-repertoire")]
    public async Task<ActionResult<List<RepertoireSongDto>>> GetMyRepertoire(
        [FromQuery] string sortBy = "lastPerformed",
        [FromQuery] string sortDir = "desc",
        [FromQuery] int? genreId = null,
        [FromQuery] bool includeAll = false)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        if (!IsValidRepertoireSort(sortBy))
        {
            return BadRequest(new ApiErrorResponse { Message = "Invalid sortBy. Use title, artist, genre, or lastPerformed." });
        }

        if (!IsValidSortDir(sortDir))
        {
            return BadRequest(new ApiErrorResponse { Message = "Invalid sortDir. Use asc or desc." });
        }

        var songs = await performanceService.GetMyRepertoireAsync(
            singerId.Value!.Value, sortBy, sortDir, genreId, includeAll);
        return Ok(songs.Select(s => s.ToDto()).ToList());
    }

    [HttpGet("my-repertoire/genres")]
    public async Task<ActionResult<List<GenreDto>>> GetMyRepertoireGenres()
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var genres = await performanceService.GetMyRepertoireGenresAsync(singerId.Value!.Value);
        return Ok(genres.Select(g => new GenreDto { Id = g.Id, GenreName = g.GenreName }).ToList());
    }

    [HttpGet("my-stale-songs")]
    public async Task<ActionResult<StaleSongsResponseDto>> GetMyStaleSongs(
        [FromQuery] int days = 90,
        [FromQuery] int limit = 5)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        if (days is < 7 or > 365)
        {
            return BadRequest(new ApiErrorResponse { Message = "Invalid days. Use a value between 7 and 365." });
        }

        if (limit is < 1 or > 20)
        {
            return BadRequest(new ApiErrorResponse { Message = "Invalid limit. Use a value between 1 and 20." });
        }

        var songs = await performanceService.GetStaleSongsAsync(singerId.Value!.Value, days, limit);
        var today = DateTime.Today;
        return Ok(new StaleSongsResponseDto
        {
            StaleAfterDays = days,
            Songs = songs.Select(s => s.ToDto(today)).ToList()
        });
    }

    [HttpGet("my-stats")]
    public async Task<ActionResult<SingerStatsDto>> GetMyStats([FromQuery] int topVenues = 3)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        if (topVenues is < 1 or > 10)
        {
            return BadRequest(new ApiErrorResponse { Message = "Invalid topVenues. Use a value between 1 and 10." });
        }

        var stats = await performanceService.GetSingerStatsAsync(singerId.Value!.Value, topVenues);
        return Ok(stats.ToDto(DateTime.Today));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PerformanceDto dto)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        if (dto.Singer is int requestedSinger && requestedSinger != singerId.Value)
        {
            return BadRequest(new ApiErrorResponse { Message = "Cannot create performances for another singer." });
        }

        dto.Singer = singerId.Value;
        var validation = await ValidatePerformanceAsync(dto);
        if (validation is not null)
        {
            return validation;
        }

        await performanceService.AddPerformanceAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PerformanceDto dto)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var existing = await performanceService.GetPerformanceByIdAsync(id);
        if (existing is null || existing.Singer != singerId.Value)
        {
            return NotFound();
        }

        dto.Id = id;
        dto.Singer = singerId.Value;
        var validation = await ValidatePerformanceAsync(dto);
        if (validation is not null)
        {
            return validation;
        }

        await performanceService.UpdatePerformanceAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var existing = await performanceService.GetPerformanceByIdAsync(id);
        if (existing is null || existing.Singer != singerId.Value)
        {
            return NotFound();
        }

        await performanceService.DeletePerformanceAsync(id);
        return NoContent();
    }

    private async Task<(int? Value, ActionResult? Result)> RequireSingerIdAsync()
    {
        var singerId = await currentUserSinger.GetSingerIdAsync(User);
        if (singerId is null)
        {
            return (null, BadRequest(new ApiErrorResponse { Message = "Your account is not linked to a singer profile." }));
        }

        return (singerId, null);
    }

    private static bool IsValidRepertoireSort(string sortBy)
    {
        return sortBy.Equals("title", StringComparison.OrdinalIgnoreCase)
            || sortBy.Equals("artist", StringComparison.OrdinalIgnoreCase)
            || sortBy.Equals("genre", StringComparison.OrdinalIgnoreCase)
            || sortBy.Equals("lastPerformed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidSortDir(string sortDir) =>
        sortDir.Equals("asc", StringComparison.OrdinalIgnoreCase)
        || sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

    private async Task<ActionResult?> ValidatePerformanceAsync(PerformanceDto dto)
    {
        if (dto.Song is not int songId)
        {
            return BadRequest(new ApiErrorResponse { Message = "Song is required." });
        }

        if (!await integrity.SongExistsAsync(songId))
        {
            return BadRequest(new ApiErrorResponse { Message = "Song was not found." });
        }

        if (dto.Venue is int venueId && !await integrity.VenueExistsAsync(venueId))
        {
            return BadRequest(new ApiErrorResponse { Message = "Venue was not found." });
        }

        return null;
    }

}
