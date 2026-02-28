# BATON: Session Tracker for Claude Code

**Project:** Cross-Session Issue & Discussion Tracker
**Baton Created:** 2026-02-28
**Session:** 002 — Solution Scaffold
**Participants:** Dennis, Claude

---

## Project Purpose

Two objectives:

1. **Personal productivity.** A REST API + Blazor Server UI that lets Claude Code persist and retrieve issues, decisions, and discussion threads across sessions. Claude reads open issues at session start and logs new ones during work.

2. **Architecture director demo.** A professional-looking tool that shows Claude Code participating in a real, traceable development workflow — not just chat.

---

## What Was Done This Session

Starting from the BATON produced in Session 001 (design and decisions only), this session produced a working, buildable .NET 10 solution. The database schema is migrated, the API is wired up, and Swagger UI is available for testing. The app has not been run yet — first run will create and seed the SQLite database automatically.

---

## Solution Structure

```
c:\Users\denmi\source\repos\issue-tracking-with-ai\
├── IssueTracker.slnx                  ← .NET 10 solution file (.slnx, not .sln)
├── CLAUDE.md                          ← Instructions for Claude Code (see issue below)
├── docs\
│   ├── BATON-Session-001-Issue-Tracker.md
│   └── BATON-Session-002-Scaffold.md  ← this file
└── IssueTracker.Web\                  ← The single Blazor Server project (API + UI)
```

---

## Project: IssueTracker.Web

Single ASP.NET Core / Blazor Server project targeting `.NET 10`. When running, it serves:
- **REST API** at `/api/*` — consumed by Claude Code via `curl.exe`
- **Blazor Server UI** at `/` — MudBlazor-based, for Dennis to view and manage issues
- **Swagger UI** at `/swagger` — for manual API testing in the browser

### Key Packages (IssueTracker.Web.csproj)

| Package | Version | Purpose |
|---------|---------|---------|
| MudBlazor | 9.0.0 | Professional Blazor component library |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.3 | SQLite provider (default, zero-infrastructure) |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.3 | SQL Server provider (for demo/team use) |
| Microsoft.EntityFrameworkCore.Design | 10.0.3 | Enables `dotnet ef` CLI commands |
| Swashbuckle.AspNetCore | 10.1.4 | Swagger/OpenAPI spec + Swagger UI |

---

## File-by-File Reference

### Configuration

| File | Purpose |
|------|---------|
| `appsettings.json` | `"DatabaseProvider": "SQLite"` switches provider. Connection strings for both SQLite (`Data Source=issuetracker.db`) and SQL Server are defined here. |
| `appsettings.Development.json` | Scaffold default — no custom content yet. |
| `Properties/launchSettings.json` | HTTP: `http://localhost:5124` / HTTPS: `https://localhost:7237`. **⚠ See Known Issues.** |

### Program.cs

Registers all services and middleware in order:

1. `AddRazorComponents().AddInteractiveServerComponents()` — Blazor Server
2. `AddControllers()` — enables `[ApiController]` routing
3. `AddMudServices()` — MudBlazor JS interop and services
4. `AddScoped<PostService>()` — the status-logic service
5. `AddEndpointsApiExplorer()` + `AddSwaggerGen()` — OpenAPI/Swagger
6. `AddDbContext<AppDbContext>()` — reads `DatabaseProvider` config, registers SQLite or SQL Server

Startup sequence (after `builder.Build()`):
1. `DatabaseSeeder.SeedAsync(app)` — runs `MigrateAsync()`, seeds Actors and default Project
2. `UseSwagger()` + `UseSwaggerUI()` at `/swagger`
3. `UseAntiforgery()`, `MapStaticAssets()`, `MapControllers()`, `MapRazorComponents<App>()`

### Data Layer

| File | Purpose |
|------|---------|
| `Data/AppDbContext.cs` | EF Core DbContext. DbSets: `Projects`, `Sessions`, `Actors`, `Posts`. `OnModelCreating` configures three `DeleteBehavior.Restrict` relationships on `Post` to avoid multiple cascade paths: `FromActor`, `ToActor`, and `Parent` (self-referencing via `ActionForId`). |
| `Data/DatabaseSeeder.cs` | Idempotent startup seeder. Calls `db.Database.MigrateAsync()` first (creates the SQLite file and applies migrations), then seeds `Actors` (Claude, Dennis, System) and the default `Project` ("Issue Tracker") only if the tables are empty. |
| `Migrations/20260228151227_InitialCreate.cs` | The sole migration. Creates Actors, Projects, Sessions, Posts tables with all FK constraints. Actors and Projects use `ON DELETE CASCADE` from Sessions; Posts use `RESTRICT` on actor FKs and the self-reference. |

### Models

All in `Models/`, all in namespace `IssueTracker.Web.Models`:

| File | Key fields |
|------|-----------|
| `Project.cs` | `Id`, `Name` |
| `Actor.cs` | `Id`, `Name` |
| `Session.cs` | `Id`, `ProjectId`, `Name`, `StartDate`, `CreatedOn`. Nav: `Project`. |
| `Post.cs` | `Id`, `ProjectId` (derived from Session), `SessionId`, `Title?`, `DateTime`, `FromActorId`, `ToActorId?`, `ActionType`, `ActionForId?` (self-ref parent), `Status?` (root only), `Tags?` (comma-delimited), `Text`. Navs: `Project`, `Session`, `FromActor`, `ToActor?`, `Parent?`, `Replies`. |

**ActionType values:** `New` | `Discuss` | `Proceed As Is` | `Proceed With Mods` | `Check` | `Hold` | `Archive`

**Status values (root posts only):** `Open` | `Closed` | `Deferred`

### Services

| File | Purpose |
|------|---------|
| `Services/PostService.cs` | All business logic for posts. `CreatePostAsync` derives `ProjectId` from the session, sets `Status="Open"` on new root posts, and updates the root post's `Status` when a child `Hold` or `Archive` action is posted. `GetPostsAsync` supports filtering by `sessionId`, `projectId`, `status`, and `tags`. `GetThreadAsync` recursively collects a root post and all descendants. |

### Controllers

All in `Controllers/`, all `[ApiController]`, `[Route("api/[controller]")]`:

| File | Endpoints |
|------|----------|
| `ProjectsController.cs` | `GET /api/projects` — list all. `POST /api/projects` — create. |
| `ActorsController.cs` | `GET /api/actors` — list all (ordered by Id). `POST /api/actors` — create. |
| `SessionsController.cs` | `GET /api/sessions?projectId=N` — filtered list. `GET /api/sessions/{id}` — single. `POST /api/sessions` — create (sets `StartDate`/`CreatedOn` server-side). |
| `PostsController.cs` | `GET /api/posts?sessionId=N&projectId=N&status=S&tags=T` — filtered list. `GET /api/posts/{id}` — single. `GET /api/posts/{id}/thread` — full recursive thread. `POST /api/posts` — create (delegates to `PostService`). All GET responses use an anonymous projection (no navigation-property cycles). |

### UI Components

| File | Purpose |
|------|---------|
| `Components/App.razor` | Root HTML shell. Loads MudBlazor CSS (from `_content/MudBlazor/`), Google Fonts (Roboto), MudBlazor JS, and Blazor JS. Hosts `<MudThemeProvider>`, `<MudDialogProvider>`, `<MudSnackbarProvider>`. |
| `Components/_Imports.razor` | Global Razor `@using` statements, including `MudBlazor` and `IssueTracker.Web.Models`. |
| `Components/Routes.razor` | Scaffold default — maps routes to components. |
| `Components/Layout/MainLayout.razor` | Full MudBlazor shell: `MudAppBar` with hamburger toggle, `MudDrawer` (open by default), `MudMainContent` with `MudContainer MaxWidth=ExtraExtraLarge`. |
| `Components/Layout/NavMenu.razor` | MudBlazor `MudNavMenu` with links to Home (`/`), Issues (`/issues`), Sessions (`/sessions`). **⚠ The Issues and Sessions pages do not exist yet.** |
| `Components/Layout/ReconnectModal.razor` | Scaffold default — Blazor Server reconnect UI. |
| `Components/Pages/Home.razor` | MudBlazor placeholder showing API endpoint reference table. Not yet a functional data grid. |
| `Components/Pages/Counter.razor` | Scaffold template — not yet removed. |
| `Components/Pages/Weather.razor` | Scaffold template — not yet removed. |
| `Components/Pages/Error.razor` | Scaffold default error page. |
| `Components/Pages/NotFound.razor` | Scaffold default 404 page. |

---

## Known Issues / Things to Fix Next Session

### 1. ⚠ CLAUDE.md has wrong port

`CLAUDE.md` says `http://localhost:5000`. The actual port from `launchSettings.json` is `http://localhost:5124` (HTTP) or `https://localhost:7237` (HTTPS). Every `curl.exe` example in CLAUDE.md needs the port corrected to `5124`.

### 2. App has never been run

The SQLite database file (`issuetracker.db`) does not exist yet. It will be created automatically on first `dotnet run` via `DatabaseSeeder.SeedAsync`. Verify that:
- The DB file is created in `IssueTracker.Web\`
- Actors (Claude=1, Dennis=2, System=3) are seeded
- The default Project (Id=1, "Issue Tracker") is seeded
- Swagger UI loads at `http://localhost:5124/swagger`
- A POST to `/api/sessions` and `/api/posts` round-trips correctly

### 3. NavMenu links to non-existent pages

`/issues` and `/sessions` are referenced in `NavMenu.razor` but the corresponding Razor pages have not been created. Clicking them will hit the 404 page.

### 4. Scaffold template pages not cleaned up

`Counter.razor` and `Weather.razor` are still present from the `dotnet new blazor` template. They work but are irrelevant to this project and should be removed.

### 5. MainLayout.razor.css not cleaned up

The scaffold-generated `MainLayout.razor.css` still exists and contains Bootstrap-based layout CSS for `.page`, `.sidebar`, `.top-row` etc. These classes are no longer used (MudBlazor replaces the layout), but the file is harmless unless it causes unexpected styling. Should be cleared or deleted.

### 6. PostService recursive thread fetch is N+1

`GetThreadAsync` / `CollectThreadAsync` fetches one post at a time recursively. Fine for small threads but will be slow for deep discussions. Not a blocker for the initial debug session, but worth noting for later optimization.

---

## Seeded Data (on first run)

| Table   | Id | Value              |
|---------|----|--------------------|
| Actor   | 1  | Claude             |
| Actor   | 2  | Dennis             |
| Actor   | 3  | System             |
| Project | 1  | Issue Tracker      |

---

## How to Start the App

```bash
cd "c:\Users\denmi\source\repos\issue-tracking-with-ai\IssueTracker.Web"
dotnet run
```

Then open:
- `http://localhost:5124` — Blazor UI (MudBlazor)
- `http://localhost:5124/swagger` — Swagger UI for API testing

---

## Quick API Smoke Test (from PowerShell, after correcting port)

```bash
# List actors — should return Claude, Dennis, System
curl.exe -s http://localhost:5124/api/actors

# List projects — should return "Issue Tracker"
curl.exe -s http://localhost:5124/api/projects

# Create a session
curl.exe -s -X POST http://localhost:5124/api/sessions ^
  -H "Content-Type: application/json" ^
  -d "{\"projectId\":1, \"name\":\"Session 003 - Debug\"}"

# Create a root post (use the sessionId returned above, e.g. 1)
curl.exe -s -X POST http://localhost:5124/api/posts ^
  -H "Content-Type: application/json" ^
  -d "{\"sessionId\":1, \"fromActorId\":1, \"actionType\":\"New\", \"title\":\"Smoke test\", \"text\":\"Verifying the API works end to end.\"}"

# Get open posts
curl.exe -s "http://localhost:5124/api/posts?status=Open&projectId=1"
```

---

## Next Steps

1. **Fix CLAUDE.md port** — change `localhost:5000` → `localhost:5124` everywhere.
2. **Run and smoke test** — `dotnet run`, verify DB, Swagger, and all API endpoints.
3. **Remove scaffold cruft** — delete `Counter.razor`, `Weather.razor`, and clear `MainLayout.razor.css`.
4. **Build the Issues page** (`/issues`) — MudDataGrid of root posts with status/tag/session filters and expandable thread view.
5. **Build the Sessions page** (`/sessions`) — list of sessions with ability to create new ones.
6. **End-to-end workflow test** — Claude creates session via API, logs an issue, Dennis views it in the UI.
7. **Seed demo data** — realistic posts to make the UI look credible for the architecture director demo.

---

*Load this BATON at the start of the next session. The app builds clean but has never been run. Start with `dotnet run`, verify the smoke tests above, then address the known issues in order.*
