using KaraokeList.Api.Mapping;
using KaraokeList.Api.Services;
using KaraokeList.Data;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/singers/me/lists")]
[Authorize]
public class SingerListsController(
    SingerListService singerListService,
    ICurrentUserSingerResolver currentUserSinger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SingerListDto>>> GetMyLists()
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var lists = await singerListService.GetListsAsync(singerId.Value!.Value);
        return Ok(lists.Select(ToDto).ToList());
    }

    [HttpGet("{listId:int}/songs")]
    public async Task<ActionResult<List<RepertoireSongDto>>> GetListSongs(
        int listId,
        [FromQuery] string sortBy = "title",
        [FromQuery] string sortDir = "asc",
        [FromQuery] int? genreId = null)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        if (!IsValidSort(sortBy))
        {
            return BadRequest(new ApiErrorResponse { Message = "Invalid sortBy. Use title, artist, genre, or lastPerformed." });
        }

        if (!IsValidSortDir(sortDir))
        {
            return BadRequest(new ApiErrorResponse { Message = "Invalid sortDir. Use asc or desc." });
        }

        var list = await singerListService.GetOwnedListAsync(singerId.Value!.Value, listId);
        if (list is null)
        {
            return NotFound();
        }

        var songs = await singerListService.GetListSongsAsync(
            singerId.Value.Value, listId, sortBy, sortDir, genreId);
        return Ok(songs.Select(s => s.ToDto()).ToList());
    }

    [HttpPost("{listId:int}/songs")]
    public async Task<IActionResult> AddSong(int listId, [FromBody] AddSingerListSongRequest request)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var result = await singerListService.TryAddSongAsync(singerId.Value!.Value, listId, request.SongId);
        if (!result.Succeeded)
        {
            var list = await singerListService.GetOwnedListAsync(singerId.Value.Value, listId);
            if (list is null)
            {
                return NotFound();
            }

            return BadRequest(new ApiErrorResponse { Message = result.Error ?? "Could not add song to list." });
        }

        return NoContent();
    }

    [HttpDelete("{listId:int}/songs/{songId:int}")]
    public async Task<IActionResult> RemoveSong(int listId, int songId)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var removed = await singerListService.RemoveSongAsync(singerId.Value!.Value, listId, songId);
        return removed ? NoContent() : NotFound();
    }

    private async Task<(int? Value, ActionResult? Result)> RequireSingerIdAsync()
    {
        var singerId = await currentUserSinger.GetSingerIdAsync(User);
        if (singerId is null)
        {
            return (null, BadRequest(new ApiErrorResponse { Message = "Your account is not linked to a singer profile." }));
        }

        return (singerId, null);
    }

    private static SingerListDto ToDto(SingerList list) => new()
    {
        Id = list.Id,
        Kind = list.Kind,
        DisplayName = SingerListKindNames.DisplayName(list.Kind)
    };

    private static bool IsValidSort(string sortBy) =>
        sortBy.Equals("title", StringComparison.OrdinalIgnoreCase)
        || sortBy.Equals("artist", StringComparison.OrdinalIgnoreCase)
        || sortBy.Equals("genre", StringComparison.OrdinalIgnoreCase)
        || sortBy.Equals("lastPerformed", StringComparison.OrdinalIgnoreCase);

    private static bool IsValidSortDir(string sortDir) =>
        sortDir.Equals("asc", StringComparison.OrdinalIgnoreCase)
        || sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
}
