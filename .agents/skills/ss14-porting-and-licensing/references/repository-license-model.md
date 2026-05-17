<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Repository License Model

This file summarizes the current Orion Station 14 repository license model for agent routing and review. It is not a legal opinion.

## Current Local Anchors

Use these root files and local metadata when checking licensing:

- `README.md`
- `LICENSE-AGPLv3.TXT`
- `LICENSE-CLA.TXT`
- `LICENSE-MIT.TXT`
- per-file `SPDX-*` headers
- neighboring `.license` files where present
- RSI `meta.json` files for sprites
- asset-specific metadata beside audio, textures, maps, or other resources

## Practical Summary

The README currently describes Orion as a Russian-language fork of Goob Station, inspired by TG Station and Shiptest. That project history matters for attribution because material may originate from several upstreams before reaching Orion.

The README’s license section states:

- code in the codebase is AGPL-3.0-or-later;
- files may include REUSE Specification headers or separate `.license` files that specify dual-license options;
- most media assets are CC-BY-SA 3.0 unless stated otherwise;
- some assets may be CC-BY-NC-SA 3.0 or similar non-commercial licenses;
- submitting a PR or commit to ASF / Orion means agreeing to the contributor license agreement in `LICENSE-CLA.TXT`.

`LICENSE-MIT.TXT` exists and contains MIT text with Space Wizards Federation and Ataraxia Space Foundation copyright notices. Do not assume a file is MIT-only merely because this file exists. Check the specific file’s headers or neighboring license metadata.

`LICENSE-CLA.TXT` says contributors retain copyright ownership, while granting ASF broad rights to distribute contributions under licenses selected by ASF. This is a contribution intake rule; it does not automatically solve compatibility for externally copied material.

## Rule

If a port touches external code or assets, read the local license files and the source license before proceeding.
