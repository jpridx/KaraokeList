using System.Net.Http.Json;
using KaraokeList.Api.Services;
using KaraokeList.Api.Services.Import;
using KaraokeList.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace KaraokeList.Api.IntegrationTests;

[Collection(KaraokeApiCollection.Name)]
public sealed class CatalogImportCanonicizeIntegrationTests(KaraokeApiFactory factory)
{
    [SkippableFact]
    public async Task ImportFile_WithCanonicize_ProcessesLearningXlsxInChunks()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        var (admin, _) = await IntegrationAuthHelper.CreateAdminClientAsync(factory);
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Learning.xlsx");
        Skip.IfNot(File.Exists(fixturePath), $"Fixture not found: {fixturePath}");

        await using var stream = File.OpenRead(fixturePath);
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", "Learning.xlsx");

        var sessionResponse = await admin.PostAsync("api/catalog/import/file?canonicize=true", content);
        var sessionBody = await sessionResponse.Content.ReadAsStringAsync();
        Assert.True(
            sessionResponse.IsSuccessStatusCode,
            $"Start import failed ({(int)sessionResponse.StatusCode}): {sessionBody}");

        var session = await sessionResponse.Content.ReadFromJsonAsync<CatalogImportSessionDto>();
        Assert.NotNull(session);
        Assert.False(string.IsNullOrWhiteSpace(session.SessionId));
        Assert.True(session.TotalRows > 0);
        Assert.Equal(CatalogImportChunkRequest.DefaultChunkSize, session.ChunkSize);

        CatalogImportChunkResultDto? lastChunk = null;
        var offset = 0;
        while (true)
        {
            var chunkResponse = await admin.PostAsync(
                $"api/catalog/import/session/{session.SessionId}/chunk?offset={offset}&limit={session.ChunkSize}",
                content: null);
            var chunkBody = await chunkResponse.Content.ReadAsStringAsync();
            Assert.True(
                chunkResponse.IsSuccessStatusCode,
                $"Import chunk failed ({(int)chunkResponse.StatusCode}): {chunkBody}");

            lastChunk = await chunkResponse.Content.ReadFromJsonAsync<CatalogImportChunkResultDto>();
            Assert.NotNull(lastChunk);
            Assert.Equal(session.TotalRows, lastChunk.TotalRows);
            Assert.True(lastChunk.ProcessedRows > offset || !lastChunk.HasMore);

            if (!lastChunk.HasMore)
            {
                break;
            }

            offset = lastChunk.NextOffset;
        }

        Assert.NotNull(lastChunk);
        Assert.Equal(session.TotalRows, lastChunk.ProcessedRows);
        Assert.Equal(session.TotalRows, lastChunk.Added + lastChunk.Skipped + lastChunk.Errors.Count);
    }

    [SkippableFact]
    public async Task XlsxParser_ParsesLearningFixture()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Learning.xlsx");
        Skip.IfNot(File.Exists(fixturePath), $"Fixture not found: {fixturePath}");

        await using var stream = File.OpenRead(fixturePath);
        var parsed = new XlsxCatalogRowParser().Parse(stream);

        Assert.Null(parsed.Error);
        Assert.InRange(parsed.Rows.Count, 35, 100);
        Assert.Contains(parsed.Rows, row =>
            row.Title.Contains("How Long", StringComparison.OrdinalIgnoreCase)
            && row.Artist.Contains("Ace", StringComparison.OrdinalIgnoreCase));
    }

    [SkippableFact]
    public async Task ImportChunkAsync_WithStubMusicBrainz_AddsRows()
    {
        Skip.IfNot(factory.IsDatabaseAvailable, IntegrationTestConnection.SkipReason);

        using var scope = factory.Services.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<CatalogImportService>();
        var rows = new List<CatalogImportRow>
        {
            new($"Import Test {Guid.NewGuid():N}", "Test Artist", null, null, 2)
        };
        var cumulative = new CatalogImportResultDto { TotalRows = rows.Count };

        var chunk = await importService.ImportChunkAsync(rows, offset: 0, limit: 25, cumulative);
        Assert.False(chunk.HasMore);
        Assert.Equal(1, chunk.ProcessedRows);
        Assert.Equal(1, chunk.Added);
    }
}
