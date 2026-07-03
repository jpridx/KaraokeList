namespace KaraokeList.Api.Services.Import;

internal record CatalogImportRow(string Title, string Artist, string? Genre, int? Year, int SourceRow);
