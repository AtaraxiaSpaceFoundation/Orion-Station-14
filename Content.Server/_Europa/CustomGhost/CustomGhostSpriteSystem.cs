using Content.Shared._Europa.CustomGhost;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Europa.CustomGhost;

//
// License-Identifier: AGPL-3.0-or-later
//

public sealed class CustomGhostSpriteSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CustomGhostSpriteComponent, PlayerAttachedEvent>(OnChangeAppearance);
    }

    private void OnChangeAppearance(EntityUid uid, CustomGhostSpriteComponent component, PlayerAttachedEvent args)
    {
        if(!_playerManager.TryGetSessionByEntity(uid, out var session))
            return;

        var playerName = session.Name;
        string ckeyToMatch;

        if (playerName.StartsWith("localhost@"))
        {
            ckeyToMatch = playerName.Substring("localhost@".Length);
        }
        else
        {
            ckeyToMatch = playerName;
        }

        TrySetCustomSprite(uid, ckeyToMatch);
    }

    public void TrySetCustomSprite(EntityUid ghostUid, string ckey)
    {
        var prototypes = _prototypeManager.EnumeratePrototypes<CustomGhostPrototype>();

        foreach (var customGhostPrototype in prototypes)
        {
            if (!string.Equals(customGhostPrototype.Ckey, ckey, StringComparison.CurrentCultureIgnoreCase))
                continue;

            _appearanceSystem.SetData(ghostUid, CustomGhostAppearance.Sprite, customGhostPrototype.CustomSpritePath.ToString());
            _appearanceSystem.SetData(ghostUid, CustomGhostAppearance.SizeOverride, customGhostPrototype.SizeOverride);

            if (customGhostPrototype.AlphaOverride > 0)
                _appearanceSystem.SetData(ghostUid, CustomGhostAppearance.AlphaOverride, customGhostPrototype.AlphaOverride);

            if (customGhostPrototype.GhostName != string.Empty)
                _metaData.SetEntityName(ghostUid, customGhostPrototype.GhostName);

            if (customGhostPrototype.GhostDescription != string.Empty)
                _metaData.SetEntityDescription(ghostUid, customGhostPrototype.GhostDescription);

            EntityManager.AddComponents(ghostUid, customGhostPrototype.Components);
            _appearanceSystem.SetData(ghostUid, CustomGhostAppearance.YAMLKOSTIL, customGhostPrototype.ID);

            return;
        }
    }
}
