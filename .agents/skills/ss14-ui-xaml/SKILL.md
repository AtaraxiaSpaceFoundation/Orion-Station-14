<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-ui-xaml
description: Work with SS14 XAML windows, controls, `.xaml.cs` code-behind, UI layout, style classes, localized UI text, FancyWindow patterns, and client-side UI widgets.
---

# SS14 XAML UI

Use this skill for XAML-first client UI layout and code-behind.

XAML should describe layout and stable widgets; code-behind should bind state, events, and formatting without becoming gameplay logic.

## Workflow

1. Find the existing UI family.
- Reuse nearby `FancyWindow`, control, style, and code-behind patterns.
- Keep `.xaml` and `.xaml.cs` paired and named consistently.
- Prefer extending existing controls over creating one-off layouts.

2. Keep layout declarative.
- Put structure, margins, containers, and named controls in XAML.
- Keep event wiring, state application, and formatting in code-behind.
- Do not build an entire window imperatively in C# when XAML is practical.

3. Localize visible text.
- Use `Loc` bindings or localized code-behind strings for titles, labels, buttons, tooltips, and empty states.
- Do not commit hardcoded player-facing UI text.

4. Respect UI ownership.
- XAML and code-behind are client presentation.
- BUI/EUI state and messages belong in their own skills; load them when the window is backed by networked UI state.

5. Validate the UI path.
- Build client code after code-behind edits.
- Run or inspect the UI in-game when practical, especially for dynamic lists and narrow windows.

## Reference Map

- `../ss14-ui-bui/SKILL.md`
- `../ss14-ui-eui/SKILL.md`
- `../ss14-localization-strings/SKILL.md`
- `../ss14-localization-code/SKILL.md`

## Good File Anchors

- `Content.Client/**/*.xaml`
- `Content.Client/**/*.xaml.cs`
- `Content.Client/**/UI/**`
- `Content.Client/**/Styles/**`

## Common Pitfalls

- Constructing full windows in C# instead of XAML.
- Hardcoding UI text.
- Adding global style changes for one window.
- Putting gameplay decisions in code-behind instead of systems or UI message handlers.
