using System.Linq;
using Content.Server.Research.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Research.Components;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Research;

[TestFixture]
[TestOf(typeof(ResearchSystem))]
public sealed class IllegalTechnologiesTest
{
    [Test]
    public async Task IllegalTechnologiesHiddenByDefault()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings {NoClient = true});
        var server = pair.Server;

        await server.WaitPost(() =>
        {
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.EntMan;

            var mapId = mapManager.CreateMap();
            var coords = new MapCoordinates(0, 0, mapId);

            // Create a research console
            var researchConsole = entityManager.SpawnEntity("ResearchAndDevelopmentServer", coords);
            
            // Get the database component
            Assert.That(entityManager.TryGetComponent<TechnologyDatabaseComponent>(researchConsole, out var database), Is.True);
            
            // Add Illegal discipline to supported disciplines for testing
            database.SupportedDisciplines.Add("Illegal");

            var researchSystem = entityManager.System<ResearchSystem>();
            
            // Get available technologies (should not include hidden ones by default)
            var availableTech = researchSystem.GetAvailableTechnologies(researchConsole, database);
            var illegalTech = availableTech.Where(t => t.Discipline == "Illegal").ToList();
            
            // Should be empty since illegal technologies are hidden
            Assert.That(illegalTech, Is.Empty, "Illegal technologies should be hidden by default");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task IllegalTechnologiesVisibleWhenEmagged()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings {NoClient = true});
        var server = pair.Server;

        await server.WaitPost(() =>
        {
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.EntMan;

            var mapId = mapManager.CreateMap();
            var coords = new MapCoordinates(0, 0, mapId);

            // Create a research console
            var researchConsole = entityManager.SpawnEntity("ResearchAndDevelopmentServer", coords);
            
            // Get the database component
            Assert.That(entityManager.TryGetComponent<TechnologyDatabaseComponent>(researchConsole, out var database), Is.True);
            
            // Add Illegal discipline to supported disciplines for testing
            database.SupportedDisciplines.Add("Illegal");

            // Add emag component to simulate emagged state
            var emagComp = entityManager.EnsureComponent<EmaggedComponent>(researchConsole);
            emagComp.Flags = EmagType.Interaction;

            var researchSystem = entityManager.System<ResearchSystem>();
            
            // Get available technologies (should include hidden ones when emagged)
            var availableTech = researchSystem.GetAvailableTechnologies(researchConsole, database);
            var illegalTech = availableTech.Where(t => t.Discipline == "Illegal").ToList();
            
            // Should contain illegal technologies since console is emagged
            Assert.That(illegalTech, Is.Not.Empty, "Illegal technologies should be visible when emagged");
            Assert.That(illegalTech.Any(t => t.ID == "IllegalImplants"), Is.True, "Should contain IllegalImplants technology");
        });

        await pair.CleanReturnAsync();
    }
}