using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KaraokeList.Data
{
    public class ArtistLookup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ArtistLookupService
    {
        private readonly string _connectionString;
        public ArtistLookupService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<ArtistLookup>> GetArtistLookupsAsync()
        {
            var artists = new List<ArtistLookup>();
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name FROM Artists";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                artists.Add(new ArtistLookup
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
            return artists;
        }
    }
}
