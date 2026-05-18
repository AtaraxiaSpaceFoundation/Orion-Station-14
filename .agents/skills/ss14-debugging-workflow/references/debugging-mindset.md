<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Debugging Mindset

## Core Approach

- find the exact step where the behavior diverges
- reduce the problem to one event, one system, one state transition, or one missing sync
- confirm the real failing assumption before changing code
- after the fix looks right, scan logs once more for warnings or errors that suggest a hidden problem remains

## Good Questions

- did the event fire?
- did the handler early-return?
- did the component mutate?
- was the state dirtied or otherwise replicated?
- is this client-only, server-only, or prediction-related?

## Rule

A fast wrong fix is worse than ten more minutes of targeted tracing.
