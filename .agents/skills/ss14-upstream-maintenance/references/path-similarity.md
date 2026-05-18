<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Path Similarity

## Rule

When creating fork-only files, mirror the upstream feature path as closely as practical.

## Example

- upstream-like behavior under `Content.Shared/<Feature>/...`
- fork-side extension under `Content.Shared/_Orion/<Feature>/...` when the fork split is warranted

## Why

- keeps drift discoverable
- makes upstream rebases easier
- helps humans and agents find the fork delta quickly
