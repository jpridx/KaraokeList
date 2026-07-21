# Learning Documentation Task

## Purpose

This folder captures the major technical learning goals of the KaraokeList project — both as a hobby application and as a vehicle for learning modern .NET web development practices.

Each topic document is a short, self-contained study guide grounded in *this* codebase: major concepts, one or two key code samples, pointers into real files, and practice exercises.

## Branch

All of this documentation was created on branch **`DocumentingLearning`**.

## Folder structure

```
docs/Learning/
├── Learning.md                              ← this file (task overview)
├── 01-blazor-wasm-api.md
├── 02-comprehensive-testing.md
├── 03-ci-cd.md
├── 04-ms-azure.md
├── 05-pwa-resiliency-caching.md
├── 06-llm-as-a-service.md
├── 07-rate-limited-api.md
├── 08-community-data-uncertainty.md
├── 09-jwt-authentication.md
└── 10-oauth-authentication.md
```

The folder is registered as a **Solution Folder** named `Learning` under the existing `docs` solution folder in `KaraokeList.sln`.

## Document format (each topic)

Every topic document follows the same template:

1. **Overview** — why this topic matters in KaraokeList
2. **Major aspects** — the core ideas to internalize
3. **Code samples** — 1–2 key snippets from the repo
4. **Further references** — 2–3 Path/Filename/lines pointers for deeper study
5. **Exercises** — 10 questions mixing multiple choice and fill-in-the-blank
6. **Answer key** — at the end of each document

## Topics covered

| # | Document | Learning goal |
|---|----------|---------------|
| 01 | Blazor WASM + API | Split hosting: browser UI + REST API + shared DTOs |
| 02 | Comprehensive testing | Unit, bUnit, integration, and Playwright E2E |
| 03 | CI/CD | GitHub Actions build/test/deploy pipelines |
| 04 | MS Azure | Resources, environments, Bicep, App Service / SWA / SQL |
| 05 | PWA / Resiliency / Caching | Service worker, LocalStorage caches, Polly retries |
| 06 | LLM as a service | OpenAI behind an API, constrained prompts, graceful fallback |
| 07 | Rate-limited API | Outbound (MusicBrainz) and inbound (auth) throttling |
| 08 | Community data uncertainty | MusicBrainz scores, ranking heuristics, human-in-the-loop |
| 09 | JWT Authentication | Issue, store, attach, and validate Bearer tokens |
| 10 | OAuth Authentication | Google/Microsoft login → one-time code → JWT |

## How to use these docs

1. Read the overview and major aspects for a topic.
2. Open the cited source files and walk the real flow in the debugger or IDE.
3. Attempt the exercises without looking at the answer key.
4. Use the further-reference pointers when you want more depth than the short guide covers.

## Related existing docs

These Learning guides complement (and do not replace) the deeper design docs already under `docs/`:

| Learning topic | Deeper design docs |
|----------------|--------------------|
| Blazor WASM + API | `docs/wasm-api-local-dev.md`, `docs/mobile-ux.md` |
| Testing | `docs/e2e-playwright.md` |
| CI/CD | `docs/github-actions.md` |
| Azure | `docs/azure-deployment.md`, `docs/deployment-roadmap.md` |
| PWA / Resilience | `docs/resilience.md`, `docs/mobile-ux.md` |
| LLM | `docs/ai-integration.md` |
| Community data | `docs/data-integrity.md`, `docs/flexible-search-options.md` |
| Auth (JWT + OAuth) | `docs/security-private-access.md`, `docs/admin-roles.md` |

## Task checklist

- [x] Create branch `DocumentingLearning`
- [x] Create `docs/Learning/` folder
- [x] Write `Learning.md` (this task document)
- [x] Write 10 topic study guides with samples, references, and exercises
- [x] Register `Learning` as a nested solution folder under `docs` in `KaraokeList.sln`
