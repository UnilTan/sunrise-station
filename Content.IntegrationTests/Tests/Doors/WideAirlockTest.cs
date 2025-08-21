using Content.Server.Atmos.Components;
using Content.Shared.Atmos.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Doors
{
    [TestFixture]
    [TestOf(typeof(MultiTileAirtightComponent))]
    public sealed class WideAirlockTest
    {
        [Test]
        public async Task DoubleAirlockHasMultiTileAirtight()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            var testMap = await pairTracker.CreateTestMap();
            var coords = testMap.GridCoords.Offset(new Vector2i(5, 5));

            await server.WaitAssertion(() =>
            {
                var entManager = server.ResolveDependency<IEntityManager>();

                // Spawn a double airlock
                var doubleAirlock = entManager.SpawnEntity("DoubleGlassAirlock", coords);

                // Verify it has the MultiTileAirtight component
                Assert.That(entManager.HasComponent<MultiTileAirtightComponent>(doubleAirlock), 
                    "DoubleGlassAirlock should have MultiTileAirtightComponent");

                var multiTileComp = entManager.GetComponent<MultiTileAirtightComponent>(doubleAirlock);

                // Should have 1 additional tile (spans 2 tiles total)
                Assert.That(multiTileComp.AdditionalTiles.Count, Is.EqualTo(1), 
                    "DoubleGlassAirlock should have 1 additional tile");

                // Should create 1 helper entity
                Assert.That(multiTileComp.HelperEntities.Count, Is.EqualTo(1), 
                    "Should create 1 helper entity for double airlock");

                // Verify the helper has airtight component
                var helperUid = multiTileComp.HelperEntities[0];
                Assert.That(entManager.HasComponent<AirtightComponent>(helperUid), 
                    "Helper entity should have AirtightComponent");
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TripleAirlockHasMultiTileAirtight()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            var testMap = await pairTracker.CreateTestMap();
            var coords = testMap.GridCoords.Offset(new Vector2i(5, 5));

            await server.WaitAssertion(() =>
            {
                var entManager = server.ResolveDependency<IEntityManager>();

                // Spawn a triple airlock
                var tripleAirlock = entManager.SpawnEntity("TripleGlassAirlock", coords);

                // Verify it has the MultiTileAirtight component
                Assert.That(entManager.HasComponent<MultiTileAirtightComponent>(tripleAirlock), 
                    "TripleGlassAirlock should have MultiTileAirtightComponent");

                var multiTileComp = entManager.GetComponent<MultiTileAirtightComponent>(tripleAirlock);

                // Should have 2 additional tiles (spans 3 tiles total)
                Assert.That(multiTileComp.AdditionalTiles.Count, Is.EqualTo(2), 
                    "TripleGlassAirlock should have 2 additional tiles");

                // Should create 2 helper entities
                Assert.That(multiTileComp.HelperEntities.Count, Is.EqualTo(2), 
                    "Should create 2 helper entities for triple airlock");

                // Verify all helpers have airtight components
                foreach (var helperUid in multiTileComp.HelperEntities)
                {
                    Assert.That(entManager.HasComponent<AirtightComponent>(helperUid), 
                        "Helper entity should have AirtightComponent");
                }
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}