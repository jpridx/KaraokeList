using KaraokeList.Api.Mapping;
using KaraokeList.Api.Services;
using KaraokeList.Api.Services.Import;
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
    RepertoireImportService repertoireImportService,
    TicklerExclusionService ticklerExclusionService,
    SongGenreService songGenreService,
    ICurrentUserSingerResolver currentUserSinger,
    IHttpClientFactory httpClientFactory) : ControllerBase
{
    private static readonly HashSet<string> CsvExtensions = [".csv", ".tsv", ".txt"];
    private static readonly HashSet<string> XlsxExtensions = [".xlsx", ".xls"];
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

    [HttpGet("~/api/singers/me/songs/{songId:int}/list-membership")]
    public async Task<ActionResult<SongListMembershipDto>> GetSongListMembership(int songId)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        if (!await singerListService.SongExistsAsync(songId))
        {
            return NotFound();
        }

        var kinds = await singerListService.GetListKindsForSongAsync(singerId.Value!.Value, songId);
        return Ok(new SongListMembershipDto { Lists = kinds });
    }

    [HttpGet("~/api/singers/me/songs/{songId:int}/tickler-exclusion")]
    public async Task<ActionResult<SongTicklerExclusionDto>> GetSongTicklerExclusion(int songId)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        if (!await singerListService.SongExistsAsync(songId))
        {
            return NotFound();
        }

        var exclusion = await ticklerExclusionService.GetExclusionAsync(singerId.Value!.Value, songId);
        return Ok(exclusion);
    }

    [HttpGet("~/api/singers/me/tickler-exclusions")]
    public async Task<ActionResult<TicklerExclusionListDto>> GetTicklerExclusions()
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var songIds = await ticklerExclusionService.GetExcludedSongIdsAsync(singerId.Value!.Value);
        return Ok(new TicklerExclusionListDto { SongIds = songIds });
    }

    [HttpPut("~/api/singers/me/songs/{songId:int}/tickler-exclusion")]
    public async Task<IActionResult> SetSongTicklerExclusion(
        int songId,
        [FromBody] UpdateSongTicklerExclusionRequest request)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var result = await ticklerExclusionService.SetExclusionAsync(
            singerId.Value!.Value, songId, request.Reason);
        if (!result.Succeeded)
        {
            return result.Error == "Song was not found."
                ? NotFound()
                : BadRequest(new ApiErrorResponse { Message = result.Error ?? "Could not update tickler exclusion." });
        }

        return NoContent();
    }

    [HttpDelete("~/api/singers/me/songs/{songId:int}/tickler-exclusion")]
    public async Task<IActionResult> RemoveSongTicklerExclusion(int songId)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var removed = await ticklerExclusionService.RemoveExclusionAsync(singerId.Value!.Value, songId);
        return removed ? NoContent() : NotFound();
    }

    [HttpPut("~/api/singers/me/songs/{songId:int}/genre")]
    public async Task<IActionResult> SetSongGenre(int songId, [FromBody] UpdateSongGenreRequest request)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var result = await songGenreService.UpdateGenreAsync(songId, request.GenreId);
        if (!result.Succeeded)
        {
            return result.Error == "Song was not found."
                ? NotFound()
                : BadRequest(new ApiErrorResponse { Message = result.Error ?? "Could not update song genre." });
        }

        return NoContent();
    }

    [HttpPost("import")]
    public async Task<ActionResult<ImportSingerListSongsResponse>> ImportSongs(
        [FromBody] ImportSingerListSongsRequest request)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        if (request.SongIds is null || request.SongIds.Count == 0)
        {
            return BadRequest(new ApiErrorResponse { Message = "At least one songId is required." });
        }

        var result = await singerListService.ImportSongsAsync(
            singerId.Value!.Value, request.ListKind, request.SongIds);
        if (!result.Succeeded)
        {
            return BadRequest(new ApiErrorResponse { Message = result.Error ?? "Could not import songs." });
        }

        return Ok(result.Result);
    }

    [HttpPost("import-file")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImportSingerListFromFileResponse>> ImportSongsFromFile(
        [FromForm] SingerListKind? listKind = null)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        if (!Request.HasFormContentType)
        {
            return BadRequest(new ApiErrorResponse { Message = "Expected multipart/form-data." });
        }

        var file = Request.Form.Files.GetFile("file") ?? Request.Form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ApiErrorResponse { Message = "No file was provided." });
        }

        var targetListKind = listKind
            ?? (Enum.TryParse<SingerListKind>(Request.Form["listKind"], out var parsedKind)
                ? parsedKind
                : SingerListKind.MyRepertoire);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        ICatalogRowParser parser;
        if (CsvExtensions.Contains(ext))
            parser = new CsvCatalogRowParser();
        else if (XlsxExtensions.Contains(ext))
            parser = new XlsxCatalogRowParser();
        else
            return BadRequest(new ApiErrorResponse { Message = $"Unsupported file type '{ext}'. Use .csv, .tsv, .xlsx, or .xls." });

        using var stream = file.OpenReadStream();
        var parsed = parser.Parse(stream);
        if (parsed.Error is not null)
            return BadRequest(new ApiErrorResponse { Message = parsed.Error });

        var result = await repertoireImportService.ImportRowsAsync(
            singerId.Value!.Value, targetListKind, parsed.Rows);
        if (!result.Succeeded)
        {
            if (result.Result is not null)
                return BadRequest(result.Result);

            return BadRequest(new ApiErrorResponse { Message = result.Error ?? "Could not import songs." });
        }

        return Ok(result.Result);
    }

    [HttpPost("import/gsheet")]
    public async Task<ActionResult<ImportSingerListFromFileResponse>> ImportSongsFromGSheet(
        [FromBody] ImportSingerListFromGSheetRequest request)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        if (string.IsNullOrWhiteSpace(request.SheetUrl))
            return BadRequest(new ApiErrorResponse { Message = "SheetUrl is required." });

        var csvUrl = GSheetImportHelper.BuildCsvExportUrl(request.SheetUrl);
        if (csvUrl is null)
            return BadRequest(new ApiErrorResponse { Message = "Could not parse a Google Sheets URL from the provided value." });

        var client = httpClientFactory.CreateClient("GoogleSheets");
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(csvUrl);
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiErrorResponse { Message = $"Failed to fetch the Google Sheet: {ex.Message}" });
        }

        if (!response.IsSuccessStatusCode)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = response.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "The Google Sheet is not publicly accessible. Share it as 'Anyone with the link can view'."
                    : $"Google Sheets returned {(int)response.StatusCode}."
            });
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var parsed = new CsvCatalogRowParser().Parse(stream);
        if (parsed.Error is not null)
            return BadRequest(new ApiErrorResponse { Message = parsed.Error });

        var result = await repertoireImportService.ImportRowsAsync(
            singerId.Value!.Value, request.ListKind, parsed.Rows);
        if (!result.Succeeded)
        {
            if (result.Result is not null)
                return BadRequest(result.Result);

            return BadRequest(new ApiErrorResponse { Message = result.Error ?? "Could not import songs." });
        }

        return Ok(result.Result);
    }

    [HttpGet("{listId:int}/songs/title-artist-collision")]
    public async Task<ActionResult<TitleArtistCollisionDto>> GetTitleArtistCollision(int listId, [FromQuery] int songId)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var list = await singerListService.GetOwnedListAsync(singerId.Value!.Value, listId);
        if (list is null)
        {
            return NotFound();
        }

        if (!await singerListService.SongExistsAsync(songId))
        {
            return NotFound(new ApiErrorResponse { Message = "Song was not found." });
        }

        var collision = await singerListService.FindTitleArtistCollisionOnListAsync(listId, songId);
        return collision is null ? NoContent() : Ok(collision);
    }

    [HttpPost("{listId:int}/songs")]
    public async Task<IActionResult> AddSong(int listId, [FromBody] AddSingerListSongRequest request)
    {
        var singerId = await RequireSingerIdAsync();
        if (singerId.Result is not null)
        {
            return singerId.Result;
        }

        var result = await singerListService.TryAddSongAsync(
            singerId.Value!.Value,
            listId,
            request.SongId,
            request.AllowTitleArtistDuplicate);
        if (!result.Succeeded)
        {
            var list = await singerListService.GetOwnedListAsync(singerId.Value.Value, listId);
            if (list is null)
            {
                return NotFound();
            }

            if (result.Error?.StartsWith("This list already has", StringComparison.Ordinal) == true)
            {
                return Conflict(new ApiErrorResponse { Message = result.Error });
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
