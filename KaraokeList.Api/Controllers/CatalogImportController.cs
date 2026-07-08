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

        var result = await importService.ImportAsync(parsed.Rows);
        return Ok(result);
    }
}
