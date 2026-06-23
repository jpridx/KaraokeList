using KaraokeList.Api.Mapping;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SingersController(SingerService singerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SingerDto>>> GetAll()
    {
        var singers = await singerService.GetSingersAsync();
        return Ok(singers.Select(s => s.ToDto()).ToList());
    }

    [HttpPost]
    [Authorize(Roles = KaraokeRoles.Admin)]
    public async Task<IActionResult> Create([FromBody] SingerDto dto)
    {
        await singerService.AddSingerAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = KaraokeRoles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] SingerDto dto)
    {
        dto.Id = id;
        await singerService.UpdateSingerAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = KaraokeRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        await singerService.DeleteSingerAsync(id);
        return NoContent();
    }
}
