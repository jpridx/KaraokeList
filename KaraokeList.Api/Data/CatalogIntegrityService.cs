using Microsoft.Data.SqlClient;

namespace KaraokeList.Data;

public class CatalogIntegrityService(string connectionString)
{
    public Task<bool> SongExistsAsync(int id) =>
        ExistsAsync("SELECT 1 FROM Songs WHERE Id = @Id", id);

    public Task<bool> SingerExistsAsync(int id) =>
        ExistsAsync("SELECT 1 FROM Singers WHERE Id = @Id", id);

    public Task<bool> VenueExistsAsync(int id) =>
        ExistsAsync("SELECT 1 FROM Venues WHERE Id = @Id", id);

    public Task<bool> ArtistExistsAsync(int id) =>
        ExistsAsync("SELECT 1 FROM Artists WHERE Id = @Id", id);

    public Task<bool> GenreExistsAsync(int id) =>
        ExistsAsync("SELECT 1 FROM Genres WHERE Id = @Id", id);

    public Task<bool> HasPerformancesForSongAsync(int songId) =>
        ExistsAsync("SELECT 1 FROM Performances WHERE Song = @Id", songId);

    public Task<bool> HasPerformancesForSingerAsync(int singerId) =>
        ExistsAsync("SELECT 1 FROM Performances WHERE Singer = @Id", singerId);

    public Task<bool> HasSongsWithArtistAsync(int artistId) =>
        ExistsAsync("SELECT 1 FROM SongArtists WHERE ArtistId = @Id", artistId);

    public Task<bool> IsSingerLinkedToUserAsync(int singerId) =>
        ExistsAsync("SELECT 1 FROM AspNetUsers WHERE SingerId = @Id", singerId);

    private async Task<bool> ExistsAsync(string sql, int id)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@Id", id);
        var result = await command.ExecuteScalarAsync();
        return result is not null;
    }
}
