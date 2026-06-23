-- Assign Admin role to an existing user by email.
-- Run after the user has registered. Edit @Email and USE database name as needed.

USE [KaraokeList-Dev];
GO

DECLARE @Email NVARCHAR(256) = N'you@example.com';

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
