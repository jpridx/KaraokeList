using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KaraokeList.Data
{
    public class Artist
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SortableName { get; set; }
        public int? MainGenre { get; set; }
        public string? Mbid { get; set; }
    }

    public class ArtistService
    {
        private readonly string _connectionString;
        public ArtistService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Artist>> GetArtistsAsync()
        {
            var artists = new List<Artist>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, SortableName, MainGenre, Mbid FROM Artists";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                artists.Add(new Artist
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    SortableName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    MainGenre = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Mbid = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }
            return artists;
        }

        public async Task AddArtistAsync(Artist artist)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO Artists (Name, SortableName, MainGenre, Mbid) VALUES (@Name, @SortableName, @MainGenre, @Mbid);";
            command.Parameters.AddWithValue("@Name", artist.Name);
            command.Parameters.AddWithValue("@SortableName", (object?)artist.SortableName ?? DBNull.Value);
            command.Parameters.AddWithValue("@MainGenre", (object?)artist.MainGenre ?? DBNull.Value);
            command.Parameters.AddWithValue("@Mbid", (object?)artist.Mbid ?? DBNull.Value);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateArtistAsync(Artist artist)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"UPDATE Artists SET Name=@Name, SortableName=@SortableName, MainGenre=@MainGenre, Mbid=@Mbid WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", artist.Id);
            command.Parameters.AddWithValue("@Name", artist.Name);
            command.Parameters.AddWithValue("@SortableName", (object?)artist.SortableName ?? DBNull.Value);
            command.Parameters.AddWithValue("@MainGenre", (object?)artist.MainGenre ?? DBNull.Value);
            command.Parameters.AddWithValue("@Mbid", (object?)artist.Mbid ?? DBNull.Value);
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteArtistAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM Artists WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }
    }
}
