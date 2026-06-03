-- Karaoke catalog schema for Azure SQL / SQL Server (run once per database).
-- Identity tables are created by EF Core migrations (ApplicationDbContext).

IF OBJECT_ID(N'dbo.Genres', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Genres (
        Id        INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        GenreName NVARCHAR(128)  NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Artists', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Artists (
        Id           INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name         NVARCHAR(128)  NOT NULL,
        SortableName NVARCHAR(128)  NULL,
        MainGenre    INT            NULL CONSTRAINT FK_Artists_Genres REFERENCES dbo.Genres (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.Singers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Singers (
        Id   INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(128)  NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Venues', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Venues (
        Id        INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        VenueName NVARCHAR(128)  NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Songs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Songs (
        Id              INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Title           NVARCHAR(128)  NOT NULL,
        Artist          INT            NULL CONSTRAINT FK_Songs_Artists REFERENCES dbo.Artists (Id),
        Genre           INT            NULL CONSTRAINT FK_Songs_Genres REFERENCES dbo.Genres (Id),
        [Year]          INT            NULL,
        SecondaryArtist INT            NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.SingerSongs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SingerSongs (
        Id        INT  IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Singer    INT  NULL CONSTRAINT FK_SingerSongs_Singers REFERENCES dbo.Singers (Id),
        Song      INT  NULL CONSTRAINT FK_SingerSongs_Songs REFERENCES dbo.Songs (Id),
        Venue     INT  NULL CONSTRAINT FK_SingerSongs_Venues REFERENCES dbo.Venues (Id),
        FirstSung DATE NULL,
        LastSung  DATE NULL,
        [Count]   INT  NOT NULL CONSTRAINT DF_SingerSongs_Count DEFAULT (0)
    );
END;
GO
