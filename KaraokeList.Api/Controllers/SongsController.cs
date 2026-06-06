using KaraokeList.Api.Mapping;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SongsController(SongService songService) : ControllerBase
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
        await songService.AddSongAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SongDto dto)
    {
        dto.Id = id;
        await songService.UpdateSongAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await songService.DeleteSongAsync(id);
        return NoContent();
    }
}
