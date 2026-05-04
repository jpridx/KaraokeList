using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KaraokeList.Data
{
    public class SingerSong
    {
        public int Id { get; set; }
        public int? Singer { get; set; }
        public int? Song { get; set; }
        public int? Venue { get; set; }
        public DateTime? FirstSung { get; set; }
        public DateTime? LastSung { get; set; }
        public int Count { get; set; }
    }

    public class SingerSongService
    {
        private readonly string _connectionString;

        public SingerSongService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<SingerSong>> GetSingerSongsAsync()
        {
            var singerSongs = new List<SingerSong>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Singer, Song, Venue, FirstSung, LastSung, Count FROM SingerSongs";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                singerSongs.Add(new SingerSong
                {
                    Id = reader.GetInt32(0),
                    Singer = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    Song = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Venue = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    FirstSung = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    LastSung = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    Count = reader.GetInt32(6)
                });
            }
            return singerSongs;
        }

        public async Task AddSingerSongAsync(SingerSong singerSong)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO SingerSongs (Singer, Song, Venue, FirstSung, LastSung, Count) VALUES (@Singer, @Song, @Venue, @FirstSung, @LastSung, @Count);";
            command.Parameters.AddWithValue("@Singer", (object?)singerSong.Singer ?? DBNull.Value);
            command.Parameters.AddWithValue("@Song", (object?)singerSong.Song ?? DBNull.Value);
            command.Parameters.AddWithValue("@Venue", (object?)singerSong.Venue ?? DBNull.Value);
            command.Parameters.AddWithValue("@FirstSung", (object?)singerSong.FirstSung ?? DBNull.Value);
            command.Parameters.AddWithValue("@LastSung", (object?)singerSong.LastSung ?? DBNull.Value);
            command.Parameters.AddWithValue("@Count", singerSong.Count);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateSingerSongAsync(SingerSong singerSong)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"UPDATE SingerSongs SET Singer=@Singer, Song=@Song, Venue=@Venue, FirstSung=@FirstSung, LastSung=@LastSung, Count=@Count WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", singerSong.Id);
            command.Parameters.AddWithValue("@Singer", (object?)singerSong.Singer ?? DBNull.Value);
            command.Parameters.AddWithValue("@Song", (object?)singerSong.Song ?? DBNull.Value);
            command.Parameters.AddWithValue("@Venue", (object?)singerSong.Venue ?? DBNull.Value);
            command.Parameters.AddWithValue("@FirstSung", (object?)singerSong.FirstSung ?? DBNull.Value);
            command.Parameters.AddWithValue("@LastSung", (object?)singerSong.LastSung ?? DBNull.Value);
            command.Parameters.AddWithValue("@Count", singerSong.Count);
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteSingerSongAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM SingerSongs WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }
    }
}
