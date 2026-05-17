<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Shared And Prediction

## Why Shared Exists

- avoids duplicating common data and logic
- allows prediction
- provides the shared types used in networking

## Prediction

- the client simulates local results immediately
- the server later confirms or corrects them
- this is why shared code may execute many times on the client

## Practical Rules

- put predict-able player-facing interactions in `Content.Shared`
- keep hidden or server-only information out of replicated state
- remember that shared code is visible to both sides
