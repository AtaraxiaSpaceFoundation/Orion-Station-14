// SPDX-FileCopyrightText: 2022 Paul Ritter <ritter.paul1@googlemail.com>
// SPDX-FileCopyrightText: 2022 Rane <60792108+Elijahrane@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Tayrtahn <tayrtahn@gmail.com>
// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared._Orion.Research;
using Content.Shared.Examine;
using Content.Shared.Research.Components;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchSystem
{
    private void InitializeServer()
    {
        SubscribeLocalEvent<ResearchServerComponent, ComponentStartup>(OnServerStartup);
        SubscribeLocalEvent<ResearchServerComponent, MapInitEvent>(OnServerMapInit); // Orion
        SubscribeLocalEvent<ResearchServerComponent, ComponentShutdown>(OnServerShutdown);
        SubscribeLocalEvent<ResearchServerComponent, TechnologyDatabaseModifiedEvent>(OnServerDatabaseModified);
        SubscribeLocalEvent<ResearchServerComponent, ExaminedEvent>(OnServerExamined); // Orion
    }

    private void OnServerStartup(EntityUid uid, ResearchServerComponent component, ComponentStartup args)
    {
        var unusedId = EntityQuery<ResearchServerComponent>(true)
            .Max(s => s.Id) + 1;
        component.Id = unusedId;

        // Orion-Start
        EnsurePointBalance(component, "General");
        Dirty(uid, component);
        // Orion-End
    }

    // Orion-Start
    private void OnServerMapInit(EntityUid uid, ResearchServerComponent component, MapInitEvent args)
    {
        AssignServerName(component);
        LogNetworkEvent(uid, "network", Loc.GetString("research-netlog-server-joined", ("server", component.ServerName)));
        Dirty(uid, component);
    }
    // Orion-End

    private void OnServerShutdown(EntityUid uid, ResearchServerComponent component, ComponentShutdown args)
    {
        // Orion-Start
        var survivingAuthority = GetNetworkServers(uid, component)
            .Where(s => s != uid)
            .OrderBy(ent => TryComp<ResearchServerComponent>(ent, out var comp) ? comp.Id : int.MaxValue)
            .FirstOrDefault();

        if (survivingAuthority != default)
        {
            LogNetworkEvent(
                survivingAuthority,
                "network",
                Loc.GetString("research-netlog-server-left", ("server", component.ServerName)));
        }
        // Orion-End

        foreach (var client in new List<EntityUid>(component.Clients))
        {
            UnregisterClient(client, uid, serverComponent: component, dirtyServer: false);
        }
    }

    // Orion-Start
    private void AssignServerName(ResearchServerComponent component)
    {
        if (!string.IsNullOrWhiteSpace(component.ServerName) && component.ServerName != "RND-Server")
            return;

        const string charset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var chars = new char[6];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = charset[_random.Next(charset.Length)];
        }

        component.ServerName = $"RND-Server {new string(chars)}";
    }
    // Orion-End

    private void OnServerDatabaseModified(EntityUid uid, ResearchServerComponent component, ref TechnologyDatabaseModifiedEvent args)
    {
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref args);
        }
    }

    private bool CanRun(EntityUid uid)
    {
        return this.IsPowered(uid, EntityManager);
    }

    private void UpdateServer(EntityUid uid, int time, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // Orion-Start
        if (GetNetworkAuthority(uid, component) != uid)
            return;
        // Orion-End

        if (!CanRun(uid))
            return;

        // Orion-Edit-Start
        foreach (var generation in GetPointGenerationPerSecond(uid, component))
        {
            ModifyServerPoints(uid, generation.Type, generation.Amount * time, component);
        }
        // Orion-Edit-End
    }

    /// <summary>
    /// Registers a client to the specified server.
    /// </summary>
    /// <param name="client">The client being registered</param>
    /// <param name="server">The server the client is being registered to</param>
    /// <param name="clientComponent"></param>
    /// <param name="serverComponent"></param>
    /// <param name="dirtyServer">Whether to dirty the server component after registration</param>
    private void RegisterClient(EntityUid client, EntityUid server, ResearchClientComponent? clientComponent = null, ResearchServerComponent? serverComponent = null,  bool dirtyServer = true) // Orion-Edit: Was public
    {
        if (!Resolve(client, ref clientComponent, false) || !Resolve(server, ref serverComponent, false))
            return;

        // Orion-Start
        var authorityServer = GetNetworkAuthority(server, serverComponent);
        if (authorityServer != server)
            serverComponent = null;

        server = authorityServer;
        if (!Resolve(server, ref serverComponent, false))
            return;
        // Orion-End

        if (serverComponent.Clients.Contains(client))
            return;

        serverComponent.Clients.Add(client);
        clientComponent.Server = server;
        SyncClientWithServer(client, clientComponent: clientComponent);

        if (dirtyServer && !TerminatingOrDeleted(server))
            Dirty(server, serverComponent);

        var ev = new ResearchRegistrationChangedEvent(server);
        RaiseLocalEvent(client, ref ev);
    }

    /// <summary>
    /// Unregisterse a client from its server
    /// </summary>
    /// <param name="client"></param>
    /// <param name="clientComponent"></param>
    /// <param name="dirtyServer"></param>
    private void UnregisterClient(EntityUid client, ResearchClientComponent? clientComponent = null, bool dirtyServer = true) // Orion-Edit: Was public
    {
        if (!Resolve(client, ref clientComponent))
            return;

        if (clientComponent.Server is not { } server)
            return;

        UnregisterClient(client, server, clientComponent, dirtyServer: dirtyServer);
    }

    /// <summary>
    /// Unregisters a client from its server
    /// </summary>
    /// <param name="client"></param>
    /// <param name="server"></param>
    /// <param name="clientComponent"></param>
    /// <param name="serverComponent"></param>
    /// <param name="dirtyServer"></param>
    private void UnregisterClient(EntityUid client, EntityUid server, ResearchClientComponent? clientComponent = null, ResearchServerComponent? serverComponent = null, bool dirtyServer = true) // Orion-Edit: Was public
    {
        if (!Resolve(client, ref clientComponent, false) || !Resolve(server, ref serverComponent, false))
            return;

        serverComponent.Clients.Remove(client);
        clientComponent.Server = null;
        SyncClientWithServer(client, clientComponent: clientComponent);

        if (dirtyServer && !TerminatingOrDeleted(server))
        {
            Dirty(server, serverComponent);
        }

        var ev = new ResearchRegistrationChangedEvent(null);
        RaiseLocalEvent(client, ref ev);
    }

/* // Orion-Edit
    /// <summary>
    /// Gets the amount of points generated by all the server's sources in a second.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public int GetPointsPerSecond(EntityUid uid, ResearchServerComponent? component = null)
    {
        var points = 0;

        if (!Resolve(uid, ref component))
            return points;

        if (!CanRun(uid))
            return points;

        var ev = new ResearchServerGetPointsPerSecondEvent(uid, points);
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref ev);
        }
        RaiseLocalEvent(uid, ref ev); // Goobstation: We raise on the server as well in case its working as a point source.
        return ev.Points;
    }
*/

    // Orion-Start
    public List<ResearchPointAmount> GetPointGenerationPerSecond(EntityUid uid, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component) || !CanRun(uid))
            return new List<ResearchPointAmount>();

        var generation = new Dictionary<string, int>();
        foreach (var networkServer in GetNetworkServers(uid, component))
        {
            var ev = new ResearchServerGetPointsPerSecondByTypeEvent(networkServer, new());

            if (TryComp<ResearchServerComponent>(networkServer, out var networkServerComp))
            {
                foreach (var client in networkServerComp.Clients)
                {
                    RaiseLocalEvent(client, ref ev);
                }
            }

            RaiseLocalEvent(networkServer, ref ev);

            foreach (var amount in ev.Points)
            {
                generation.TryAdd(amount.Type, 0);
                generation[amount.Type] += amount.Amount;
            }
        }

        return generation.Select(x => new ResearchPointAmount { Type = x.Key, Amount = x.Value }).ToList();
    }
    // Orion-End

    /// <summary>
    /// Adds a specified number of points to a server.
    /// </summary>
    /// <param name="uid">The server</param>
    /// <param name="points">The amount of points being added</param>
    /// <param name="component"></param>
    public void ModifyServerPoints(EntityUid uid, int points, ResearchServerComponent? component = null)
    // Orion-Start
    {
        ModifyServerPoints(uid, "General", points, component);
    }
    // Orion-End

    public void ModifyServerPoints(EntityUid uid, string type, int points, ResearchServerComponent? component = null) // Orion
    {
        if (points == 0)
            return;

        if (!Resolve(uid, ref component))
            return;

        // Orion-Start
        var authorityServer = GetNetworkAuthority(uid, component);
        if (authorityServer != uid)
        {
            uid = authorityServer;
            component = null;

            if (!Resolve(uid, ref component, false))
                return;
        }

        EnsurePointBalance(component, type);
        var totalByType = 0;
        for (var i = 0; i < component.PointBalances.Count; i++)
        {
            if (component.PointBalances[i].Type != type)
                continue;

            var balance = component.PointBalances[i];
            balance.Amount += points;
            component.PointBalances[i] = balance;
            totalByType = balance.Amount;
            break;
        }

        component.Points = GetPointBalance(uid, "General", component);
        // Orion-End
        var ev = new ResearchServerPointsChangedEvent(uid, component.Points, points);
        var typeEv = new ResearchServerPointTypeChangedEvent(uid, type, totalByType, points); // Orion
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref ev);
            RaiseLocalEvent(client, ref typeEv); // Orion
        }
        Dirty(uid, component);
    }

    // Orion-Start
    private int GetPointBalance(EntityUid uid, string type, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return 0;

        foreach (var balance in component.PointBalances)
        {
            if (balance.Type == type)
                return balance.Amount;
        }

        return 0;
    }

    private bool HasSufficientPoints(EntityUid uid, IEnumerable<ResearchPointAmount> costs, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        var aggregatedCosts = costs
            .GroupBy(cost => cost.Type)
            .Select(group => new ResearchPointAmount
            {
                Type = group.Key,
                Amount = group.Sum(cost => cost.Amount),
            })
            .ToList();

        foreach (var cost in aggregatedCosts)
        {
            if (GetPointBalance(uid, cost.Type, component) < cost.Amount)
                return false;
        }

        return true;
    }

    private void TryConsumePoints(EntityUid uid, IEnumerable<ResearchPointAmount> costs, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var costList = costs
            .GroupBy(cost => cost.Type)
            .Select(group => new ResearchPointAmount
            {
                Type = group.Key,
                Amount = group.Sum(cost => cost.Amount),
            })
            .ToList();

        if (!HasSufficientPoints(uid, costList, component))
            return;

        foreach (var cost in costList)
        {
            ModifyServerPoints(uid, cost.Type, -cost.Amount, component);
        }
    }

    private IEnumerable<EntityUid> GetNetworkServers(EntityUid uid, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return [uid];

        var servers = new List<EntityUid>();
        var query = EntityQueryEnumerator<ResearchServerComponent>();
        while (query.MoveNext(out var serverUid, out var serverComp))
        {
            if (serverComp.NetworkId != component.NetworkId)
                continue;

            servers.Add(serverUid);
        }

        return servers;
    }

    private EntityUid GetNetworkAuthority(EntityUid uid, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return uid;

        return GetNetworkServers(uid, component)
            .OrderBy(ent => TryComp<ResearchServerComponent>(ent, out var comp) ? comp.Id : int.MaxValue)
            .FirstOrDefault(uid);
    }

    public string GetResearchLogUserName(EntityUid? user)
    {
        if (user is not { } uid)
            return Loc.GetString("research-netlog-user-system");

        return TryComp(uid, out MetaDataComponent? meta)
            ? meta.EntityName
            : ToPrettyString(uid);
    }

    public void LogNetworkEvent(EntityUid uid, string category, string message, EntityUid? actor = null, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var servers = GetNetworkServers(uid, component).ToList();
        if (servers.Count == 0)
            servers.Add(uid);

        foreach (var serverUid in servers)
        {
            if (!TryComp(serverUid, out ResearchServerComponent? serverComponent))
                continue;

            serverComponent.Logs.Add(new ResearchLogEntry
            {
                Timestamp = _timing.CurTime,
                Category = category,
                Message = message,
                Actor = actor.HasValue ? GetNetEntity(actor.Value) : null,
            });

            if (serverComponent.Logs.Count > 30)
                serverComponent.Logs.RemoveAt(0);

            Dirty(serverUid, serverComponent);
        }
    }

    private static void EnsurePointBalance(ResearchServerComponent component, string type)
    {
        if (component.PointBalances.Any(balance => balance.Type == type))
            return;

        component.PointBalances.Add(new ResearchPointAmount
        {
            Type = type,
            Amount = 0,
        });
    }

    private void OnServerExamined(EntityUid uid, ResearchServerComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var generation = GetPointGenerationPerSecond(uid, component);
        var points = generation.Sum(x => x.Amount);
        var typePoints = string.Join(", ",
            generation
                .OrderBy(x => x.Type)
                .Select(x => Loc.GetString("research-server-examine-type-entry",
                    ("type", LocalizePointType(x.Type)),
                    ("amount", x.Amount))));

        var msg = Loc.GetString("research-server-examine",
            ("name", component.ServerName),
            ("points", points));

        if (!string.IsNullOrEmpty(typePoints))
            msg += "\n" + typePoints;

        args.PushMarkup(msg);
    }

    private string LocalizePointType(string type)
    {
        var key = $"research-point-type-{type.ToLowerInvariant()}";
        return Loc.TryGetString(key, out var localized) ? localized : type;
    }
    // Orion-End
}
