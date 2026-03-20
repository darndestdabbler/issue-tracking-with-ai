## Permission Management

When you encounter a permission denial or are prompted for a tool permission 
that seems like it will be needed repeatedly, proactively add a generalized 
permission rule to `.claude/settings.local.json`. 

Rules:
- For Bash commands, generalize to the executable level: if `Bash(sqlite3 .\\metadata.db "SELECT ...")` 
  is denied, add `"Bash(sqlite3 *)"` to the allow list.
- For compound commands (using &&, ||, or |), add separate permissions for each executable involved.
- Read the current settings.local.json first, parse it, add the new entry to 
  `permissions.allow` (avoiding duplicates), and write it back with proper formatting.
- After editing the file, inform me what you added and retry the command.
- Never add overly broad permissions like `"Bash"` with no arguments.
- Never remove existing deny rules.
```

There's one important caveat: changes to `settings.local.json` are picked up on the next tool invocation — Claude Code re-reads settings each time — so the retry should work immediately. But the *first* attempt still gets blocked, and Claude has to do a read-edit-retry cycle. It's not as seamless as a custom UI button, but it's practical and will reduce the interruptions significantly after the first few sessions as the file accumulates the patterns you actually need.

**On question 2:** Yes, absolutely put this in a shared cross-project file. Permission friction is a universal annoyance, not specific to the Northwind migration. It fits naturally alongside your git and issue instructions as a "how to work with me" convention.

That said, I'd keep the *specific* permissions in each project's `settings.local.json` (since different projects need different executables), and only the *behavioral instruction* — "here's how to handle permission issues" — in the shared file. Something like:
```
project-root/
├── .claude/
│   ├── settings.local.json        ← project-specific permissions
│   └── CLAUDE.md                  ← references shared files
│
shared-claude-instructions/
├── git-instructions.md
├── issue-instructions.md
├── baton-instructions.md
└── permission-management.md       ← the new shared instruction