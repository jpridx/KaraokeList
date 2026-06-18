using KaraokeList.Api.Mapping;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ArtistsController(ArtistService artistService, ArtistLookupService artistLookupService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ArtistDto>>> GetAll()
    {
        var artists = await artistService.GetArtistsAsync();
        return Ok(artists.Select(a => a.ToDto()).ToList());
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<List<ArtistLookupDto>>> GetLookup()
    {
        var artists = await artistLookupService.GetArtistLookupsAsync();
        return Ok(artists.Select(a => a.ToDto()).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ArtistDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SortableName))
        {
            dto.SortableName = SortableNameFormatting.FromDisplayName(dto.Name);
        }

        await artistService.AddArtistAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ArtistDto dto)
    {
        dto.Id = id;
        await artistService.UpdateArtistAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await artistService.DeleteArtistAsync(id);
        return NoContent();
    }
}
