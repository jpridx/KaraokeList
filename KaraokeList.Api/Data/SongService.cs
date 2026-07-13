using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KaraokeList.Data
{
    public class Song
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? Genre { get; set; }
        public int? Year { get; set; }
        public string? RecordingMbid { get; set; }
        public string? ArtistCreditDisplay { get; set; }
    }

    public class SongService
    {
        private readonly string _connectionString;
        public SongService(string connectionString)
        {
            _connectionString = connectionString;
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
    }
}
