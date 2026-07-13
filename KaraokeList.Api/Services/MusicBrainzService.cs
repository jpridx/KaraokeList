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
    private const int SearchLimit = 15;

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

        var client = httpClientFactory.CreateClient("MusicBrainz");
        var recordings = await SearchRecordingsAsync(client, title.Trim(), artist.Trim(), cancellationToken);
        if (recordings.Count == 0)
        {
            return new CanonicalLookupResponse();
        }

        var bestRecording = SelectHeadOfClassRecording(recordings);
        if (bestRecording?.Id is null)
        {
            return new CanonicalLookupResponse();
        }

        var enriched = await FetchRecordingDetailsAsync(client, bestRecording, cancellationToken);
        var mapped = await MapRecordingAsync(enriched ?? bestRecording, client, cancellationToken);
        if (!mapped.Found)
        {
            return new CanonicalLookupResponse();
        }

        var matches = new List<CanonicalMatchDto> { mapped };
        foreach (var recording in recordings.Where(r => r.Id != bestRecording.Id))
        {
            var alternative = await MapRecordingAsync(recording, client, cancellationToken, includeMetadata: false);
            if (alternative.Found)
            {
                matches.Add(alternative);
            }
        }

        return new CanonicalLookupResponse
        {
            Match = matches[0],
            Alternatives = matches.Skip(1).ToList()
        };
    }

    private async Task<List<MusicBrainzRecording>> SearchRecordingsAsync(
        HttpClient client,
        string title,
        string artist,
        CancellationToken cancellationToken)
    {
        foreach (var query in MusicBrainzSearchHelper.BuildSearchQueries(title, artist))
        {
            var recordings = await ExecuteSearchAsync(client, query, cancellationToken);
            if (recordings.Count > 0)
            {
                return recordings;
            }
        }

        var apostropheFreeTitle = MusicBrainzSearchHelper.WithoutApostrophes(
            MusicBrainzSearchHelper.NormalizeSearchTerm(title));
        var apostropheFreeArtist = MusicBrainzSearchHelper.WithoutApostrophes(
            MusicBrainzSearchHelper.NormalizeSearchTerm(artist));
        if (!string.Equals(apostropheFreeTitle, title, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(apostropheFreeArtist, artist, StringComparison.OrdinalIgnoreCase))
        {
            var fallbackQuery =
                $"{MusicBrainzSearchHelper.EscapeQuery(apostropheFreeTitle)} AND artist:{MusicBrainzSearchHelper.EscapeQuery(apostropheFreeArtist)}";
            var fallbackRecordings = await ExecuteSearchAsync(client, fallbackQuery, cancellationToken);
            if (fallbackRecordings.Count > 0)
            {
                return fallbackRecordings;
            }
        }

        return [];
    }

    private async Task<List<MusicBrainzRecording>> ExecuteSearchAsync(
        HttpClient client,
        string query,
        CancellationToken cancellationToken)
    {
        await EnforceRateLimitAsync(cancellationToken);
        var url =
            $"recording?query={Uri.EscapeDataString(query)}&fmt=json&limit={SearchLimit}&inc=genres+tags+artist-credits+releases";

        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<MusicBrainzSearchResponse>(stream, JsonOptions, cancellationToken);
        return payload?.Recordings ?? [];
    }

    /// <summary>
    /// Picks the studio-style original from MusicBrainz search hits — earliest release wins over reissues.
    /// </summary>
    internal static MusicBrainzRecording? SelectHeadOfClassRecording(IReadOnlyList<MusicBrainzRecording> recordings)
    {
        return recordings
            .OrderBy(r => IsUnwantedRecording(r) ? 1 : 0)
            .ThenBy(r => GetEarliestReleaseYear(r) ?? int.MaxValue)
            .ThenByDescending(r => r.Score ?? 0)
            .FirstOrDefault();
    }

    internal static int? GetEarliestReleaseYear(MusicBrainzRecording recording)
    {
        var dates = new List<string?> { recording.FirstReleaseDate };

        if (recording.ReleaseGroups is { Count: > 0 })
        {
            dates.AddRange(recording.ReleaseGroups.Select(rg => rg.FirstReleaseDate));
        }

        if (recording.Releases is { Count: > 0 })
        {
            foreach (var release in recording.Releases)
            {
                dates.Add(release.Date);
                dates.Add(release.ReleaseGroup?.FirstReleaseDate);
            }
        }

        return MusicBrainzSearchHelper.ResolveEarliestReleaseYear(dates);
    }

    private async Task<MusicBrainzRecording?> FetchRecordingDetailsAsync(
        HttpClient client,
        MusicBrainzRecording recording,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recording.Id))
        {
            return recording;
        }

        await EnforceRateLimitAsync(cancellationToken);
        using var response = await client.GetAsync(
            $"recording/{recording.Id}?fmt=json&inc=genres+tags+release-groups+artist-credits+releases",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return recording;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<MusicBrainzRecording>(stream, JsonOptions, cancellationToken)
            ?? recording;
    }

    private async Task<CanonicalMatchDto> MapRecordingAsync(
        MusicBrainzRecording recording,
        HttpClient client,
        CancellationToken cancellationToken,
        bool includeMetadata = true)
    {
        var credits = BuildArtistCredits(recording.ArtistCredit);
        var artistCreditDisplay = ComposeArtistCreditDisplay(credits);
        var primary = credits.FirstOrDefault();

        var match = new CanonicalMatchDto
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

        if (includeMetadata)
        {
            match.SuggestedGenreName = ResolveSuggestedGenre(recording);
        }

        match.Year = GetEarliestReleaseYear(recording);

        return match;
    }

    private static string? ResolveSuggestedGenre(MusicBrainzRecording recording)
    {
        var candidates = new List<(string Name, int Count)>();
        AddGenreCandidates(candidates, recording.Genres);
        AddGenreCandidates(candidates, recording.Tags);

        foreach (var credit in recording.ArtistCredit ?? [])
        {
            AddGenreCandidates(candidates, credit.Artist?.Genres);
            AddGenreCandidates(candidates, credit.Artist?.Tags);
        }

        foreach (var releaseGroup in recording.ReleaseGroups ?? [])
        {
            AddGenreCandidates(candidates, releaseGroup.Genres);
            AddGenreCandidates(candidates, releaseGroup.Tags);
        }

        return MusicBrainzGenreResolver.ResolveBestGenre(candidates);
    }

    private static void AddGenreCandidates(List<(string Name, int Count)> candidates, List<MusicBrainzLabel>? labels)
    {
        if (labels is not { Count: > 0 })
        {
            return;
        }

        foreach (var label in labels)
        {
            if (string.IsNullOrWhiteSpace(label.Name))
            {
                continue;
            }

            candidates.Add((label.Name.Trim(), Math.Max(label.Count ?? 1, 1)));
        }
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

    private static bool IsUnwantedRecording(MusicBrainzRecording recording) =>
        IsLiveRecording(recording) || IsCompilationOrRemaster(recording);

    private static bool IsLiveRecording(MusicBrainzRecording recording)
    {
        if (recording.Disambiguation?.Contains("live", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return HasSecondaryType(recording, "Live");
    }

    private static bool IsCompilationOrRemaster(MusicBrainzRecording recording)
    {
        if (recording.Disambiguation?.Contains("compilation", StringComparison.OrdinalIgnoreCase) == true
            || recording.Disambiguation?.Contains("remaster", StringComparison.OrdinalIgnoreCase) == true
            || recording.Disambiguation?.Contains("reissue", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return HasSecondaryType(recording, "Compilation")
            || HasSecondaryType(recording, "Remaster")
            || HasSecondaryType(recording, "Reissue");
    }

    private static bool HasSecondaryType(MusicBrainzRecording recording, string typeName)
    {
        foreach (var secondaryTypes in EnumerateSecondaryTypes(recording))
        {
            foreach (var secondaryType in secondaryTypes)
            {
                if (secondaryType.Contains(typeName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<IReadOnlyList<string>> EnumerateSecondaryTypes(MusicBrainzRecording recording)
    {
        if (recording.Releases is not null)
        {
            foreach (var release in recording.Releases)
            {
                if (release.ReleaseGroup?.SecondaryTypes is { Count: > 0 } secondaryTypes)
                {
                    yield return secondaryTypes;
                }
            }
        }

        if (recording.ReleaseGroups is not null)
        {
            foreach (var releaseGroup in recording.ReleaseGroups)
            {
                if (releaseGroup.SecondaryTypes is { Count: > 0 } secondaryTypes)
                {
                    yield return secondaryTypes;
                }
            }
        }
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

    internal sealed class MusicBrainzSearchResponse
    {
        [JsonPropertyName("recordings")]
        public List<MusicBrainzRecording>? Recordings { get; set; }
    }

    internal sealed class MusicBrainzRecording
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("score")]
        public int? Score { get; set; }

        [JsonPropertyName("disambiguation")]
        public string? Disambiguation { get; set; }

        [JsonPropertyName("first-release-date")]
        public string? FirstReleaseDate { get; set; }

        [JsonPropertyName("genres")]
        public List<MusicBrainzLabel>? Genres { get; set; }

        [JsonPropertyName("tags")]
        public List<MusicBrainzLabel>? Tags { get; set; }

        [JsonPropertyName("artist-credit")]
        public List<MusicBrainzArtistCredit>? ArtistCredit { get; set; }

        [JsonPropertyName("releases")]
        public List<MusicBrainzRelease>? Releases { get; set; }

        [JsonPropertyName("release-groups")]
        public List<MusicBrainzReleaseGroup>? ReleaseGroups { get; set; }
    }

    internal sealed class MusicBrainzRelease
    {
        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("release-group")]
        public MusicBrainzReleaseGroup? ReleaseGroup { get; set; }
    }

    internal sealed class MusicBrainzReleaseGroup
    {
        [JsonPropertyName("first-release-date")]
        public string? FirstReleaseDate { get; set; }

        [JsonPropertyName("genres")]
        public List<MusicBrainzLabel>? Genres { get; set; }

        [JsonPropertyName("tags")]
        public List<MusicBrainzLabel>? Tags { get; set; }

        [JsonPropertyName("secondary-types")]
        public List<string>? SecondaryTypes { get; set; }
    }

    internal sealed class MusicBrainzLabel
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("count")]
        public int? Count { get; set; }
    }

    internal sealed class MusicBrainzArtistCredit
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("joinphrase")]
        public string? JoinPhrase { get; set; }

        [JsonPropertyName("artist")]
        public MusicBrainzArtist? Artist { get; set; }
    }

    internal sealed class MusicBrainzArtist
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("genres")]
        public List<MusicBrainzLabel>? Genres { get; set; }

        [JsonPropertyName("tags")]
        public List<MusicBrainzLabel>? Tags { get; set; }
    }

    private sealed class MusicBrainzArtistDetail
    {
        [JsonPropertyName("sort-name")]
        public string? SortName { get; set; }
    }
}
