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
public class SongsController(SongService songService, CatalogIntegrityService integrity, CatalogMergeService mergeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SongDto>>> GetAll()
    {
        var songs = await songService.GetSongsAsync();
        return Ok(songs.Select(s => s.ToDto()).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<SongDto>> Create([FromBody] SongDto dto)
    {
        var validation = await ValidateArtistReferencesAsync(dto);
        if (validation is not null)
        {
            return validation;
        }

        var created = await songService.AddSongAsync(dto.ToEntity());
        var result = created.ToDto();
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = KaraokeRoles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] SongDto dto)
    {
        dto.Id = id;
        var validation = await ValidateArtistReferencesAsync(dto);
        if (validation is not null)
        {
            return validation;
        }

        await songService.UpdateSongAsync(dto.ToEntity());
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

        await songService.DeleteSongAsync(id);
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

    private async Task<ActionResult?> ValidateArtistReferencesAsync(SongDto dto)
    {
        if (dto.Artist is int artistId && !await integrity.ArtistExistsAsync(artistId))
        {
            return BadRequest(new ApiErrorResponse { Message = "Primary artist was not found." });
        }

        if (dto.SecondaryArtist is int secondaryId && !await integrity.ArtistExistsAsync(secondaryId))
        {
            return BadRequest(new ApiErrorResponse { Message = "Secondary artist was not found." });
        }

        return null;
    }
}
