#!/bin/bash
# import-batons.sh — One-shot script to import historical BATON markdown files as tracker posts.
#
# Usage:
#   ./scripts/import-batons.sh <project_id> <baton_directory> [--dry-run]
#
# Each BATON file becomes a root post tagged "baton" in the matching session.
# The auto-archive logic in PostService closes older BATONs automatically.
#
# Prerequisites:
#   - Issue Tracker API running at $API_URL
#   - Sessions must already exist for the project (or will be created)

set -euo pipefail

API_URL="${ISSUE_TRACKER_API_URL:-http://localhost:5124/api}"
DRY_RUN=false

if [[ $# -lt 2 ]]; then
    echo "Usage: $0 <project_id> <baton_directory> [--dry-run]"
    exit 1
fi

PROJECT_ID="$1"
BATON_DIR="$2"
[[ "${3:-}" == "--dry-run" ]] && DRY_RUN=true

echo "=== BATON Import ==="
echo "Project ID: $PROJECT_ID"
echo "BATON dir:  $BATON_DIR"
echo "API URL:    $API_URL"
echo "Dry run:    $DRY_RUN"
echo ""

# Verify API is reachable
if ! curl.exe -s --fail "$API_URL/projects/$PROJECT_ID" > /dev/null 2>&1; then
    echo "ERROR: Cannot reach API at $API_URL or project $PROJECT_ID not found."
    exit 1
fi

# Get existing sessions for this project
SESSIONS_JSON=$(curl.exe -s "$API_URL/sessions?projectId=$PROJECT_ID&includeArchived=true")

# Collect BATON files sorted by session number
BATON_FILES=()
while IFS= read -r f; do
    BATON_FILES+=("$f")
done < <(ls "$BATON_DIR"/BATON-Session-*.md 2>/dev/null | sort)

if [[ ${#BATON_FILES[@]} -eq 0 ]]; then
    echo "No BATON files found in $BATON_DIR"
    exit 0
fi

echo "Found ${#BATON_FILES[@]} BATON files."

# Get existing baton posts for this project to skip duplicates
EXISTING_BATONS=$(curl.exe -s "$API_URL/posts?projectId=$PROJECT_ID&tags=baton&pageSize=500" | python -c "
import sys, json
posts = json.load(sys.stdin)
for p in posts:
    print(p['title'])
" 2>/dev/null || echo "")

echo ""

# Track results
CREATED=0
SKIPPED=0
ERRORS=0
inc() { eval "$1=\$((\$$1 + 1))"; }

for baton_file in "${BATON_FILES[@]}"; do
    filename=$(basename "$baton_file")

    # Extract session number (e.g., BATON-Session-004-Deep-Population.md → 004)
    session_num=$(echo "$filename" | sed -n 's/BATON-Session-\([0-9]*\)-.*/\1/p')
    # Remove leading zeros for display but keep for matching
    session_num_int=$((10#$session_num))

    # Extract descriptive name (e.g., Deep-Population → Deep Population)
    desc_part=$(echo "$filename" | sed -n 's/BATON-Session-[0-9]*-\(.*\)\.md/\1/p')
    desc_name=$(echo "$desc_part" | tr '-' ' ')

    title="BATON $session_num — $desc_name"

    echo "--- Processing: $filename"
    echo "    Session: $session_num_int | Title: $title"

    # Skip if already imported
    if echo "$EXISTING_BATONS" | grep -qF "$title"; then
        echo "    SKIPPED (already exists)"
        inc SKIPPED
        continue
    fi

    # Find matching session by looking for the session number in the session name
    # Match patterns like "Session 004", "Session 4", "Session 004 —"
    session_id=$(echo "$SESSIONS_JSON" | python -c "
import sys, json
sessions = json.load(sys.stdin)
target = $session_num_int
for s in sessions:
    name = s['name']
    # Match 'Session NNN' pattern in the name
    import re
    m = re.search(r'Session\s+0*(\d+)', name)
    if m and int(m.group(1)) == target:
        print(s['id'])
        break
" 2>/dev/null || echo "")

    if [[ -z "$session_id" ]]; then
        echo "    No matching session found. Creating session..."
        if [[ "$DRY_RUN" == "true" ]]; then
            echo "    [DRY RUN] Would create session: Session $session_num — $desc_name"
            session_id="DRY_RUN"
        else
            session_name="Session $session_num — $desc_name"
            session_json=$(python -c "
import json, sys
print(json.dumps({'projectId': int(sys.argv[1]), 'name': sys.argv[2]}))
" "$PROJECT_ID" "$session_name")
            create_resp=$(curl.exe -s -X POST "$API_URL/sessions" \
                -H "Content-Type: application/json" \
                -d "$session_json")
            session_id=$(echo "$create_resp" | python -c "import sys,json; print(json.load(sys.stdin)['id'])" 2>/dev/null || echo "")
            if [[ -z "$session_id" ]]; then
                echo "    ERROR: Failed to create session. Response: $create_resp"
                inc ERRORS
                continue
            fi
            echo "    Created session ID: $session_id"
            # Refresh sessions cache
            SESSIONS_JSON=$(curl.exe -s "$API_URL/sessions?projectId=$PROJECT_ID&includeArchived=true")
        fi
    else
        echo "    Matched session ID: $session_id"
    fi

    if [[ "$DRY_RUN" == "true" ]]; then
        content_len=$(wc -c < "$baton_file")
        echo "    [DRY RUN] Would create post: $title ($content_len bytes) in session $session_id"
        inc CREATED
        continue
    fi

    # Build JSON payload using python to handle all encoding correctly
    # Python reads the file directly to avoid bash mangling content
    json_payload=$(python -c "
import json, sys

content = open(sys.argv[3], 'r', encoding='utf-8').read()
payload = {
    'sessionId': int(sys.argv[2]),
    'fromActorId': 1,
    'actionType': 'New',
    'title': sys.argv[1],
    'tags': 'baton',
    'text': content
}
print(json.dumps(payload))
" "$title" "$session_id" "$baton_file")

    # Create the post
    resp=$(curl.exe -s -X POST "$API_URL/posts" \
        -H "Content-Type: application/json" \
        -d "$json_payload")

    # Check for success (response should have an 'id' field)
    post_id=$(echo "$resp" | python -c "import sys,json; print(json.load(sys.stdin).get('id',''))" 2>/dev/null || echo "")

    if [[ -n "$post_id" && "$post_id" != "None" ]]; then
        echo "    Created post ID: $post_id"
        inc CREATED
    else
        echo "    ERROR: Failed to create post. Response: $resp"
        inc ERRORS
    fi

    # Small delay to ensure sequential timestamps for auto-archive ordering
    sleep 0.2
done

echo ""
echo "=== Import Complete ==="
echo "Created: $CREATED"
echo "Skipped: $SKIPPED"
echo "Errors:  $ERRORS"
