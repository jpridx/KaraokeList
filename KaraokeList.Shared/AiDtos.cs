namespace KaraokeList.Shared;

public class GenreSuggestionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
}

/// <summary>
/// Returned by POST api/ai/suggest-genre.
/// <see cref="GenreId"/> and <see cref="GenreName"/> are null when the model
/// could not match any genre in the catalog (caller should fall back to manual pick).
/// </summary>
public class GenreSuggestionResponse
{
    public int? GenreId { get; set; }
    public string? GenreName { get; set; }
}
