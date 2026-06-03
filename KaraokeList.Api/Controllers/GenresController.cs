using KaraokeList.Api.Mapping;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GenresController(GenreService genreService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<GenreDto>>> GetAll()
    {
        var genres = await genreService.GetGenresAsync();
        return Ok(genres.Select(g => g.ToDto()).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GenreDto dto)
    {
        await genreService.AddGenreAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] GenreDto dto)
    {
        dto.Id = id;
        await genreService.UpdateGenreAsync(dto.ToEntity());
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await genreService.DeleteGenreAsync(id);
        return NoContent();
    }
}
