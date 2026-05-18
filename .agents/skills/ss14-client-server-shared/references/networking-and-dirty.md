<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Networking And Dirty

## Network Events

- define the event in shared code
- use `[Serializable, NetSerializable]`
- prefer `NetEntity` in payloads instead of raw `EntityUid`
- raise with `RaiseNetworkEvent(...)`
- receive with `SubscribeNetworkEvent(...)`, usually from `Initialize()`

## Replicated Components

- shared component
- `[NetworkedComponent]`
- `[AutoGenerateComponentState]`
- `[AutoNetworkedField]` on fields the client must know

## Dirty

- if authoritative component data changes and the client should know, call `Dirty(...)`
- use `DirtyField(...)` when only one field should delta-sync

## Rule

If you changed authoritative networked state and forgot to dirty it, the client effectively did not get the change.
