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
        SubscribeLocalEvent<ResearchServerComponent, MapInitEvent>(OnServerMapInit);
        SubscribeLocalEvent<ResearchServerComponent, ComponentShutdown>(OnServerShutdown);
        SubscribeLocalEvent<ResearchServerComponent, TechnologyDatabaseModifiedEvent>(OnServerDatabaseModified);
        SubscribeLocalEvent<ResearchServerComponent, ExaminedEvent>(OnServerExamined); // Orion
    }

    private void OnServerStartup(EntityUid uid, ResearchServerComponent component, ComponentStartup args)
    {
        var unusedId = EntityQuery<ResearchServerComponent>(true)
            .Max(s => s.Id) + 1;
        component.Id = unusedId;

        EnsurePointBalance(component, "General");
        Dirty(uid, component);
    }

    private void OnServerMapInit(EntityUid uid, ResearchServerComponent component, MapInitEvent args)
    {
        AssignServerName(component);
        LogNetworkEvent(uid, "network", Loc.GetString("research-netlog-server-joined", ("server", component.ServerName)));
        Dirty(uid, component);
    }

    private void OnServerShutdown(EntityUid uid, ResearchServerComponent component, ComponentShutdown args)
    {
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

        foreach (var client in new List<EntityUid>(component.Clients))
        {
            UnregisterClient(client, uid, serverComponent: component, dirtyServer: false);
        }
    }

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

        if (GetNetworkAuthority(uid, component) != uid)
            return;

        if (!CanRun(uid))
            return;

        foreach (var generation in GetPointGenerationPerSecond(uid, component))
        {
            ModifyServerPoints(uid, generation.Type, generation.Amount * time, component);
        }
    }

    /// <summary>
    /// Registers a client to the specified server.
    /// </summary>
    /// <param name="client">The client being registered</param>
    /// <param name="server">The server the client is being registered to</param>
    /// <param name="clientComponent"></param>
    /// <param name="serverComponent"></param>
    /// <param name="dirtyServer">Whether to dirty the server component after registration</param>
    private void RegisterClient(EntityUid client, EntityUid server, ResearchClientComponent? clientComponent = null, ResearchServerComponent? serverComponent = null,  bool dirtyServer = true)
    {
        if (!Resolve(client, ref clientComponent, false) || !Resolve(server, ref serverComponent, false))
            return;

        var authorityServer = GetNetworkAuthority(server, serverComponent);
        if (authorityServer != server)
            serverComponent = null;

        server = authorityServer;
        if (!Resolve(server, ref serverComponent, false))
            return;

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
    private void UnregisterClient(EntityUid client, ResearchClientComponent? clientComponent = null, bool dirtyServer = true)
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
    private void UnregisterClient(EntityUid client, EntityUid server, ResearchClientComponent? clientComponent = null, ResearchServerComponent? serverComponent = null, bool dirtyServer = true)
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

    /// <summary>
    /// Adds a specified number of points to a server.
    /// </summary>
    /// <param name="uid">The server</param>
    /// <param name="points">The amount of points being added</param>
    /// <param name="component"></param>
    public void ModifyServerPoints(EntityUid uid, int points, ResearchServerComponent? component = null)
    {
        ModifyServerPoints(uid, "General", points, component);
    }

    public void ModifyServerPoints(EntityUid uid, string type, int points, ResearchServerComponent? component = null)
    {
        if (points == 0)
            return;

        if (!Resolve(uid, ref component))
            return;

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
        var ev = new ResearchServerPointsChangedEvent(uid, component.Points, points);
        var typeEv = new ResearchServerPointTypeChangedEvent(uid, type, totalByType, points);
        foreach (var client in component.Clients)
        {
            RaiseLocalEvent(client, ref ev);
            RaiseLocalEvent(client, ref typeEv);
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

    public void LogNetworkEvent(EntityUid uid, string category, string message, EntityUid? actor = null, ResearchServerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var authorityUid = GetNetworkAuthority(uid, component);
        ResearchServerComponent? authorityComponent;

        if (authorityUid == uid)
        {
            authorityComponent = component;
        }
        else if (!TryComp(authorityUid, out authorityComponent))
        {
            authorityUid = uid;
            authorityComponent = component;
        }

        authorityComponent!.Logs.Add(new ResearchLogEntry
        {
            Timestamp = _timing.CurTime,
            Category = category,
            Message = message,
            Actor = actor.HasValue ? GetNetEntity(actor.Value) : null
        });

        if (authorityComponent.Logs.Count > 30)
            authorityComponent.Logs.RemoveAt(0);

        Dirty(authorityUid, authorityComponent);
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
            generation.Select(x => $"{x.Type}: {x.Amount}"));

        var msg = Loc.GetString("research-server-examine",
            ("name", component.ServerName),
            ("points", points));

        if (!string.IsNullOrEmpty(typePoints))
            msg += "\n" + typePoints;

        args.PushMarkup(msg);
    }
    // Orion-End
}
