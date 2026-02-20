using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared._Orion.Morph;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Utility;

namespace Content.Server._Orion.GameTicking;

public sealed class MorphRuleSystem : GameRuleSystem<MorphRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    protected override void AppendRoundEndText(EntityUid uid, MorphRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var hasAnyLine = false;
        var sessionData = _antag.GetAntagIdentifiers(uid);
        foreach (var (mind, data, name) in sessionData)
        {
            if (!TryComp<MindComponent>(mind, out var mindComp) ||
                mindComp.OwnedEntity == null ||
                !TryComp<MorphComponent>(mindComp.OwnedEntity.Value, out var morph))
                continue;

            var morphUid = mindComp.OwnedEntity.Value;
            var escapedName = FormattedMessage.EscapeText(name);

            args.AddLine(Loc.GetString("objectives-with-objectives",
                ("custody", string.Empty),
                ("title", escapedName),
                ("agent", Loc.GetString("morph-round-end-agent-name"))));

            AddObjectiveResultLine(args,
                Loc.GetString("morph-round-end-objective-survive"),
                HasComp<MobStateComponent>(morphUid) && _mobState.IsAlive(morphUid) ? 1f : 0f);

            var devourTarget = Math.Max(1, morph.RoundEndDevourTarget);
            AddObjectiveResultLine(args,
                Loc.GetString("morph-round-end-objective-devour", ("current", morph.LivingDevoured), ("target", devourTarget)),
                MathF.Min(1f, morph.LivingDevoured / (float) devourTarget));

            var reproduceTarget = Math.Max(1, morph.RoundEndReproduceTarget);
            AddObjectiveResultLine(args,
                Loc.GetString("morph-round-end-objective-reproduce", ("current", morph.TotalChildren), ("target", reproduceTarget)),
                MathF.Min(1f, morph.TotalChildren / (float) reproduceTarget));

            var count = morph.TotalChildren;
            args.AddLine(count > 0
                ? Loc.GetString("morph-name-user", ("name", name), ("username", data.UserName), ("count", count))
                : Loc.GetString("morph-name-user-lone", ("name", name), ("username", data.UserName), ("count", count)));

            hasAnyLine = true;
        }

        if (hasAnyLine)
            args.AddLine(string.Empty);
    }

    private void AddObjectiveResultLine(RoundEndTextAppendEvent args, string objective, float progress)
    {
        var key = progress > 0.99f
            ? "objectives-objective-success"
            : "objectives-objective-fail";

        args.AddLine($"- {Loc.GetString(key, ("objective", objective), ("progress", progress))}");
    }
}
