using KaraokeList.Api.Mapping;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SingerSongsController(SingerSongService singerSongService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SingerSongDto>>> GetAll()
    {
        var singerSongs = await singerSongService.GetSingerSongsAsync();
        return Ok(singerSongs.Select(s => s.ToDto()).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SingerSongDto dto)
    {
        await singerSongService.AddSingerSongAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SingerSongDto dto)
    {
        dto.Id = id;
        await singerSongService.UpdateSingerSongAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await singerSongService.DeleteSingerSongAsync(id);
        return NoContent();
    }
}
