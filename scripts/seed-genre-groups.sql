-- Seed fixed genre groups and classify catalog genres by GenreName.
-- Idempotent: safe to re-run on LocalDB or Azure SQL after adding new leaf genres.
--
-- Local dev:
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -d KaraokeList -i scripts/seed-genre-groups.sql
--
-- Azure / SSMS: uncomment and set USE, then run the script.
-- USE [KaraokeList-Dev];
-- GO

SET NOCOUNT ON;

INSERT INTO GenreGroups (GroupName, SortOrder)
SELECT src.GroupName, src.SortOrder
FROM (VALUES
    (N'Rock', 1),
    (N'Pop', 2),
    (N'Country', 3),
    (N'Christian', 4),
    (N'R&B / Soul', 5),
    (N'Standards & Show Tunes', 6)
) AS src(GroupName, SortOrder)
WHERE NOT EXISTS (
    SELECT 1 FROM GenreGroups gg WHERE gg.GroupName = src.GroupName);

;WITH Mapping (GenreName, GroupName, IsPrimary) AS (
    SELECT N'Alternative Rock', N'Rock', 1 UNION ALL
    SELECT N'Arena Rock', N'Rock', 1 UNION ALL
    SELECT N'Blues Rock', N'Rock', 1 UNION ALL
    SELECT N'Classic Rock', N'Rock', 1 UNION ALL
    SELECT N'Folk Rock', N'Rock', 1 UNION ALL
    SELECT N'Glam Rock', N'Rock', 1 UNION ALL
    SELECT N'Hair Metal', N'Rock', 1 UNION ALL
    SELECT N'Hard Rock', N'Rock', 1 UNION ALL
    SELECT N'New Wave', N'Rock', 1 UNION ALL
    SELECT N'Rock', N'Rock', 1 UNION ALL
    SELECT N'Rockabilly', N'Rock', 1 UNION ALL
    SELECT N'Rockabilly', N'Country', 0 UNION ALL
    SELECT N'Soft Rock', N'Rock', 1 UNION ALL
    SELECT N'Southern Rock', N'Rock', 1 UNION ALL
    SELECT N'Country Rock', N'Rock', 1 UNION ALL
    SELECT N'Country Rock', N'Country', 0 UNION ALL
    SELECT N'Pop Rock', N'Rock', 1 UNION ALL
    SELECT N'Pop Rock', N'Pop', 0 UNION ALL
    SELECT N'Country', N'Country', 1 UNION ALL
    SELECT N'Outlaw Country', N'Country', 1 UNION ALL
    SELECT N'Country Pop', N'Country', 1 UNION ALL
    SELECT N'Country Pop', N'Pop', 0 UNION ALL
    SELECT N'Adult Contemporary', N'Pop', 1 UNION ALL
    SELECT N'Easy Listening', N'Pop', 1 UNION ALL
    SELECT N'Pop', N'Pop', 1 UNION ALL
    SELECT N'Synth-Pop', N'Pop', 1 UNION ALL
    SELECT N'Disco', N'R&B / Soul', 1 UNION ALL
    SELECT N'R&B', N'R&B / Soul', 1 UNION ALL
    SELECT N'Soul', N'R&B / Soul', 1 UNION ALL
    SELECT N'Show Tunes', N'Standards & Show Tunes', 1
)
MERGE GenreGroupGenres AS target
USING (
    SELECT gg.Id AS GenreGroupId, g.Id AS GenreId, m.IsPrimary
    FROM Mapping m
    INNER JOIN Genres g ON g.GenreName = m.GenreName
    INNER JOIN GenreGroups gg ON gg.GroupName = m.GroupName
) AS source
ON target.GenreGroupId = source.GenreGroupId AND target.GenreId = source.GenreId
WHEN MATCHED AND target.IsPrimary <> source.IsPrimary THEN
    UPDATE SET IsPrimary = source.IsPrimary
WHEN NOT MATCHED BY TARGET THEN
    INSERT (GenreGroupId, GenreId, IsPrimary)
    VALUES (source.GenreGroupId, source.GenreId, source.IsPrimary);

-- Preview assignments (unmapped genres such as Comedy / Unclassified appear only here).
SELECT
    g.GenreName,
    gg.GroupName,
    ggg.IsPrimary
FROM Genres g
LEFT JOIN GenreGroupGenres ggg ON ggg.GenreId = g.Id
LEFT JOIN GenreGroups gg ON gg.Id = ggg.GenreGroupId
ORDER BY g.GenreName, gg.SortOrder;
