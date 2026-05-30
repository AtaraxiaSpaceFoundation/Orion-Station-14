<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# SS14 Client UI

Use this when editing `Content.Client/` or XAML.

- Load `ss14-ui-bui`, `ss14-prediction`, `ss14-localization-strings`, `ss14-localization-code`.
- Prefer XAML-first windows and localized UI text.
- Read already-networked component state before inventing duplicate BUI state.
- Keep presentation-only work in the client unless shared state really has to change.
