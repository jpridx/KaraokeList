namespace KaraokeList.Shared;

public class SongAboutDto
{
    public int SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistDisplay { get; set; } = string.Empty;
    public IReadOnlyList<string> ArtistNames { get; set; } = [];
    public int? Year { get; set; }
    public string? GenreName { get; set; }
    public SongAboutEnrichmentDto? Enrichment { get; set; }
}

public class SongAboutEnrichmentDto
{
    public string? NotableRelease { get; set; }
    public IReadOnlyList<string> StyleTags { get; set; } = [];
    public int? DurationMs { get; set; }
    public string? VersionNote { get; set; }
    public string? ExternalUrl { get; set; }
}
