using System.Text.Json;
using KaraokeList.Data;
using KaraokeList.Shared;
using OpenAI.Chat;

namespace KaraokeList.Api.Services;

public interface IAiGenreService
{
    /// <summary>
    /// Returns a genre suggestion for the given song, or null fields when the model
    /// could not match any genre in the catalog or when AI is not configured.
    /// </summary>
    Task<GenreSuggestionResponse> SuggestGenreAsync(string title, string artist);
}

public sealed class AiGenreService(IConfiguration configuration, GenreService genreService) : IAiGenreService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GenreSuggestionResponse> SuggestGenreAsync(string title, string artist)
    {
        var apiKey = configuration["Ai:OpenAiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new GenreSuggestionResponse();
        }

        var genres = await genreService.GetGenresAsync();
        if (genres.Count == 0)
        {
            return new GenreSuggestionResponse();
        }

        var genreList = string.Join(", ", genres.Select(g => g.GenreName));

        var client = new ChatClient(model: "gpt-4o-mini", apiKey: apiKey);

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage(
                "You are a music genre classifier. " +
                "When given a song title and artist, respond with a JSON object containing a single key \"genre\" " +
                "whose value is exactly one genre name from the provided list. " +
                "If no genre fits well, use the closest match. " +
                "Respond only with the JSON object, nothing else."),
            ChatMessage.CreateUserMessage(
                $"Song: \"{title}\" | Artist: \"{artist}\" | Genres: {genreList}")
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        var completion = await client.CompleteChatAsync(messages, options);
        var raw = completion.Value.Content[0].Text;

        // Parse the JSON and match the returned genre name back to a catalog id.
        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("genre", out var genreElement))
        {
            return new GenreSuggestionResponse();
        }

        var suggested = genreElement.GetString();
        if (string.IsNullOrWhiteSpace(suggested))
        {
            return new GenreSuggestionResponse();
        }

        var match = genres.FirstOrDefault(g =>
            string.Equals(g.GenreName, suggested.Trim(), StringComparison.OrdinalIgnoreCase));

        return match is not null
            ? new GenreSuggestionResponse { GenreId = match.Id, GenreName = match.GenreName }
            : new GenreSuggestionResponse();
    }
}
