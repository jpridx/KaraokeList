-- Idempotent: add SingerId to AspNetUsers when EF migration was not applied.
IF COL_LENGTH(N'dbo.AspNetUsers', N'SingerId') IS NULL
BEGIN
    ALTER TABLE dbo.AspNetUsers ADD SingerId INT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_AspNetUsers_SingerId' AND object_id = OBJECT_ID(N'dbo.AspNetUsers'))
BEGIN
    CREATE UNIQUE INDEX IX_AspNetUsers_SingerId ON dbo.AspNetUsers (SingerId)
    WHERE SingerId IS NOT NULL;
END;
GO
