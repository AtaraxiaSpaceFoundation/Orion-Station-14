using System.Linq;
using Content.Server.Research.Systems;
using Content.Shared._Orion.Research;
using Content.Shared.Popups;
using Content.Shared._Orion.Research.Components;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;

namespace Content.Server._Orion.Research.Systems;

public sealed class DestructiveAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DestructiveAnalyzerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
    }

    private void OnAfterInteractUsing(Entity<DestructiveAnalyzerComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;

        if (!TryComp<ResearchAnalyzableComponent>(used, out var analyzable) || analyzable.DestructiveReward.Count == 0)
            return;

        if (!TryComp<ResearchClientComponent>(ent, out var client))
            return;

        var server = client.Server ?? _research.GetServers(ent).OrderBy(s => s.Comp.Id).FirstOrDefault().Owner;
        if (server == EntityUid.Invalid)
            return;

        foreach (var reward in analyzable.DestructiveReward)
        {
            _research.ModifyServerPoints(server, reward.Type, reward.Amount);
        }

        _research.TryProgressExperimentsWithEntity(server, used, args.User);
        foreach (var action in analyzable.ExperimentActions)
        {
            _research.TryProgressExperimentsByAction(server, action);
        }

        _research.NotifyDiscoveryEvent(server,
            new ResearchSystem.DiscoveryEventData
        {
            Type = ResearchDiscoveryEventType.MachineInsertion,
            Subject = used,
            Machine = ent,
            User = args.User,
        });

        if (!string.IsNullOrWhiteSpace(analyzable.DiscoveryTrigger))
            _research.TriggerDiscovery(server, analyzable.DiscoveryTrigger!);

        _research.LogNetworkEvent(server, "destructive-analyzer", $"Destructively analyzed {ToPrettyString(used)} for {analyzable.DestructiveReward.Count} reward channels.", args.User);

        Del(used);
        _popup.PopupEntity(Loc.GetString("research-destructive-analyzer-success"), ent, args.User, PopupType.SmallCaution);
        args.Handled = true;
    }
}
