using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public sealed class CatalogImportFileResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public CatalogImportResultDto? Response { get; init; }

    public static CatalogImportFileResult Ok(CatalogImportResultDto response) =>
        new() { Succeeded = true, Response = response };

    public static CatalogImportFileResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
