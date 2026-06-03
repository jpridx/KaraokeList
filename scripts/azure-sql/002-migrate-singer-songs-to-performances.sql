-- Upgrade databases that still have the legacy SingerSongs summary table.
IF OBJECT_ID(N'dbo.SingerSongs', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Performances', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Performances (
        Id                 INT  IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Singer             INT  NULL CONSTRAINT FK_Performances_Singers REFERENCES dbo.Singers (Id),
        Song               INT  NULL CONSTRAINT FK_Performances_Songs REFERENCES dbo.Songs (Id),
        Venue              INT  NULL CONSTRAINT FK_Performances_Venues REFERENCES dbo.Venues (Id),
        PerformedOn        DATE NOT NULL,
        KeyChangeSemitones INT  NULL
    );

    INSERT INTO dbo.Performances (Singer, Song, Venue, PerformedOn, KeyChangeSemitones)
    SELECT
        Singer,
        Song,
        Venue,
        COALESCE(LastSung, FirstSung, CAST(SYSUTCDATETIME() AS DATE)),
        NULL
    FROM dbo.SingerSongs;

    DROP TABLE dbo.SingerSongs;
END;
GO
