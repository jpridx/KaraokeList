using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace KaraokeList.Web.Tests.Components;

public sealed class SlowApiNoticeTests : AuthPageTestContext
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton<ApiSlowRequestNotifier>();
    }

    [Fact]
    public void Hidden_when_no_slow_requests()
    {
        var cut = Render<SlowApiNotice>();

        Assert.DoesNotContain(ApiTransientFailure.ColdStartInProgressMessage, cut.Markup);
    }

    [Fact]
    public void Shows_message_when_request_is_slow()
    {
        var notifier = Services.GetRequiredService<ApiSlowRequestNotifier>();
        var tracker = notifier.TrackRequest();
        var cut = Render<SlowApiNotice>();

        tracker.MarkSlow();
        cut.Render();

        Assert.Contains(ApiTransientFailure.ColdStartInProgressMessage, cut.Markup);
        tracker.Dispose();
    }
}
