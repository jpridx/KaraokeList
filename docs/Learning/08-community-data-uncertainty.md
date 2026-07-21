# 08 — Community Data with Uncertainty

## Overview

Catalog enrichment uses **MusicBrainz**, a community-maintained music database. Results are *pretty good* but never guaranteed: duplicate recordings, live/demo versions, reissues, ambiguous titles, and noisy tags all appear. KaraokeList treats MusicBrainz data as **signals**, surfaces confidence, ranks alternatives, and keeps a **human in the loop** for applying changes.

| Concern | Approach in this repo |
|---------|------------------------|
| Search score | Keep MB `Score`; show as “% confidence” in UI |
| Ambiguous hits | Rank heuristics (prefer studio originals, title/artist match) |
| Wrong auto-apply | Only backfill when names confidently match |
| Genre tags | Vote by tag count; prefer specific over generic |
| User control | Alternatives list + explicit Apply |

Related but different: `docs/data-integrity.md` covers FK/orphan/delete policy — referential integrity, not entity resolution.

## Major aspects

1. **Scores are not truth** — a high MusicBrainz score can still be the wrong recording.
2. **Progressive / relaxed search** — helpers normalize and broaden queries carefully.
3. **Disambiguation filters** — demote live, demo, remix, obvious reissues when looking for karaoke catalog titles.
4. **Alternatives** — return more than the top hit so users can choose.
5. **Gated auto-backfill** — MBIDs/year/genre apply automatically only when name match + credibility checks pass.
6. **UI honesty** — show confidence and let the user apply or override.
7. **Separate integrity from resolution** — FK rules protect the database; heuristics protect metadata quality.

## Code samples

### Sample 1 — Rank matches under ambiguity (score is not the only sort key)

```250:271:KaraokeList.Shared/MusicBrainzSearchHelper.cs
    public static List<CanonicalMatchDto> RankMatches(
        IEnumerable<CanonicalMatchDto> matches,
        string searchTitle,
        Func<CanonicalMatchDto, bool>? isSoftUnwanted = null,
        string? searchArtist = null)
    {
        // ...
        return list
            .OrderBy(m => GetClearlyUnwantedRank(m))
            .ThenBy(m => TitleMatchesSearch(m.Title, searchTitle) ? 0 : 1)
            .ThenBy(m => GetSoftUnwantedRank(m, searchTitle, oldestExactTitleYear, isSoftUnwanted))
            .ThenBy(m => m.Year ?? int.MaxValue)
            .ThenBy(m => GetArtistMatchRank(m, searchArtist))
            .ThenByDescending(m => m.Score)
            .ThenBy(m => m.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
```

### Sample 2 — UI surfaces MusicBrainz score as confidence

```razor
@* KaraokeList.Web/Components/CanonicalNameCheck.razor *@
<span>Names match MusicBrainz@(_match.Score > 0 ? $" ({_match.Score}% confidence)" : "").</span>
```

## Further references

| Path | Lines | Why it matters |
|------|-------|----------------|
| `KaraokeList.Api/Services/MusicBrainzService.cs` | ~61–111 | Search, preserve scores, build match + alternatives |
| `KaraokeList.Api/Services/CanonicalCatalogService.cs` | ~172–209 | Auto-backfill only when `NamesMatch` / credibility gates pass |
| `KaraokeList.Shared/MusicBrainzGenreResolver.cs` | ~103–120 | Tag-count voting; prefer specific genres over generic |

## Exercises

1. **Multiple choice.** MusicBrainz data should be treated as:
   - A) Authoritative truth that never needs review
   - B) Useful but uncertain community signals
   - C) A replacement for SQL
   - D) Identical to JWT claims

2. **Fill in the blank.** The UI often displays MusicBrainz `Score` as a ________ percentage.

3. **Multiple choice.** Why does ranking demote live/demo versions for this app?
   - A) They break JWT auth
   - B) Karaoke catalogs usually want the familiar studio/original title
   - C) MusicBrainz forbids live recordings
   - D) Polly cannot retry them

4. **Fill in the blank.** Multiple candidate recordings are returned as ________ so the user can choose.

5. **Multiple choice.** Auto-backfill of MBID/year/genre should happen when:
   - A) Any result exists
   - B) Name/credibility gates pass (confident match)
   - C) The service worker is offline
   - D) The user is anonymous

6. **Fill in the blank.** Genre resolution prefers ________ genres over generic ones (e.g. Classic Rock over Rock).

7. **Multiple choice.** `docs/data-integrity.md` mainly addresses:
   - A) OpenAI prompts
   - B) Referential integrity / delete & orphan policy
   - C) PWA asset hashing
   - D) GitHub path filters

8. **Fill in the blank.** Entity-resolution helpers for MusicBrainz live largely in ________.

9. **Multiple choice.** True Levenshtein fuzzy search in this project is currently:
   - A) Fully implemented as the default search
   - B) Deferred / discussed as a future option
   - C) Required by Azure SQL
   - D) Used only for JWT validation

10. **Fill in the blank.** Keeping a ________-in-the-loop (Apply / alternatives) is the main defense against wrong community matches.

## Answer key

1. B  
2. confidence  
3. B  
4. alternatives  
5. B  
6. specific  
7. B  
8. `MusicBrainzSearchHelper` (or Shared MusicBrainz helpers)  
9. B  
10. human / user  
