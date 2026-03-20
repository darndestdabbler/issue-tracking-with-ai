# BATON: Session 013 — Pending Review Status, Issue Ownership, and Actor Roles

**Project:** Cross-Session Issue & Discussion Tracker
**Baton Created:** 2026-03-20
**Session:** 013 — Add Resolve ActionType, Pending Review status, Archive enforcement, Actor roles
**Participants:** Human, Claude (Opus 4.6)

---

## Summary of Work Completed

Added issue ownership enforcement, a new "Pending Review" status, and Actor roles to support the IV&V workflow where AI-created findings need human verification before closure.

### Changes

| Area | File | Change |
|------|------|--------|
| Model | `Actor.cs` | Added `Role` property (Admin, User, AI, System) |
| Service | `PostService.cs` | Resolve→Pending Review status, Archive enforcement (owner/delegate/Admin only), ToActorId propagation to root |
| Controller | `PostsController.cs` | Catch `InvalidOperationException` → 403 Forbidden |
| Seeder | `DatabaseSeeder.cs` | Roles on actors, Gemini actor (id=4, AI), demo Thread 6 showing Resolve workflow |
| Migration | `AddActorRole` | Added Role column (TEXT, default "User") to Actors table |
| Tests | `PostsTests.cs` | 7 new tests: Resolve→Pending Review, Reopen from Pending Review, filter by Pending Review, Admin can Archive, non-owner rejected (403), delegate can Archive, ToActorId propagation |
| Tests | `ActorsTests.cs` | Updated for 4 actors (added Gemini), Role field in DTO |

### Key Decisions

- **No role-change API** — Roles are managed via DB seeding only. This inherently prevents AI from self-promoting since there's no endpoint to change roles.
- **ToActorId propagation** — When a child post includes ToActorId, the root post's ToActorId is updated. This keeps "current assignee" visible without querying the thread. Child posts preserve the full assignment history.
- **Archive enforcement is server-side** — The API returns 403, not just a client-side convention. This ensures enforcement regardless of which client calls the API.
- **Existing tests unaffected** — The existing Archive test (actor 1 archiving its own post) continues to pass because owner can always archive their own issues.

### Status Lifecycle

```
Open ──Resolve──> Pending Review   (anyone — signals work complete)
Pending Review ──Archive──> Closed (owner, delegate, or Admin only)
Pending Review ──Reopen──> Open    (anyone)
Open ──Archive──> Closed           (owner or Admin only)
Open ──Hold──> Deferred            (unchanged)
Deferred ──Reopen──> Open          (unchanged)
Closed ──Reopen──> Open            (unchanged)
```

---

## Tracker State After This Session

| Status | Count | Issues |
|--------|-------|--------|
| Open | 2 | #23: Add comprehensive README.md, #26: Update Home page and add splash logo |
| Deferred | 0 | — |
| Closed | 8 | #1, #4, #9, #12, #14, #19, #21, #24 |

---

## Known Issues / Remaining Work

1. **#23 — Add comprehensive README.md** (P6, docs/onboarding) — Needs updating to document the new ownership model, Resolve action, and Actor roles.
2. **#26 — Update Home page and add splash logo** (ui/branding)
3. **Existing database migration** — Production databases with existing actors will get Role="User" by default. After migration, manually update roles: `UPDATE Actors SET Role='AI' WHERE Name IN ('Claude','Gemini'); UPDATE Actors SET Role='Admin' WHERE Name='Human'; UPDATE Actors SET Role='System' WHERE Name='System';`

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

*Load this BATON at the start of Session 014. Recommended focus: Update README (#23) to document ownership model and Resolve workflow, then Home page (#26).*
