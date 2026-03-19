using System.Linq;
using Content.Server.Chat.Systems;
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
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Research.Systems;

public sealed class ExperiScannerSystem : EntitySystem
{
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExperiScannerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ExperiScannerComponent, OpenResearchServerMenuMessage>(OnOpenServerMenu);
        SubscribeLocalEvent<ExperiScannerComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ExperiScannerComponent, ResearchRegistrationChangedEvent>(OnResearchRegistrationChanged);
    }

    private void OnUiOpened(Entity<ExperiScannerComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnResearchRegistrationChanged(Entity<ExperiScannerComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUi(ent);
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

        ent.Comp.LastResult = completed.Count > 0
            ? Loc.GetString("research-experi-scanner-completed-named", ("experiments", string.Join(", ", completed.Select(GetExperimentName))))
            : popup;

        _audio.PlayPvs(ent.Comp.SuccessSound, ent, AudioParams.Default.WithVolume(-2f));
        _popup.PopupEntity(popup, ent, args.User, PopupType.SmallCaution);
        _chat.TrySendInGameICMessage(ent.Owner, ent.Comp.LastResult, Shared.Chat.InGameICChatType.Speak, false);
        UpdateUi(ent);

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
        ent.Comp.LastResult = Loc.GetString(message);
        _audio.PlayPvs(ent.Comp.FailureSound, ent, AudioParams.Default.WithVolume(-4f));
        _popup.PopupEntity(ent.Comp.LastResult, ent, user);
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<ExperiScannerComponent> ent)
    {
        string? serverName = null;
        var experiments = new List<ResearchMachineExperimentUiData>();

        if (_research.TryGetClientServer(ent.Owner, out var serverUid, out var server) &&
            TryComp<TechnologyDatabaseComponent>(serverUid, out var db))
        {
            serverName = server.ServerName;

            foreach (var experimentId in db.ActiveExperiments)
            {
                if (!_prototype.TryIndex<ResearchExperimentPrototype>(experimentId, out var prototype) ||
                    !prototype.SupportedSources.HasFlag(ExperimentSourceFlags.HandheldScanner))
                    continue;

                var progress = db.ExperimentProgress.FirstOrDefault(p => p.ExperimentId == experimentId);
                var target = progress.Target > 0 ? progress.Target : Math.Max(1, prototype.Objective.Target);
                var objective = Loc.GetString($"research-experiment-objective-{prototype.Objective.Kind.ToString().ToLowerInvariant()}");
                experiments.Add(new ResearchMachineExperimentUiData(
                    prototype.ID,
                    Loc.GetString(prototype.Name),
                    Loc.GetString(prototype.Description),
                    progress.Progress,
                    target,
                    objective));
            }
        }

        _ui.SetUiState(ent.Owner, ExperiScannerUiKey.Key, new ExperiScannerBoundInterfaceState(serverName, experiments, ent.Comp.LastResult));
    }

    private string GetExperimentName(string experimentId)
    {
        return _prototype.TryIndex<ResearchExperimentPrototype>(experimentId, out var prototype)
            ? Loc.GetString(prototype.Name)
            : experimentId;
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
