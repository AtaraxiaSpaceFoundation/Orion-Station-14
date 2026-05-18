<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# BUI Prediction

## Rules

- prefer reading existing networked component state on the client when possible
- use predicted UI messages such as `SendPredictedMessage`
- keep server-only effects out of predicted shared UI code

## Example Anchor

- `Content.Client/Atmos/UI/GasCanisterBoundUserInterface.cs`

## Reminder

Predicted UI code can run repeatedly. Keep it idempotent and presentation-focused.
