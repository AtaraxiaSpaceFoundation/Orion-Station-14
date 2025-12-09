using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Language.Events;

//
// License-Identifier: AGPL-3.0-or-later
//

/// <summary>
/// Raised after the doafter is completed when using the item.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class LanguageLearnDoAfterEvent : SimpleDoAfterEvent;
