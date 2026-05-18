<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Predicted BUI Patterns

## Prefer

- reading already-networked component state when the client already has it
- `SendPredictedMessage` for predicted user actions
- updating client visuals from replicated state after it lands

## Example Anchor

- `Content.Client/Atmos/UI/GasCanisterBoundUserInterface.cs`

## Do Not

- invent duplicate BUI state without a reason
- mix server-only effects into predicted shared paths
