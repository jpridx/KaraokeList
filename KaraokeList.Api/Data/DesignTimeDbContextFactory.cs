using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace KaraokeList.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var projectDir = ResolveApiProjectDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(projectDir)
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

        connectionString = ResolveSqliteDataSource(connectionString, projectDir);
        KaraokeDbPaths.EnsureDataSourceDirectory(connectionString);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite(connectionString);
        return new ApplicationDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Finds <c>KaraokeList.Api</c> whether cwd is the repo root or the project folder
    /// (dotnet ef often uses the project directory).
    /// </summary>
    internal static string ResolveApiProjectDirectory()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            var nested = Path.Combine(dir.FullName, "KaraokeList.Api");
            if (File.Exists(Path.Combine(nested, "KaraokeList.Api.csproj")))
            {
                return nested;
            }

            if (File.Exists(Path.Combine(dir.FullName, "KaraokeList.Api.csproj")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Makes relative <c>Data Source=...</c> paths absolute under the API project directory
    /// so EF tools and <c>dotnet run</c> share the same file.
    /// </summary>
    internal static string ResolveSqliteDataSource(string connectionString, string projectDir)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource)
            || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            || Path.IsPathRooted(dataSource))
        {
            return builder.ConnectionString;
        }

        builder.DataSource = Path.GetFullPath(Path.Combine(projectDir, dataSource));
        return builder.ConnectionString;
    }
}
