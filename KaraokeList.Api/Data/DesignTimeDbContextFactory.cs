using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace KaraokeList.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<DesignTimeDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=Data/karaokelist.dev.db";

        if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("localdb", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                """
                Design-time EF uses SQLite. Your DefaultConnection still looks like SQL Server
                (user secrets or appsettings). On the SQLite branch, set:

                  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=Data/karaokelist.dev.db" --project KaraokeList.Api

                Or run:

                  dotnet ef database update --project KaraokeList.Api --connection "Data Source=Data/karaokelist.dev.db"
                """);
        }

        KaraokeDbPaths.EnsureDataSourceDirectory(connectionString);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite(connectionString);
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
