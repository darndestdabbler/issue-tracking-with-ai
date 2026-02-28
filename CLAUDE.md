# Issue Tracker — Claude Code Instructions

This project includes a REST API for cross-session issue tracking. Use it to persist decisions, blockers, and action items across sessions.

**Prerequisite:** The app must be running (`dotnet run` from `IssueTracker.Web/`) before making API calls.

---

## Known IDs (seeded on first run)

| Entity  | Id | Name    |
|---------|----|---------|
| Actor   | 1  | Claude  |
| Actor   | 2  | Dennis  |
| Actor   | 3  | System  |
| Project | 1  | Issue Tracker |

---

## At Session Start

1. Create a session — store the returned `id` as your SessionId for this session.
2. Retrieve open issues to understand what needs attention.

```bash
# 1. Create session (replace "Session Name" with a meaningful name)
curl.exe -s -X POST http://localhost:5124/api/sessions ^
  -H "Content-Type: application/json" ^
  -d "{\"projectId\":1, \"name\":\"Session 002 — Scaffold\"}"

# 2. Get open issues
curl.exe -s "http://localhost:5124/api/posts?status=Open&projectId=1"
```

---

## During Work — When to Log Posts

Log a post when something needs to survive beyond this session:
- A decision was made (actionType: "New" or "Discuss")
- A blocker or open question exists (actionType: "New", status will be "Open")
- Work is deferred (actionType: "Hold")
- An item is resolved (actionType: "Archive")
- A review is requested from Dennis (actionType: "Check", toActorId: 2)

**Do not log** routine implementation steps — only things with cross-session significance.

**Bundle related items** — When multiple related items arise (e.g., several UI review points, a set of similar fixes), create a single issue with a checklist in the text rather than separate issues for each. This keeps the tracker focused and avoids noise.

---

## API Reference

**Base URL:** `http://localhost:5124`

### Create a new issue (root post)

```bash
curl.exe -s -X POST http://localhost:5124/api/posts ^
  -H "Content-Type: application/json" ^
  -d "{\"sessionId\":1, \"fromActorId\":1, \"actionType\":\"New\", \"title\":\"Token refresh broken\", \"tags\":\"auth,token\", \"text\":\"Null ref when token is expired at middleware layer.\"}"
```

### Add discussion to an existing post

```bash
curl.exe -s -X POST http://localhost:5124/api/posts ^
  -H "Content-Type: application/json" ^
  -d "{\"sessionId\":1, \"fromActorId\":1, \"actionType\":\"Discuss\", \"actionForId\":12, \"text\":\"Root cause found in TokenRefreshMiddleware line 47.\"}"
```

### Archive (close) an issue

```bash
curl.exe -s -X POST http://localhost:5124/api/posts ^
  -H "Content-Type: application/json" ^
  -d "{\"sessionId\":1, \"fromActorId\":1, \"actionType\":\"Archive\", \"actionForId\":12, \"text\":\"Fixed in commit abc123. Token refresh now handles null expiry.\"}"
```

### Hold (defer) an issue

```bash
curl.exe -s -X POST http://localhost:5124/api/posts ^
  -H "Content-Type: application/json" ^
  -d "{\"sessionId\":1, \"fromActorId\":1, \"actionType\":\"Hold\", \"actionForId\":12, \"text\":\"Waiting on auth library update from upstream.\"}"
```

### Get open issues

```bash
curl.exe -s "http://localhost:5124/api/posts?status=Open&projectId=1"
```

### Get all posts for this session

```bash
curl.exe -s "http://localhost:5124/api/posts?sessionId=1"
```

### Get full thread for a post

```bash
curl.exe -s http://localhost:5124/api/posts/12/thread
```

### Filter by tags

```bash
curl.exe -s "http://localhost:5124/api/posts?tags=auth&status=Open&projectId=1"
```

---

## ActionType Reference

| ActionType        | Use when...                                          | Status effect on root |
|-------------------|------------------------------------------------------|-----------------------|
| New               | Opening a new issue or topic                         | → Open                |
| Discuss           | Adding commentary or investigation notes             | (no change)           |
| Proceed As Is     | Approving something without modification             | (no change)           |
| Proceed With Mods | Approving with changes (describe in Text)            | (no change)           |
| Check             | Requesting Dennis to review (set toActorId: 2)       | (no change)           |
| Hold              | Pausing/deferring work                               | → Deferred            |
| Archive           | Closing/resolving an issue                           | → Closed              |
| Reopen            | Reopening a closed or deferred issue                 | → Open                |

---

## At Session End

Review posts created this session, then write a BATON to `docs/BATON-Session-NNN-*.md` covering:
- Session narrative and approach taken
- High-level context for next session
- Link to open issues in the tracker (retrieve via `GET /api/posts?status=Open&projectId=1`)

The BATON carries the story. The tracker carries the ledger.
