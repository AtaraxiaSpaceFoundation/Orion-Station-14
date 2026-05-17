# <!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

#!/usr/bin/env python3

import json
import sys


KEYWORDS = (
    "test",
    "tests",
    "build",
    "validate",
    "validation",
    "verified",
    "verification",
    "localiz",
    "prediction",
    "changelog",
)


def main() -> int:
    try:
        payload = json.load(sys.stdin)
    except Exception:
        json.dump({"continue": True}, sys.stdout)
        return 0

    if payload.get("stop_hook_active"):
        json.dump({"continue": True}, sys.stdout)
        return 0

    message = (payload.get("last_assistant_message") or "").lower()
    if any(keyword in message for keyword in KEYWORDS):
        json.dump({"continue": True}, sys.stdout)
        return 0

    json.dump(
        {
            "continue": True,
            "systemMessage": (
                "SS14 final check: mention what you verified and whether "
                "localization, prediction, or changelog impact were considered."
            ),
        },
        sys.stdout,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
