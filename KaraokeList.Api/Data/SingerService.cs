using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KaraokeList.Data
{
    public class Singer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SingerService
    {
        private readonly string _connectionString;

        public SingerService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Singer>> GetSingersAsync()
        {
            var singers = new List<Singer>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name FROM Singers";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                singers.Add(new Singer
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
            return singers;
        }

        public async Task<int> AddSingerAsync(Singer singer)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO Singers (Name) VALUES (@Name); SELECT CAST(SCOPE_IDENTITY() AS int);";
            command.Parameters.AddWithValue("@Name", singer.Name);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task UpdateSingerAsync(Singer singer)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"UPDATE Singers SET Name=@Name WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", singer.Id);
            command.Parameters.AddWithValue("@Name", singer.Name);
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteSingerAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM Singers WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }
    }
}
