using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KaraokeList.Data
{
    public class Genre
    {
        public int Id { get; set; }
        public string GenreName { get; set; } = string.Empty;
    }

    public class GenreService
    {
        private readonly string _connectionString;

        public GenreService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Genre>> GetGenresAsync()
        {
            var genres = new List<Genre>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, GenreName FROM Genres";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                genres.Add(new Genre
                {
                    Id = reader.GetInt32(0),
                    GenreName = reader.GetString(1)
                });
            }
            return genres;
        }

        public async Task AddGenreAsync(Genre genre)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO Genres (GenreName) VALUES (@GenreName);";
            command.Parameters.AddWithValue("@GenreName", genre.GenreName);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateGenreAsync(Genre genre)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"UPDATE Genres SET GenreName=@GenreName WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", genre.Id);
            command.Parameters.AddWithValue("@GenreName", genre.GenreName);
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteGenreAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM Genres WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }
    }
}
