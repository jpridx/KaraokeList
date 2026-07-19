using KaraokeList.Api.IntegrationTests.TestDoubles;
using KaraokeList.Api.Services;
using KaraokeList.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace KaraokeList.Api.IntegrationTests;

public sealed class KaraokeApiFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "INTEGRATION_TEST_JWT_KEY_32_CHARS_MIN!!";

    public string ConnectionString { get; } = IntegrationTestConnection.Resolve();

    private readonly Lazy<bool> isDatabaseAvailable = new(IntegrationTestConnection.EnsureDatabaseReady);

    public bool IsDatabaseAvailable => isDatabaseAvailable.Value;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Avoid Development — that loads API user secrets and appsettings.Development.json (Azure SQL).
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Testing.json"),
                optional: false,
                reloadOnChange: false);

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:Issuer"] = "KaraokeList.Test",
                ["Jwt:Audience"] = "KaraokeList.Web.Test",
                ["Jwt:Key"] = TestJwtKey,
                ["Security:Registration:AllowRegistration"] = "true",
                ["Security:Registration:RequireInviteCode"] = "false",
                ["Security:Registration:AllowPasswordRecovery"] = "true",
                ["App:WebBaseUrl"] = "http://localhost:5262"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccountEmailSender>();
            services.AddSingleton<CapturingAccountEmailSender>();
            services.AddSingleton<IAccountEmailSender>(sp => sp.GetRequiredService<CapturingAccountEmailSender>());

            // All integration tests share one in-memory rate-limit bucket ("unknown" IP).
            services.RemoveAll<IAuthRateLimiter>();
            services.AddSingleton<IAuthRateLimiter, UnlimitedAuthRateLimiter>();

            services.RemoveAll<IMusicBrainzService>();
            services.AddSingleton<IMusicBrainzService, PassthroughMusicBrainzStub>();
        });
    }
}

internal sealed class UnlimitedAuthRateLimiter : IAuthRateLimiter
{
    public bool AllowAttempt(string action, string clientKey, int maxAttempts, TimeSpan window) => true;
}

[CollectionDefinition(Name)]
public sealed class KaraokeApiCollection : ICollectionFixture<KaraokeApiFactory>
{
    public const string Name = nameof(KaraokeApiCollection);
}
