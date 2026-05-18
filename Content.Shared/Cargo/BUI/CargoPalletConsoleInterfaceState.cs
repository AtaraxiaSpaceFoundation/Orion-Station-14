// SPDX-FileCopyrightText: 2023 Checkraze <71046427+Cheackraze@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.BUI;

[NetSerializable, Serializable]
public sealed class CargoPalletConsoleInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// estimated apraised value of all the entities on top of pallets on the same grid as the console
    /// </summary>
    public int Appraisal;

    /// <summary>
    /// number of entities on top of pallets on the same grid as the console
    /// </summary>
    public int Count;

    /// <summary>
    /// are the buttons enabled
    /// </summary>
    public bool Enabled;

    // Orion-Start
    public List<CargoPalletMarketChangeData> ActiveChanges;
    public List<CargoPalletMarketChangeData> RecentChanges;
    // Orion-End

    public CargoPalletConsoleInterfaceState(
        int appraisal,
        int count,
        bool enabled,
        List<CargoPalletMarketChangeData>? activeChanges = null, // Orion
        List<CargoPalletMarketChangeData>? recentChanges = null) // Orion
    {
        Appraisal = appraisal;
        Count = count;
        Enabled = enabled;
        ActiveChanges = activeChanges ?? new(); // Orion
        RecentChanges = recentChanges ?? new(); // Orion
    }
}

// Orion-Start
[NetSerializable, Serializable]
public sealed class CargoPalletMarketChangeData
{
    public string MaterialProto;
    public float Multiplier;
    public int Sequence;

    public CargoPalletMarketChangeData(string materialProto, float multiplier, int sequence)
    {
        MaterialProto = materialProto;
        Multiplier = multiplier;
        Sequence = sequence;
    }
}
// Orion-End
