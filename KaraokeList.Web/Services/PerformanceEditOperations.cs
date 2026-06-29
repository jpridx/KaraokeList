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
            var updateResult = await api.TryUpdatePerformanceAsync(new PerformanceDto
            {
                Id = performanceId,
                Singer = singerId,
                Song = songId,
                Venue = venueId,
                PerformedOn = performedOn,
                KeyChangeSemitones = keyChangeSemitones == 0 ? null : keyChangeSemitones,
                OtherPerformers = otherPerformers.ToList()
            });

            return (updateResult.Succeeded, updateResult.ErrorMessage);
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
        var result = await api.TryDeletePerformanceAsync(performanceId);
        return (result.Succeeded, result.ErrorMessage);
    }

    public static async Task<(bool Succeeded, string? ErrorMessage)> SaveAdminAsync(
        IKaraokeApiClient api,
        PerformanceDto dto)
    {
        if (dto.KeyChangeSemitones == 0)
        {
            dto.KeyChangeSemitones = null;
        }

        if (dto.Id <= 0)
        {
            var createResult = await api.TryCreatePerformanceAsync(dto);
            return (createResult.Succeeded, createResult.ErrorMessage);
        }

        var updateResult = await api.TryUpdatePerformanceAsync(dto);
        return (updateResult.Succeeded, updateResult.ErrorMessage);
    }
}
