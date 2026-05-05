using Microsoft.Data.Sqlite;
using System;
using System.IO;

var dbPath = Path.GetFullPath(Path.Combine("..", "KaraokeList", "bin", "Debug", "net10.0", "Temp", "Karaoke.sqlite3"));
Console.WriteLine($"DB: {dbPath}");
using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = "PRAGMA table_info('Singers');";
using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    var cid = reader.GetInt32(0);
    var name = reader.GetString(1);
    var type = reader.GetString(2);
    var notnull = reader.GetInt32(3);
    var dflt_value = reader.IsDBNull(4) ? "NULL" : reader.GetString(4);
    var pk = reader.GetInt32(5);
    Console.WriteLine($"{cid} | {name} | {type} | {notnull} | {dflt_value} | {pk}");
}
