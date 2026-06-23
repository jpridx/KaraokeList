# Admin roles

KaraokeList uses ASP.NET Identity roles with a single elevated role: **Admin**.

## Member (default)

Every registered user is a member. Members can:

- Log performances (own singer only)
- Browse **My Songs** / **My Performances**
- Add songs, artists, and venues inline while logging
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

The API creates the empty **Admin** role on startup. Assign your account in SQL (adjust email and database):

```sql
-- scripts/seed-admin-user.sql
DECLARE @Email NVARCHAR(256) = N'you@example.com';

INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u
CROSS JOIN AspNetRoles r
WHERE u.Email = @Email
  AND r.NormalizedName = N'ADMIN'
  AND NOT EXISTS (
      SELECT 1 FROM AspNetUserRoles ur
      WHERE ur.UserId = u.Id AND ur.RoleId = r.Id);
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
