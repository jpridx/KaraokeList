using KaraokeList.Shared;
using KaraokeList.Web.Services;
using Moq;

namespace KaraokeList.Web.Tests.Services;

public sealed class PerformanceEditOperationsTests
{
    [Fact]
    public async Task UpdateAsync_returns_error_when_venue_missing()
    {
        var api = new Mock<IKaraokeApiClient>();

        var result = await PerformanceEditOperations.UpdateAsync(
            api.Object,
            performanceId: 1,
            singerId: 2,
            songId: 3,
            performedOn: DateTime.Today,
            venueId: null,
            keyChangeSemitones: null,
            otherPerformers: []);

        Assert.False(result.Succeeded);
        Assert.Equal("Pick a venue.", result.ErrorMessage);
        api.Verify(client => client.UpdatePerformanceAsync(It.IsAny<PerformanceDto>()), Times.Never);
    }

    [Fact]
    public void ToCoPerformerInputs_maps_singer_and_display_name()
    {
        var inputs = PerformanceEditOperations.ToCoPerformerInputs(
        [
            new CoPerformerDto { SingerId = 5, Name = "Alex" },
            new CoPerformerDto { Name = "Guest" }
        ]);

        Assert.Equal(5, inputs[0].SingerId);
        Assert.Equal("Guest", inputs[1].DisplayName);
    }
}
