-- Assign Admin role to an existing user by email.
-- IMPORTANT: Use the same database your API connects to.
--
-- Local dev (appsettings.Development.json): LocalDB database KaraokeList
-- Azure (appsettings.json): KaraokeList-Dev on karaokelist.database.windows.net
--
-- After running: sign out and sign in again so your JWT includes the Admin role.

-- === LocalDB (dotnet run in Development) ===
-- sqlcmd -S "(localdb)\MSSQLLocalDB" -d KaraokeList -i seed-admin-user.sql

-- === Azure / SSMS: uncomment and set USE ===
-- USE [KaraokeList-Dev];
-- GO

DECLARE @Email NVARCHAR(256) = N'you@example.com';

-- Ensure Admin role exists (API startup also does this)
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = N'ADMIN')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), N'Admin', N'ADMIN', NEWID());
END

INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u
CROSS JOIN AspNetRoles r
WHERE u.Email = @Email
  AND r.NormalizedName = N'ADMIN'
  AND NOT EXISTS (
      SELECT 1
      FROM AspNetUserRoles ur
      WHERE ur.UserId = u.Id AND ur.RoleId = r.Id);

-- Verify (should return one row: your email + Admin)
SELECT u.Email, r.Name AS RoleName
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON ur.UserId = u.Id
JOIN AspNetRoles r ON r.Id = ur.RoleId
WHERE u.Email = @Email;