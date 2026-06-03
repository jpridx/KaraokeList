using Microsoft.Data.SqlClient;

namespace KaraokeList.Data;

public static class KaraokeSchemaInitializer
{
    public static async Task EnsureSchemaAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "scripts", "azure-sql", "001-karaoke-schema.sql");
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException(
                "Karaoke schema script not found. Expected scripts/azure-sql/001-karaoke-schema.sql next to the published app.",
                scriptPath);
        }

        var script = await File.ReadAllTextAsync(scriptPath, cancellationToken);
        var batches = script.Split(["GO", "go"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

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
