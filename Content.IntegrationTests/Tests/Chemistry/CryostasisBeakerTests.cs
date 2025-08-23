using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Chemistry;

[TestFixture]
[TestOf(typeof(CryostasisBeakerSystem))]
public sealed class CryostasisBeakerTests
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: TestCryostasisBeaker
  components:
  - type: SolutionContainerManager
    solutions:
      beaker:
        maxVol: 50
        canReact: false
  - type: CryostasisBeaker
    maxTemperature: 293.15

- type: reagent
  id: TestReagent
  name: reagent-name-nothing
  desc: reagent-desc-nothing
  physicalDesc: reagent-physical-desc-nothing
";

    [Test]
    public async Task CryostasisBeakerPreventsHeating()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();
        var solutionSystem = server.ResolveDependency<SharedSolutionContainerSystem>();

        await server.WaitAssertion(() =>
        {
            var beaker = server.EntManager.SpawnEntity("TestCryostasisBeaker", testMap.GridCoords);
            
            // Get the solution
            Assert.That(solutionSystem.TryGetSolution(beaker, "beaker", out var solutionEntity, out var solution));
            
            // Add some reagent
            solutionSystem.TryAddReagent(solutionEntity.Value, "TestReagent", FixedPoint2.New(10));
            
            // Try to heat it to a high temperature (well above room temperature)
            solutionSystem.SetTemperature(solutionEntity.Value, 500.0f);
            
            // The temperature should be capped at room temperature (293.15K)
            Assert.That(solution!.Temperature, Is.LessThanOrEqualTo(293.15f));
            
            // Try to add thermal energy
            solutionSystem.AddThermalEnergy(solutionEntity.Value, 10000.0f);
            
            // Temperature should still be capped
            Assert.That(solution.Temperature, Is.LessThanOrEqualTo(293.15f));
        });
    }

    [Test]
    public async Task NormalBeakerAllowsHeating()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var testMap = await pair.CreateTestMap();
        var solutionSystem = server.ResolveDependency<SharedSolutionContainerSystem>();

        await server.WaitAssertion(() =>
        {
            // Create a normal beaker without CryostasisBeaker component
            var beaker = server.EntManager.SpawnEntity(null, testMap.GridCoords);
            var solutionManager = server.EntManager.AddComponent<SolutionContainerManagerComponent>(beaker);
            solutionManager.Solutions.Add("beaker", new SolutionComponent()
            {
                Solution = new() { MaxVolume = FixedPoint2.New(50) }
            });
            
            // Get the solution
            Assert.That(solutionSystem.TryGetSolution(beaker, "beaker", out var solutionEntity, out var solution));
            
            // Add some reagent
            solutionSystem.TryAddReagent(solutionEntity.Value, "TestReagent", FixedPoint2.New(10));
            
            // Heat it to a high temperature
            solutionSystem.SetTemperature(solutionEntity.Value, 500.0f);
            
            // Normal beaker should allow high temperatures
            Assert.That(solution!.Temperature, Is.EqualTo(500.0f));
        });
    }
}