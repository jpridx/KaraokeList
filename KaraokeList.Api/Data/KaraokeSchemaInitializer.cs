using Microsoft.Data.SqlClient;

namespace KaraokeList.Data;

public static class KaraokeSchemaInitializer
{
    public static async Task EnsureSchemaAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var scriptsDirectory = Path.Combine(AppContext.BaseDirectory, "scripts", "azure-sql");
        if (!Directory.Exists(scriptsDirectory))
        {
            throw new FileNotFoundException(
                "Karaoke schema scripts not found. Expected scripts/azure-sql next to the published app.",
                scriptsDirectory);
        }

        var scriptPaths = Directory.GetFiles(scriptsDirectory, "*.sql")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (scriptPaths.Length == 0)
        {
            throw new FileNotFoundException("No karaoke schema scripts found.", scriptsDirectory);
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var scriptPath in scriptPaths)
        {
            var script = await File.ReadAllTextAsync(scriptPath, cancellationToken);
            var batches = script.Split(["GO", "go"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var batch in batches)
            {
                if (string.IsNullOrWhiteSpace(batch))
                {
                    continue;
                }

                await using var command = connection.CreateCommand();
                command.CommandText = batch;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }
}
