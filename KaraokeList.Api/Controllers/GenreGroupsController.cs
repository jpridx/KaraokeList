using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/genre-groups")]
[Authorize]
public class GenreGroupsController(GenreGroupService genreGroupService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<GenreGroupDto>>> GetAll()
    {
        var groups = await genreGroupService.GetAllAsync();
        return Ok(groups);
    }

    [HttpPut("{id:int}/genres")]
    [Authorize(Roles = KaraokeRoles.Admin)]
    public async Task<IActionResult> ReplaceGenres(int id, [FromBody] UpdateGenreGroupGenresRequest request)
    {
        var (succeeded, error) = await genreGroupService.ReplaceGroupGenresAsync(id, request.Genres);
        if (!succeeded)
        {
            return BadRequest(new ApiErrorResponse { Message = error });
        }

        return NoContent();
    }
}
