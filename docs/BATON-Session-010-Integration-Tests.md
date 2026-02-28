# BATON: Session 010 — Integration Tests

**Project:** Cross-Session Issue & Discussion Tracker
**Baton Created:** 2026-02-28
**Session:** 010 — Integration test project with WebApplicationFactory + in-memory SQLite
**Participants:** Human, Claude (Opus 4.6)

---

## Summary of Work Completed

This session added a full integration test project (`IssueTracker.Tests`) with 26 xUnit tests covering all 4 REST API controllers. Tests use `WebApplicationFactory<Program>` with in-memory SQLite for fast, isolated execution.

### Integration Test Project (P4.1 — resolved)

| File | Change |
|------|--------|
| `IssueTracker.Tests/IssueTracker.Tests.csproj` | **New** — xUnit test project targeting net10.0 with Mvc.Testing |
| `IssueTracker.Tests/GlobalUsings.cs` | **New** — global `using Xunit` |
| `IssueTracker.Tests/IssueTrackerFactory.cs` | **New** — Custom `WebApplicationFactory` swapping DB to in-memory SQLite |
| `IssueTracker.Tests/PostsTests.cs` | **New** — 14 tests: CRUD, status transitions, paging, filtering, threads |
| `IssueTracker.Tests/SessionsTests.cs` | **New** — 6 tests: CRUD, archive/restore lifecycle |
| `IssueTracker.Tests/ProjectsTests.cs` | **New** — 5 tests: CRUD, seeded data verification |
| `IssueTracker.Tests/ActorsTests.cs` | **New** — 1 test: seeded actors verification |
| `IssueTracker.slnx` | Enhanced — added test project to solution |
| `IssueTracker.Web/IssueTracker.Web.csproj` | Enhanced — `InternalsVisibleTo` for test project |
| `IssueTracker.Web/Data/DatabaseSeeder.cs` | Enhanced — `EnsureCreatedAsync` for in-memory SQLite |
| `CLAUDE.md` | Enhanced — added "bundle related items" instruction |

### Test Coverage by Controller

| Controller | Tests | Scenarios |
|-----------|-------|-----------|
| Posts | 14 | Root-only filtering, status/tag filters, paging, thread CTE, create root + reply, Hold/Archive/Reopen status transitions, edit root vs reply, invalid session |
| Sessions | 6 | List, filter by project, get by ID, create, archive+restore lifecycle, 404 |
| Projects | 5 | List seeded, get by ID, 404, create, update |
| Actors | 1 | List seeded actors |

### Key Design Decisions

- **In-memory SQLite** via shared `SqliteConnection` kept open for factory lifetime
- **DatabaseSeeder compatibility**: Single production code change — detect `:memory:` connection string and use `EnsureCreatedAsync()` instead of `MigrateAsync()`
- **IClassFixture pattern**: One factory per test class, tests share seed data, create own data for mutations to avoid ordering issues
- **No mocking**: Full integration through the real HTTP pipeline and database

---

## Tracker State After This Session

| Status | Count | Issues |
|--------|-------|--------|
| Open | 2 | #23: Add comprehensive README.md, #24: Add documentation comments to codebase |
| Deferred | 0 | — |
| Closed | 7 | #1, #4, #9, #12, #14, #19, #21 |

---

## Known Issues / Remaining Work

### Priority 4 — Testing & Tooling
1. **Swagger sample JSON files** — Sample request JSON for each POST/PUT endpoint.

### Priority 5 — Nice to Have
2. **Show archived sessions toggle** — Add checkbox on Projects & Sessions page (API already supports `includeArchived=true`).
3. **Session rename** — Add PUT endpoint for renaming sessions.

### Priority 6 — Documentation
4. **Comprehensive README.md** (#23) — Project objective, setup, API usage, session workflow, BATON pattern.
5. **Documentation comments** (#24) — XML doc comments across models, services, controllers.

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

Run tests (app does NOT need to be running):
```bash
cd "c:\Users\denmi\source\repos\issue-tracking-with-ai"
dotnet test
```

To re-seed: delete `IssueTracker.Web/issuetracker.db` and restart.

---

*Load this BATON at the start of Session 011. Recommended focus: P5.3 archived sessions toggle + P5.4 session rename, or P6 documentation (README + code comments).*
