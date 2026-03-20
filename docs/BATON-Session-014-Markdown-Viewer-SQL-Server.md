# BATON: Session 014 — Markdown Viewer Infrastructure + SQL Server Migration

**Project:** Issue Tracker
**Baton Created:** 2026-03-20
**Session:** 014 — Markdown viewer, configurable truncation, SQL Server migration
**Participants:** Human, Claude (Opus 4.6)

---

## Summary of Work Completed

Two major areas of work this session: markdown rendering infrastructure for long-form content (BATONs-as-issues preparation), and migrating the database from SQLite to SQL Server.

### Part 1: Markdown Viewer Infrastructure

| Area | File | Change |
|------|------|--------|
| Package | `IssueTracker.Web.csproj` | Added Markdig 1.1.1, suppressed CS8669 warnings |
| Service | `Services/MarkdownService.cs` | Markdig pipeline: DisableHtml (XSS), PipeTables, AutoLinks, TaskLists, EmphasisExtras |
| Component | `Components/MarkdownRenderer.razor` | Reusable markdown-to-HTML renderer |
| Component | `Components/MarkdownViewerDialog.razor` | Scrollable modal viewer (70vh max-height) |
| CSS | `wwwroot/app.css` | `.markdown-body` styles (tables, code, blockquotes, headings); font-size: 1rem |
| Page | `Components/Pages/Issues.razor` | Thread replies: truncation at configurable length + "View full" link opening modal; inline markdown for short text |
| Config | `appsettings.json` | `MarkdownTruncateLength: 500` (configurable) |
| Seeder | `Data/DatabaseSeeder.cs` | Added `FixActorRolesAsync` — auto-corrects actors stuck with default "User" role |
| Tests | `Tests/MarkdownServiceTests.cs` | 9 tests: headings, tables, code, XSS stripping, autolinks, task lists, large content |

### Part 2: SQL Server Migration

| Area | File | Change |
|------|------|--------|
| Config | `appsettings.json` | DatabaseProvider: "SqlServer", connection: `Server=localhost;Trusted_Connection=True;TrustServerCertificate=True` |
| Model | `Data/AppDbContext.cs` | All FKs set to `DeleteBehavior.Restrict` (SQL Server doesn't allow multiple cascade paths) |
| Migration | `Migrations/InitialCreate` | Fresh SQL Server migration (`int`, `nvarchar(max)`, `datetime2`, `IDENTITY`) |
| Tests | `Tests/IssueTrackerFactory.cs` | Removes SQL Server provider services before registering in-memory SQLite |
| Backup | `Migrations.SQLite.backup/` | Old SQLite migrations preserved at solution root |
| Backup | `issuetracker-backup.db` | SQLite database backup |

**Data migrated:** 4 actors, 7 projects, 139 sessions, 397 posts — all IDs preserved via `SET IDENTITY_INSERT ON`.

### Key Decisions

- **All FKs use Restrict delete** — SQL Server rejects multiple cascade paths (Project→Session→Post AND Project→Post). Changed all relationships to Restrict for consistency across providers.
- **Truncation length is configurable** — `MarkdownTruncateLength` in appsettings.json (default 500). User requested this over hardcoded value.
- **CS8669 warnings suppressed** — Razor source generator emits nullable annotations without `#nullable` directive; this is a known .NET issue. Suppressed via `<NoWarn>` in csproj.
- **FixActorRolesAsync in seeder** — Automatically corrects actors with default "User" role on startup, eliminating the need for manual SQL after migration.
- **Test factory updated** — Removes SQL Server provider registrations before adding in-memory SQLite, preventing "multiple providers" error.

---

## Multi-Session Plan: BATONs as Issues

A multi-session plan was approved for the full BATON-as-issues migration. Sessions A (SQL Server) is complete. Remaining:

### Session B: BATON Auto-Archive + API Support
- Auto-archive logic in PostService: when a new "baton"-tagged post is created, close previous Open baton for that project
- Convenience endpoint: `GET /api/posts/latest-baton?projectId=N`
- Integration tests

### Session C: Historical BATON Import
- Parse all ~140+ BATON markdown files across 6 projects
- Import as posts with tag "baton", mapping to existing sessions
- All IDs preserved — no reference rewriting needed

### Session D: Update Claude Instructions
- Rewrite `baton.md` for API-based workflow (replace file reads/writes with API calls)
- Update `issue-tracking.md` with BATON steps
- Update CLAUDE.md in each project

### Session E: Cleanup & Verification
- End-to-end workflow test
- Archive old BATON files
- Update README.md (#23)

---

## Tracker State After This Session

| Status | Count | Issues |
|--------|-------|--------|
| Open | 3 | #23: README.md, #26: Home page + logo, #398: Actor admin page |
| Closed | 8 | #1, #4, #9, #12, #14, #19, #21, #24 |

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

To switch back to SQLite temporarily: change `DatabaseProvider` to `"SQLite"` in appsettings.json. Note: SQLite migrations were removed; the app will use `EnsureCreated()` for in-memory SQLite (tests) but file-based SQLite would need migrations regenerated.

---

*Load this BATON at the start of Session 015. Recommended focus: Session B — BATON auto-archive + API support (PostService logic, latest-baton endpoint, tests).*
