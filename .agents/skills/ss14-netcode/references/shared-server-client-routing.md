<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Shared Server Client Routing

## Put It In `Content.Shared/` When

- Both sides must know the type.
- The event crosses the network.
- The state is predicted or replicated.

## Put It In `Content.Server/` When

- The logic is purely authoritative and has no mirrored message or replicated state shape.
- It only validates or commits a request that was already defined in shared code.

## Put It In `Content.Client/` When

- The work is presentation-only after replicated state arrives.
- The type never crosses the wire and never needs server knowledge.

## Rule Of Thumb

If you are unsure whether the client needs to know about a request or response type, start by checking whether the payload itself must be shared. If yes, the type belongs in shared code even if only one side raises it directly.
