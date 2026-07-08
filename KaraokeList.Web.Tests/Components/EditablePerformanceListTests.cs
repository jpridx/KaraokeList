using Bunit;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class EditablePerformanceListTests : BunitTestContext
{
    public EditablePerformanceListTests()
    {
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetVenuesAsync()).ReturnsAsync([]);
        api.Setup(client => client.GetSingersAsync()).ReturnsAsync([]);
        Services.AddSingleton(api.Object);
    }

    [Fact]
    public void Hides_edit_and_delete_when_mutations_not_allowed()
    {
        var cut = Render<EditablePerformanceList>(parameters => parameters
            .Add(p => p.SingerId, 1)
            .Add(p => p.AllowMutations, false)
            .Add(p => p.Entries,
            [
                EditablePerformanceEntry.FromBrowse(new()
                {
                    Id = 1,
                    SongId = 10,
                    Title = "Test Song",
                    ArtistName = "Test Artist",
                    PerformedOn = DateTime.Today,
                    VenueName = "Venue"
                })
            ]));

        Assert.Contains("Log", cut.Markup);
        Assert.DoesNotContain("Edit", cut.Markup);
        Assert.DoesNotContain("Delete", cut.Markup);
    }
}
