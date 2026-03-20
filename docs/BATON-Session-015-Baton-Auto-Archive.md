# BATON: Session 015 — BATON Auto-Archive + Latest-Baton API

**Project:** Issue Tracker
**Baton Created:** 2026-03-20
**Session:** 015 — BATON auto-archive logic, latest-baton endpoint, integration tests
**Participants:** Human, Claude (Opus 4.6)

---

## Summary of Work Completed

This is Session B of the multi-session "BATONs as Issues" plan. Added the service-layer auto-archive logic and convenience API endpoint that will allow BATON documents (stored as posts tagged "baton") to be automatically managed.

| Area | File | Change |
|------|------|--------|
| Helper | `Services/PostService.cs` | `HasTag` private static method — precise comma-delimited tag matching (case-insensitive) |
| Auto-archive | `Services/PostService.cs` | In `CreatePostAsync`: when a new baton-tagged root post is created, all previously-Open baton posts for the same project are auto-closed with a system Archive child post (fromActorId=3) |
| Query | `Services/PostService.cs` | `GetLatestBatonAsync(int projectId)` — returns the most recent baton-tagged root post regardless of status |
| Endpoint | `Controllers/PostsController.cs` | `GET /api/posts/latest-baton?projectId=N` — returns 200 with latest baton or 404 |
| Tests | `Tests/PostsTests.cs` | 8 new integration tests (52 total, all passing) |

### Key Decisions

- **System-generated Archive posts** (fromActorId=3) bypass ownership enforcement — created via `db.Posts.Add()` directly, not through `CreatePostAsync`. This is intentional for system-initiated auto-archives.
- **Two SaveChanges calls** in `CreatePostAsync` for baton auto-archive — the new post needs a generated ID before auto-archive text can reference it. Acceptable since batons are low-frequency.
- **`GetLatestBatonAsync` returns regardless of status** — the caller wants the content of the most recent baton, whether Open or Closed.
- **Tag matching uses `HasTag` in-memory** for trigger detection (precise) and `LIKE '%baton%'` in SQL queries (consistent with existing tag filtering). False positives from substring matching are negligible given controlled tag vocabulary.

---

## Multi-Session Plan: BATONs as Issues

| Session | Focus | Status |
|---------|-------|--------|
| A | SQL Server migration | Done (Session 014) |
| **B** | **BATON auto-archive + latest-baton API** | **Done (this session)** |
| C | Historical BATON import (~140+ files across 6 projects) | Next |
| D | Update Claude instructions (baton.md, issue-tracking.md, CLAUDE.md per project) | Pending |
| E | Cleanup, verification, README (#23) | Pending |

---

## Remaining Work

Open issues in tracker: #23 (README.md), #26 (Home page + logo), #398 (Actor admin page). Sessions C-E of BATONs-as-Issues plan remain.

---

## How to Start

```bash
cd "c:\Users\denmi\source\repos\issue-tracking-with-ai\IssueTracker.Web"
dotnet run
```

**Requires:** SQL Server running on localhost (default instance, Windows Auth).

- UI: `http://localhost:5124`
- Swagger: `http://localhost:5124/swagger`
- API: `http://localhost:5124/api/*`

Run tests (app does NOT need to be running):
```bash
cd "c:\Users\denmi\source\repos\issue-tracking-with-ai"
dotnet test
```

---

*Load this BATON at the start of Session 016. Recommended focus: Session C — Historical BATON import (parse ~140+ BATON markdown files across 6 projects, import as posts with tag "baton").*
