using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KaraokeList.Data
{
    public class Venue
    {
        public int Id { get; set; }
        public string VenueName { get; set; } = string.Empty;
    }

    public class VenueService
    {
        private readonly string _connectionString;

        public VenueService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Venue>> GetVenuesAsync()
        {
            var venues = new List<Venue>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, VenueName FROM Venues";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                venues.Add(new Venue
                {
                    Id = reader.GetInt32(0),
                    VenueName = reader.GetString(1)
                });
            }
            return venues;
        }

        public async Task<int> AddVenueAsync(Venue venue)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO Venues (VenueName) VALUES (@VenueName);
                                    SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@VenueName", venue.VenueName);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async Task UpdateVenueAsync(Venue venue)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"UPDATE Venues SET VenueName=@VenueName WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", venue.Id);
            command.Parameters.AddWithValue("@VenueName", venue.VenueName);
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteVenueAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = @"DELETE FROM Venues WHERE Id=@Id;";
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync();
        }
    }
}
