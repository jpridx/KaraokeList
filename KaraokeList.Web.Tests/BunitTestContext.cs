using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace KaraokeList.Web.Tests;

/// <summary>
/// Base test context for Blazor component tests. Extend and call <see cref="ConfigureServices"/>
/// when a component needs DI services (e.g. mocked <c>IKaraokeApiClient</c>).
/// </summary>
public abstract class BunitTestContext : TestContext
{
    protected BunitTestContext()
    {
        ConfigureServices(Services);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }
}
