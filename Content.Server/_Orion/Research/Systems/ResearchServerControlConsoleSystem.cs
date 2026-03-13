using System.Linq;
using Content.Server.Research.Systems;
using Content.Shared._Orion.Research;
using Content.Shared._Orion.Research.Components;
using Content.Shared.Research.Components;
using Robust.Server.GameObjects;

namespace Content.Server._Orion.Research.Systems;

public sealed class ResearchServerControlConsoleSystem : EntitySystem
{
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ResearchServerControlConsoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ResearchServerControlConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ResearchServerControlConsoleComponent, ToggleServerGenerationMessage>(OnToggleGeneration);
    }

    private void OnInit(Entity<ResearchServerControlConsoleComponent> ent, ref ComponentInit args)
    {
        EnsureComp<ResearchClientComponent>(ent);
    }

    private void OnUiOpened(Entity<ResearchServerControlConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnToggleGeneration(Entity<ResearchServerControlConsoleComponent> ent, ref ToggleServerGenerationMessage args)
    {
        if (!_research.TryGetServerById(ent, args.ServerId, out var serverUid, out _))
            return;

        if (!TryComp<ResearchServerControlStatusComponent>(serverUid, out var status))
            status = EnsureComp<ResearchServerControlStatusComponent>(serverUid.Value);

        status.GenerationEnabled = !status.GenerationEnabled;
        Dirty(serverUid.Value, status);

        _research.LogNetworkEvent(serverUid.Value,
            "server-control",
            Loc.GetString("research-netlog-server-control-generation-toggled",
                ("state", Loc.GetString(status.GenerationEnabled
                    ? "research-netlog-server-control-state-enabled"
                    : "research-netlog-server-control-state-disabled")),
                ("user", _research.GetResearchLogUserName(args.Actor))),
            args.Actor);
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<ResearchServerControlConsoleComponent> ent)
    {
        var entries = _research.GetServers(ent)
            .Select(s =>
            {
                var status = CompOrNull<ResearchServerControlStatusComponent>(s);
                return new ResearchServerControlEntry(
                    s.Comp.Id,
                    s.Comp.ServerName,
                    status?.GenerationEnabled ?? true,
                    _research.GetPointsPerSecond(s, s.Comp),
                    _research.GetPointGenerationPerSecond(s, s.Comp),
                    s.Comp.PointBalances.Select(p => new ResearchPointAmount { Type = p.Type, Amount = p.Amount }).ToList(),
                    s.Comp.Logs.Count);
            })
            .OrderBy(s => s.Id)
            .ToList();

        _ui.SetUiState(ent.Owner, ResearchServerControlUiKey.Key, new ResearchServerControlBoundInterfaceState(entries));
    }
}
