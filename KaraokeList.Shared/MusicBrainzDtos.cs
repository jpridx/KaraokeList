namespace KaraokeList.Shared;

public class CanonicalLookupRequest
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
}

public class CanonicalArtistCreditDto
{
    public string Name { get; set; } = string.Empty;
    public string? ArtistMbid { get; set; }
    public int DisplayOrder { get; set; }
    public string? JoinPhrase { get; set; }
}

public class CanonicalMatchDto
{
    public bool Found { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string ArtistCreditDisplay { get; set; } = string.Empty;
    public string? RecordingMbid { get; set; }
    public string? ArtistMbid { get; set; }
    public int Score { get; set; }
    public string? Disambiguation { get; set; }
    public List<CanonicalArtistCreditDto> ArtistCredits { get; set; } = [];
}

public class CanonicalLookupResponse
{
    public CanonicalMatchDto Match { get; set; } = new();
    public List<CanonicalMatchDto> Alternatives { get; set; } = [];
}

public class ApplyCanonicalRequest
{
    public int SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string? ArtistCreditDisplay { get; set; }
    public string? RecordingMbid { get; set; }
    public string? ArtistMbid { get; set; }
    public List<CanonicalArtistCreditDto> ArtistCredits { get; set; } = [];
}

public class ApplyCanonicalResponse
{
    public int SongId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string ArtistCreditDisplay { get; set; } = string.Empty;
    public int? ArtistId { get; set; }
    public string? RecordingMbid { get; set; }
    public string? ArtistMbid { get; set; }
    public List<SongArtistDto> Artists { get; set; } = [];
}

public class CatalogVerifyRequest
{
    public int Offset { get; set; }
    public int Limit { get; set; } = 25;
    public bool UnverifiedOnly { get; set; } = true;
}

public class CatalogVerifyResultDto
{
    public int TotalMatching { get; set; }
    public int Scanned { get; set; }
    public int Offset { get; set; }
    public bool HasMore { get; set; }
    public List<CatalogVerifyItemDto> Items { get; set; } = [];
}

public class CatalogVerifyItemDto
{
    public int SongId { get; set; }
    public string CurrentTitle { get; set; } = string.Empty;
    public string CurrentArtistName { get; set; } = string.Empty;
    public string CurrentArtistDisplay { get; set; } = string.Empty;
    public string? RecordingMbid { get; set; }
    public CanonicalMatchDto? Suggestion { get; set; }
    public bool NamesMatch { get; set; }
}
