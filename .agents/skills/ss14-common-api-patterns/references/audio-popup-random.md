<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Audio Popup Random

## Audio

- use `PlayPvs` for normal area sounds
- use `PlayGlobal` for filtered global notifications
- use `PlayEntity` for moving sound sources
- use `PlayStatic` for world-position sounds
- use `PlayPredicted` in shared predicted flows

## Popups

- `PopupEntity`
- `PopupCoordinates`
- `PopupCursor`
- `PopupClient`
- `PopupPredicted`
- `PopupPredictedCoordinates`
- `PopupPredictedCursor`

## Random

- use robust random only on authoritative or otherwise safe sides
- do not use non-predicted randomness in predicted shared logic

## Rule

If a side effect runs in shared predicted code, always check whether there is a predicted-specific API first.
