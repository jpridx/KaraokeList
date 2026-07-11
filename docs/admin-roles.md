# Admin roles

KaraokeList uses ASP.NET Identity roles with a single elevated role: **Admin**.

## Member (default)

Every registered user is a member. Members can:

- Log performances (own singer only)
- Browse **My Songs** / **My Performances**
- Add songs, artists, and venues inline while logging
- Reclassify a song's genre from **My Songs** song detail (catalog-wide; does not require the admin grid)
- Invite friends (when registration is open)
- Link their login to a **new** singer profile (stage name)

Members cannot edit the shared catalog in bulk, manage other users, or link to someone else's singer profile.

## Admin

Admins can do everything members can, plus:

- **Catalog grids** under **More → Catalog** (songs, artists, genres, singers, venues, performances grid)
- **User management** at **More → Admin → Users** (`/admin/users`): grant/revoke Admin, assign singer profile to any login
- **API**: `PUT`/`DELETE` on catalog entities; all singer/genre mutations; `GET/PUT api/admin/users`

At least one admin must always remain. Admins cannot remove their own admin role.

## First admin (manual seed)

The API creates the empty **Admin** role on startup. Assign your account in SQL (adjust email and **use the database your API actually uses**):

| Environment | Database |
|-------------|----------|
| Local `dotnet run` (Development) | `(localdb)\MSSQLLocalDB` → **KaraokeList** |
| Azure / production appsettings | **KaraokeList-Dev** on `karaokelist.database.windows.net` |

```sql
-- scripts/seed-admin-user.sql — set @Email, run against the correct database (see table above)
```

Local one-liner:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d KaraokeList -v Email="you@example.com" -i scripts/seed-admin-user.sql
```

Or edit `@Email` in the script and run in SSMS/Azure Data Studio.

**Verify** — should return one row with `RoleName = Admin`:

```sql
SELECT u.Email, r.Name AS RoleName
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON ur.UserId = u.Id
JOIN AspNetRoles r ON r.Id = ur.RoleId
WHERE u.Email = N'your@email.com';
```

Sign out and sign in again so your JWT includes the Admin role claim.

## JWT

Role claims are issued at login/register/link-singer. Changing roles or singer assignment in **Users** takes effect on the user's **next sign-in**.

## API

| Endpoint | Auth |
|----------|------|
| `GET api/admin/users` | Admin |
| `PUT api/admin/users/{userId}` | Admin |
| `POST api/songs`, `POST api/artists`, `POST api/venues` | Member (Log flow) |
| `PUT`/`DELETE` catalog entities | Admin |
| `POST`/`PUT`/`DELETE api/genres`, `api/singers` | Admin |

See also [security-private-access.md](security-private-access.md) for invite-only registration.
