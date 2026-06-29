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
        api.Verify(client => client.TryUpdatePerformanceAsync(It.IsAny<PerformanceDto>()), Times.Never);
    }

    [Fact]
    public async Task SaveAdminAsync_uses_try_create_for_new_rows()
    {
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.TryCreatePerformanceAsync(It.IsAny<PerformanceDto>()))
            .ReturnsAsync(new PerformanceCreateResult(true, false, null));

        var dto = new PerformanceDto { Id = 0, Singer = 1, Song = 2, Venue = 3 };

        var result = await PerformanceEditOperations.SaveAdminAsync(api.Object, dto);

        Assert.True(result.Succeeded);
        api.Verify(client => client.TryCreatePerformanceAsync(It.IsAny<PerformanceDto>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_uses_try_delete()
    {
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.TryDeletePerformanceAsync(9))
            .ReturnsAsync(CatalogMutateResult.Ok());

        var result = await PerformanceEditOperations.DeleteAsync(api.Object, 9);

        Assert.True(result.Succeeded);
        api.Verify(client => client.TryDeletePerformanceAsync(9), Times.Once);
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
