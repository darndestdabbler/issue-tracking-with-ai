# BATON: Session Tracker for Claude Code

**Project:** Cross-Session Issue & Discussion Tracker
**Baton Created:** 2026-02-28
**Session:** 001 — Initial Planning & Design
**Participants:** Dennis, Claude

---

## Project Objectives

There are two objectives driving this project:

1. **Personal productivity.** Dennis works on a multi-session personal project and needs a way to persist and retrieve issues, discussions, and decisions across Claude Code sessions. Claude Code starts fresh every session — it has no internal session IDs, topic IDs, or memory. This tool gives Claude a way to pick up where it left off, and gives Dennis visibility into what was discussed and decided.

2. **Demo for an architecture director.** Dennis will demo Claude Code to an architecture director who is evaluating whether to adopt it. The tracker demonstrates that Claude Code can participate in a managed development workflow with real traceability — not just chat, but structured issue tracking with a polished UI. The tool needs to look professional, not like a student project.

---

## Architecture — Decisions Made

### Single Blazor Server Project

The API and UI are hosted in **one ASP.NET Core / Blazor Server project**. When you run `dotnet run`, you get both the REST API (for Claude Code) and the MudBlazor UI (for Dennis) in a single process. No separate frontend, no CORS, no multiple things to start.

### Dual Database Support (EF Core)

EF Core with a **switchable provider** — SQLite or SQL Server — controlled by a setting in `appsettings.json`.

- **At home / personal project:** SQLite. Zero infrastructure. The database is a single file in the project folder.
- **For demo / eventual team use:** SQL Server. Flip the connection string, run migrations, done.

The models and DbContext are identical for both. EF Core abstracts the provider.

### Claude Code Integration

Claude Code calls the REST API using `curl.exe` (native on Windows 10+). Alternatively, a small Node.js or PowerShell helper script could wrap the curl calls for convenience.

**Important:** In PowerShell, `curl` is aliased to `Invoke-WebRequest`. Claude must use `curl.exe` (with the .exe) to get real curl.

Workflow instructions for Claude live in the project's `CLAUDE.md` file, which Claude Code reads automatically at session start.

### Technology Stack

| Component        | Choice                          | Rationale                                                    |
|------------------|---------------------------------|--------------------------------------------------------------|
| Database         | SQLite (home) / SQL Server (demo/team) | Switchable via EF Core config                        |
| ORM              | Entity Framework Core           | Handles both providers, migrations, relationships            |
| API + UI Host    | ASP.NET Core Blazor Server      | One project, one process — API controllers + Blazor UI       |
| UI Components    | MudBlazor                       | Professional polish out of the box — grids, filters, forms   |
| Claude Interface | curl.exe via REST API           | Native on Windows, no dependencies                           |

---

## Data Model

### Design Philosophy

Instead of separate tables for issues, discussions, and review items, **everything is a Post**. A post can be a new topic, a discussion reply, a status change, a review request, or an approval. Meaning comes from the `ActionType` and the self-referencing `ActionForId`.

This is simpler, more flexible, and maps naturally to how conversations actually flow between Claude and a human.

### Reference Tables

```sql
-- Project: groups sessions
CREATE TABLE Project (
    Id      INTEGER PRIMARY KEY AUTOINCREMENT,  -- INT IDENTITY for SQL Server
    Name    VARCHAR(255) NOT NULL
);

-- Session: a single working session within a project
CREATE TABLE Session (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    ProjectId   INT NOT NULL,
    Name        VARCHAR(255) NOT NULL,
    StartDate   DATETIME DEFAULT (datetime('now')),   -- GETUTCDATE() for SQL Server
    CreatedOn   DATETIME DEFAULT (datetime('now')),
    CONSTRAINT fkSessionProject FOREIGN KEY (ProjectId) REFERENCES Project(Id)
);

-- Actor: who is participating
CREATE TABLE Actor (
    Id      INTEGER PRIMARY KEY AUTOINCREMENT,
    Name    VARCHAR(50) NOT NULL    -- e.g., "Claude", "Dennis", "System"
);
```

### Transaction Table

```sql
CREATE TABLE Post (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    ProjectId       INT NOT NULL,
    SessionId       INT NOT NULL,
    Title           VARCHAR(255) NULL,          -- Required for New posts; optional for replies
    DateTime        DATETIME DEFAULT (datetime('now')),
    FromActorId     INT NOT NULL,               -- FK → Actor
    ToActorId       INT NULL,                   -- FK → Actor; nullable (not all posts are directed)
    ActionType      VARCHAR(50) NOT NULL,       -- New | Discuss | Proceed As Is |
                                                -- Proceed With Mods | Check | Hold | Archive
    ActionForId     INT NULL,                   -- FK → Post (self-referencing); NULL = root/new topic
    Status          VARCHAR(50) NULL,           -- Open | Closed | Deferred
                                                -- Only meaningful on root posts (ActionForId IS NULL)
    Tags            VARCHAR(500) NULL,          -- Comma-delimited (e.g., "auth,middleware,token")
    Text            TEXT NOT NULL,

    CONSTRAINT fkPostProject    FOREIGN KEY (ProjectId)   REFERENCES Project(Id),
    CONSTRAINT fkPostSession    FOREIGN KEY (SessionId)   REFERENCES Session(Id),
    CONSTRAINT fkPostFromActor  FOREIGN KEY (FromActorId) REFERENCES Actor(Id),
    CONSTRAINT fkPostToActor    FOREIGN KEY (ToActorId)   REFERENCES Actor(Id),
    CONSTRAINT fkPostParent     FOREIGN KEY (ActionForId) REFERENCES Post(Id)
);
```

### ActionType Values and Their Meaning

| ActionType         | Purpose                                      | Status Side Effect              |
|--------------------|----------------------------------------------|---------------------------------|
| **New**            | Creates a new topic/issue                    | Sets root Status to "Open"      |
| **Discuss**        | Adds commentary to an existing post          | No status change                |
| **Proceed As Is**  | Approves something without changes           | No status change (or → Closed)  |
| **Proceed With Mods** | Approves with modifications (details in Text) | No status change             |
| **Check**          | Requests review from the ToActor             | No status change                |
| **Hold**           | Pauses work                                  | Updates root Status → Deferred  |
| **Archive**        | Closes the thread                            | Updates root Status → Closed    |

### Status Tracking Mechanics

- The `Status` field lives on the **root post** (where `ActionForId IS NULL`).
- Status is **never edited directly** on the root post.
- To change status, you create a **new child post** with the appropriate ActionType (Hold, Archive, etc.). The API/service layer then updates the root post's Status behind the scenes.
- This ensures every status change has a full audit trail: who changed it, when, and why (captured in the child post's Text).

**Example chain:**

| Post Id | ActionForId | ActionType | Status on Root | Text                                    |
|---------|-------------|------------|----------------|-----------------------------------------|
| 12      | NULL        | New        | Open           | "Token refresh is broken"               |
| 15      | 12          | Discuss    | (unchanged)    | "Might be in the middleware"             |
| 18      | 12          | Hold       | → Deferred     | "Waiting on auth library update"         |
| 23      | 12          | Archive    | → Closed       | "Fixed in commit abc123"                 |

### Tags

Stored as comma-delimited strings (e.g., `"auth,security,token-handling"`). Searchable via `LIKE '%auth%'` or equivalent. This avoids the overhead of a join table while still being functional for filtering and search in the UI. Can be normalized later if needed.

### Defaults and Ease of Entry

- `DateTime` defaults to UTC now.
- `Status` defaults to "Open" when ActionType is "New" (handled by service layer).
- `ProjectId` auto-populated from Session when creating a Post within a Session (enforced by service layer).
- Only truly required fields on a create call: `SessionId`, `FromActorId`, `ActionType`, `Text`. Everything else has defaults or is optional.

---

## API Design

Hosted as controllers within the Blazor Server project.

### Session Endpoints

| Method | Route                                | Purpose                               |
|--------|--------------------------------------|---------------------------------------|
| POST   | `/api/sessions`                      | Create a new session, return Id       |
| GET    | `/api/sessions?projectId=N`          | List sessions for a project           |
| GET    | `/api/sessions/{id}`                 | Get session by Id                     |

### Post Endpoints

| Method | Route                                | Purpose                                        |
|--------|--------------------------------------|-------------------------------------------------|
| POST   | `/api/posts`                         | Create a post (new topic or reply)              |
| GET    | `/api/posts?sessionId=N`             | All posts for a session                         |
| GET    | `/api/posts?status=Open&projectId=N` | Open root posts across sessions                 |
| GET    | `/api/posts/{id}`                    | Single post                                     |
| GET    | `/api/posts/{id}/thread`             | Full thread for a root post (all descendants)   |
| GET    | `/api/posts?tags=auth&status=Open`   | Filter by tags and/or status                    |

### Reference Data Endpoints

| Method | Route                                | Purpose                               |
|--------|--------------------------------------|---------------------------------------|
| GET    | `/api/projects`                      | List projects                         |
| POST   | `/api/projects`                      | Create project                        |
| GET    | `/api/actors`                        | List actors                           |
| POST   | `/api/actors`                        | Create actor                          |

### API Response Design

Responses should be **compact** — Claude reads terminal output, so minimal JSON. A POST should return the created entity with its Id. No deep nesting unless explicitly requested (e.g., `/thread`).

---

## Blazor UI Design (MudBlazor)

### Layout

- **Top bar:** Project selector + Session selector (searchable autocomplete). Option to create new sessions.
- **Main area:** MudDataGrid of root posts (where ActionForId IS NULL) for the selected session or across sessions.
- **Columns:** Id, Title, Status, ActionType, From, Tags, DateTime.
- **Filters:** Status, Tags, ActionType, date range, cross-session toggle.
- **Expandable rows:** Clicking/expanding a root post reveals the full thread (child posts) in a timeline or chat-style layout.
- **Inline actions:** Quick status changes via ActionType buttons (Hold, Archive, etc.) — each creates a new child post behind the scenes.
- **New post form:** For creating new topics or adding to a thread.

### Demo-Worthy Features

- Professional MudBlazor styling — no custom CSS needed for polish.
- Cross-session search and filtering.
- Expandable master/detail view showing the full conversation thread per issue.
- Real-time data from a real API (not static files).
- Clean data grid with sorting, paging, and filtering.

---

## Claude Code Workflow

### Prerequisites

The session tracker API must be running (`dotnet run`) before starting Claude Code.

### CLAUDE.md Instructions (to be placed in project root)

The `CLAUDE.md` file tells Claude Code about the API and when to use it. Key sections:

1. **At session start:** Create a session via `POST /api/sessions`. Store the returned SessionId. Retrieve open issues via `GET /api/posts?status=Open&projectId=N`.
2. **During work:** Log posts for things that need to survive beyond this session — decisions, issues, questions for the human, deferred items. Don't log routine work steps.
3. **At session end:** Review posts created this session. Generate a lighter BATON (narrative context only — detailed issue tracking lives in the API).

### How Claude Makes API Calls

Using `curl.exe` from the terminal:

```bash
# Create a session
curl.exe -s -X POST http://localhost:5000/api/sessions ^
  -H "Content-Type: application/json" ^
  -d "{\"projectId\":1, \"name\":\"Auth Refactor\"}"

# Create a new issue
curl.exe -s -X POST http://localhost:5000/api/posts ^
  -H "Content-Type: application/json" ^
  -d "{\"sessionId\":1, \"fromActorId\":1, \"actionType\":\"New\", \"title\":\"Token refresh broken\", \"tags\":\"auth,token\", \"text\":\"Null reference when token is expired...\"}"

# Get open issues
curl.exe -s http://localhost:5000/api/posts?status=Open^&projectId=1

# Add discussion to an existing post
curl.exe -s -X POST http://localhost:5000/api/posts ^
  -H "Content-Type: application/json" ^
  -d "{\"sessionId\":1, \"fromActorId\":1, \"actionType\":\"Discuss\", \"actionForId\":12, \"text\":\"Found the root cause in middleware...\"}"
```

### Role of the BATON Going Forward

The BATON doesn't go away, but its role changes:

- **BATON carries:** Session narrative, approach taken, high-level context, links to the tracker.
- **Tracker carries:** Specific issues, discussions, status changes, action items — with full audit trail.

These complement each other: the BATON is the "story," the tracker is the "ledger."

---

## Prior Work / Context

Dennis had been working with Gemini on an HTML/JS "Checklist Editor" that persists issues and review items to JSON files. Key aspects:

- **Functional:** Opens/saves JSON files via File System Access API. Two tabs: Review (checklist items with pass/fail and feedback) and Other Issues (CRUD with discussion log modal).
- **Data model:** Per-session JSON files with Review array and OtherIssues array. Each issue has a Log (array of {Date, User, Entry}).
- **Limitation:** No cross-session aggregation — this is the gap the new tracker fills.
- **Status:** One session completed (produced an Excel checklist, not yet in the JSON format). The HTML editor exists but the workflow isn't fully established. Now is the right time for a breaking change.
- **Code:** The HTML source for the Checklist Editor has been shared and is available for reference. Concepts like the issue log modal and review checklist may inform UI design.

The new tracker replaces this approach. The "Review" checklist functionality could be incorporated later as a type of Post (ActionType: Check) if needed.

---

## Open Questions

1. **MudBlazor version:** Target latest stable (v7+)?
2. **EF Core approach:** Code-first with migrations (recommended for greenfield)?
3. **Helper script:** Should we provide a Node.js or PowerShell wrapper around curl for Claude's API calls, or is raw curl sufficient?
4. **Review/Checklist integration:** Incorporate the checklist concept (from the Gemini editor) into the Post model now, or defer?
5. **Seed data:** Pre-populate Actor table with Claude, Dennis, System? Pre-populate a default Project?
6. **Blazor interactive render mode:** Use Interactive Server throughout, or selectively per component?

---

## Next Steps

1. **Finalize open questions** above.
2. **Scaffold the .NET solution** — single Blazor Server project with API controllers.
3. **Create EF Core models and DbContext** with switchable SQLite/SQL Server provider.
4. **Run initial migration** and create the database (SQLite first for speed).
5. **Build API endpoints** — Sessions, Posts, reference data.
6. **Build the Blazor UI** — Session selector, post grid with expandable threads, filters.
7. **Write the CLAUDE.md** with API instructions for Claude Code.
8. **Test the full workflow** — Claude creates a session, logs issues, Dennis reviews in UI.
9. **Prepare the demo** — seed realistic data, rehearse the narrative for the architecture director.

---

*This BATON should be loaded at the start of the next session. Claude should read it to understand the project context, all decisions made, and where to pick up.*
