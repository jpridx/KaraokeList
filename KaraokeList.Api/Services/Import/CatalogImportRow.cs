namespace KaraokeList.Api.Services.Import;

public record CatalogImportRow(string Title, string Artist, string? Genre, int? Year, int SourceRow);
