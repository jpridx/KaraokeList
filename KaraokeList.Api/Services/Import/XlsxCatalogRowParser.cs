using ExcelDataReader;

namespace KaraokeList.Api.Services.Import;

internal sealed class XlsxCatalogRowParser : ICatalogRowParser
{
    public ParseResult Parse(Stream data)
    {
        try
        {
            using var reader = ExcelReaderFactory.CreateReader(data);
            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = false
                }
            });

            if (dataSet.Tables.Count == 0)
                return new ParseResult([], "The file contains no worksheets.");

            var table = dataSet.Tables[0];

            var rawRows = new List<IReadOnlyList<string>>();
            foreach (System.Data.DataRow row in table.Rows)
            {
                var cells = row.ItemArray
                    .Select(v => v?.ToString()?.Trim() ?? string.Empty)
                    .ToList();
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
            return new ParseResult([], $"Could not parse XLSX file: {ex.Message}");
        }
    }
}
