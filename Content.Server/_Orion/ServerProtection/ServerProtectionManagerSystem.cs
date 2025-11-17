using Content.Server._Orion.ServerProtection.Chat;
using Content.Server._Orion.ServerProtection.Emoting;

namespace Content.Server._Orion.ServerProtection;

/// <summary>
/// System that manually initializes all protection systems through direct dependencies.
/// </summary>
public sealed class ServerProtectionManagerSystem : EntitySystem
{
    [Dependency] private readonly ServerProtectionPunishmentSystem _punishment = default!;
    [Dependency] private readonly ChatProtectionSystem _chatProtection = default!;
    [Dependency] private readonly EmoteProtectionSystem _emoteProtection = default!;

    public override void Initialize()
    {
        base.Initialize();

        _punishment.Initialize();
        _chatProtection.Initialize();
        _emoteProtection.Initialize();
    }
}
