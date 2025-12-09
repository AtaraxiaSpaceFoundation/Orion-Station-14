// SPDX-FileCopyrightText: 2024 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 BombasterDS <115770678+BombasterDS@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 whateverusername0 <whateveremail>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Common.Standing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LayingDownComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float StandingUpTime { get; set; } = 1.5f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float SpeedModify { get; set; } = .3f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool AutoGetUp = true;
}

[Serializable, NetSerializable]
public sealed class ChangeLayingDownEvent : CancellableEntityEventArgs;

public sealed class CheckAutoGetUpEvent : EntityEventArgs;