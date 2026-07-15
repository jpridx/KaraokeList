using KaraokeList.Api.Services;
using KaraokeList.Shared;

namespace KaraokeList.Api.Tests;

public sealed class MusicBrainzRecordingEnrichmentTests
{
    [Fact]
    public void MapRecordingToEnrichment_maps_release_tags_duration_and_link()
    {
        var recording = new MusicBrainzService.MusicBrainzRecording
        {
            Id = "11111111-1111-1111-1111-111111111111",
            Disambiguation = "album version",
            Length = 245000,
            Tags =
            [
                new MusicBrainzService.MusicBrainzLabel { Name = "rock", Count = 5 },
                new MusicBrainzService.MusicBrainzLabel { Name = "classic rock", Count = 2 }
            ],
            Releases =
            [
                new MusicBrainzService.MusicBrainzRelease
                {
                    Date = "1979",
                    Title = "Single Release",
                    ReleaseGroup = new MusicBrainzService.MusicBrainzReleaseGroup
                    {
                        Title = "Get the Knack",
                        FirstReleaseDate = "1979"
                    }
                }
            ]
        };

        var enrichment = MusicBrainzService.MapRecordingToEnrichment(recording);

        Assert.NotNull(enrichment);
        Assert.Equal("Get the Knack (1979)", enrichment.NotableRelease);
        Assert.Equal(["rock", "classic rock"], enrichment.StyleTags);
        Assert.Equal(245000, enrichment.DurationMs);
        Assert.Equal("album version", enrichment.VersionNote);
        Assert.Equal(
            "https://musicbrainz.org/recording/11111111-1111-1111-1111-111111111111",
            enrichment.ExternalUrl);
    }

    [Fact]
    public void MapRecordingToEnrichment_returns_null_when_recording_id_missing()
    {
        var enrichment = MusicBrainzService.MapRecordingToEnrichment(
            new MusicBrainzService.MusicBrainzRecording { Title = "Untitled" });

        Assert.Null(enrichment);
    }

    [Fact]
    public void FormatNotableRelease_prefers_earliest_release_year()
    {
        var recording = new MusicBrainzService.MusicBrainzRecording
        {
            Releases =
            [
                new MusicBrainzService.MusicBrainzRelease
                {
                    Date = "1994",
                    ReleaseGroup = new MusicBrainzService.MusicBrainzReleaseGroup
                    {
                        Title = "Greatest Hits",
                        FirstReleaseDate = "1994"
                    }
                },
                new MusicBrainzService.MusicBrainzRelease
                {
                    Date = "1979",
                    ReleaseGroup = new MusicBrainzService.MusicBrainzReleaseGroup
                    {
                        Title = "Get the Knack",
                        FirstReleaseDate = "1979"
                    }
                }
            ]
        };

        var notableRelease = MusicBrainzService.FormatNotableRelease(recording);

        Assert.Equal("Get the Knack (1979)", notableRelease);
    }
}
