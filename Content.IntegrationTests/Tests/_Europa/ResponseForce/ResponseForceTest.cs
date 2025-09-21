#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Server._Europa.ResponseForce;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.CCVar;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Europa.ResponseForce;

[TestFixture]
public sealed class ResponseForceTest
{
    /// <summary>
    /// A list of spec forces that can be ignored by this test.
    /// </summary>
    private readonly HashSet<string> _ignoredPrototypes = new() {};

    [Test]
    public async Task CallResponseForce()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
            Connected = true,
            DummyTicker = false,
            InLobby = false,
        });
        var (server, client) = (pair.Server, pair.Client);

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var entSysManager = server.ResolveDependency<IEntitySystemManager>();
        var entMan = server.EntMan;
        var responseForceSystem = entSysManager.GetEntitySystem<ResponseForceSystem>();
        var cfg = server.ResolveDependency<Robust.Shared.Configuration.IConfigurationManager>();
        cfg.SetCVar(CCVars.ResponseForceDelay, 0);

        // Try to spawn every ResponseForceTeam
        await server.WaitAssertion(() =>
        {
            foreach (var teamProto in protoManager.EnumeratePrototypes<ResponseForceTeamPrototype>())
            {
                if (_ignoredPrototypes.Contains(teamProto.ID))
                    continue;

                var total = teamProto.GuaranteedSpawn.Count + responseForceSystem.GetCount(teamProto);
                total -= teamProto.GuaranteedSpawn.Count(spawnEntry => spawnEntry.SpawnProbability < 1);

                Assert.That(responseForceSystem.CallResponseForce(new ProtoId<ResponseForceTeamPrototype>(teamProto.ID)));

                Assert.Multiple(() =>
                {
                    Assert.That(entMan.Count<GhostRoleComponent>(), Is.GreaterThanOrEqualTo(total));
                    Assert.That(entMan.Count<ResponseForceComponent>(), Is.GreaterThanOrEqualTo(total));
                });

                var ghostRoles = entMan.EntityQueryEnumerator<GhostRoleComponent>();
                while (ghostRoles.MoveNext(out var uid, out var ghostRole))
                {
                    Assert.That(uid.Valid);

                    Assert.Multiple(() =>
                    {
                        Assert.That(ghostRole.RoleName, Is.Not.EqualTo("Unknown"));
                        Assert.That(ghostRole.RoleDescription, Is.Not.EqualTo("Unknown"));
                    });
                }
            }
        });

        await pair.CleanReturnAsync();
    }
}
