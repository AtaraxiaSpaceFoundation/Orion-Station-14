<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-audio
description: Work with SS14 audio systems, sound specifiers, sound collections, ambient or lobby music, jukeboxes, predicted sound feedback, or audio assets under `Resources/Audio` and `Resources/Prototypes/SoundCollections`.
---

# SS14 Audio

Use this skill when a change adds, routes, predicts, or reviews sound.

Audio changes usually cross code, prototypes, localization, and asset licensing. Keep the sound source data-driven, keep immediate player feedback predicted when practical, and keep reusable audio paths out of random call sites.

## Workflow

1. Identify the audio kind.
- Gameplay feedback: usually a component field, prototype value, or sound collection consumed by an entity system.
- Ambient, lobby, jukebox, or global music: usually split between server selection and client playback or UI.
- UI-only sound: keep it client-side unless other players should hear it.

2. Prefer data-driven sound references.
- Use `SoundSpecifier`, component fields, prototypes, and sound collections for reusable sounds.
- Avoid hardcoded raw paths in systems when the sound should be configurable by content.
- Put new audio assets in the narrowest existing `Resources/Audio` subtree; use `_Orion` for Orion-only assets and the existing inherited/vendor subtree for features that already own one.

3. Keep prediction and PVS aligned.
- Use predicted audio APIs for local player actions that should feel immediate.
- Use PVS-scoped playback for world sounds other nearby players should hear.
- Do not play the same sound once predicted and once authoritatively unless the existing pattern handles de-duplication.

4. Preserve networking boundaries.
- Shared code may decide that a sound should happen, but server/client systems should own side-specific playback.
- Do not introduce client-only audio dependencies into `Content.Shared`.

5. Validate resources.
- Check paths, sound collection IDs, and prototype references.
- Confirm attribution or license text for new assets.

## Reference Map

- `../ss14-common-api-patterns/references/audio-popup-random.md`
- `../ss14-prediction/references/predicted-feedback.md`
- `../ss14-pvs/SKILL.md`
- `../ss14-porting-and-licensing/SKILL.md`

## Good File Anchors

- `Content.Shared/**/Audio*.cs`
- `Content.Server/**/Audio*.cs`
- `Content.Client/Audio/**`
- `Resources/Audio/**`
- `Resources/Prototypes/SoundCollections/**`

## Common Pitfalls

- Hardcoding sound paths in gameplay logic instead of using component data or prototypes.
- Using server-only `PlayPvs` for a local predicted action that should have immediate feedback.
- Adding audio assets without attribution or with unclear licensing.
- Routing music, ambient loops, or UI-only sounds through gameplay systems without a reason.
