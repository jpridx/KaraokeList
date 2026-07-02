using KaraokeList.Api.Services;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController(IAiGenreService aiGenreService) : ControllerBase
{
    /// <summary>
    /// Suggests a genre for a song using an LLM. Returns null fields when AI is
    /// not configured or the model cannot match any genre in the catalog.
    /// </summary>
    [HttpPost("suggest-genre")]
    public async Task<ActionResult<GenreSuggestionResponse>> SuggestGenre(
        [FromBody] GenreSuggestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Artist))
        {
            return BadRequest(new ApiErrorResponse { Message = "Title and Artist are required." });
        }

        var suggestion = await aiGenreService.SuggestGenreAsync(request.Title.Trim(), request.Artist.Trim());
        return Ok(suggestion);
    }
}
