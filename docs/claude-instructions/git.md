# Git Workflow — Claude Instructions

These instructions tell Claude how to manage git operations for a project. They are designed to be referenced from any project's CLAUDE.md.

---

## Git Initialization Check

At the start of a session, verify that git is initialized:

1. Check if a `.git/` directory exists in the project root.
2. **If `.git/` does not exist:**
   - Prompt the user: "Git is not initialized for this project. Would you like me to initialize it?"
   - If the user approves, run `git init`.
   - Then proceed to .gitignore setup below.

---

## .gitignore Setup

If `.gitignore` does not exist in the project root:

1. **Detect the project type** by examining the folder structure for known indicators:

   | Indicator file           | Stack / Language        |
   |--------------------------|------------------------|
   | `*.csproj`, `*.sln*`     | .NET / C#              |
   | `package.json`           | Node.js / JavaScript   |
   | `requirements.txt`, `setup.py`, `pyproject.toml` | Python |
   | `Cargo.toml`             | Rust                   |
   | `go.mod`                 | Go                     |
   | `pom.xml`, `build.gradle`| Java                   |
   | `Gemfile`                | Ruby                   |

2. **Recommend appropriate .gitignore contents** based on the detected stack. Include patterns for:
   - Build artifacts and output directories
   - Package manager caches and lock files (if not needed)
   - IDE and editor files (`.vs/`, `.idea/`, `.vscode/` settings)
   - OS files (`.DS_Store`, `Thumbs.db`)
   - Environment and secrets files (`.env`, `*.key`, credentials)
   - Database files if applicable (`.db`, `.sqlite`)
   - Log and temp files

3. **Present the proposed .gitignore** to the user for review and approval before creating the file.

---

## Pre-Commit .gitignore Review

Before each commit:

1. Run `git status` to see all untracked and changed files.
2. **Check for new patterns** that should be added to `.gitignore`:
   - New build artifact types or output directories
   - IDE-specific files that weren't previously present
   - Generated files that shouldn't be tracked
3. **Check for sensitive files** that should NOT be committed:
   - `.env`, `.env.local`, `.env.production`
   - Files containing API keys, tokens, passwords, or credentials
   - Private key files (`*.key`, `*.pem`)
   - Database files with production data
4. If `.gitignore` changes are needed, propose the updates and get user approval before proceeding with the commit.
5. If sensitive files are staged, warn the user and do NOT commit until they are removed from staging.

---

## Commit Workflow

Follow standard Claude Code commit practices:

- Write clear, concise commit messages that explain *why* the change was made
- End commit messages with: `Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>`
- Only commit when explicitly asked by the user

---

## Push Behavior

After a commit is created:

1. Check if a remote is configured:
   ```bash
   git remote -v
   ```

2. **If a remote exists:** Push to the current branch.
   ```bash
   git push origin HEAD
   ```

3. **If no remote is configured:** Inform the user:
   > No git remote is configured. To add one:
   > ```bash
   > git remote add origin <repository-url>
   > ```
   > After adding a remote, you can push with `git push -u origin main`.

   Do NOT prompt to create a remote automatically — just inform the user how to set one up.
