namespace KaraokeList.Api.Services.Import;

internal interface ICatalogRowParser
{
    ParseResult Parse(Stream data);
}

internal record ParseResult(IReadOnlyList<CatalogImportRow> Rows, string? Error);
