# <!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

#!/usr/bin/env python3

import json
import re
import sys


BLOCK_PATTERNS = (
    (
        re.compile(r"\bgit\s+reset\s+--hard\b", re.IGNORECASE),
        "Destructive git reset blocked for this SS14 repo.",
    ),
    (
        re.compile(r"\brm\s+-[^\n]*rf\b|\brm\s+-[^\n]*fr\b", re.IGNORECASE),
        "Recursive delete blocked for this SS14 repo.",
    ),
    (
        re.compile(r"\bRemove-Item\b.*\s-Recurse\b.*\s-Force\b|\bRemove-Item\b.*\s-Force\b.*\s-Recurse\b", re.IGNORECASE),
        "Recursive PowerShell delete blocked for this SS14 repo.",
    ),
    (
        re.compile(r"\b(?:rd|rmdir)\b.*\s/s\b.*\s/q\b|\b(?:rd|rmdir)\b.*\s/q\b.*\s/s\b", re.IGNORECASE),
        "Recursive directory delete blocked for this SS14 repo.",
    ),
    (
        re.compile(r"\bdel\b.*\s/s\b.*\s/q\b|\bdel\b.*\s/q\b.*\s/s\b", re.IGNORECASE),
        "Recursive file delete blocked for this SS14 repo.",
    ),
)


def main() -> int:
    try:
        payload = json.load(sys.stdin)
    except Exception:
        return 0

    command = payload.get("tool_input", {}).get("command", "")
    for pattern, reason in BLOCK_PATTERNS:
        if pattern.search(command):
            result = {
                "hookSpecificOutput": {
                    "hookEventName": "PreToolUse",
                    "permissionDecision": "deny",
                    "permissionDecisionReason": reason,
                }
            }
            json.dump(result, sys.stdout)
            return 0

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
