using System.Linq;
using Content.Server.Popups;
using Content.Server.Research.Systems;
using Content.Shared._Orion.Research.Components;
using Content.Shared._Orion.Research.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;

namespace Content.Server._Orion.Research.Systems;

public sealed class ExperiScannerSystem : EntitySystem
{
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExperiScannerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ExperiScannerComponent, OpenResearchServerMenuMessage>(OnOpenServerMenu);
    }

    private void OnOpenServerMenu(Entity<ExperiScannerComponent> ent, ref OpenResearchServerMenuMessage args)
    {
        _ui.TryToggleUi(ent.Owner, ResearchClientUiKey.Key, args.Actor);
    }

    private void OnAfterInteract(Entity<ExperiScannerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target)
            return;

        if (!_interaction.InRangeUnobstructed(args.User, target, range: ent.Comp.ScanRange))
            return;

        args.Handled = true;

        if (!TryResolveServer(ent.Owner, out var server))
        {
            Fail(ent, args.User, "research-experi-scanner-no-server");
            return;
        }

        if (!_research.TryProgressExperimentsWithEntity(server,
                target,
                args.User,
                out var changed,
                out var completed,
                out var result,
                source: ExperimentSourceFlags.HandheldScanner))
        {
            var loc = result switch
            {
                ExperimentProgressAttemptResult.NoSourceCompatibleExperiment => "research-experi-scanner-no-compatible-experiments",
                ExperimentProgressAttemptResult.AlreadyScanned => "research-experi-scanner-already-scanned",
                _ => "research-experi-scanner-no-match",
            };

            Fail(ent, args.User, loc);
            return;
        }

        var targetName = Name(target);
        var popup = completed.Count > 0
            ? Loc.GetString("research-experi-scanner-completed", ("count", completed.Count), ("target", targetName))
            : Loc.GetString("research-experi-scanner-progress", ("target", targetName));

        _audio.PlayPvs(ent.Comp.SuccessSound, ent, AudioParams.Default.WithVolume(-2f));
        _popup.PopupEntity(popup, ent, args.User, PopupType.SmallCaution);

        _research.LogNetworkEvent(server,
            "experi-scanner",
            Loc.GetString("research-netlog-experi-scanner-scan",
                ("user", _research.GetResearchLogUserName(args.User)),
                ("scanner", Name(ent.Owner)),
                ("target", targetName),
                ("completed", completed.Count),
                ("progressed", Loc.GetString(changed ? "research-netlog-experimental-destructive-scanner-progress-yes" : "research-netlog-experimental-destructive-scanner-progress-no"))),
            args.User);
    }

    private void Fail(Entity<ExperiScannerComponent> ent, EntityUid user, string message)
    {
        _audio.PlayPvs(ent.Comp.FailureSound, ent, AudioParams.Default.WithVolume(-4f));
        _popup.PopupEntity(Loc.GetString(message), ent, user);
    }

    private bool TryResolveServer(EntityUid uid, out EntityUid server)
    {
        server = EntityUid.Invalid;

        if (TryComp<ResearchClientComponent>(uid, out var client) && client.Server is { } selected)
        {
            server = selected;
            return true;
        }

        var fallback = _research.GetServers(uid).OrderBy(s => s.Comp.Id).FirstOrDefault();
        if (fallback.Owner == EntityUid.Invalid)
            return false;

        server = fallback.Owner;
        return true;
    }
}
