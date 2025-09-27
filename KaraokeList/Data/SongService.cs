using Microsoft.Data.Sqlite;
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
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Title, Artist, Genre, Year, SecondaryArtist FROM Songs";
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
                    SecondaryArtist = reader.IsDBNull(5) ? null : reader.GetInt32(5)
                });
            }
            return songs;
        }
        // Add, Update, Delete methods can be added here
    }
}
