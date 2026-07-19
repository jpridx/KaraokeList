using KaraokeList.Api.Services.Import;
using KaraokeList.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace KaraokeList.Api.Services;

public sealed class CatalogImportSessionStore(IMemoryCache cache)
{
    private const string CacheKeyPrefix = "catalog-import:";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(1);

    public CatalogImportSessionDto CreateSession(string userId, IReadOnlyList<CatalogImportRow> rows)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var session = new CatalogImportSessionState
        {
            UserId = userId,
            Rows = rows,
            Cumulative = new CatalogImportResultDto { TotalRows = rows.Count },
            NextOffset = 0
        };

        cache.Set(CacheKeyPrefix + sessionId, session, SessionLifetime);
        return new CatalogImportSessionDto
        {
            SessionId = sessionId,
            TotalRows = rows.Count,
            ChunkSize = CatalogImportChunkRequest.DefaultChunkSize
        };
    }

    internal CatalogImportSessionState? GetSession(string sessionId, string userId)
    {
        if (!cache.TryGetValue(CacheKeyPrefix + sessionId, out CatalogImportSessionState? session)
            || session is null
            || !string.Equals(session.UserId, userId, StringComparison.Ordinal))
        {
            return null;
        }

        return session;
    }

    public void RemoveSession(string sessionId) => cache.Remove(CacheKeyPrefix + sessionId);
}

internal sealed class CatalogImportSessionState
{
    public required string UserId { get; init; }
    public required IReadOnlyList<CatalogImportRow> Rows { get; init; }
    public CatalogImportResultDto Cumulative { get; set; } = new();
    public int NextOffset { get; set; }
}
