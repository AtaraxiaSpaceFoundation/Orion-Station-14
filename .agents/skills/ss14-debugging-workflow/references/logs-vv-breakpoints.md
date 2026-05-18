<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Logs VV Breakpoints

## Logs

- use logs to confirm branches, values, and failure reasons
- prefer structured, system-scoped logging over random one-off spam
- use `Debug` or `Info` for temporary tracing, `Warning` for recoverable bad states, and `Error` for real failures
- prefer a dedicated sawmill so runtime and CI logs show which system emitted the message

## View Variables

- inspect both server and client component state
- compare replicated fields when chasing misprediction or sync issues

## Breakpoints

- put them near the suspected failing step
- remember that shared predicted code can hit many times on the client

## Rule

If you need to understand the runtime truth of a component, VV is usually faster than guessing from static code.
