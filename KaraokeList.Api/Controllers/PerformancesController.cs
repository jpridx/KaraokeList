using KaraokeList.Api.Mapping;
using KaraokeList.Api.Services;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PerformancesController(
    PerformanceService performanceService,
    ICurrentUserSingerResolver currentUserSinger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PerformanceDto>>> GetAll([FromQuery] int? singerId, [FromQuery] int? songId)
    {
        var performances = await performanceService.GetPerformancesAsync(singerId, songId);
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

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PerformanceDto dto)
    {
        if (dto.Singer is null)
        {
            var singerId = await currentUserSinger.GetSingerIdAsync(User);
            if (singerId is null)
            {
                return BadRequest(new ApiErrorResponse { Message = "Your account is not linked to a singer profile." });
            }

            dto.Singer = singerId;
        }

        await performanceService.AddPerformanceAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PerformanceDto dto)
    {
        dto.Id = id;
        await performanceService.UpdatePerformanceAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
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

}
