<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Prototype Manager And Spawn

## Prototype Access

- `Index(protoId)`: use when the prototype must exist and missing data is an error
- `TryIndex(protoId, out proto)`: use when a missing prototype is a normal branch you want to handle
- `HasIndex(protoId)`
- `EnumeratePrototypes<T>()`

## Spawn Helpers

- `Spawn(proto, coords)`: generic spawn at the coordinates you already have
- `SpawnAttachedTo(proto, coords)`: spawn attached to the same coordinate anchor instead of converting through map position first
- `SpawnAtPosition(proto, coords)`: spawn at an explicit position when you want placement rather than attachment semantics
- `SpawnNextToOrDrop(proto, uid)`: try to place the entity beside a source entity, then fall back to a safe drop

## Rule

If a prototype ID is known at compile time, prefer typed IDs like `ProtoId<T>` or `EntProtoId` instead of raw strings.
