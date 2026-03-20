# Issue Tracker for AI Coding Sessions

A cross-session issue tracker that gives AI coding assistants structured memory — and gives you visibility into what they decided.

---

## Table of Contents

- [The Problem](#the-problem)
- [How It Works](#how-it-works)
- [Session Workflow](#session-workflow)
- [How Claude Code Uses the API](#how-claude-code-uses-the-api)
- [How You Use the UI](#how-you-use-the-ui)
- [Using the Tracker with Another Project](#using-the-tracker-with-another-project)
- [Exporting a Project to SQLite](#exporting-a-project-to-sqlite)
- [Prerequisites and Setup](#prerequisites-and-setup)
- [Running Tests](#running-tests)
- [Project Structure](#project-structure)
- [Data Model](#data-model)
- [API Reference](#api-reference)
- [Tech Stack](#tech-stack)

---

## The Problem

AI coding assistants start every session with amnesia. They have no memory of what was decided yesterday, what was deferred last week, or which issues are still open. Every session begins from scratch.

**BATONs** help. A BATON is a handoff document written at the end of each session and read at the start of the next. It carries the narrative — what happened, what was tried, what to focus on next. BATONs give the next session a running start instead of a cold start.

But BATONs are linear. Each one captures a single session's story. Twelve sessions in, when you need to know *"what issues are still open across all sessions?"* or *"when did we decide to defer that refactor?"*, you would have to read through every BATON to piece it together. That doesn't scale.

This project fills the gap. It's a structured, queryable issue tracker that runs alongside your project. Claude Code logs decisions, blockers, and resolutions via REST API calls during each session. You review, filter, and manage them through a web UI at any time. The tracker accumulates context across sessions so that neither you nor your AI assistant has to hold it all in memory.

**BATONs are stored as tracker posts.** Each BATON is a post tagged `baton`. When a new BATON is created, the system automatically archives all previous BATONs for that project. The latest BATON is always retrievable via a dedicated API endpoint.

Together, BATONs and the tracker form a complete handoff system:

> *The BATON carries the story. The tracker carries the ledger.*

---

## How It Works

The Issue Tracker is a single .NET application that serves both a **REST API** and a **Blazor Server web UI** on the same port.

```
Claude Code (curl) ──> REST API ──> SQL Server DB <── Blazor UI (browser)
```

- **Claude Code** interacts via `curl` calls to the REST API, guided by instructions in your project's `CLAUDE.md` file.
- **You** interact via the web UI at `http://localhost:5124` — filtering issues, viewing discussion threads, managing sessions and projects.
- **Swagger** is available at `/swagger` for interactive API exploration.

The data model is intentionally simple: everything is a **Post**. An issue is a root post. Replies, status changes, and review requests are child posts linked to their parent. Status flows automatically — close a reply with `Archive` and the root issue moves to `Closed`.

### Key capabilities

- **Multi-project support** — one tracker serves multiple projects, each with its own sessions and issues
- **BATON auto-archive** — creating a new BATON automatically closes older BATONs for the same project
- **Markdown rendering** — issue text supports full markdown with configurable truncation and a "view full" modal
- **Pending Review status** — an IV&V workflow where `Resolve` marks work complete and only the owner or delegate can `Archive`
- **Issue ownership enforcement** — only the issue creator, assigned delegate, or an Admin can archive an issue
- **SQLite export** — archive a project's issues as a portable SQLite file for git storage
- **Dual database provider** — runs on SQL Server (primary) or SQLite, switchable via configuration

---

## Session Workflow

Each coding session follows this workflow:

### 1. Read context

Retrieve the latest BATON and review open issues:

```bash
curl.exe -s "http://localhost:5124/api/posts/latest-baton?projectId=1"
curl.exe -s "http://localhost:5124/api/posts?status=Open&projectId=1"
```

### 2. Plan the session

Identify what to tackle based on open issues and the BATON narrative. Create a session in the tracker:

```bash
curl.exe -s -X POST http://localhost:5124/api/sessions ^
  -H "Content-Type: application/json" ^
  -d "{\"ProjectId\":1, \"Name\":\"Session 013 — README\"}"
```

### 3. Execute

Write code, run tests, iterate. The tracker is not for logging every implementation step — only things with cross-session significance.

### 4. Update the tracker

Log events that need to survive beyond this session:

| What happened | ActionType |
|---|---|
| New issue or decision | `New` |
| Investigation notes or commentary | `Discuss` |
| Work deferred for later | `Hold` |
| Issue resolved | `Archive` |
| Work complete, awaiting review | `Resolve` |
| Need human review | `Check` |
| Reopening a closed/deferred item | `Reopen` |

**Bundle related items** — create a single issue with a checklist rather than many small issues.

### 5. Write the BATON

Create a BATON post via the API with the tag `baton`. The system automatically archives previous BATONs for the project. The BATON should summarize:
- What was accomplished
- Key decisions and their rationale
- What the next session should focus on
- Open issue numbers for reference

### 6. Commit to git

Preserve code changes and any configuration updates.

---

## How Claude Code Uses the API

Claude Code reads `CLAUDE.md` from your project root at the start of every session. This project provides modular instruction files in `docs/claude-instructions/` that any project's `CLAUDE.md` can reference:

| File | Purpose |
|---|---|
| `issue-tracking.md` | Issue tracking workflow, first-session project registration, full API reference |
| `baton.md` | BATON workflow — read via API at session start, write via POST at session end |
| `git.md` | Git workflow — initialization, .gitignore management, commit and push behavior |
| `permission-management.md` | Auto-add permission rules to `settings.local.json` on denial |
| `TEMPLATE-CLAUDE.md` | Starter `CLAUDE.md` to copy into any project |

Each project copies these files into its own `docs/claude-instructions/` folder and renames `TEMPLATE-CLAUDE.md` to `CLAUDE.md` in the project root. See [Using the Tracker with Another Project](#using-the-tracker-with-another-project) for setup details.

> **Windows note:** Use `curl.exe`, not `curl`. In PowerShell, `curl` is an alias for `Invoke-WebRequest`.

### Seeded actor IDs

| Actor | ID | Role | Purpose |
|---|---|---|---|
| Claude | 1 | AI | AI assistant (fromActorId when Claude logs posts) |
| Human | 2 | Admin | You (fromActorId when you log posts, toActorId for review requests) |
| System | 3 | System | Automated processes (BATON auto-archive) |
| Gemini | 4 | AI | Alternative AI assistant |

### Example: Create a new issue

```bash
curl.exe -s -X POST http://localhost:5124/api/posts ^
  -H "Content-Type: application/json" ^
  -d "{\"sessionId\":5, \"fromActorId\":1, \"actionType\":\"New\", \"title\":\"Token refresh broken\", \"tags\":\"auth,token\", \"text\":\"Null ref when token is expired at middleware layer.\"}"
```

### Example: Add discussion to an existing issue

```bash
curl.exe -s -X POST http://localhost:5124/api/posts ^
  -H "Content-Type: application/json" ^
  -d "{\"sessionId\":5, \"fromActorId\":1, \"actionType\":\"Discuss\", \"actionForId\":12, \"text\":\"Root cause found in TokenRefreshMiddleware line 47.\"}"
```

### Example: Close an issue

```bash
curl.exe -s -X POST http://localhost:5124/api/posts ^
  -H "Content-Type: application/json" ^
  -d "{\"sessionId\":5, \"fromActorId\":1, \"actionType\":\"Archive\", \"actionForId\":12, \"text\":\"Fixed in commit abc123. Token refresh now handles null expiry.\"}"
```

### ActionType reference

| ActionType | Use when... | Status effect |
|---|---|---|
| New | Opening a new issue or topic | &rarr; Open |
| Discuss | Adding commentary or investigation notes | (no change) |
| Proceed As Is | Approving without modification | (no change) |
| Proceed With Mods | Approving with changes (describe in text) | (no change) |
| Check | Requesting human review (set toActorId: 2) | (no change) |
| Hold | Pausing or deferring work | &rarr; Deferred |
| Resolve | Marking work complete, awaiting review | &rarr; Pending Review |
| Archive | Closing or resolving an issue | &rarr; Closed |
| Reopen | Reopening a closed or deferred issue | &rarr; Open |

---

## How You Use the UI

Open `http://localhost:5124` in your browser while the app is running.

### Issues page (`/issues`)

The main workspace for viewing and managing issues.

- **Filter bar** — Filter by project, session, status (Open / Deferred / Closed / Pending Review), and tags
- **DataGrid** — Server-side paging (10, 25, or 50 per page) and sortable columns
- **Quick-action buttons** — Hold (defer), Archive (close), or Reopen an issue with one click
- **Expandable threads** — Click any issue to see its full discussion timeline with color-coded action types
- **Create Issue** — Opens a dialog to log a new issue with title, description, tags, and action type
- **Reply** — Add discussion, decisions, or status changes to an existing issue
- **Edit** — Modify the title, tags, or description of a root issue
- **Markdown rendering** — Post text renders as markdown with configurable truncation and a "View full" modal for long content

### Projects and Sessions page (`/sessions`)

Manage the organizational structure.

- **Projects pane** — Create and rename projects; navigate to a project's issues
- **Sessions pane** — Create, rename, and archive sessions; view post counts per session
- **Archive toggle** — Show or hide archived sessions; restore archived sessions if needed

### Swagger (`/swagger`)

Interactive API documentation generated from the controller XML doc comments. Use it to explore endpoints, see request/response shapes, and try out calls directly from the browser.

---

## Using the Tracker with Another Project

The Issue Tracker supports multiple projects. You can set up any project to use the tracker in a few steps.

### Recommended setup

Open the Issue Tracker in a **separate VS Code window** from your main project. The tracker only needs to be running — you don't need to actively develop in it. Just `dotnet run` and leave it.

**Tip:** Install the [Peacock](https://marketplace.visualstudio.com/items?itemName=johnpapa.vscode-peacock) extension by John Papa to color-code your VS Code windows. For example, blue for your main project and green for the Issue Tracker. This makes it easy to tell them apart at a glance when switching between windows.

### Connecting your other project

1. Copy the `docs/claude-instructions/` folder from this repo into your other project's `docs/` folder.
2. Rename `docs/claude-instructions/TEMPLATE-CLAUDE.md` to `CLAUDE.md` and place it in your project root.
3. Fill in the constants:
   - `{{PROJECT_NAME}}` — your project's name (defaults to folder name)
   - `{{ISSUE_TRACKER_API_URL}}` — the tracker API URL (default: `http://localhost:5124/api`)
4. Leave `{{PROJECT_ID}}` as the placeholder. On the first session, Claude will detect it's unset, prompt you to register the project, and update the constant automatically.

Claude Code will then read the referenced instruction files at session start and interact with the tracker — creating sessions, logging issues, and writing BATONs.

---

## Exporting a Project to SQLite

When a project reaches end of life, you can export its issues as a portable SQLite file to archive alongside the project in git.

```bash
curl.exe -o project-archive.sqlite http://localhost:5124/api/projects/4/export/sqlite
```

The exported file contains:
- The project record
- All sessions for the project
- All posts (issues, replies, BATONs, status changes)
- All actors referenced by those posts

The file is self-contained — open it with any SQLite client (DB Browser for SQLite, DataGrip, `sqlite3` CLI) to browse the archived issues. The central database is not affected; deletion from the central DB is a separate decision.

This endpoint is also available via Swagger at `/swagger`.

---

## Prerequisites and Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)
- **SQL Server** on localhost (default instance, Windows Authentication) — or SQLite via config

### Getting started

```bash
git clone <repo-url>
cd issue-tracking-with-ai
dotnet run --project IssueTracker.Web
```

The app will be available at `http://localhost:5124`.

### What happens on first run

- EF Core migrations are applied automatically
- Seed data is inserted: four actors (Claude, Human, System, Gemini) and two sample projects
- Demo sessions and issues are created showing the workflow in action

### Database configuration

The database provider is controlled by `DatabaseProvider` in `IssueTracker.Web/appsettings.json`:

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=IssueTracker;Trusted_Connection=True;TrustServerCertificate=True",
    "SQLite": "Data Source=issuetracker.db"
  }
}
```

Set `"DatabaseProvider": "SQLite"` to use SQLite instead of SQL Server.

### Per-project SQLite database

To use a separate SQLite database per project, set the `ISSUETRACKER_DB_PATH` environment variable:

```bash
ISSUETRACKER_DB_PATH=/path/to/project/issues.db dotnet run --project IssueTracker.Web
```

The database file will be created automatically on first run, with tables migrated and seed data inserted.

### Database reset (SQLite only)

Delete the database file and restart to re-seed from scratch:

```bash
rm IssueTracker.Web/issuetracker.db
dotnet run --project IssueTracker.Web
```

---

## Running Tests

```bash
dotnet test
```

- 56 xUnit integration tests using `WebApplicationFactory` with in-memory SQLite
- The app does **not** need to be running — the test factory spins up its own host
- Tests cover: posts (filtering, paging, sorting, status transitions, threading, edit), sessions (CRUD, archive/restore), projects (CRUD, SQLite export), actors, and markdown rendering

---

## Project Structure

```
issue-tracking-with-ai/
  IssueTracker.slnx                         # .NET 10 solution file
  CLAUDE.md                                  # Project-specific Claude Code instructions
  README.md                                  # This file
  docs/
    claude-instructions/
      issue-tracking.md                      # Reusable issue tracking instructions
      baton.md                               # Reusable BATON workflow instructions
      git.md                                 # Reusable git workflow instructions
      permission-management.md               # Permission auto-add instructions
      TEMPLATE-CLAUDE.md                     # Starter CLAUDE.md for new projects
  IssueTracker.Web/
    Program.cs                               # Startup, DI, middleware
    appsettings.json                         # Database provider, connection strings
    Models/
      Post.cs                                # Issues, replies, status changes
      Session.cs                             # Work sessions within a project
      Project.cs                             # Top-level project container
      Actor.cs                               # Participants (Claude, Human, System, Gemini)
    Data/
      AppDbContext.cs                         # EF Core DbContext
      DatabaseSeeder.cs                      # Auto-migration and seed data
    Services/
      PostService.cs                         # Query, create, and status logic
      MarkdownService.cs                     # Markdig pipeline for rendering
      ProjectExportService.cs                # SQLite export for project archival
    Controllers/
      PostsController.cs                     # /api/posts endpoints
      SessionsController.cs                  # /api/sessions endpoints
      ProjectsController.cs                  # /api/projects endpoints + SQLite export
      ActorsController.cs                    # /api/actors endpoints
    Components/
      Pages/
        Issues.razor                         # Issue management DataGrid
        Sessions.razor                       # Project and session management
        Home.razor                           # Landing page
      CreateIssueDialog.razor                # New issue form
      ReplyDialog.razor                      # Reply and action form
      EditIssueDialog.razor                  # Edit root post form
      ProjectDialog.razor                    # Create/edit project form
      MarkdownRenderer.razor                 # Markdown-to-HTML component
      MarkdownViewerDialog.razor             # Full-content markdown modal
      SharedDtos.cs                          # Shared DTOs for Blazor components
  IssueTracker.Tests/
    IssueTrackerFactory.cs                   # WebApplicationFactory with in-memory SQLite
    PostsTests.cs                            # Post endpoint tests
    SessionsTests.cs                         # Session endpoint tests
    ProjectsTests.cs                         # Project endpoint tests
    ProjectExportTests.cs                    # SQLite export tests
    ActorsTests.cs                           # Actor endpoint tests
    MarkdownServiceTests.cs                  # Markdown rendering tests
  scripts/
    import-batons.sh                         # One-shot historical BATON import script
  Migrations.SQLite.backup/                  # Archived SQLite migrations (reference only)
```

---

## Data Model

Everything is a **Post**. Instead of separate tables for issues, comments, and status changes, a single entity handles all of them. The `ActionType` field determines what kind of post it is, and `ActionForId` links child posts to their parent. This gives a complete audit trail — every status change records who did it, when, and why.

```
Project  1──*  Session  1──*  Post
                              Post  *──1  Post  (self-referencing via ActionForId)
Actor    1──*  Post  (as FromActor)
Actor    1──*  Post  (as ToActor, optional)
```

### Status lifecycle

```
         ┌──────── Reopen ─────────┐
         │                         │
         v                         │
       Open ──── Hold ───> Deferred
         │                    │
         │                    │
         ├─── Resolve ──> Pending Review
         │                    │
         └──── Archive ──>  Closed
                              ^
                              │
        Deferred ── Archive ──┘
   Pending Review ─ Archive ──┘
```

A root post starts as **Open** when created with `ActionType: New`. Child posts drive status changes:
- `Hold` moves it to **Deferred**
- `Resolve` moves it to **Pending Review** (signals work complete, awaiting approval)
- `Archive` moves it to **Closed** (requires ownership: creator, delegate, or Admin)
- `Reopen` moves it back to **Open** from any non-Open status

### Issue ownership

Archive actions are restricted to the issue creator (`FromActorId`), the assigned delegate (`ToActorId`), or actors with the Admin role. This supports an IV&V workflow where the person who did the work cannot unilaterally close the review.

---

## API Reference

| Method | Route | Description |
|---|---|---|
| GET | `/api/posts` | List and filter root posts (supports paging) |
| GET | `/api/posts/{id}` | Single post by ID |
| GET | `/api/posts/{id}/thread` | Full thread (root + all replies via recursive CTE) |
| GET | `/api/posts/latest-baton?projectId=N` | Most recent BATON post for a project |
| POST | `/api/posts` | Create a post (issue or reply) |
| PUT | `/api/posts/{id}` | Edit a root post (title, tags, text) |
| GET | `/api/sessions` | List sessions (filterable, excludes archived by default) |
| GET | `/api/sessions/{id}` | Single session by ID |
| POST | `/api/sessions` | Create a session |
| PUT | `/api/sessions/{id}` | Rename a session |
| PUT | `/api/sessions/{id}/archive` | Archive a session |
| PUT | `/api/sessions/{id}/restore` | Restore an archived session |
| GET | `/api/projects` | List projects |
| GET | `/api/projects/{id}` | Single project by ID |
| POST | `/api/projects` | Create a project |
| PUT | `/api/projects/{id}` | Update a project |
| GET | `/api/projects/{id}/export/sqlite` | Export project data as SQLite file |
| GET | `/api/actors` | List actors |

### Query parameters for `GET /api/posts`

| Parameter | Description |
|---|---|
| `projectId` | Filter by project |
| `sessionId` | Filter by session |
| `status` | Filter by status: `Open`, `Closed`, `Deferred`, `Pending Review` |
| `tags` | Comma-delimited tags (AND logic — all must match) |
| `page` | Page index (enables paged response with `totalCount`) |
| `pageSize` | Items per page (default: 10) |
| `sortBy` | Sort column: `Title`, `Status`, `ActionType`, `FromActor`, `DateTime` |
| `sortDesc` | `true` for descending sort |

Interactive API docs available at `/swagger` when the app is running.

---

## Tech Stack

| Component | Technology | Version |
|---|---|---|
| Runtime | .NET | 10 |
| Web framework | ASP.NET Core Blazor Server | 10 |
| UI components | MudBlazor | 9 |
| ORM | Entity Framework Core | 10 |
| Database | SQL Server (primary) / SQLite | — |
| Markdown | Markdig | 1.1.1 |
| API docs | Swashbuckle (Swagger/OpenAPI) | 10.1.4 |
| Tests | xUnit + WebApplicationFactory | 2.9.3 |
| Solution format | .slnx (.NET 10) | — |
