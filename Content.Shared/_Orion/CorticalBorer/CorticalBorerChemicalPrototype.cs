// SPDX-FileCopyrightText: 2025 Coenx-flex
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.CorticalBorer;

/// <summary>
///     Prototype for chemicals that can be applied by the cortical borer
/// </summary>
[Prototype("borerChemical")]
public sealed partial class CorticalBorerChemicalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     Chemical cost per unit of reagent
    /// </summary>
    [DataField]
    public int Cost { get; set; } = 5;

    /// <summary>
    ///     Reagent to inject into host
    /// </summary>
    [DataField]
    public string Reagent { get; set; } = "";

    /// <summary>
    ///     Reagent severity used in logs when injecting.
    /// </summary>
    [DataField]
    public int Severity { get; set; } = 1;
}
