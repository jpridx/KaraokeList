using Bunit;
using KaraokeList.Web.Services;
using KaraokeList.Web.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;

namespace KaraokeList.Web.Tests;

/// <summary>
/// Base test context for Blazor component tests. Extend and override <see cref="ConfigureServices"/>
/// when a component needs DI services (e.g. mocked <c>IKaraokeApiClient</c>).
/// </summary>
public abstract class BunitTestContext : BunitContext
{
    protected BunitTestContext()
    {
        ConfigureServices(Services);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IPerformanceCacheCoordinator, NoOpPerformanceCacheCoordinator>();
    }

    protected void AddSyncfusionServices(IServiceCollection services)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        services.AddSyncfusionBlazor();
    }
}
