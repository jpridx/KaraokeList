using KaraokeList.Shared;

namespace KaraokeList.Web.Services;

public static class PerformanceEditOperations
{
    public static List<CoPerformerInputDto> ToCoPerformerInputs(IEnumerable<CoPerformerDto> performers) =>
        performers
            .Select(p => p.SingerId is int singerId
                ? new CoPerformerInputDto { SingerId = singerId }
                : new CoPerformerInputDto { DisplayName = p.Name })
            .ToList();

    public static async Task<(bool Succeeded, string? ErrorMessage)> UpdateAsync(
        IKaraokeApiClient api,
        int performanceId,
        int singerId,
        int songId,
        DateTime performedOn,
        int? venueId,
        int? keyChangeSemitones,
        IReadOnlyList<CoPerformerInputDto> otherPerformers)
    {
        if (venueId is null)
        {
            return (false, "Pick a venue.");
        }

        try
        {
            await api.UpdatePerformanceAsync(new PerformanceDto
            {
                Id = performanceId,
                Singer = singerId,
                Song = songId,
                Venue = venueId,
                PerformedOn = performedOn,
                KeyChangeSemitones = keyChangeSemitones == 0 ? null : keyChangeSemitones,
                OtherPerformers = otherPerformers.ToList()
            });

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static async Task<(bool Succeeded, string? ErrorMessage)> DeleteAsync(
        IKaraokeApiClient api,
        int performanceId)
    {
        try
        {
            await api.DeletePerformanceAsync(performanceId);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
