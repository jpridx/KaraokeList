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
        var singerId = await currentUserSinger.GetSingerIdAsync(User);
        if (singerId is null)
        {
            return BadRequest(new ApiErrorResponse { Message = "Your account is not linked to a singer profile." });
        }

        var summary = await performanceService.GetSongPerformanceSummaryAsync(singerId.Value, songId);
        return Ok(summary?.ToDto() ?? new SongPerformanceSummaryDto { SongId = songId });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PerformanceDto dto)
    {
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

}
