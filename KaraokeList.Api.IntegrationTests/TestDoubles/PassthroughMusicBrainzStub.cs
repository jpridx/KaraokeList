using KaraokeList.Api.Services;
using KaraokeList.Shared;

namespace KaraokeList.Api.IntegrationTests.TestDoubles;

internal sealed class PassthroughMusicBrainzStub : IMusicBrainzService
{
    public Task<CanonicalLookupResponse> LookupAsync(string title, string artist, CancellationToken cancellationToken = default) =>
        Task.FromResult(BuildResponse(title, artist));

    public Task<CanonicalLookupResponse> LookupForImportAsync(string title, string artist, CancellationToken cancellationToken = default) =>
        Task.FromResult(BuildResponse(title, artist));

    public Task<SongAboutEnrichmentDto?> GetRecordingEnrichmentAsync(string recordingMbid, CancellationToken cancellationToken = default) =>
        Task.FromResult<SongAboutEnrichmentDto?>(null);

    private static CanonicalLookupResponse BuildResponse(string title, string artist)
    {
        var trimmedTitle = title.Trim();
        var trimmedArtist = artist.Trim();
        return new CanonicalLookupResponse
        {
            Match = new CanonicalMatchDto
            {
                Found = true,
                Title = trimmedTitle,
                ArtistName = trimmedArtist,
                ArtistCreditDisplay = trimmedArtist,
                RecordingMbid = Guid.NewGuid().ToString(),
                ArtistCredits =
                [
                    new CanonicalArtistCreditDto
                    {
                        Name = trimmedArtist,
                        DisplayOrder = 0
                    }
                ]
            }
        };
    }
}
