using Content.Shared._EinsteinEngines.Language;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.Language.Components;

//
// License-Identifier: AGPL-3.0-or-later
//

/// <summary>
///     Additional languages that will be added to the entity's existing known languages.
///     Used to modify languages without overwriting parent languages.
/// </summary>
[RegisterComponent]
public sealed partial class AdditionalLanguageComponent : Component
{
    /// <summary>
    ///     Languages to add to the entity's spoken languages.
    /// </summary>
    [DataField("speaks")]
    public HashSet<ProtoId<LanguagePrototype>> SpokenLanguages = new();

    /// <summary>
    ///     Languages to add to the entity's understood languages.
    /// </summary>
    [DataField("understands")]
    public HashSet<ProtoId<LanguagePrototype>> UnderstoodLanguages = new();
}
