using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Roles;
using Content.Shared._Orion.Morph;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Orion.GameTicking;

public sealed class MorphRuleSystem : GameRuleSystem<MorphRuleComponent>
{
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    protected override void AppendRoundEndText(EntityUid uid, MorphRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var sessionData = _antag.GetAntagIdentifiers(uid);
        foreach (var (_, data, name) in sessionData)
        {
            var count = MorphComponent.TotalChildren;

            args.AddLine(count != 1
                ? Loc.GetString("morph-name-user", ("name", name), ("username", data.UserName), ("count", count))
                : Loc.GetString("morph-name-user-lone", ("name", name), ("username", data.UserName), ("count", count)));
        }
    }
}
