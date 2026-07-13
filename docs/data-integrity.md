# Data integrity and account lifecycle

Planning doc for referential integrity, delete policy, and account anonymization. Schema source of truth remains EF migrations — see [database.md](database.md).

## Design principle

**Performances are facts** (“I sang X at Y on Z”). **Catalog rows are reference data.** Reference data should not disappear while facts point at it. When someone leaves the app, **anonymize the person; keep performance history**.

Hard deletes should be the exception (e.g. failed registration rollback), not the default.

---

## What the database enforces today

| Layer | Enforced? |
|-------|-----------|
| Identity child tables → user/role | Yes — **CASCADE** on delete |
| `AspNetUsers.SingerId` → `Singers` | Yes — FK + **filtered unique index** (one account per singer) |
| `Artists.Name` | Yes — **UNIQUE** |
| Catalog / performance links | **No** — plain nullable `int` columns |

Catalog CRUD uses raw SQL `DELETE FROM … WHERE Id=@Id` with no referential checks. Reference script `scripts/azure-sql/001-karaoke-schema.sql` shows FKs for catalog tables, but EF migrations never applied them.

---

## Relationship analysis

### `Performances` → `Songs` — enforce (**RESTRICT**)

**Column:** `Performances.Song`

**Today:** No FK. Admin can delete a song while performance rows remain.

**Problems:**

- **Silent UI loss** — singer queries (`my-history`, repertoire, stale songs) use `INNER JOIN Songs`. Orphaned performances still exist but vanish from lists.
- **Inconsistent stats** — `GetSingerStatsAsync` counts from `Performances` without joining `Songs`, so totals can include ghost logs the user cannot see or edit.
- **Bad inserts** — `POST api/performances` does not verify `Song` exists.

**Recommendation:** `FK_Performances_Songs` with **`ON DELETE RESTRICT`**. Do not delete songs while performances reference them.

---

### `Performances` → `Singers` — enforce (**RESTRICT**)

**Column:** `Performances.Singer`

**Today:** No FK. `AspNetUsers.SingerId` blocks deleting a singer only when a user account is linked (SQL Server default **NO ACTION** on that FK).

**Problems:**

- Admin can delete an unlinked singer while performances still reference that id.
- Registration rollback deletes a new singer on failed link — acceptable for that narrow transactional case only.
- Quit-app policy: do not delete; anonymize account and retain singer + performances.

**Recommendation:** `FK_Performances_Singers` with **`ON DELETE RESTRICT`**. Remove admin singer delete when performances exist. Anonymize accounts instead of deleting.

---

### `Performances` → `Venues` — enforce (**SET NULL** or **RESTRICT**)

**Column:** `Performances.Venue`

**Today:** No FK. Deletes leave performances with a dead venue id.

**Problems:**

- Mild — queries use `LEFT JOIN Venues`; rows still appear with empty or “Unknown venue” labels.
- Stats top-venues grouping still counts orphaned venue ids via `ISNULL`.
- Any authenticated member can create venues (`POST api/venues`); only admins delete. Venues are shared and often referenced.

**Recommendation:** `FK_Performances_Venues` with **`ON DELETE SET NULL`** (preserve log, drop venue name) **or** **`RESTRICT`** if venues must never be removed. Prefer **RESTRICT** unless soft-delete for venues is added.

---

### `SongArtists` → `Songs` / `Artists` — enforce (**CASCADE** / **RESTRICT**)

**Table:** `SongArtists` (`SongId`, `ArtistId`, `DisplayOrder`)

**Today:** Junction table links songs to one or more artists. `DisplayOrder = 0` is primary.

**Recommendation:**

- `SongId` → **`ON DELETE CASCADE`** (delete credits when song is deleted).
- `ArtistId` → **`ON DELETE RESTRICT`** (admin must remove artist credits before deleting an artist).

---

### `Songs.SecondaryArtist` → `Artists` — legacy (**SET NULL**)

**Column:** `Songs.SecondaryArtist` (deprecated; replaced by `SongArtists`)

**Today:** Optional int; backfilled to `SongArtists` with `DisplayOrder = 1`. Column dropped after API/UI migration.

**Recommendation:** FK with **`ON DELETE SET NULL`** until column is removed.

---

### `Songs` → `Artists` (primary) — legacy (**RESTRICT** or **SET NULL**)

**Column:** `Songs.Artist` (deprecated; replaced by `SongArtists` primary row)

**Today:** No FK.

**Problems:**

- Deleted artist → song keeps stale id; grids and JOINs show blank artist.
- Song remains loggable; performance history shows empty artist via `LEFT JOIN`.
- Catalog quality degrades silently.

**Recommendation:** **`RESTRICT`** if artist is required for a valid catalog row; admin must reassign songs before removing an artist.

---

### `Songs` → `Genres` — enforce (**RESTRICT**)

**Column:** `Songs.Genre`

**Today:** No FK.

**Problems:**

- Repertoire genre filters use `INNER JOIN Genres` — songs with deleted genres drop out of filtered views.
- Genre delete with referencing songs → orphan ids and blank genre in grids.

**Recommendation:** **`RESTRICT`** — admin must reassign songs before deleting a genre.

---

### `Artists.MainGenre` → `Genres` — enforce (**SET NULL**)

**Column:** `Artists.MainGenre`

**Today:** No FK.

**Problems:** Artist grid shows blank main genre after genre delete.

**Recommendation:** **`ON DELETE SET NULL`**.

---

### `AspNetUsers` → `Singers` — keep; align with anonymization

**Column:** `AspNetUsers.SingerId`

**Today:** FK + filtered unique index.

**Problems:**

- Registration rollback deletes user and singer on failed link — OK for failed signup only.
- No public “delete my account” endpoint yet; admin could delete an unlinked singer via API.
- Conflicts with permanent performance history tied to singer identity.

**Recommendation:** Keep FK. On quit: anonymize user (clear email/username, lock out), keep `SingerId` and `Singers` row (optionally rename to a generic label). Do not hard-delete.

---

### Identity cascades — keep as-is

Claims, logins, roles, and tokens cascade on user delete. Relevant only if users are hard-deleted — moving away from that for quit flow.

---

## Delete surfaces today

| Actor | Target | Risk |
|-------|--------|------|
| Admin | Genres, artists, songs, singers, venues | Orphans across catalog and performances |
| Singer | Own performance row | Removes history; conflicts with no-deletion direction |
| System | User + singer on failed registration | Acceptable transactional cleanup |
| Nobody | Account quit | **Gap** — needs anonymize flow |

API delete endpoints (admin unless noted):

- `DELETE api/genres/{id}`, `api/artists/{id}`, `api/songs/{id}`, `api/singers/{id}`, `api/venues/{id}`
- `DELETE api/performances/{id}` — singer-scoped (own rows only)

---

## Recommended rollout

### Phase 1 — high impact

1. EF migration: FKs on `Performances` → `Songs`, `Singers`, `Venues` (delete rules above).
2. Clean existing orphan rows before applying migration (script or manual fix).
3. Gate or remove admin hard-deletes on referenced catalog rows (return 409 Conflict).
4. Validate `Song` / `Venue` exist on `POST` / `PUT` performances.

### Phase 2 — catalog

1. FKs on `Songs` → `Artists`, `Genres`; `Songs.SecondaryArtist` → `Artists`; `Artists.MainGenre` → `Genres`.
2. Same orphan cleanup before migration.

### Phase 3 — accounts and performance policy

1. **`POST api/auth/close-account`** (or similar): anonymize PII, disable login, retain singer + performances.
2. Remove or replace singer performance hard-delete (soft-delete or disallow).
3. Keep registration rollback delete as narrow exception.

### Phase 4 — optional

- Soft-delete flags (`IsActive`) on catalog entities to retire bad rows without breaking FKs.
- Admin UI: hide delete when references exist; show reassign workflow.

---

## Backlog checklist

- [x] EF migration: performance FKs + NOT NULL (`20260623235540_AddCatalogForeignKeys`)
- [x] EF migration: song→artist FKs (same migration)
- [x] API: block catalog delete when referenced (409 Conflict)
- [x] API: validate performance foreign keys on write
- [ ] Document and fix existing orphan rows in dev/prod before applying migration in each environment
- [ ] Account anonymization endpoint + WASM “Close account” UX
- [ ] Revisit performance delete policy

---

## Related docs

| Doc | Topic |
|-----|--------|
| [database.md](database.md) | Migrations, seed, test DB alignment |
| [Performances.md](Performances.md) | Performance schema and API |
| [admin-roles.md](admin-roles.md) | Who can mutate catalog |
