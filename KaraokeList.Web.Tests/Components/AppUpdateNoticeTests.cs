using KaraokeList.Web.Components;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace KaraokeList.Web.Tests.Components;

public sealed class AppUpdateNoticeTests : AuthPageTestContext
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton<AppUpdateNotifier>();
        services.AddScoped<IAppUpdateService>(_ => new MockAppUpdateService());
    }

    [Fact]
    public void Hidden_when_no_update_available()
    {
        var cut = RenderComponent<AppUpdateNotice>();

        Assert.DoesNotContain("A new version of KaraokeList is ready.", cut.Markup);
    }

    [Fact]
    public void Shows_banner_when_update_is_available()
    {
        var notifier = Services.GetRequiredService<AppUpdateNotifier>();
        var cut = RenderComponent<AppUpdateNotice>();

        notifier.MarkUpdateAvailable();
        cut.Render();

        Assert.Contains("A new version of KaraokeList is ready.", cut.Markup);
        Assert.Contains("Refresh now", cut.Markup);
        Assert.Contains("Tap refresh to load the update.", cut.Markup);
        Assert.DoesNotContain("text-white-50", cut.Markup);
    }

    private sealed class MockAppUpdateService : IAppUpdateService
    {
        public Task ApplyUpdateAsync() => Task.CompletedTask;
        public Task ClearCacheAndReloadAsync() => Task.CompletedTask;
    }
}
