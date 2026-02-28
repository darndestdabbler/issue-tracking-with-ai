# BATON: Session 005 — Quick Wins & Demo Readiness

**Project:** Cross-Session Issue & Discussion Tracker
**Baton Created:** 2026-02-28
**Session:** 005 — Inline actions, Edit feature, From Actor fix, demo data & narrative
**Participants:** Human, Claude (Opus 4.6)

---

## Summary of Work Completed

This session addressed two priority tiers from BATON-004's remaining work list: Quick Wins (Priority 1) and Demo Readiness (Priority 2). Six features/fixes were delivered, all frontend + backend, plus a demo narrative document.

### 1. Inline Hold/Archive Buttons (Priority 1A)

Added a "Quick Actions" column to the Issues DataGrid with icon buttons for one-click status changes:

- **Hold** (pause icon) — visible on Open issues. Creates a child post with `ActionType: "Hold"` and text "Deferred via quick action". Root post status changes to Deferred.
- **Archive** (archive icon) — visible on Open or Deferred issues. Creates a child post with `ActionType: "Archive"` and text "Closed via quick action". Root post status changes to Closed.

Both buttons use `MudTooltip` for hover labels (MudBlazor v9's `MudIconButton` doesn't have a `Title` parameter — the analyzer flagged this during initial build).

Session ID for quick actions defaults to the current filter session, falling back to the most recent session.

### 2. Fix From Actor in Create Issue Dialog (Priority 1B)

| File | Change |
|------|--------|
| `SharedDtos.cs` | Added `FromActorId` property to `CreateIssueRequest` |
| `CreateIssueDialog.razor` | Added From Actor `MudSelect` dropdown (mirroring ReplyDialog pattern), defaults to Human (id: 2) |
| `Issues.razor:212` | Changed `fromActorId = 1` (hardcoded Claude) to `fromActorId = request.FromActorId` |

The Create Issue dialog now has From and Action Type side by side (xs="6" each), matching the ReplyDialog layout.

### 3. Edit Post Feature (Priority 1C — new scope)

Added the ability to edit root post Title, Tags, and Text after creation. This was not in BATON-004's roadmap — added based on user feedback during planning.

**Backend:**

| File | Change |
|------|--------|
| `PostService.cs` | New `UpdatePostAsync(int id, string? title, string? tags, string? text)` — only allows editing root posts (`ActionForId == null`). Null parameters are skipped (partial update). |
| `PostsController.cs` | New `[HttpPut("{id:int}")]` endpoint accepting `UpdatePostRequest` body. Returns 404 for non-existent or non-root posts. |
| `SharedDtos.cs` | New `UpdatePostRequest` DTO with nullable `Title`, `Tags`, `Text` |

**Frontend:**

| File | Change |
|------|--------|
| `EditIssueDialog.razor` | **New file.** Pre-populates Title, Tags, Text from the current post. Returns `UpdatePostRequest` via `DialogResult.Ok()`. |
| `Issues.razor` | Added Edit button (with edit icon) in expanded thread `MudCardActions` next to Reply. `OpenEditDialog()` and `SubmitEdit()` methods PUTs to `/api/posts/{id}` and refreshes both thread and grid. |

### 4. Tags Displayed in Issues Grid

Converted the Title column from `PropertyColumn` to `TemplateColumn`. Tags are rendered as `MudChip` components (small, outlined) below the title text. Tags are split by comma with whitespace trimming.

### 5. Actor Rename (Priority 1D)

Changed Actor 2 from "Dennis" to "Human" in `DatabaseSeeder.cs`. No personal names hardcoded in the application.

### 6. Demo Data Seeding (Priority 2A)

Extended `DatabaseSeeder.cs` with `SeedDemoDataAsync()` — runs only if no sessions exist. Deleted the previous SQLite DB for a clean start.

**Seeded data:**

| Sessions | |
|----------|---|
| Session 001 — Initial Scaffold | Feb 25 |
| Session 002 — API & Data Model | Feb 26 |
| Session 003 — UI Implementation | Feb 27 |

| Issue Thread | Status | Posts | Tags |
|-------------|--------|-------|------|
| Design post-based data model | Closed | New → Discuss → Discuss → Archive | architecture, data-model |
| Token refresh returns null on expired sessions | Closed | New → Discuss → Check (to Human) → Archive | bug, auth |
| Add pagination to issues grid | Deferred | New → Discuss → Hold | ui, enhancement |
| Centralize API base URL | Open | New → Discuss | tech-debt, config |
| Seed demo data for architecture review | Open | New | demo, onboarding |

Timestamps are staggered across sessions. Actors alternate between Claude and Human for realistic variety.

### 7. Demo Narrative (Priority 2B)

New file: `docs/DEMO-NARRATIVE.md` — structured talking points for the architecture director demo covering elevator pitch, the problem, the solution, architecture walkthrough, live demo flow, and key takeaways.

---

## Files Changed This Session

| File | Change Type |
|------|-------------|
| `Components/SharedDtos.cs` | Enhanced — added `FromActorId` to `CreateIssueRequest`, added `UpdatePostRequest` DTO |
| `Components/CreateIssueDialog.razor` | Enhanced — added From Actor dropdown, default to Human |
| `Services/PostService.cs` | Enhanced — added `UpdatePostAsync` method |
| `Controllers/PostsController.cs` | Enhanced — added `[HttpPut]` endpoint and `UpdatePostRequest` class |
| `Components/EditIssueDialog.razor` | **New** — edit dialog for root posts |
| `Components/Pages/Issues.razor` | Enhanced — tag chips in title, Quick Actions column, Edit button, `QuickAction()`, `OpenEditDialog()`, `SubmitEdit()`, fixed `fromActorId` |
| `Data/DatabaseSeeder.cs` | Enhanced — renamed Actor 2 to "Human", added `SeedDemoDataAsync()` with 3 sessions + 5 threads |
| `docs/DEMO-NARRATIVE.md` | **New** — demo talking points |
| `docs/BATON-Session-005-Quick-Wins-and-Demo.md` | **New** — this file |

---

## Current Application State

### What Works End-to-End
- **Sessions page:** Create sessions, view list, navigate to filtered issues
- **Issues page:** Create issues with selectable From Actor, filter by status/tags/session, expand threads
- **Tag display:** Tags shown as chips under issue titles in the grid
- **Inline status changes:** Hold/Archive icon buttons on the grid for one-click status transitions
- **Reply dialog:** Reply with Discuss/Hold/Archive/Check/Proceed actions
- **Edit dialog:** Edit Title, Tags, Text on root posts via PUT endpoint
- **Status propagation:** Hold → Deferred, Archive → Closed (both via Reply and Quick Actions)
- **Thread display:** Full timeline with color-coded action types
- **Demo data:** 3 sessions, 5 issue threads (2 Closed, 1 Deferred, 2 Open) seeded on first run
- **API:** All endpoints functional — GET, POST, PUT — consumed by both UI and `curl.exe`

### Seeded Actors
| Id | Name |
|----|------|
| 1 | Claude |
| 2 | Human |
| 3 | System |

### API Endpoints
| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/posts?status=&projectId=&tags=&sessionId=` | Filter posts |
| GET | `/api/posts/{id}` | Get single post |
| GET | `/api/posts/{id}/thread` | Get full thread |
| POST | `/api/posts` | Create post (issue or reply) |
| PUT | `/api/posts/{id}` | Edit root post (title, tags, text) |
| GET | `/api/sessions?projectId=` | List sessions |
| POST | `/api/sessions` | Create session |
| GET | `/api/actors` | List actors |

---

## Known Issues / Remaining Work

### Priority 3 — Polish (deferred from this session)

1. **Centralize API base URL** — `http://localhost:5124` is hardcoded in 8+ places across `Issues.razor` and `Sessions.razor`. Should register a named `HttpClient` with `BaseAddress` in `Program.cs`.
2. **Sorting/paging on DataGrid** — Not yet enabled. Fine for small data, but needed as data grows. MudDataGrid supports `ServerData` for server-side paging.
3. **Session delete/archive** — No way to remove or close sessions from UI.
4. **N+1 thread fetch optimization** — `PostService.CollectThreadAsync` loads posts one-at-a-time recursively. Could use a single recursive CTE query.
5. **Duplicate SessionDto definitions** — Defined in both `SharedDtos.cs` and locally in `Sessions.razor` (with different fields). Should consolidate.
6. **Reopen action** — No way to reopen a Closed or Deferred issue. Could add a "Reopen" ActionType that sets status back to Open.

### Observations
- **MudBlazor v9:** `MudIconButton` does not have a `Title` parameter. Use `MudTooltip` wrapper for hover text. The MUD0002 analyzer catches this.
- **Blazor Razor Language Server:** Phantom errors are common with MudBlazor components. If `dotnet build` succeeds with 0 errors, the IDE squiggles are false positives.
- **SQLite DB location:** `IssueTracker.Web/issuetracker.db` — delete this file to re-seed from scratch.

---

## Architecture Recap (Updated)

- **Single project:** `IssueTracker.Web` — API controllers + Blazor Server UI in one process
- **Data model:** Everything is a Post. Status changes via child posts with audit trail. Root posts editable via PUT.
- **Database:** SQLite (switchable to SQL Server via `appsettings.json`)
- **UI:** MudBlazor 9.0.0 — DataGrid, Dialogs, Timeline, Chips, Tooltips, Snackbar
- **Seeded actors:** Claude=1, Human=2, System=3 | Default project: "Issue Tracker"=1
- **API base:** `http://localhost:5124`

---

## How to Start

```bash
cd "c:\Users\denmi\source\repos\issue-tracking-with-ai\IssueTracker.Web"
dotnet run
```

Then:
- UI: `http://localhost:5124`
- Swagger: `http://localhost:5124/swagger`
- API: `http://localhost:5124/api/*`

---

*Load this BATON at the start of Session 006. Recommended focus: centralize API base URL and consolidate duplicate SessionDto.*
