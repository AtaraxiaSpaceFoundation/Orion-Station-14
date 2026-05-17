<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Console And Runtime Tools

## Useful Controls

- `~` debug console
- `F3` debug overlay
- `F5` entity spawn window
- `Shift + F4` toggle UI

## Useful Commands

- `list`, `help <command>`: discover available console commands and their arguments
- `vv`, `vvread`, `vvwrite`, `vvinvoke`: inspect, read, edit, or invoke values through View Variables
- `sudo ...` for server-console-only commands: run commands that require server-console permissions
- `net_graph 1`: show network timing and packet information when checking lag or desync
- `quickinspect`: inspect paired server/client component state for the same entity faster than manual hopping

## Prediction Debugging

- test with extra fake lag when possible
- compare server/client VV state for the same entity
- doubled audio or repeated popups usually mean the wrong predicted API was used
