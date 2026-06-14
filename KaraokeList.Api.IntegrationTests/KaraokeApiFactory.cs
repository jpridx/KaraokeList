using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace KaraokeList.Api.IntegrationTests;

public sealed class KaraokeApiFactory : WebApplicationFactory<Program>
{
    public string ConnectionString { get; } = IntegrationTestConnection.Resolve();

    public bool IsDatabaseAvailable { get; } = IntegrationTestConnection.CanConnect(
        IntegrationTestConnection.Resolve());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Testing.json"),
                optional: false,
                reloadOnChange: false);

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString
            });
        });
    }
}

[CollectionDefinition(Name)]
public sealed class KaraokeApiCollection : ICollectionFixture<KaraokeApiFactory>
{
    public const string Name = nameof(KaraokeApiCollection);
}
