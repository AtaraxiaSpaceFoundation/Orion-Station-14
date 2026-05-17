<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Client Server Primer

## Server

- authoritative state
- validates actions
- runs simulation and sends updates

## Client

- renders the game
- accepts player input
- sends requests
- shows results and local feedback

## Shared

- code both sides understand
- shared events and data types
- prediction-safe logic
- replicated component definitions

## Rule

The client should feel immediate, but the server is still the source of truth.
