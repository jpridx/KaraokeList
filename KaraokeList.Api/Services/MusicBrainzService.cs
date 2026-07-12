using System.Text.Json;
using System.Text.Json.Serialization;
using KaraokeList.Shared;

namespace KaraokeList.Api.Services;

public interface IMusicBrainzService
{
    Task<CanonicalLookupResponse> LookupAsync(string title, string artist, CancellationToken cancellationToken = default);
}

public sealed class MusicBrainzService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IMusicBrainzService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly SemaphoreSlim RateLimiter = new(1, 1);
    private static DateTime _lastRequestUtc = DateTime.MinValue;

    public async Task<CanonicalLookupResponse> LookupAsync(
        string title,
        string artist,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("MusicBrainz:Enabled", true))
        {
            return new CanonicalLookupResponse();
        }

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(artist))
        {
            return new CanonicalLookupResponse();
        }

        await EnforceRateLimitAsync(cancellationToken);

        var query = $"\"{EscapeQuery(title.Trim())}\" AND artist:\"{EscapeQuery(artist.Trim())}\"";
        var client = httpClientFactory.CreateClient("MusicBrainz");
        var url = $"recording?query={Uri.EscapeDataString(query)}&fmt=json&limit=5";

        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new CanonicalLookupResponse();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<MusicBrainzSearchResponse>(stream, JsonOptions, cancellationToken);
        if (payload?.Recordings is not { Count: > 0 })
        {
            return new CanonicalLookupResponse();
        }

        var matches = payload.Recordings
            .Select(MapRecording)
            .Where(m => m.Found)
            .ToList();

        if (matches.Count == 0)
        {
            return new CanonicalLookupResponse();
        }

        return new CanonicalLookupResponse
        {
            Match = matches[0],
            Alternatives = matches.Skip(1).ToList()
        };
    }

    private static CanonicalMatchDto MapRecording(MusicBrainzRecording recording)
    {
        var artistCredit = recording.ArtistCredit?.FirstOrDefault();
        return new CanonicalMatchDto
        {
            Found = true,
            Title = recording.Title?.Trim() ?? string.Empty,
            ArtistName = artistCredit?.Name?.Trim() ?? string.Empty,
            RecordingMbid = recording.Id,
            ArtistMbid = artistCredit?.Artist?.Id,
            Score = recording.Score ?? 0,
            Disambiguation = string.IsNullOrWhiteSpace(recording.Disambiguation) ? null : recording.Disambiguation.Trim()
        };
    }

    private static string EscapeQuery(string value) => value.Replace("\"", "\\\"", StringComparison.Ordinal);

    private static async Task EnforceRateLimitAsync(CancellationToken cancellationToken)
    {
        await RateLimiter.WaitAsync(cancellationToken);
        try
        {
            var elapsed = DateTime.UtcNow - _lastRequestUtc;
            if (elapsed < TimeSpan.FromSeconds(1))
            {
                await Task.Delay(TimeSpan.FromSeconds(1) - elapsed, cancellationToken);
            }

            _lastRequestUtc = DateTime.UtcNow;
        }
        finally
        {
            RateLimiter.Release();
        }
    }

    private sealed class MusicBrainzSearchResponse
    {
        [JsonPropertyName("recordings")]
        public List<MusicBrainzRecording>? Recordings { get; set; }
    }

    private sealed class MusicBrainzRecording
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("score")]
        public int? Score { get; set; }

        [JsonPropertyName("disambiguation")]
        public string? Disambiguation { get; set; }

        [JsonPropertyName("artist-credit")]
        public List<MusicBrainzArtistCredit>? ArtistCredit { get; set; }
    }

    private sealed class MusicBrainzArtistCredit
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("artist")]
        public MusicBrainzArtist? Artist { get; set; }
    }

    private sealed class MusicBrainzArtist
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
