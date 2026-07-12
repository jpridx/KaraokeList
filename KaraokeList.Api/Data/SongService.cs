using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KaraokeList.Data
{
    public class Song
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? Artist { get; set; }
        public int? Genre { get; set; }
        public int? Year { get; set; }
        public int? SecondaryArtist { get; set; }
        public string? RecordingMbid { get; set; }
    }

    public class SongService
    {
        private readonly string _connectionString;
        public SongService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Song>> GetSongsAsync()
        {
            var songs = new List<Song>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Title, Artist, Genre, Year, SecondaryArtist, RecordingMbid FROM Songs";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                songs.Add(new Song
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Artist = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Genre = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Year = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    SecondaryArtist = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    RecordingMbid = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }
            return songs;
        }

        public async Task<Song> AddSongAsync(Song song)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO Songs (Title, Artist, Genre, Year, SecondaryArtist, RecordingMbid)
                OUTPUT INSERTED.Id, INSERTED.Title, INSERTED.Artist, INSERTED.Genre, INSERTED.Year, INSERTED.SecondaryArtist, INSERTED.RecordingMbid
                VALUES (@Title, @Artist, @Genre, @Year, @SecondaryArtist, @RecordingMbid);
                """;
            command.Parameters.AddWithValue("@Title", song.Title);
            command.Parameters.AddWithValue("@Artist", (object?)song.Artist ?? DBNull.Value);
            command.Parameters.AddWithValue("@Genre", (object?)song.Genre ?? DBNull.Value);
            command.Parameters.AddWithValue("@Year", (object?)song.Year ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecondaryArtist", (object?)song.SecondaryArtist ?? DBNull.Value);
            command.Parameters.AddWithValue("@RecordingMbid", (object?)song.RecordingMbid ?? DBNull.Value);
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return new Song
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Artist = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Genre = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Year = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                SecondaryArtist = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                RecordingMbid = reader.IsDBNull(6) ? null : reader.GetString(6)
            };
        }

        public async Task UpdateSongAsync(Song song)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"UPDATE Songs SET Title=@Title, Artist=@Artist, Genre=@Genre, Year=@Year, SecondaryArtist=@SecondaryArtist, RecordingMbid=@RecordingMbid WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", song.Id);
            command.Parameters.AddWithValue("@Title", song.Title);
            command.Parameters.AddWithValue("@Artist", (object?)song.Artist ?? DBNull.Value);
            command.Parameters.AddWithValue("@Genre", (object?)song.Genre ?? DBNull.Value);
            command.Parameters.AddWithValue("@Year", (object?)song.Year ?? DBNull.Value);
            command.Parameters.AddWithValue("@SecondaryArtist", (object?)song.SecondaryArtist ?? DBNull.Value);
            command.Parameters.AddWithValue("@RecordingMbid", (object?)song.RecordingMbid ?? DBNull.Value);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> UpdateSongGenreAsync(int songId, int? genreId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Songs SET Genre = @Genre WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", songId);
            command.Parameters.AddWithValue("@Genre", (object?)genreId ?? DBNull.Value);
            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task DeleteSongAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM Songs WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }
    }
}
