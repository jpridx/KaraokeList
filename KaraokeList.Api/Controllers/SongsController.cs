using KaraokeList.Api.Mapping;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SongsController(SongService songService, CatalogIntegrityService integrity) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SongDto>>> GetAll()
    {
        var songs = await songService.GetSongsAsync();
        return Ok(songs.Select(s => s.ToDto()).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SongDto dto)
    {
        var validation = await ValidateArtistReferencesAsync(dto);
        if (validation is not null)
        {
            return validation;
        }

        await songService.AddSongAsync(dto.ToEntity());
        return NoContent();
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
