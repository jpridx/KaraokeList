using System.Text;
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

        var matches = new List<CanonicalMatchDto>();
        foreach (var recording in payload.Recordings)
        {
            var mapped = await MapRecordingAsync(recording, client, cancellationToken);
            if (mapped.Found)
            {
                matches.Add(mapped);
            }
        }

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

    private async Task<CanonicalMatchDto> MapRecordingAsync(
        MusicBrainzRecording recording,
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var credits = BuildArtistCredits(recording.ArtistCredit);
        var artistCreditDisplay = ComposeArtistCreditDisplay(credits);
        var primary = credits.FirstOrDefault();

        return new CanonicalMatchDto
        {
            Found = true,
            Title = recording.Title?.Trim() ?? string.Empty,
            ArtistName = primary?.Name ?? string.Empty,
            ArtistCreditDisplay = artistCreditDisplay,
            RecordingMbid = recording.Id,
            ArtistMbid = primary?.ArtistMbid,
            Score = recording.Score ?? 0,
            Disambiguation = string.IsNullOrWhiteSpace(recording.Disambiguation) ? null : recording.Disambiguation.Trim(),
            ArtistCredits = credits
        };
    }

    internal async Task<Dictionary<string, string>> FetchSortNamesAsync(
        HttpClient client,
        IEnumerable<string?> artistMbids,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mbid in artistMbids.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct(StringComparer.Ordinal))
        {
            var sortName = await FetchArtistSortNameAsync(client, mbid!, cancellationToken);
            if (!string.IsNullOrWhiteSpace(sortName))
            {
                result[mbid!] = sortName;
            }
        }

        return result;
    }

    private static List<CanonicalArtistCreditDto> BuildArtistCredits(List<MusicBrainzArtistCredit>? artistCredit)
    {
        if (artistCredit is not { Count: > 0 })
        {
            return [];
        }

        var credits = new List<CanonicalArtistCreditDto>();
        foreach (var entry in artistCredit)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            credits.Add(new CanonicalArtistCreditDto
            {
                Name = entry.Name.Trim(),
                ArtistMbid = entry.Artist?.Id,
                DisplayOrder = credits.Count,
                JoinPhrase = string.IsNullOrWhiteSpace(entry.JoinPhrase) ? null : entry.JoinPhrase
            });
        }

        return credits;
    }

    internal static string ComposeArtistCreditDisplay(IReadOnlyList<CanonicalArtistCreditDto> credits)
    {
        if (credits.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(credits[0].Name);
        for (var i = 1; i < credits.Count; i++)
        {
            var joinPhrase = credits[i].JoinPhrase;
            if (!string.IsNullOrWhiteSpace(joinPhrase))
            {
                builder.Append(joinPhrase.StartsWith(' ') ? joinPhrase : $" {joinPhrase}");
            }
            else
            {
                builder.Append(", ");
            }

            builder.Append(credits[i].Name);
        }

        return builder.ToString();
    }

    private async Task<string?> FetchArtistSortNameAsync(
        HttpClient client,
        string artistMbid,
        CancellationToken cancellationToken)
    {
        await EnforceRateLimitAsync(cancellationToken);
        using var response = await client.GetAsync($"artist/{artistMbid}?fmt=json", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var artist = await JsonSerializer.DeserializeAsync<MusicBrainzArtistDetail>(stream, JsonOptions, cancellationToken);
        return string.IsNullOrWhiteSpace(artist?.SortName) ? null : artist.SortName.Trim();
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

        [JsonPropertyName("joinphrase")]
        public string? JoinPhrase { get; set; }

        [JsonPropertyName("artist")]
        public MusicBrainzArtist? Artist { get; set; }
    }

    private sealed class MusicBrainzArtist
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private sealed class MusicBrainzArtistDetail
    {
        [JsonPropertyName("sort-name")]
        public string? SortName { get; set; }
    }
}
