using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace KaraokeList.Api.Services.Import;

internal sealed class CsvCatalogRowParser : ICatalogRowParser
{
    public ParseResult Parse(Stream data)
    {
        try
        {
            using var reader = new StreamReader(data, leaveOpen: true);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim,
            };

            using var csv = new CsvReader(reader, config);

            var rawRows = new List<IReadOnlyList<string>>();
            while (csv.Read())
            {
                var cells = new List<string>();
                for (int i = 0; i < 32 && csv.TryGetField<string>(i, out var val); i++)
                    cells.Add(val ?? string.Empty);
                rawRows.Add(cells);
            }

            if (rawRows.Count == 0)
                return new ParseResult([], null);

            ColumnMap map;
            int startRow;
            if (ColumnMap.IsHeaderRow(rawRows[0]))
            {
                map = ColumnMap.FromHeaders(rawRows[0]);
                startRow = 1;
            }
            else
            {
                map = ColumnMap.Default;
                startRow = 0;
            }

            var rows = new List<CatalogImportRow>();
            for (int i = startRow; i < rawRows.Count; i++)
            {
                var row = map.ToRow(rawRows[i], i + 1);
                if (row is not null)
                    rows.Add(row);
            }

            return new ParseResult(rows, null);
        }
        catch (Exception ex)
        {
            return new ParseResult([], $"Could not parse CSV file: {ex.Message}");
        }
    }
}
