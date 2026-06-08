using System.Collections.Generic;
using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Construction.Components;
using Content.Server.Research.Systems;
using Content.Shared._Orion.Construction.Prototypes;
using Content.Shared._Orion.Research.Prototypes;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Orion.Research;

[TestFixture]
[TestOf(typeof(ResearchSystem))]
public sealed class ResearchExperimentMatchingTest
{
    private static readonly ProtoId<ResearchExperimentPrototype> HaloperidolExperiment = "ScanningReagentHaloperidol";
    private static readonly ProtoId<MachinePartPrototype> ServoPart = "Servo";
    private static readonly ProtoId<MachinePartPrototype> CapacitorPart = "Capacitor";

    [TestPrototypes]
    private const string Prototypes = """

        - type: entity
          id: ResearchExperimentSolutionTarget
          components:
          - type: SolutionContainerManager
            solutions:
              beaker:
                maxVol: 100

        - type: entity
          id: ResearchExperimentStrongExplosive
          components:
          - type: Explosive
            explosionType: Default
            totalIntensity: 10

        - type: entity
          id: ResearchExperimentWeakExplosive
          components:
          - type: Explosive
            explosionType: Default
            totalIntensity: 1

        - type: entity
          id: ResearchExperimentMachine
          components:
          - type: Machine

        """;

    [Test]
    public async Task ScanEntityObjectivesMatchConfiguredRequirements()
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var research = server.System<ResearchSystem>();
            var solution = server.System<SharedSolutionContainerSystem>();
            var containers = server.System<SharedContainerSystem>();
            var prototypes = server.ResolveDependency<IPrototypeManager>();
            var experiment = prototypes.Index(HaloperidolExperiment);
            var reagentObjective = (ScanEntityExperimentObjective) experiment.Objective;

            var pure = entMan.SpawnEntity("ResearchExperimentSolutionTarget", map.GridCoords);
            Assert.Multiple(() =>
            {
                Assert.That(solution.TryGetSolution(pure, "beaker", out var pureSolution, out _));
                Assert.That(solution.TryAddReagent(pureSolution!.Value, "Haloperidol", FixedPoint2.New(100), out _));
            });

            var contaminated = entMan.SpawnEntity("ResearchExperimentSolutionTarget", map.GridCoords);
            Assert.Multiple(() =>
            {
                Assert.That(solution.TryGetSolution(contaminated, "beaker", out var contaminatedSolution, out _));
                Assert.That(solution.TryAddReagent(contaminatedSolution!.Value, "Haloperidol", FixedPoint2.New(97), out _));
                Assert.That(solution.TryAddReagent(contaminatedSolution.Value, "Water", FixedPoint2.New(3), out _));
            });

            var explosiveObjective = new ScanEntityExperimentObjective { MinExplosiveIntensity = 5f };
            var strong = entMan.SpawnEntity("ResearchExperimentStrongExplosive", map.GridCoords);
            var weak = entMan.SpawnEntity("ResearchExperimentWeakExplosive", map.GridCoords);

            var nonMachine = entMan.SpawnEntity(null, map.GridCoords);
            var noMachineRequirement = new ScanEntityExperimentObjective();
            var tierRequirement = new ScanEntityExperimentObjective { RequiredMachinePartTier = 1 };

            var machineUid = entMan.SpawnEntity("ResearchExperimentMachine", map.GridCoords);
            var partUid = entMan.SpawnEntity("MicroServoStockPart", map.GridCoords);
            var machine = entMan.GetComponent<MachineComponent>(machineUid);
            Assert.That(containers.Insert(partUid, machine.PartContainer), Is.True);

            var matchingPart = new ScanEntityExperimentObjective
            {
                RequiredMachineParts = new List<ProtoId<MachinePartPrototype>> { ServoPart },
            };
            var wrongPart = new ScanEntityExperimentObjective
            {
                RequiredMachineParts = new List<ProtoId<MachinePartPrototype>> { CapacitorPart },
            };

            Assert.Multiple(() =>
            {
                Assert.That(research.MatchesEntityObjective(pure, reagentObjective), Is.True);
                Assert.That(research.MatchesEntityObjective(contaminated, reagentObjective), Is.False);
                Assert.That(research.MatchesEntityObjective(strong, explosiveObjective), Is.True);
                Assert.That(research.MatchesEntityObjective(weak, explosiveObjective), Is.False);
                Assert.That(research.MatchesEntityObjective(nonMachine, noMachineRequirement), Is.True);
                Assert.That(research.MatchesEntityObjective(nonMachine, tierRequirement), Is.False);
                Assert.That(research.MatchesEntityObjective(machineUid, tierRequirement), Is.True);
                Assert.That(research.MatchesEntityObjective(machineUid, matchingPart), Is.True);
                Assert.That(research.MatchesEntityObjective(machineUid, wrongPart), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }
}
