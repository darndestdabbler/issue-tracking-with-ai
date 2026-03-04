# BATON — Session Handoff Instructions

These instructions tell Claude how to read and write BATON documents for cross-session continuity. They are designed to be referenced from any project's CLAUDE.md.

A **BATON** is a session handoff document that carries narrative context between Claude sessions. It tells the next session what happened, what decisions were made, and what to focus on next.

---

## BATON Location and Naming

- **Location:** `docs/` folder in the project root
- **Naming convention:** `BATON-Session-NNN-Descriptive-Name.md`
  - `NNN` = zero-padded 3-digit session number (001, 002, ..., 010, ...)
  - `Descriptive-Name` = short kebab-case summary of the session's focus
  - Example: `BATON-Session-013-Auth-Refactor.md`

---

## Determining the Session Number

Count existing `BATON-Session-*.md` files in the `docs/` folder and add 1:

```bash
ls docs/BATON-Session-*.md 2>/dev/null | wc -l
```

If the count is 0, this is Session 001 (the first session).

---

## At Session Start

1. **Find the latest BATON:** Look for the highest-numbered `BATON-Session-*.md` file in `docs/`.
2. **Read it** to understand:
   - What was accomplished in the previous session
   - Key decisions and their rationale
   - Remaining work and recommended focus
   - How to start the project (build/run commands)
3. **If the Issue Tracker is available:** Also retrieve open issues from the API (see `issue-tracking.md`). The tracker is the authoritative source for issue state.
4. **If the Issue Tracker is unavailable:** Check whether the latest BATON contains an "Issues" fallback section with unsynced issue data from a previous BATON-only session.

---

## At Session End

Before ending the session, write the next BATON document.

### BATON Structure

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

[Numbered list of remaining tasks with priorities. When the Issue Tracker is available, reference tracker issue numbers (e.g., "#23") rather than duplicating full details here. When working in BATON-only mode, include full issue details.]

---

## How to Start

[Commands needed to build, run, and test the project.]

---

*Load this BATON at the start of Session NNN+1. Recommended focus: [brief guidance].*
```

### Writing Guidelines

- **The BATON carries the story.** Focus on narrative context that helps the next session understand *why* things were done, not just *what* was done.
- **Keep it concise.** A BATON should be scannable in under 2 minutes.
- **When the Issue Tracker is available:** Reference issue numbers and direct the reader to the tracker for details. Do not duplicate issue data in the BATON.
- **When in BATON-only mode:** Include full issue details since the tracker is unavailable (see Issue Fallback below).

---

## Issue Fallback (BATON-Only Mode)

When the Issue Tracker was unavailable during a session and the user opted to work in BATON-only mode, include an additional **Issues** section in the BATON:

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

At the start of the next session, if the Issue Tracker becomes available, sync these BATON-tracked issues to the tracker:
1. Create new issues via POST `/api/posts`
2. Apply status changes via appropriate ActionType posts (Hold, Archive, Reopen)
3. Once synced, the BATON's issue section is no longer authoritative — the tracker is.
