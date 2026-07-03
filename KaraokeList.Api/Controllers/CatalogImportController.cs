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
    IHttpClientFactory httpClientFactory) : ControllerBase
{
    private static readonly HashSet<string> CsvExtensions = [".csv", ".tsv", ".txt"];
    private static readonly HashSet<string> XlsxExtensions = [".xlsx", ".xls"];

    [HttpPost("file")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<CatalogImportResultDto>> ImportFile(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiErrorResponse { Message = "No file was provided." });

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

        var result = await importService.ImportAsync(parsed.Rows);
        return Ok(result);
    }

    [HttpPost("gsheet")]
    public async Task<ActionResult<CatalogImportResultDto>> ImportGSheet([FromBody] GSheetImportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SheetUrl))
            return BadRequest(new ApiErrorResponse { Message = "SheetUrl is required." });

        var csvUrl = BuildGSheetCsvUrl(request.SheetUrl);
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

        var result = await importService.ImportAsync(parsed.Rows);
        return Ok(result);
    }

    /// <summary>
    /// Converts a Google Sheets sharing URL to a CSV export URL.
    /// Handles /edit, /pub, and plain spreadsheet URLs, preserving the gid (tab id) when present.
    /// </summary>
    private static string? BuildGSheetCsvUrl(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri))
            return null;

        if (!uri.Host.Equals("docs.google.com", StringComparison.OrdinalIgnoreCase))
            return null;

        // Extract sheet id from path: /spreadsheets/d/{id}/...
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var dIdx = Array.IndexOf(segments, "d");
        if (dIdx < 0 || dIdx + 1 >= segments.Length)
            return null;

        var sheetId = segments[dIdx + 1];

        // Try to preserve gid from fragment (#gid=123) or query (?gid=123)
        var gid = ExtractGid(uri.Fragment) ?? ExtractGid(uri.Query);
        var gidParam = gid is not null ? $"&gid={gid}" : string.Empty;

        return $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv{gidParam}";
    }

    private static string? ExtractGid(string haystack)
    {
        if (string.IsNullOrEmpty(haystack)) return null;
        var prefix = "gid=";
        var idx = haystack.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var start = idx + prefix.Length;
        var end = haystack.IndexOf('&', start);
        return end < 0 ? haystack[start..] : haystack[start..end];
    }
}
