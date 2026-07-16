using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KaraokeList.Shared;

namespace KaraokeList.Api.Services;

public interface IMusicBrainzService
{
    Task<CanonicalLookupResponse> LookupAsync(string title, string artist, CancellationToken cancellationToken = default);
    Task<SongAboutEnrichmentDto?> GetRecordingEnrichmentAsync(string recordingMbid, CancellationToken cancellationToken = default);
}

public sealed class MusicBrainzService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IMusicBrainzService
{
    private const int SearchLimit = 50;

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
        var trimmedTitle = title.Trim();
        var trimmedArtist = artist.Trim();
        var recordings = await SearchRecordingsAsync(client, trimmedTitle, trimmedArtist, cancellationToken);
        if (recordings.Count == 0)
        {
            return new CanonicalLookupResponse();
        }

        var recordingsById = recordings
            .Where(r => !string.IsNullOrWhiteSpace(r.Id))
            .ToDictionary(r => r.Id!, StringComparer.Ordinal);
        var searchScores = recordings
            .Where(r => !string.IsNullOrWhiteSpace(r.Id))
            .ToDictionary(r => r.Id!, r => r.Score ?? 0, StringComparer.Ordinal);

        var allMatches = new List<CanonicalMatchDto>();
        foreach (var recording in recordings)
        {
            if (string.IsNullOrWhiteSpace(recording.Id))
            {
                continue;
            }

            var mapped = await MapRecordingAsync(
                recordingsById[recording.Id],
                client,
                cancellationToken,
                includeMetadata: false);
            if (mapped.Found)
            {
                allMatches.Add(mapped);
            }
        }

        var ordered = RankMatches(allMatches, trimmedTitle, recordingsById);
        if (ordered.Count == 0)
        {
            return new CanonicalLookupResponse();
        }

        var bestMatch = ordered[0];
        if (bestMatch.RecordingMbid is { Length: > 0 } bestId
            && recordingsById.TryGetValue(bestId, out var bestRecording))
        {
            var searchScore = searchScores.GetValueOrDefault(bestId);
            var enriched = await FetchRecordingDetailsAsync(client, bestRecording, cancellationToken);
            if (enriched?.Id is not null)
            {
                enriched.Score = searchScore;
                recordingsById[enriched.Id] = enriched;
                bestMatch = await MapRecordingAsync(enriched, client, cancellationToken, includeMetadata: true);
                bestMatch.Score = searchScore;
                ordered[0] = bestMatch;
            }
        }

        return new CanonicalLookupResponse
        {
            Match = ordered[0],
            Alternatives = ordered.Skip(1).ToList()
        };
    }

    public async Task<SongAboutEnrichmentDto?> GetRecordingEnrichmentAsync(
        string recordingMbid,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("MusicBrainz:Enabled", true)
            || string.IsNullOrWhiteSpace(recordingMbid))
        {
            return null;
        }

        var client = httpClientFactory.CreateClient("MusicBrainz");
        var recording = await FetchRecordingDetailsAsync(
            client,
            new MusicBrainzRecording { Id = recordingMbid.Trim() },
            cancellationToken);

        return MapRecordingToEnrichment(recording);
    }

    internal static SongAboutEnrichmentDto? MapRecordingToEnrichment(MusicBrainzRecording? recording)
    {
        if (recording?.Id is not { Length: > 0 } id)
        {
            return null;
        }

        var styleTags = GetTopStyleTags(recording, max: 5);
        var notableRelease = FormatNotableRelease(recording);
        var versionNote = string.IsNullOrWhiteSpace(recording.Disambiguation)
            ? null
            : recording.Disambiguation.Trim();

        if (notableRelease is null
            && styleTags.Count == 0
            && recording.Length is not int
            && versionNote is null)
        {
            return null;
        }

        return new SongAboutEnrichmentDto
        {
            NotableRelease = notableRelease,
            StyleTags = styleTags,
            DurationMs = recording.Length,
            VersionNote = versionNote,
            ExternalUrl = $"https://musicbrainz.org/recording/{id}"
        };
    }

    internal static List<string> GetTopStyleTags(MusicBrainzRecording recording, int max)
    {
        var candidates = new List<(string Name, int Count)>();
        AddGenreCandidates(candidates, recording.Tags);
        AddGenreCandidates(candidates, recording.Genres);

        foreach (var releaseGroup in recording.ReleaseGroups ?? [])
        {
            AddGenreCandidates(candidates, releaseGroup.Tags);
            AddGenreCandidates(candidates, releaseGroup.Genres);
        }

        return candidates
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Name: g.Key, Count: g.Sum(x => x.Count)))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .Select(x => x.Name)
            .ToList();
    }

    internal static string? FormatNotableRelease(MusicBrainzRecording recording)
    {
        var candidates = new List<(string Title, int? Year)>();

        foreach (var release in recording.Releases ?? [])
        {
            var title = release.ReleaseGroup?.Title ?? release.Title;
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var year = MusicBrainzSearchHelper.ResolveEarliestReleaseYear(
            [
                release.Date,
                release.ReleaseGroup?.FirstReleaseDate
            ]);
            candidates.Add((title.Trim(), year));
        }

        foreach (var releaseGroup in recording.ReleaseGroups ?? [])
        {
            if (string.IsNullOrWhiteSpace(releaseGroup.Title))
            {
                continue;
            }

            var year = MusicBrainzSearchHelper.ResolveEarliestReleaseYear([releaseGroup.FirstReleaseDate]);
            candidates.Add((releaseGroup.Title.Trim(), year));
        }

        var best = candidates
            .OrderBy(c => c.Year ?? int.MaxValue)
            .ThenBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(best.Title))
        {
            return null;
        }

        return best.Year is int yearValue
            ? $"{best.Title} ({yearValue})"
            : best.Title;
    }

    internal static List<CanonicalMatchDto> RankMatches(
        IEnumerable<CanonicalMatchDto> matches,
        string searchTitle,
        IReadOnlyDictionary<string, MusicBrainzRecording> recordingsById) =>
        MusicBrainzSearchHelper.RankMatches(
            matches,
            searchTitle,
            match => IsSoftUnwantedRecording(match, recordingsById));

    internal static List<CanonicalMatchDto> OrderMatchesOldestFirst(
        IEnumerable<CanonicalMatchDto> matches,
        IReadOnlyDictionary<string, MusicBrainzRecording> recordingsById,
        string searchTitle) =>
        RankMatches(matches, searchTitle, recordingsById);

    private static bool IsSoftUnwantedRecording(
        CanonicalMatchDto match,
        IReadOnlyDictionary<string, MusicBrainzRecording> recordingsById)
    {
        if (match.RecordingMbid is null
            || !recordingsById.TryGetValue(match.RecordingMbid, out var recording))
        {
            return false;
        }

        return IsCompilationOrRemaster(recording);
    }

    private static int? GetOldestExactTitleYear(
        IEnumerable<MusicBrainzRecording> recordings,
        string searchTitle)
    {
        var years = recordings
            .Where(r => MusicBrainzSearchHelper.TitleMatchesSearch(r.Title ?? string.Empty, searchTitle))
            .Select(GetEarliestReleaseYear)
            .Where(year => year.HasValue)
            .Select(year => year!.Value)
            .ToList();

        return years.Count > 0 ? years.Min() : null;
    }

    private async Task<List<MusicBrainzRecording>> SearchRecordingsAsync(
        HttpClient client,
        string title,
        string artist,
        CancellationToken cancellationToken)
    {
        var merged = new Dictionary<string, MusicBrainzRecording>(StringComparer.Ordinal);
        var queries = MusicBrainzSearchHelper.BuildSearchQueries(title, artist);
        int? previousOldestExactYear = null;

        for (var i = 0; i < queries.Count; i++)
        {
            var recordings = await ExecuteSearchAsync(client, queries[i], cancellationToken);
            MergeRecordings(merged, recordings);

            var oldestExactYear = GetOldestExactTitleYear(merged.Values, title);
            if (oldestExactYear.HasValue
                && previousOldestExactYear.HasValue
                && oldestExactYear.Value < previousOldestExactYear.Value)
            {
                break;
            }

            if (i >= 2
                && oldestExactYear.HasValue
                && previousOldestExactYear.HasValue
                && oldestExactYear.Value >= previousOldestExactYear.Value)
            {
                break;
            }

            if (oldestExactYear.HasValue)
            {
                previousOldestExactYear = oldestExactYear;
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
            MergeRecordings(merged, fallbackRecordings);
        }

        foreach (var studioQuery in MusicBrainzSearchHelper.BuildStudioSearchQueries(title, artist))
        {
            var studioRecordings = await ExecuteSearchAsync(client, studioQuery, cancellationToken);
            MergeRecordings(merged, studioRecordings);
        }

        return merged.Values.ToList();
    }

    private static void MergeRecordings(
        Dictionary<string, MusicBrainzRecording> merged,
        IEnumerable<MusicBrainzRecording> recordings)
    {
        foreach (var recording in recordings)
        {
            if (string.IsNullOrWhiteSpace(recording.Id))
            {
                continue;
            }

            if (!merged.TryGetValue(recording.Id, out var existing)
                || (recording.Score ?? 0) > (existing.Score ?? 0))
            {
                merged[recording.Id] = recording;
            }
        }
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
    internal static MusicBrainzRecording? SelectHeadOfClassRecording(
        IReadOnlyList<MusicBrainzRecording> recordings,
        string searchTitle = "")
    {
        var matches = recordings
            .Where(r => !string.IsNullOrWhiteSpace(r.Id))
            .Select(r => new CanonicalMatchDto
            {
                Found = true,
                Title = r.Title?.Trim() ?? string.Empty,
                RecordingMbid = r.Id,
                Score = r.Score ?? 0,
                Disambiguation = string.IsNullOrWhiteSpace(r.Disambiguation) ? null : r.Disambiguation.Trim(),
                Year = GetEarliestReleaseYear(r)
            })
            .ToList();

        var recordingsById = recordings
            .Where(r => !string.IsNullOrWhiteSpace(r.Id))
            .ToDictionary(r => r.Id!, StringComparer.Ordinal);

        var ranked = RankMatches(matches, searchTitle, recordingsById);
        var bestId = ranked.FirstOrDefault()?.RecordingMbid;
        return bestId is null ? null : recordingsById.GetValueOrDefault(bestId);
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
        IsLiveRecording(recording)
        || MusicBrainzSearchHelper.IsClearlyUnwantedDisambiguation(recording.Disambiguation)
        || IsCompilationOrRemaster(recording);

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

        [JsonPropertyName("length")]
        public int? Length { get; set; }

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
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("release-group")]
        public MusicBrainzReleaseGroup? ReleaseGroup { get; set; }
    }

    internal sealed class MusicBrainzReleaseGroup
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

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
