namespace KaraokeList.Data;

/// <summary>
/// Shared seed SQL for genre groups (EF migration + scripts/seed-genre-groups.sql).
/// </summary>
public static class GenreGroupSeedSql
{
    public const string GroupsSql =
        """
        INSERT INTO GenreGroups (GroupName, SortOrder)
        SELECT src.GroupName, src.SortOrder
        FROM (
            SELECT 'Rock' AS GroupName, 1 AS SortOrder
            UNION ALL SELECT 'Pop', 2
            UNION ALL SELECT 'Country', 3
            UNION ALL SELECT 'Christian', 4
            UNION ALL SELECT 'R&B / Soul', 5
            UNION ALL SELECT 'Standards & Show Tunes', 6
        ) AS src
        WHERE NOT EXISTS (
            SELECT 1 FROM GenreGroups gg WHERE gg.GroupName = src.GroupName);
        """;

    public const string MappingsSql =
        """
        WITH Mapping (GenreName, GroupName, IsPrimary) AS (
            SELECT 'Alternative Rock', 'Rock', 1 UNION ALL
            SELECT 'Arena Rock', 'Rock', 1 UNION ALL
            SELECT 'Blues Rock', 'Rock', 1 UNION ALL
            SELECT 'Classic Rock', 'Rock', 1 UNION ALL
            SELECT 'Folk Rock', 'Rock', 1 UNION ALL
            SELECT 'Glam Rock', 'Rock', 1 UNION ALL
            SELECT 'Hair Metal', 'Rock', 1 UNION ALL
            SELECT 'Hard Rock', 'Rock', 1 UNION ALL
            SELECT 'New Wave', 'Rock', 1 UNION ALL
            SELECT 'Rock', 'Rock', 1 UNION ALL
            SELECT 'Rockabilly', 'Rock', 1 UNION ALL
            SELECT 'Rockabilly', 'Country', 0 UNION ALL
            SELECT 'Soft Rock', 'Rock', 1 UNION ALL
            SELECT 'Southern Rock', 'Rock', 1 UNION ALL
            SELECT 'Country Rock', 'Rock', 1 UNION ALL
            SELECT 'Country Rock', 'Country', 0 UNION ALL
            SELECT 'Pop Rock', 'Rock', 1 UNION ALL
            SELECT 'Pop Rock', 'Pop', 0 UNION ALL
            SELECT 'Country', 'Country', 1 UNION ALL
            SELECT 'Outlaw Country', 'Country', 1 UNION ALL
            SELECT 'Country Pop', 'Country', 1 UNION ALL
            SELECT 'Country Pop', 'Pop', 0 UNION ALL
            SELECT 'Adult Contemporary', 'Pop', 1 UNION ALL
            SELECT 'Easy Listening', 'Pop', 1 UNION ALL
            SELECT 'Pop', 'Pop', 1 UNION ALL
            SELECT 'Synth-Pop', 'Pop', 1 UNION ALL
            SELECT 'Disco', 'R&B / Soul', 1 UNION ALL
            SELECT 'R&B', 'R&B / Soul', 1 UNION ALL
            SELECT 'Soul', 'R&B / Soul', 1 UNION ALL
            SELECT 'Show Tunes', 'Standards & Show Tunes', 1
        )
        INSERT INTO GenreGroupGenres (GenreGroupId, GenreId, IsPrimary)
        SELECT gg.Id, g.Id, m.IsPrimary
        FROM Mapping m
        INNER JOIN Genres g ON g.GenreName = m.GenreName
        INNER JOIN GenreGroups gg ON gg.GroupName = m.GroupName
        WHERE NOT EXISTS (
            SELECT 1
            FROM GenreGroupGenres existing
            WHERE existing.GenreGroupId = gg.Id AND existing.GenreId = g.Id);
        """;
}
