using KaraokeList.Api.Services;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SongsController(
    SongCatalogService songCatalogService,
    ISongAboutService songAboutService,
    CatalogIntegrityService integrity,
    CatalogMergeService mergeService) : ControllerBase
{
    [HttpGet("{id:int}/about")]
    public async Task<ActionResult<SongAboutDto>> GetAbout(
        int id,
        [FromQuery] bool enrich = false,
        CancellationToken cancellationToken = default)
    {
        var about = await songAboutService.GetAboutAsync(id, enrich, cancellationToken);
        return about is null
            ? NotFound(new ApiErrorResponse { Message = "Song was not found." })
            : Ok(about);
    }

    [HttpGet]
    public async Task<ActionResult<List<SongDto>>> GetAll()
    {
        var songs = await songCatalogService.GetSongsAsync();
        return Ok(songs);
    }

    [HttpPost]
    public async Task<ActionResult<SongDto>> Create([FromBody] SongDto dto)
    {
        var validation = await songCatalogService.ValidateArtistReferencesAsync(dto.Artists);
        if (validation is not null)
        {
            return BadRequest(new ApiErrorResponse { Message = validation });
        }

        var created = await songCatalogService.AddSongAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = KaraokeRoles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] SongDto dto)
    {
        dto.Id = id;
        var validation = await songCatalogService.ValidateArtistReferencesAsync(dto.Artists);
        if (validation is not null)
        {
            return BadRequest(new ApiErrorResponse { Message = validation });
        }

        await songCatalogService.UpdateSongAsync(dto);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = KaraokeRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        if (await integrity.HasPerformancesForSongAsync(id))
        {
            return Conflict(new ApiErrorResponse
            {
                Message = "Cannot delete this song because performances reference it."
            });
        }

        await songCatalogService.DeleteSongAsync(id);
        return NoContent();
    }

    [HttpPost("{sourceId:int}/merge-into/{targetId:int}")]
    [Authorize(Roles = KaraokeRoles.Admin)]
    public async Task<IActionResult> MergeInto(int sourceId, int targetId)
    {
        var (succeeded, error) = await mergeService.MergeAsync(sourceId, targetId);
        if (!succeeded)
            return BadRequest(new ApiErrorResponse { Message = error ?? "Merge failed." });

        return NoContent();
    }
}
