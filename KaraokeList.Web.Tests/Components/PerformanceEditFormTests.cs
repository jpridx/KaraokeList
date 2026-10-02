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

public sealed class PerformanceEditFormTests : BunitTestContext
{
    public PerformanceEditFormTests()
    {
        AddSyncfusionServices(Services);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        var api = new Mock<IKaraokeApiClient>();
        api.Setup(client => client.GetSingersAsync()).ReturnsAsync([]);
        services.AddSingleton(api.Object);
    }

    [Fact]
    public async Task Save_flushes_local_venue_to_parent_before_OnSave()
    {
        var callbackOrder = new List<string>();
        int? venueSentToParent = null;

        var cut = Render<PerformanceEditForm>(parameters => parameters
            .Add(p => p.SingerId, 1)
            .Add(p => p.Venues,
            [
                new VenueDto { Id = 1, VenueName = "Old Venue" },
                new VenueDto { Id = 2, VenueName = "New Venue" }
            ])
            .Add(p => p.PerformedOn, new DateTime(2026, 3, 1))
            .Add(p => p.VenueId, 1)
            .Add(p => p.VenueIdChanged, EventCallback.Factory.Create<int?>(this, value =>
            {
                callbackOrder.Add("venue");
                venueSentToParent = value;
            }))
            .Add(p => p.PerformedOnChanged, EventCallback.Factory.Create<DateTime>(this, _ => callbackOrder.Add("date")))
            .Add(p => p.KeyChangeSemitonesChanged, EventCallback.Factory.Create<int?>(this, _ => callbackOrder.Add("key")))
            .Add(p => p.OtherPerformersChanged, EventCallback.Factory.Create<List<CoPerformerInputDto>>(this, _ => callbackOrder.Add("coPerformers")))
            .Add(p => p.OnSave, EventCallback.Factory.Create(this, () => callbackOrder.Add("save"))));

        var venueField = typeof(PerformanceEditForm).GetField("_venueId", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(venueField);
        venueField.SetValue(cut.Instance, 2);

        cut.Find("button.btn-primary").Click();
        cut.WaitForAssertion(() => Assert.Equal("save", callbackOrder[^1]));

        Assert.Equal(2, venueSentToParent);
        Assert.True(callbackOrder.IndexOf("venue") < callbackOrder.IndexOf("save"));
    }
}
