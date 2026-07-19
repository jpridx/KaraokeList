using System.Security.Claims;
using KaraokeList.Api.Services;
using KaraokeList.Api.Services.Import;
using KaraokeList.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KaraokeList.Api.Controllers;

[ApiController]
[Route("api/catalog/import")]
[Authorize(Roles = KaraokeRoles.Admin)]
public class CatalogImportController(
    CatalogImportService importService,
    CatalogImportSessionStore sessionStore,
    IHttpClientFactory httpClientFactory) : ControllerBase
{
    private static readonly HashSet<string> CsvExtensions = [".csv", ".tsv", ".txt"];
    private static readonly HashSet<string> XlsxExtensions = [".xlsx", ".xls"];

    [HttpPost("file")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> ImportFile(
        IFormFile file,
        [FromQuery] bool canonicize = true,
        CancellationToken cancellationToken = default)
    {
        var parsed = await ParseUploadedFileAsync(file);
        if (parsed.ErrorResult is not null)
        {
            return parsed.ErrorResult;
        }

        if (canonicize)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var session = sessionStore.CreateSession(userId, parsed.Rows!);
            return Ok(session);
        }

        var result = await importService.ImportAsync(parsed.Rows!, canonicize: false, cancellationToken);
        return Ok(result);
    }

    [HttpPost("gsheet")]
    public async Task<IActionResult> ImportGSheet(
        [FromBody] GSheetImportRequest request,
        [FromQuery] bool canonicize = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SheetUrl))
        {
            return BadRequest(new ApiErrorResponse { Message = "SheetUrl is required." });
        }

        var parsed = await FetchAndParseGSheetAsync(request.SheetUrl);
        if (parsed.ErrorResult is not null)
        {
            return parsed.ErrorResult;
        }

        if (canonicize)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var session = sessionStore.CreateSession(userId, parsed.Rows!);
            return Ok(session);
        }

        var result = await importService.ImportAsync(parsed.Rows!, canonicize: false, cancellationToken);
        return Ok(result);
    }

    [HttpPost("session/{sessionId}/chunk")]
    public async Task<ActionResult<CatalogImportChunkResultDto>> ImportChunk(
        string sessionId,
        [FromQuery] int? offset,
        [FromQuery] int? limit,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var session = sessionStore.GetSession(sessionId, userId);
        if (session is null)
        {
            return NotFound(new ApiErrorResponse { Message = "Import session not found or expired." });
        }

        var effectiveOffset = offset ?? session.NextOffset;
        if (effectiveOffset != session.NextOffset)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = $"Import must continue at row offset {session.NextOffset}."
            });
        }

        var chunkSize = Math.Clamp(
            limit ?? CatalogImportChunkRequest.DefaultChunkSize,
            1,
            CatalogImportChunkRequest.DefaultChunkSize);

        var chunkResult = await importService.ImportChunkAsync(
            session.Rows,
            effectiveOffset,
            chunkSize,
            session.Cumulative,
            cancellationToken);

        session.NextOffset = chunkResult.NextOffset;
        chunkResult.SessionId = sessionId;

        if (!chunkResult.HasMore)
        {
            sessionStore.RemoveSession(sessionId);
        }

        return Ok(chunkResult);
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    private async Task<(IReadOnlyList<CatalogImportRow>? Rows, IActionResult? ErrorResult)> ParseUploadedFileAsync(
        IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return (null, BadRequest(new ApiErrorResponse { Message = "No file was provided." }));
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        ICatalogRowParser parser;

        if (CsvExtensions.Contains(ext))
        {
            parser = new CsvCatalogRowParser();
        }
        else if (XlsxExtensions.Contains(ext))
        {
            parser = new XlsxCatalogRowParser();
        }
        else
        {
            return (null, BadRequest(new ApiErrorResponse
            {
                Message = $"Unsupported file type '{ext}'. Use .csv, .tsv, .xlsx, or .xls."
            }));
        }

        using var stream = file.OpenReadStream();
        var parsed = parser.Parse(stream);
        if (parsed.Error is not null)
        {
            return (null, BadRequest(new ApiErrorResponse { Message = parsed.Error }));
        }

        return (parsed.Rows, null);
    }

    private async Task<(IReadOnlyList<CatalogImportRow>? Rows, IActionResult? ErrorResult)> FetchAndParseGSheetAsync(
        string sheetUrl)
    {
        var csvUrl = GSheetImportHelper.BuildCsvExportUrl(sheetUrl);
        if (csvUrl is null)
        {
            return (null, BadRequest(new ApiErrorResponse
            {
                Message = "Could not parse a Google Sheets URL from the provided value."
            }));
        }

        var client = httpClientFactory.CreateClient("GoogleSheets");
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(csvUrl);
        }
        catch (Exception ex)
        {
            return (null, BadRequest(new ApiErrorResponse
            {
                Message = $"Failed to fetch the Google Sheet: {ex.Message}"
            }));
        }

        if (!response.IsSuccessStatusCode)
        {
            return (null, BadRequest(new ApiErrorResponse
            {
                Message = response.StatusCode == System.Net.HttpStatusCode.Forbidden
                    ? "The Google Sheet is not publicly accessible. Share it as 'Anyone with the link can view'."
                    : $"Google Sheets returned {(int)response.StatusCode}."
            }));
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var parsed = new CsvCatalogRowParser().Parse(stream);
        if (parsed.Error is not null)
        {
            return (null, BadRequest(new ApiErrorResponse { Message = parsed.Error }));
        }

        return (parsed.Rows, null);
    }
}
