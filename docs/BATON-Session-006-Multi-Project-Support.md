# BATON: Session 006 — Multi-Project Support & Filter Restructure

**Project:** Cross-Session Issue & Discussion Tracker
**Baton Created:** 2026-02-28
**Session:** 006 — Multi-project CRUD, cascading filters, HttpClient centralization, DTO consolidation
**Participants:** Human, Claude (Opus 4.6)

---

## Summary of Work Completed

This session delivered a structural overhaul: the app now supports multiple projects with full CRUD, the Issues page uses cascading Project/Session filters instead of session-only filtering, the Sessions page was rewritten as a master-detail "Projects & Sessions" page, and several long-standing tech debt items were resolved.

### 1. API Base URL Centralization (resolved tracker issue #12)

| File | Change |
|------|--------|
| `appsettings.json` | Added `"ApiBaseUrl": "http://localhost:5124"` |
| `Program.cs` | Replaced generic `AddHttpClient()` with named HttpClient `"IssueTrackerApi"` configured with `BaseAddress` from config. Added scoped factory so `@inject HttpClient Http` resolves with BaseAddress already set. |
| `Issues.razor` | Stripped 8 hardcoded `http://localhost:5124/` prefixes — all API calls now use relative paths |
| `Sessions.razor` | Stripped 3 hardcoded prefixes |

No Razor component injection changes needed — the scoped `HttpClient` factory transparently provides a configured client.

### 2. ProjectsController Completion

| File | Change |
|------|--------|
| `Controllers/ProjectsController.cs` | Added `[HttpGet("{id:int}")]` for single project retrieval. Added `[HttpPut("{id:int}")]` for project rename. Fixed `CreatedAtAction` to point to `GetById` instead of `GetAll`. |

### 3. SharedDtos Consolidation (resolved BATON-005 backlog item)

| File | Change |
|------|--------|
| `Components/SharedDtos.cs` | Added `ProjectDto` (Id, Name). Added `PostDto` (moved from Issues.razor). Expanded `SessionDto` to include ProjectId, ProjectName, CreatedOn, PostCount. |
| `Issues.razor` | Deleted local `PostDto` class (was lines 426-442) |
| `Sessions.razor` | Deleted local `SessionDto` class (was lines 143-152) |

All pages now share a single set of DTOs from `SharedDtos.cs`.

### 4. Issues Page Filter Restructure

The Issues page was overhauled from session-centric to project-centric filtering:

**Before:** Status + Tags filters, session set via URL `?sessionId=X`, `projectId=1` hardcoded everywhere.

**After:** Four-column filter panel:
| Column | Control | Behavior |
|--------|---------|----------|
| Project | `MudSelect<int>` | Mandatory. Auto-filters on change. Cascades session dropdown. |
| Session | `MudSelect<int?>` | Optional, defaults to "All Sessions". Auto-filters on change. |
| Status | `MudSelect<string?>` | Unchanged. Uses Filter button. |
| Tags | `MudTextField` | Unchanged. Uses Filter button. |

URL query params `?projectId=X&sessionId=Y` are parsed on load and reflected in the dropdowns, so navigation from the Projects & Sessions page sets the controls correctly.

Other changes:
- Page title is now dynamic: `"ProjectName — Issues"`
- `CreateIssue` no longer sends hardcoded `projectId = 1` (API derives it from Session)
- Dialog parameters pass `selectedSessionId` instead of the old `filterSessionId`

### 5. Projects & Sessions Page (Master-Detail Rewrite)

`Sessions.razor` was completely rewritten as a two-column master-detail layout:

**Left column (sm=4):** Project list via `MudList` with:
- Click-to-select highlighting
- Add Project button → opens `ProjectDialog` (MudDialog)
- Per-project View Issues button → navigates to `/issues?projectId=X`
- Per-project Edit button → opens `ProjectDialog` in edit mode

**Right column (sm=8):** Sessions grid for selected project via `MudDataGrid` with:
- Name, Started, Posts, Actions columns
- New Session button (creates under selected project)
- Per-session View Issues → navigates to `/issues?projectId=X&sessionId=Y`

Dual routes: `@page "/sessions"` and `@page "/projects"` — both work.

### 6. ProjectDialog (New File)

| File | Change |
|------|--------|
| `Components/ProjectDialog.razor` | **New.** Single dialog for create and edit. Takes optional `ProjectId` and `InitialName` parameters. Returns project name via `DialogResult.Ok()`. Parent page handles POST/PUT. |

### 7. NavMenu Update

Changed "Sessions" link to "Projects & Sessions" with `AccountTree` icon and `href="projects"`.

### 8. Database Seeder Update (resolved tracker issue #14)

Added "Sample Project" as a second seeded project. Only affects fresh databases (guarded by `if (!await db.Projects.AnyAsync())`).

### 9. MudBlazor v9 Learnings

Two new v9 conventions discovered during this session:
- `MudList` no longer has a `Clickable` parameter — removed without replacement (list items are clickable by default)
- `OnClick:stopPropagation` directive does **not** work on MudBlazor components — must wrap in `<span @onclick:stopPropagation="true">` instead

---

## Files Changed This Session

| File | Change Type |
|------|-------------|
| `appsettings.json` | Enhanced — added `ApiBaseUrl` |
| `Program.cs` | Enhanced — named HttpClient with BaseAddress + scoped factory |
| `Controllers/ProjectsController.cs` | Enhanced — added GET by id, PUT for rename |
| `Components/SharedDtos.cs` | Enhanced — added `ProjectDto`, `PostDto`; expanded `SessionDto` |
| `Components/Pages/Issues.razor` | Enhanced — cascading Project/Session dropdowns, stripped hardcoded URLs, removed local PostDto |
| `Components/Pages/Sessions.razor` | **Rewritten** — master-detail Projects & Sessions page |
| `Components/ProjectDialog.razor` | **New** — create/edit project dialog |
| `Components/Layout/NavMenu.razor` | Enhanced — renamed link, updated href and icon |
| `Data/DatabaseSeeder.cs` | Enhanced — seeds second project |
| `docs/BATON-Session-006-Multi-Project-Support.md` | **New** — this file |

No model changes. No EF migrations. No new NuGet packages.

---

## Current Application State

### What Works End-to-End
- **Projects & Sessions page:** Master-detail layout, create/edit projects, create sessions under selected project, view issues per project or per session
- **Issues page:** Cascading Project + Session filters, status/tags filters, create issues, reply, edit root posts, quick Hold/Archive actions
- **Multi-project:** Full support — switch between projects, each with independent sessions and issues
- **API base URL:** Centralized via named HttpClient — no hardcoded URLs in UI
- **Demo data:** 2 projects seeded, 3 sessions and 5 threads under "Issue Tracker", empty "Sample Project"

### Seeded Data
| Entity | Id | Name |
|--------|----|------|
| Actor | 1 | Claude |
| Actor | 2 | Human |
| Actor | 3 | System |
| Project | 1 | Issue Tracker |
| Project | 2 | Sample Project |

### API Endpoints
| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/projects` | List all projects |
| GET | `/api/projects/{id}` | Get single project |
| POST | `/api/projects` | Create project |
| PUT | `/api/projects/{id}` | Edit project name |
| GET | `/api/posts?status=&projectId=&tags=&sessionId=` | Filter posts |
| GET | `/api/posts/{id}` | Get single post |
| GET | `/api/posts/{id}/thread` | Get full thread |
| POST | `/api/posts` | Create post (issue or reply) |
| PUT | `/api/posts/{id}` | Edit root post |
| GET | `/api/sessions?projectId=` | List sessions |
| POST | `/api/sessions` | Create session |
| GET | `/api/actors` | List actors |

### Tracker State After This Session
| Status | Count | Issues |
|--------|-------|--------|
| Open | 0 | — |
| Deferred | 1 | #9: Add pagination to issues grid |
| Closed | 4 | #1: Design post-based data model, #4: Token refresh null on expired sessions, #12: Centralize API base URL, #14: Seed demo data |

---

## Known Issues / Remaining Work

### Priority 3 — Polish

1. **Sorting/paging on DataGrid** — Not yet enabled. MudDataGrid supports `ServerData` for server-side paging. Would need a paginated API endpoint with skip/take params and a TotalCount wrapper.
2. **Session delete/archive** — No way to remove or close sessions from UI.
3. **N+1 thread fetch optimization** — `PostService.CollectThreadAsync` loads posts one-at-a-time recursively. Could use a single recursive CTE query. Also, session post count is calculated via N+1 API calls on the Projects & Sessions page.
4. **Reopen action** — No way to reopen a Closed or Deferred issue. Could add a "Reopen" ActionType that sets status back to Open.

### Priority 4 — Testing & Tooling (New)

5. **Integration test project** — Add a separate test project (`IssueTracker.Tests`) with integration tests covering the API endpoints. Use `WebApplicationFactory<Program>` for in-memory test server with SQLite in-memory database.
6. **Swagger sample JSON files** — Create sample request JSON files for each endpoint (POST/PUT) for manual Swagger testing. Store in `docs/swagger-samples/` or similar.

---

## Architecture Recap

- **Single project:** `IssueTracker.Web` — API controllers + Blazor Server UI in one process
- **Data model:** Everything is a Post. Status changes via child posts with audit trail. Root posts editable via PUT.
- **Database:** SQLite (switchable to SQL Server via `appsettings.json`)
- **HttpClient:** Named `"IssueTrackerApi"` with `BaseAddress` from `appsettings.json` `ApiBaseUrl`. Scoped factory provides configured client to all Razor components.
- **UI:** MudBlazor 9.0.0 — DataGrid, Dialogs, Timeline, Chips, Tooltips, Snackbar, List, Grid
- **Seeded data:** 2 projects, 3 actors | API base: `http://localhost:5124`

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

To re-seed: delete `IssueTracker.Web/issuetracker.db` and restart.

---

*Load this BATON at the start of Session 007. Recommended focus: integration test project + Swagger sample JSON files (Priority 4), or pagination (Priority 3.1).*
