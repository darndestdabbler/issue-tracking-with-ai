# Project Constants

Whenever these names appear in this document or in referenced instruction files,
use the values defined here. If a command that includes a constant fails,
prompt the user to see if the constant should be updated.

- {{PROJECT_NAME}}: Issue Tracker
- {{PROJECT_ID}}: 1
- {{ISSUE_TRACKER_API_URL}}: http://localhost:5124/api
- {{ISSUE_TRACKER_PATH}}: c:\Users\denmi\source\repos\issue-tracking-with-ai

# Cross-Session Workflow

At the start of every session, read and follow the instructions in these files
(in this order):

1. `docs/claude-instructions/baton.md` — Read the latest BATON first to understand context
2. `docs/claude-instructions/issue-tracking.md` — Create session, retrieve open issues
3. `docs/claude-instructions/git.md` — Verify git is initialized and .gitignore is up to date

At the end of every session, follow the session-end instructions in `baton.md` and `issue-tracking.md`.

# Project-Specific Instructions

- This is the Issue Tracker project itself — a .NET Blazor Server app with REST API
- Run: `dotnet run` from `IssueTracker.Web/`
- Test: `dotnet test` from solution root (app does NOT need to be running)
- UI: `http://localhost:5124` | Swagger: `http://localhost:5124/swagger`
- SQLite DB: `IssueTracker.Web/issuetracker.db` — delete to re-seed
- .NET 10 uses `.slnx` format (not `.sln`)
- In PowerShell, use `curl.exe` (not `curl`, which aliases to Invoke-WebRequest)
- User prefers no personal names hardcoded (Actor 2 = "Human", not "Dennis")
- Blazor Razor Language Server shows phantom errors with MudBlazor — trust `dotnet build`
