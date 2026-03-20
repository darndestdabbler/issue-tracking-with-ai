# BATON — Session Handoff Instructions

These instructions tell Claude how to read and write BATON documents for cross-session continuity. They are designed to be referenced from any project's CLAUDE.md.

A **BATON** is a session handoff document that carries narrative context between Claude sessions. It tells the next session what happened, what decisions were made, and what to focus on next.

BATONs are stored as posts in the Issue Tracker with the tag `baton`. When a new BATON is created, the system automatically archives (closes) all previous BATONs for that project.

---

## At Session Start

1. **Retrieve the latest BATON** from the Issue Tracker API:

```bash
curl.exe -s "{{ISSUE_TRACKER_API_URL}}/posts/latest-baton?projectId={{PROJECT_ID}}"
```

This returns the most recent baton-tagged post (regardless of status). Read its `text` field to understand:
   - What was accomplished in the previous session
   - Key decisions and their rationale
   - Remaining work and recommended focus
   - How to start the project (build/run commands)

2. **If the Issue Tracker is available:** Also retrieve open issues from the API (see `issue-tracking.md`). The tracker is the authoritative source for issue state.

3. **If the Issue Tracker is unavailable:** Work in BATON-only mode — write the BATON to a temporary local file and sync it at the start of the next session (see Issue Fallback below).

---

## Determining the Session Number

The session number is determined from the Issue Tracker session that was created for this session (see `issue-tracking.md`). Extract the session number from the session name, or look at the latest BATON's title to determine the next number.

---

## At Session End

Before ending the session, create a new BATON post in the Issue Tracker.

### Creating a BATON Post

```bash
curl.exe -s -X POST {{ISSUE_TRACKER_API_URL}}/posts \
  -H "Content-Type: application/json" \
  -d @- <<'ENDJSON'
{
  "sessionId": SESSION_ID,
  "fromActorId": 1,
  "actionType": "New",
  "title": "BATON NNN — Descriptive Title",
  "tags": "baton",
  "text": "BATON_CONTENT_HERE"
}
ENDJSON
```

Use Python to build the JSON payload for reliable encoding of the markdown content:

```bash
python -c "
import json, sys
payload = {
    'sessionId': SESSION_ID,
    'fromActorId': 1,
    'actionType': 'New',
    'title': 'BATON NNN — Descriptive Title',
    'tags': 'baton',
    'text': sys.stdin.read()
}
print(json.dumps(payload))
" <<'BATONEOF' | curl.exe -s -X POST {{ISSUE_TRACKER_API_URL}}/posts -H "Content-Type: application/json" -d @-
BATON MARKDOWN CONTENT HERE
BATONEOF
```

The auto-archive system will automatically close all previous BATONs for this project when the new one is created.

### BATON Content Structure

```markdown
# BATON: Session NNN — Descriptive Title

**Project:** [Project name]
**Baton Created:** [Today's date, YYYY-MM-DD]
**Session:** NNN — [Session focus description]
**Participants:** Human, Claude (Opus 4.6)

---

## Summary of Work Completed

[Narrative overview of what was accomplished. Include tables or lists for detailed breakdowns if helpful.]

### Key Decisions

[Bullet list of significant decisions made and their rationale. Include out-of-scope items.]

---

## Remaining Work

[Numbered list of remaining tasks with priorities. Reference tracker issue numbers (e.g., "#23") rather than duplicating full details here.]

---

## How to Start

[Commands needed to build, run, and test the project.]

---

*Recommended focus for next session: [brief guidance].*
```

### Writing Guidelines

- **The BATON carries the story.** Focus on narrative context that helps the next session understand *why* things were done, not just *what* was done.
- **Keep it concise.** A BATON should be scannable in under 2 minutes.
- **Reference issue numbers** and direct the reader to the tracker for details. Do not duplicate issue data in the BATON.

---

## Issue Fallback (BATON-Only Mode)

When the Issue Tracker is unavailable during a session and the user opts to work in BATON-only mode:

1. Write the BATON content to a temporary local file: `docs/BATON-PENDING.md`
2. Include an additional **Issues** section for any issues that arose:

```markdown
## Issues (BATON-Only — Sync to Tracker)

> These issues were tracked in the BATON because the Issue Tracker was unavailable.
> Sync them to the tracker at the start of the next session when the API is available.

### New Issues
- **[Title]** (tags: tag1, tag2) — [Description]

### Status Changes
- **#[ref]** [Title] — Changed from [old status] to [new status]. [Reason.]

### Resolved
- **#[ref]** [Title] — [Resolution details.]
```

At the start of the next session, if the Issue Tracker becomes available:
1. Create the BATON post via the API (as described above)
2. Create new issues via POST `/api/posts`
3. Apply status changes via appropriate ActionType posts (Hold, Archive, Reopen)
4. Delete the temporary `docs/BATON-PENDING.md` file
5. Once synced, the tracker is authoritative.
