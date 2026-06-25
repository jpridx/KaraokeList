-- One-time: add every catalog song not already on My repertoire for one singer.
-- Run against KaraokeList (production) or LocalDB after reviewing the preview SELECT.
--
-- Option A: set @ListId directly (e.g. 1).
-- Option B: set @Email to resolve My repertoire (Kind = 0) from AspNetUsers.

SET NOCOUNT ON;

DECLARE @ListId INT = 1;  -- change or set NULL to use @Email
DECLARE @Email NVARCHAR(256) = NULL;  -- e.g. N'you@example.com'

IF @ListId IS NULL
BEGIN
    SELECT @ListId = sl.Id
    FROM SingerLists sl
    INNER JOIN AspNetUsers u ON u.SingerId = sl.SingerId
    WHERE u.Email = @Email AND sl.Kind = 0;

    IF @ListId IS NULL
    BEGIN
        RAISERROR('Could not resolve My repertoire list for that email.', 16, 1);
        RETURN;
    END
END

-- Preview: confirm list and counts before inserting.
SELECT
    sl.Id AS ListId,
    sl.SingerId,
    sl.Kind,
    si.Name AS SingerName,
    (SELECT COUNT(*) FROM Songs) AS TotalCatalogSongs,
    (SELECT COUNT(*) FROM SingerListSongs sls WHERE sls.ListId = sl.Id) AS AlreadyOnList,
    (SELECT COUNT(*)
     FROM Songs s
     WHERE NOT EXISTS (
         SELECT 1 FROM SingerListSongs sls
         WHERE sls.ListId = sl.Id AND sls.SongId = s.Id)) AS SongsToAdd
FROM SingerLists sl
INNER JOIN Singers si ON si.Id = sl.SingerId
WHERE sl.Id = @ListId;

IF NOT EXISTS (SELECT 1 FROM SingerLists WHERE Id = @ListId AND Kind = 0)
BEGIN
    RAISERROR('ListId is not My repertoire (Kind = 0). Stop and verify.', 16, 1);
    RETURN;
END

INSERT INTO SingerListSongs (ListId, SongId, AddedUtc)
SELECT @ListId, s.Id, GETUTCDATE()
FROM Songs s
WHERE NOT EXISTS (
    SELECT 1 FROM SingerListSongs sls
    WHERE sls.ListId = @ListId AND sls.SongId = s.Id);

SELECT @@ROWCOUNT AS SongsAdded;

SELECT COUNT(*) AS TotalOnListAfter
FROM SingerListSongs
WHERE ListId = @ListId;
