# Demo Narrative — Cross-Session Issue Tracker

## Elevator Pitch

This is a lightweight issue tracker designed to give AI coding assistants (like Claude Code) persistent memory across sessions. Both the human developer and the AI can create, discuss, and resolve issues through the same REST API — producing a complete audit trail of decisions, blockers, and context that survives session boundaries.

## The Problem

AI coding assistants are stateless. Every time you start a new Claude Code session, the AI has no memory of what was decided, what was deferred, or what's blocked. Developers compensate with handoff documents, but those are unstructured and easy to lose. The result: repeated context-setting, lost decisions, and duplicated investigation.

## The Solution

A shared issue tracker that both human and AI participate in as first-class actors:

- **Claude** logs decisions, findings, and blockers via `curl` to the REST API
- **The developer** manages issues through the Blazor web UI
- **BATON documents** carry the session narrative (what happened, why, what's next)
- **The tracker** carries the ledger (open issues, closed decisions, deferred work)

Together, they form a cross-session memory system.

## Architecture

**Single-project design:** `IssueTracker.Web` runs both the API controllers and the Blazor Server UI in one process. No separate frontend/backend deployment.

**Data model — everything is a Post:**
- A root post (ActionType: "New") creates an issue with a title, tags, and status
- Child posts reply to root posts via `ActionForId` — adding discussion, reviews, or status changes
- Status propagation: a "Hold" reply defers the root issue; an "Archive" reply closes it
- Full audit trail — no edits to history, only new posts

**Key entities:**
- **Post** — the core entity (issue, reply, status change — all in one table)
- **Actor** — who performed the action (Claude, Human, System)
- **Session** — groups work into logical chunks (maps to Claude Code sessions)
- **Project** — top-level grouping (currently just "Issue Tracker")

**Tech stack:**
- .NET 10, Blazor Server (Interactive Server render mode)
- MudBlazor 9 for UI components (DataGrid, Dialogs, Timeline, Snackbar)
- SQLite (switchable to SQL Server via appsettings.json)
- Entity Framework Core with code-first migrations

## Live Demo Flow

1. **Show the Issues page** — pre-seeded with a mix of Open, Closed, and Deferred issues
2. **Expand a Closed thread** (e.g., "Design post-based data model") — show the timeline: New → Discuss → Discuss → Archive, with different actors and color-coded action types
3. **Quick actions** — click the Hold button on an Open issue to defer it; click Archive on a Deferred issue to close it. Show the grid updating immediately.
4. **Create a new issue** — open the dialog, select an actor (Human or Claude), fill in title/tags/description, submit. Show it appear in the grid.
5. **Reply to an issue** — expand a thread, click Reply, choose an action type (e.g., Check to request review), submit. Show the timeline update.
6. **Edit an issue** — expand a thread, click Edit, change the tags, save. Show the updated tags in the grid.
7. **Show how Claude uses it** — demonstrate a `curl` command creating an issue or reply via the API (same endpoint the UI uses)
8. **Filter by status/tags** — show how the grid filters to find specific issues

## Key Takeaways

- **AI as a first-class participant:** Claude doesn't just write code — it logs decisions, asks for reviews, and creates issues through the same API the human uses
- **Audit trail by design:** The Post-based model means every action is recorded. You can always trace back to why a decision was made and who made it.
- **Session handoff pattern:** BATON documents + the issue tracker together solve the statefulness problem. The BATON carries the story; the tracker carries the ledger.
- **Simple architecture, real value:** Single project, single table for all posts, no complex infrastructure. The value comes from the workflow, not the technology.
