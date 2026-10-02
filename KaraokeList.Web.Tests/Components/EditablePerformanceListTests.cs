using System.Reflection;
using Bunit;
using KaraokeList.Shared;
using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KaraokeList.Web.Tests.Components;

public sealed class EditablePerformanceListTests : BunitTestContext
{
    private readonly Mock<IKaraokeApiClient> api = new();

    public EditablePerformanceListTests()
    {
        AddSyncfusionServices(Services);
        api.Setup(client => client.GetVenuesAsync()).ReturnsAsync(
        [
            new VenueDto { Id = 1, VenueName = "Old Venue" },
            new VenueDto { Id = 2, VenueName = "New Venue" }
        ]);
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

    [Fact]
    public async Task Save_sends_updated_venue_in_put_request()
    {
        PerformanceDto? updated = null;
        api.Setup(client => client.TryUpdatePerformanceAsync(It.IsAny<PerformanceDto>()))
            .Callback<PerformanceDto>(dto => updated = dto)
            .ReturnsAsync(CatalogMutateResult.Ok());

        var cut = Render<EditablePerformanceList>(parameters => parameters
            .Add(p => p.SingerId, 1)
            .Add(p => p.Entries,
            [
                EditablePerformanceEntry.FromBrowse(new()
                {
                    Id = 42,
                    SongId = 10,
                    Title = "Test Song",
                    ArtistName = "Test Artist",
                    PerformedOn = DateTime.Today,
                    VenueId = 1,
                    VenueName = "Old Venue"
                })
            ]));

        var editButton = cut.FindAll("button.btn-link")
            .First(button => button.TextContent?.Contains("Edit", StringComparison.Ordinal) == true);
        editButton.Click();

        var form = cut.FindComponent<PerformanceEditForm>();
        var venueField = typeof(PerformanceEditForm).GetField("_venueId", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(venueField);
        venueField.SetValue(form.Instance, 2);

        cut.Find("button.btn-primary").Click();
        cut.WaitForAssertion(() => Assert.NotNull(updated));

        Assert.NotNull(updated);
        Assert.Equal(42, updated.Id);
        Assert.Equal(2, updated.Venue);
    }
}
