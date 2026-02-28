# BATON: Session 007 — Paging, Sorting & Session Archive

**Project:** Cross-Session Issue & Discussion Tracker
**Baton Created:** 2026-02-28
**Session:** 007 — Server-side paging/sorting on Issues grid, session archive (soft delete)
**Participants:** Human, Claude (Opus 4.6)

---

## Summary of Work Completed

This session delivered two Priority 3 items from the BATON-006 backlog: server-side sorting/paging on the Issues DataGrid, and session archive (soft delete).

### 1. Server-Side Paging & Sorting (resolved tracker issue #9)

| File | Change |
|------|--------|
| `Components/SharedDtos.cs` | Added `PagedResponse<T>` (Items + TotalCount) for deserializing paged API responses |
| `Services/PostService.cs` | Added `GetPostsPagedAsync` — applies filters, `CountAsync()`, sort switch (Title/Status/ActionType/FromActor/DateTime), `Skip`/`Take`. Existing `GetPostsAsync` kept for CLI backward compat. |
| `Controllers/PostsController.cs` | `GetAll` accepts optional `page`, `pageSize`, `sortBy`, `sortDesc`. When `page` present → returns `{ items, totalCount }` wrapper. Without `page` → flat array (backward compatible). |
| `Components/Pages/Issues.razor` | Replaced `Items="@posts"` with `ServerData="LoadServerData"` callback. Added `MudDataGridPager` (10/25/50). All 5 data columns are sortable `PropertyColumn`s. Removed `FilterPosts()`, `loading` flag, `posts` list. All mutations call `ReloadServerData()`. |

**API shape when paging:**
```
GET /api/posts?projectId=1&page=0&pageSize=10&sortBy=Title&sortDesc=true
→ { "items": [...], "totalCount": 16 }
```

**API shape without paging (backward compat):**
```
GET /api/posts?status=Open&projectId=1
→ [ ... ]
```

### 2. Session Archive (Soft Delete)

| File | Change |
|------|--------|
| `Models/Session.cs` | Added `bool IsArchived` property (default `false`) |
| `Controllers/SessionsController.cs` | `GetAll` filters `WHERE !IsArchived` by default (opt-in `includeArchived=true`). Added `PUT /api/sessions/{id}/archive` and `PUT /api/sessions/{id}/restore` endpoints. |
| `Components/Pages/Sessions.razor` | Added Archive icon button per-session with `ShowMessageBoxAsync` confirmation. On confirm → calls archive endpoint, reloads grid. |
| Migration `AddSessionIsArchived` | Adds `IsArchived` column to Sessions table |

Archived sessions are hidden from all session dropdowns (Issues page, Sessions page) but remain in the database. Reversible via `PUT /api/sessions/{id}/restore`.

### 3. MudBlazor v9 Learnings

Three new v9 conventions discovered:
- `ServerData` delegate signature: `Func<GridState<T>, CancellationToken, Task<GridData<T>>>` (requires `CancellationToken`)
- `SortLabel` does **not** exist in v9 — use `PropertyColumn` with `Property` expression; `SortDefinition.SortBy` contains the property name string
- `ShowMessageBox` was renamed to `ShowMessageBoxAsync`

---

## Files Changed This Session

| File | Change Type |
|------|-------------|
| `Components/SharedDtos.cs` | Enhanced — added `PagedResponse<T>` |
| `Services/PostService.cs` | Enhanced — added `GetPostsPagedAsync` |
| `Controllers/PostsController.cs` | Enhanced — paging/sorting params on `GetAll` |
| `Components/Pages/Issues.razor` | Enhanced — ServerData pattern, pager, sortable columns |
| `Models/Session.cs` | Enhanced — added `IsArchived` |
| `Controllers/SessionsController.cs` | Enhanced — archive filter, archive/restore endpoints |
| `Components/Pages/Sessions.razor` | Enhanced — archive button with confirmation |
| Migration `AddSessionIsArchived` | **New** — schema migration |
| `docs/BATON-Session-007-Paging-Sorting-Session-Archive.md` | **New** — this file |

No new NuGet packages. One EF migration.

---

## Current Application State

### What Works End-to-End
- **Issues page:** Server-side paging (10/25/50), sortable columns (Title, Status, Action, From, Date), cascading Project+Session filters, create/reply/edit/quick-action all reload grid
- **Projects & Sessions page:** Master-detail layout, create/edit projects, create sessions, archive sessions with confirmation, view issues per project/session
- **Session archive:** Soft delete with `IsArchived` flag. Archived sessions hidden from all dropdowns. Reversible via API.
- **Multi-project:** Full support with independent sessions and issues
- **API backward compat:** CLI `curl` calls without `page` param return flat arrays as before

### API Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/projects` | List all projects |
| GET | `/api/projects/{id}` | Get single project |
| POST | `/api/projects` | Create project |
| PUT | `/api/projects/{id}` | Edit project name |
| GET | `/api/posts?status=&projectId=&tags=&sessionId=&page=&pageSize=&sortBy=&sortDesc=` | Filter posts (paged if `page` present) |
| GET | `/api/posts/{id}` | Get single post |
| GET | `/api/posts/{id}/thread` | Get full thread |
| POST | `/api/posts` | Create post (issue or reply) |
| PUT | `/api/posts/{id}` | Edit root post |
| GET | `/api/sessions?projectId=&includeArchived=` | List sessions (excludes archived by default) |
| GET | `/api/sessions/{id}` | Get single session |
| POST | `/api/sessions` | Create session |
| PUT | `/api/sessions/{id}/archive` | Archive session |
| PUT | `/api/sessions/{id}/restore` | Restore archived session |
| GET | `/api/actors` | List actors |

### Tracker State After This Session

| Status | Count | Issues |
|--------|-------|--------|
| Open | 0 | — |
| Deferred | 0 | — |
| Closed | 5 | #1: Design post-based data model, #4: Token refresh null, #9: Add pagination, #12: Centralize API base URL, #14: Seed demo data |

---

## Known Issues / Remaining Work

### Priority 3 — Polish (remaining)

1. **N+1 thread fetch optimization** — `PostService.CollectThreadAsync` loads posts one-at-a-time recursively. Could use a single recursive CTE query. Also, session post count on Projects & Sessions page is calculated via N+1 API calls.
2. **Reopen action** — No way to reopen a Closed or Deferred issue. Could add a "Reopen" ActionType that sets status back to Open.

### Priority 4 — Testing & Tooling

3. **Integration test project** — Add `IssueTracker.Tests` with `WebApplicationFactory<Program>` + SQLite in-memory. Cover API endpoints.
4. **Swagger sample JSON files** — Sample request JSON for each POST/PUT endpoint.

### Priority 5 — Nice to Have

5. **Show archived sessions toggle** — Add a checkbox/switch on the Projects & Sessions page to show/hide archived sessions (API already supports `includeArchived=true`).
6. **Session rename** — No PUT endpoint for renaming sessions.

---

## Architecture Recap

- **Single project:** `IssueTracker.Web` — API controllers + Blazor Server UI in one process
- **Data model:** Everything is a Post. Status changes via child posts with audit trail. Root posts editable via PUT.
- **Database:** SQLite (switchable to SQL Server via `appsettings.json`)
- **HttpClient:** Named `"IssueTrackerApi"` with `BaseAddress` from `appsettings.json`
- **UI:** MudBlazor 9.0.0 — DataGrid (ServerData), Dialogs, Timeline, Chips, Tooltips, Snackbar, List, Grid, Pager
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

*Load this BATON at the start of Session 008. Recommended focus: N+1 optimization (P3.1), Reopen action (P3.2), or integration tests (P4.3).*
