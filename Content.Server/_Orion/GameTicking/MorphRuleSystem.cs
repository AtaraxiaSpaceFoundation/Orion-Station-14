using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared._Orion.Morph;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;

namespace Content.Server._Orion.GameTicking;

public sealed class MorphRuleSystem : GameRuleSystem<MorphRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    protected override void AppendRoundEndText(EntityUid uid, MorphRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var sessionData = _antag.GetAntagIdentifiers(uid);
        foreach (var (mind, data, name) in sessionData)
        {
            if (!TryComp<MindComponent>(mind, out var mindComp) ||
                mindComp.OwnedEntity == null ||
                !TryComp<MorphComponent>(mindComp.OwnedEntity.Value, out var morph))
                continue;

            var count = morph.TotalChildren;

            args.AddLine(count != 1
                ? Loc.GetString("morph-name-user", ("name", name), ("username", data.UserName), ("count", count))
                : Loc.GetString("morph-name-user-lone", ("name", name), ("username", data.UserName), ("count", count)));
        }
    }
}
