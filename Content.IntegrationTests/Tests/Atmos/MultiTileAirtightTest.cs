using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Atmos
{
    [TestFixture]
    [TestOf(typeof(MultiTileAirtightComponent))]
    public sealed class MultiTileAirtightTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: MultiTileAirtightTestDummy
  id: MultiTileAirtightTestDummy
  components:
  - type: Transform
    anchored: true
  - type: Airtight
    noAirWhenFullyAirBlocked: false
  - type: MultiTileAirtight
    additionalTiles:
      - 1,0
      - 2,0
";

        [Test]
        public async Task MultiTileAirtightSpawnsHelpers()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            var testMap = await pairTracker.CreateTestMap();
            var coords = testMap.GridCoords.Offset(new Vector2i(5, 5));

            await server.WaitAssertion(() =>
            {
                var entManager = server.ResolveDependency<IEntityManager>();
                var multiTileAirtightSystem = entManager.System<MultiTileAirtightSystem>();

                // Spawn the multi-tile airtight entity
                var entity = entManager.SpawnEntity("MultiTileAirtightTestDummy", coords);

                // Verify the entity has the required components
                Assert.That(entManager.HasComponent<MultiTileAirtightComponent>(entity), "Entity should have MultiTileAirtightComponent");
                Assert.That(entManager.HasComponent<AirtightComponent>(entity), "Entity should have AirtightComponent");

                var multiTileComp = entManager.GetComponent<MultiTileAirtightComponent>(entity);

                // The system should create helper entities on the additional tiles
                Assert.That(multiTileComp.HelperEntities.Count, Is.EqualTo(2), "Should have created 2 helper entities");

                // Verify helper entities exist and have airtight components
                foreach (var helperUid in multiTileComp.HelperEntities)
                {
                    Assert.That(entManager.EntityExists(helperUid), "Helper entity should exist");
                    Assert.That(entManager.HasComponent<AirtightComponent>(helperUid), "Helper should have AirtightComponent");
                }
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task MultiTileAirtightCleanupsOnDeletion()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            var testMap = await pairTracker.CreateTestMap();
            var coords = testMap.GridCoords.Offset(new Vector2i(5, 5));

            await server.WaitAssertion(() =>
            {
                var entManager = server.ResolveDependency<IEntityManager>();

                // Spawn the multi-tile airtight entity
                var entity = entManager.SpawnEntity("MultiTileAirtightTestDummy", coords);
                var multiTileComp = entManager.GetComponent<MultiTileAirtightComponent>(entity);

                // Store helper entity UIDs
                var helperUids = new List<EntityUid>(multiTileComp.HelperEntities);
                Assert.That(helperUids.Count, Is.EqualTo(2), "Should have created 2 helper entities");

                // Delete the main entity
                entManager.DeleteEntity(entity);

                // Verify helper entities are also deleted
                foreach (var helperUid in helperUids)
                {
                    Assert.That(entManager.EntityExists(helperUid), Is.False, "Helper entity should be deleted");
                }
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task MultiTileAirtightUpdatesOnAirtightChange()
        {
            await using var pairTracker = await PoolManager.GetServerClient();
            var server = pairTracker.Pair.Server;

            var testMap = await pairTracker.CreateTestMap();
            var coords = testMap.GridCoords.Offset(new Vector2i(5, 5));

            await server.WaitAssertion(() =>
            {
                var entManager = server.ResolveDependency<IEntityManager>();
                var airtightSystem = entManager.System<AirtightSystem>();

                // Spawn the multi-tile airtight entity
                var entity = entManager.SpawnEntity("MultiTileAirtightTestDummy", coords);
                var airtightComp = entManager.GetComponent<AirtightComponent>(entity);
                var multiTileComp = entManager.GetComponent<MultiTileAirtightComponent>(entity);

                // Initially should be air blocked
                Assert.That(airtightComp.AirBlocked, Is.True, "Main entity should be air blocked initially");

                // Helpers should also be air blocked
                foreach (var helperUid in multiTileComp.HelperEntities)
                {
                    var helperAirtight = entManager.GetComponent<AirtightComponent>(helperUid);
                    Assert.That(helperAirtight.AirBlocked, Is.True, "Helper should be air blocked initially");
                }

                // Change air blocked state
                airtightSystem.SetAirblocked((entity, airtightComp), false);

                // Helpers should now also be unblocked
                foreach (var helperUid in multiTileComp.HelperEntities)
                {
                    var helperAirtight = entManager.GetComponent<AirtightComponent>(helperUid);
                    Assert.That(helperAirtight.AirBlocked, Is.False, "Helper should be unblocked after main entity change");
                }
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}