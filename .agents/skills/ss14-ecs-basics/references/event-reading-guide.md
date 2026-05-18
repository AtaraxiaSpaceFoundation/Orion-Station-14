<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Event Reading Guide

## What To Look For

- `SubscribeLocalEvent<Component, Event>(Handler)`
- whether the event is by-ref
- whether the handler only routes or performs behavior directly

## Questions To Ask

- what raised this event?
- what entity or component does it target?
- is it cancellable or handled?
- should this logic be reusable through a public system API?

## Rule

When reading unfamiliar SS14 code, event subscriptions are often the fastest way to find the real behavior path.
