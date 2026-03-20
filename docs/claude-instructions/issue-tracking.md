# Issue Tracking — Claude Instructions

These instructions tell Claude how to use the Issue Tracker API for cross-session issue tracking. They are designed to be referenced from any project's CLAUDE.md.

**Constants used below** — these must be defined in the project's CLAUDE.md:
- `{{ISSUE_TRACKER_API_URL}}` — e.g., `http://localhost:5124/api`
- `{{ISSUE_TRACKER_PATH}}` — absolute path to the issue-tracking-with-ai repo checkout
- `{{PROJECT_ID}}` — the project's registered ID (set during first-session registration)

---

## Prerequisites

The Issue Tracker app must be running before making API calls:

```bash
cd "{{ISSUE_TRACKER_PATH}}/IssueTracker.Web"
dotnet run
```

To use a project-specific SQLite database, set the `ISSUETRACKER_DB_PATH` environment variable:

```bash
ISSUETRACKER_DB_PATH=/path/to/project/issues.db dotnet run --project "{{ISSUE_TRACKER_PATH}}/IssueTracker.Web/"
```

---

## First-Session Detection

At the start of every session, check whether `{{PROJECT_ID}}` in the project's CLAUDE.md has a numeric value.

**If `{{PROJECT_ID}}` is still a placeholder** (e.g., `[Will be set automatically on first session]`), this is the first session for this project:

1. Prompt the user: "This appears to be the first session for this project. Would you like to register it with the Issue Tracker? Suggested name: **[root folder name]**"
2. If the user approves, register the project:

```bash
curl.exe -s -X POST {{ISSUE_TRACKER_API_URL}}/projects \
  -H "Content-Type: application/json" \
  -d "{\"name\":\"ProjectName\"}"
```

3. The API returns the created project with its auto-generated ID:

```json
{ "id": 3, "name": "ProjectName" }
```

4. Update the project's CLAUDE.md to replace the `{{PROJECT_ID}}` placeholder with the returned `id` value.

---

## Known Actor IDs

These actor IDs are seeded in the Issue Tracker database:

| Actor  | Id | Role   | Use when...                        |
|--------|----|---------|------------------------------------|
| Claude | 1  | AI     | Claude is the author (fromActorId) |
| Human  | 2  | Admin  | The user is the author or reviewer |
| System | 3  | System | System-generated entries           |
| Gemini | 4  | AI     | Gemini is the author               |

### Issue Ownership and Archive Enforcement

- **Owner** = the `fromActorId` on the root post (the actor who created the issue).
- **Only the owner, a delegated reviewer, or an Admin** can Archive (close) an issue.
- AI actors (Claude, Gemini) should use **Resolve** instead of Archive when they've addressed an issue. Claude should **never use Archive** directly.
- The API returns **403 Forbidden** if a non-owner/non-Admin/non-delegate attempts to Archive.

**Delegation:** When any child post includes a `toActorId`, the root post's `toActorId` is updated to track the current assignee. That assignee gains Archive authority.

---

## At Session Start

After first-session detection is resolved:

1. **Create a session** — store the returned `id` as your SessionId for the rest of this session:

```bash
curl.exe -s -X POST {{ISSUE_TRACKER_API_URL}}/sessions \
  -H "Content-Type: application/json" \
  -d "{\"projectId\":{{PROJECT_ID}}, \"name\":\"Session NNN — Description\"}"
```

The API returns the created session with its ID:

```json
{ "id": 5, "name": "Session 013 — Feature X", "projectId": 3, "startDate": "..." }
```

2. **Retrieve open and pending-review issues** to understand what needs attention:

```bash
curl.exe -s "{{ISSUE_TRACKER_API_URL}}/posts?status=Open&projectId={{PROJECT_ID}}"
curl.exe -s "{{ISSUE_TRACKER_API_URL}}/posts?status=Pending Review&projectId={{PROJECT_ID}}"
```

---

## During Work — When to Log Posts

Log a post when something needs to survive beyond this session:

- A **decision** was made (actionType: `New` or `Discuss`)
- A **blocker or open question** exists (actionType: `New`)
- Work is **deferred** (actionType: `Hold`)
- An item has been **addressed and is ready for review** (actionType: `Resolve`, toActorId: 2)
- A **review** is requested from the user (actionType: `Check`, toActorId: 2)

**Important:** Claude should use **Resolve** (not Archive) when work on an issue is complete. This sets the status to "Pending Review" and signals the human owner to verify before closing. Only issue owners and Admins can Archive.

**Do not log** routine implementation steps — only things with cross-session significance.

**Bundle related items** — When multiple related items arise (e.g., several UI review points, a set of similar fixes), create a single issue with a checklist in the text rather than separate issues for each.

---

## API Reference

### Create a new issue (root post)

```bash
curl.exe -s -X POST {{ISSUE_TRACKER_API_URL}}/posts \
  -H "Content-Type: application/json" \
  -d "{\"sessionId\":SESSION_ID, \"fromActorId\":1, \"actionType\":\"New\", \"title\":\"Issue title\", \"tags\":\"tag1,tag2\", \"text\":\"Description of the issue.\"}"
```

### Add discussion to an existing post

```bash
curl.exe -s -X POST {{ISSUE_TRACKER_API_URL}}/posts \
  -H "Content-Type: application/json" \
  -d "{\"sessionId\":SESSION_ID, \"fromActorId\":1, \"actionType\":\"Discuss\", \"actionForId\":POST_ID, \"text\":\"Discussion or investigation notes.\"}"
```

### Resolve an issue (mark as Pending Review)

Use this when Claude has addressed an issue. Sets status to "Pending Review".

```bash
curl.exe -s -X POST {{ISSUE_TRACKER_API_URL}}/posts \
  -H "Content-Type: application/json" \
  -d "{\"sessionId\":SESSION_ID, \"fromActorId\":1, \"actionType\":\"Resolve\", \"actionForId\":POST_ID, \"toActorId\":2, \"text\":\"Work complete. Details here.\"}"
```

### Archive (close) an issue

**Restricted:** Only the issue owner, a delegated reviewer (root post's toActorId), or an Admin can archive. Claude should use Resolve instead.

```bash
curl.exe -s -X POST {{ISSUE_TRACKER_API_URL}}/posts \
  -H "Content-Type: application/json" \
  -d "{\"sessionId\":SESSION_ID, \"fromActorId\":2, \"actionType\":\"Archive\", \"actionForId\":POST_ID, \"text\":\"Verified and closing.\"}"
```

### Hold (defer) an issue

```bash
curl.exe -s -X POST {{ISSUE_TRACKER_API_URL}}/posts \
  -H "Content-Type: application/json" \
  -d "{\"sessionId\":SESSION_ID, \"fromActorId\":1, \"actionType\":\"Hold\", \"actionForId\":POST_ID, \"text\":\"Reason for deferral.\"}"
```

### Reopen a closed or deferred issue

```bash
curl.exe -s -X POST {{ISSUE_TRACKER_API_URL}}/posts \
  -H "Content-Type: application/json" \
  -d "{\"sessionId\":SESSION_ID, \"fromActorId\":1, \"actionType\":\"Reopen\", \"actionForId\":POST_ID, \"text\":\"Reason for reopening.\"}"
```

### Request user review

```bash
curl.exe -s -X POST {{ISSUE_TRACKER_API_URL}}/posts \
  -H "Content-Type: application/json" \
  -d "{\"sessionId\":SESSION_ID, \"fromActorId\":1, \"actionType\":\"Check\", \"actionForId\":POST_ID, \"toActorId\":2, \"text\":\"Please review this.\"}"
```

### Get open issues

```bash
curl.exe -s "{{ISSUE_TRACKER_API_URL}}/posts?status=Open&projectId={{PROJECT_ID}}"
```

### Get all posts for this session

```bash
curl.exe -s "{{ISSUE_TRACKER_API_URL}}/posts?sessionId=SESSION_ID"
```

### Get full thread for a post

```bash
curl.exe -s "{{ISSUE_TRACKER_API_URL}}/posts/POST_ID/thread"
```

### Filter by tags

```bash
curl.exe -s "{{ISSUE_TRACKER_API_URL}}/posts?tags=tagname&status=Open&projectId={{PROJECT_ID}}"
```

---

## ActionType Reference

| ActionType        | Use when...                                          | Status effect on root | Access control       |
|-------------------|------------------------------------------------------|-----------------------|----------------------|
| New               | Opening a new issue or topic                         | → Open                | Anyone               |
| Discuss           | Adding commentary or investigation notes             | (no change)           | Anyone               |
| Proceed As Is     | Approving something without modification             | (no change)           | Anyone               |
| Proceed With Mods | Approving with changes (describe in Text)            | (no change)           | Anyone               |
| Check             | Requesting the user to review (set toActorId: 2)    | (no change)           | Anyone               |
| Resolve           | Work complete, requesting owner/Admin review         | → Pending Review      | Anyone               |
| Hold              | Pausing/deferring work                               | → Deferred            | Anyone               |
| Archive           | Closing/resolving an issue (verified by owner)       | → Closed              | Owner, delegate, Admin |
| Reopen            | Reopening a closed, deferred, or pending issue       | → Open                | Anyone               |

---

## API Unavailable Fallback

If curl commands to `{{ISSUE_TRACKER_API_URL}}` fail (connection refused, timeout, etc.):

1. Prompt the user: "The Issue Tracker API is not reachable at `{{ISSUE_TRACKER_API_URL}}`. Would you like to work in BATON-only mode for this session?"
2. If the user agrees, note any issues that arise during the session in the BATON document instead (see `baton.md` for the issue fallback format).
3. These BATON-tracked issues should be synced to the Issue Tracker in the next session when the API becomes available.
