using KaraokeList.Api.Mapping;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VenuesController(VenueService venueService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<VenueDto>>> GetAll()
    {
        var venues = await venueService.GetVenuesAsync();
        return Ok(venues.Select(v => v.ToDto()).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VenueDto dto)
    {
        await venueService.AddVenueAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] VenueDto dto)
    {
        dto.Id = id;
        await venueService.UpdateVenueAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await venueService.DeleteVenueAsync(id);
        return NoContent();
    }
}
