using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Economy.Components;

[RegisterComponent]
public sealed partial class CreditHolochipComponent : Component;

[Serializable, NetSerializable]
public enum CreditHolochipVisuals
{
    BaseState,
    OverlayState,
    BaseColor,
}
