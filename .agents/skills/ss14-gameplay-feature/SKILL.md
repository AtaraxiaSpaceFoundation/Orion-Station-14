<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-gameplay-feature
description: Implement or review gameplay code changes in this SS14 fork. Use when modifying C# entity systems, components, actions, events, or predicted interactions across `Content.Shared`, `Content.Server`, or `Content.Client`; when deciding which assembly or feature folder owns a mechanic; or when a gameplay change also needs prototypes, localization, UI, networking, or tests.
---

# SS14 Gameplay Feature

Implement gameplay changes in the existing SS14 ECS, prediction, and content-data style.

Open nearby examples first, then use the reference files to route the task before editing.

## Workflow

1. Open [open-space-gameplay-map.md](references/open-space-gameplay-map.md) first.
- Use it to map the request to assemblies, domain folders, paired resource files, and fork-specific `_Orion` areas.

2. Open [prediction-and-cross-assembly.md](references/prediction-and-cross-assembly.md) when the feature crosses networking or UI.
- Use it for shared vs server/client placement, prediction rules, BUIs, and dirtying networked state.
3. Open [feature-checklist.md](references/feature-checklist.md) to confirm code, prototypes, locale, UI, and tests move together.

3. Read the nearest existing implementation in the same feature area before editing.
- Prefer extending the established system/component/prototype flow over inventing a new pattern.

4. Preserve ECS boundaries.
- Keep components data-only.
- Put behavior in entity systems.
- Prefer public system methods over method events.
- Public system APIs that operate on entities should usually take `Entity<T?>` or `EntityUid` first and call `Resolve(...)` early.
- Prefer `[Dependency]` fields over ad-hoc `IoCManager.Resolve(...)` inside methods.
- Use `EntityUid?` for optional entities, not `EntityUid.Invalid`.
- Prefer `ProtoId<T>` and prototypes over raw strings and enums for in-game content types.

5. Treat prediction and networking as design decisions, not cleanup.
- If the local player should see the result immediately, check whether the path should be predicted.
- Predicted systems and relevant components belong in `Content.Shared/`.
- Use `NetworkedComponent`, `AutoGenerateComponentState`, and `AutoNetworkedField` on shared components that must sync.
- Dirty changed networked state immediately. Use `DirtyField(...)` when field deltas make sense.
- Use predicted APIs such as `PopupPredicted`, `PopupClient`, `PlayPredicted`, predicted BUI messages, and predicted spawn/delete helpers.
- Do not add `NetworkedComponent` to purely server-only or purely client-only components.
- If a shared system needs client/server special cases, keep a shared base plus both server and client concrete systems.

6. Treat data definitions as part of the feature.
- Add or update prototypes, sound collections, sprites, and UI resources that the behavior depends on.
- Localize every player-facing string.
- Follow existing feature-folder placement instead of inventing a new top-level area.

7. Validate the smallest meaningful slice.
- Baseline build: `dotnet restore` then `dotnet build --configuration DebugOpt --no-restore /m`
- Unit/content tests: `dotnet test --no-build --configuration DebugOpt Content.Tests/Content.Tests.csproj`
- Integration tests: `dotnet test --no-build --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj`
- YAML/resource edits: `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj`
- RSI edits: `py -3 Schemas/validate_rsis.py Resources`
- If you cannot run in-game verification for gameplay or UI behavior, say so explicitly.

## Reference Map

- `references/open-space-gameplay-map.md`: repo-specific subsystem map, folder routing, `_Orion` extensions, and common pairings between code and resources.
- `references/prediction-and-cross-assembly.md`: prediction checklist, network state rules, BUI flow, and validation guidance.
- `references/feature-checklist.md`: end-to-end gameplay feature checklist for code, resources, locale, and validation.
- `../ss14-client-server-shared/references/client-server-primer.md`: assembly ownership and trust boundaries.
- `../ss14-client-server-shared/references/shared-and-prediction.md`: why prediction belongs in shared code.
- `../ss14-prototype-basics/references/first-prototype-workflow.md`: how to route gameplay ideas into real prototypes.
- `../ss14-debugging-workflow/references/debugging-mindset.md`: investigation workflow when the feature behaves unexpectedly.

## Useful References

- `../ss14-ecs-basics/references/ecs-primer.md`
- `../ss14-common-api-patterns/references/entitysystem-functions.md`
- `../ss14-prototypes-locale/references/prototype-locale-checklist.md`
- `../ss14-tests-authoring/references/test-selection.md`

## Common Pitfalls

- Putting gameplay logic inside components.
- Forgetting to localize a new player-facing string.
- Mutating networked state without `Dirty(...)` or `DirtyField(...)`.
- Leaving a new user interaction server-only when it should be predicted.
- Using raw prototype IDs or other magic strings where `ProtoId<T>` or constants should exist.
- Mixing feature work with unrelated cleanup or refactors.
