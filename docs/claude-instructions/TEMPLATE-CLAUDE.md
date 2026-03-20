# Project Constants

Whenever these names appear in this document or in referenced instruction files,
use the values defined here. If a command that includes a constant fails,
prompt the user to see if the constant should be updated.

- {{PROJECT_NAME}}: [Your project name — defaults to root folder name]
- {{PROJECT_ID}}: [Will be set automatically on first session]
- {{ISSUE_TRACKER_API_URL}}: http://localhost:5124/api


# Permissions from Human

Please refer to the permissions instructions in @docs/claude-instructions/permission-management.md


# Cross-Session Workflow

At the start of every session, read and follow the instructions in these files
(in this order):

1. @docs/claude-instructions/baton.md — Read the latest BATON first to understand context
2. @docs/claude-instructions/issue-tracking.md — Register project (first session only), create session, retrieve open issues
3. @docs/claude-instructions/git.md — Verify git is initialized and .gitignore is up to date

At the end of every session, follow the session-end instructions in `baton.md` and `issue-tracking.md`.

# Project-Specific Instructions

[Add your project's custom instructions, coding conventions, architecture notes, and other guidance here.]
