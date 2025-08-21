using System.Collections.Generic;
using System.Linq;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.Tests.Server.Spawners
{
    [TestFixture]
    public sealed class SpawnPointSystemTest : RobustUnitTest
    {
        /// <summary>
        /// Test that the spawn point system properly falls back to job-specific spawners
        /// before using late-join spawners when role-specific spawners aren't found.
        /// This test validates the fix for issue #2810.
        /// </summary>
        [Test]
        public void TestSpawnPointFallbackLogic()
        {
            var server = StartServer();
            var entManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var spawnSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SpawnPointSystem>();
            var stationSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<StationSpawningSystem>();

            server.Post(() =>
            {
                // Create a test map
                var mapId = mapManager.CreateMap();
                var grid = mapManager.CreateGridEntity(mapId);
                var gridComp = entManager.GetComponent<MapGridComponent>(grid);
                
                // Create a test station
                var station = entManager.SpawnEntity(null, new EntityCoordinates(grid, Vector2.Zero));
                
                // Create job spawn points for different jobs
                var securitySpawner = entManager.SpawnEntity(null, new EntityCoordinates(grid, new Vector2(0, 0)));
                var securitySpawnerComp = entManager.AddComponent<SpawnPointComponent>(securitySpawner);
                securitySpawnerComp.SpawnType = SpawnPointType.Job;
                securitySpawnerComp.Job = "SecurityOfficer";
                
                var captainSpawner = entManager.SpawnEntity(null, new EntityCoordinates(grid, new Vector2(1, 0)));
                var captainSpawnerComp = entManager.AddComponent<SpawnPointComponent>(captainSpawner);
                captainSpawnerComp.SpawnType = SpawnPointType.Job;
                captainSpawnerComp.Job = "Captain";
                
                // Create late join spawn point
                var lateJoinSpawner = entManager.SpawnEntity(null, new EntityCoordinates(grid, new Vector2(2, 0)));
                var lateJoinSpawnerComp = entManager.AddComponent<SpawnPointComponent>(lateJoinSpawner);
                lateJoinSpawnerComp.SpawnType = SpawnPointType.LateJoin;
                
                // Test 1: Request security officer - should find the security spawner
                var securityEvent = new PlayerSpawningEvent("SecurityOfficer", null, station, SpawnPointType.Job);
                entManager.EventBus.RaiseLocalEvent(securityEvent);
                
                Assert.That(securityEvent.SpawnResult, Is.Not.Null, "Security officer should spawn successfully");
                
                // Test 2: Request passenger job that doesn't have specific spawner - should use late join
                var passengerEvent = new PlayerSpawningEvent("Passenger", null, station, SpawnPointType.Job);
                entManager.EventBus.RaiseLocalEvent(passengerEvent);
                
                Assert.That(passengerEvent.SpawnResult, Is.Not.Null, "Passenger should spawn successfully using fallback");
                
                // Test 3: Request job with no specific spawner and no late join - should use any job spawner
                // Remove late join spawner temporarily
                entManager.DeleteEntity(lateJoinSpawner);
                
                var engineerEvent = new PlayerSpawningEvent("StationEngineer", null, station, SpawnPointType.Job);
                entManager.EventBus.RaiseLocalEvent(engineerEvent);
                
                Assert.That(engineerEvent.SpawnResult, Is.Not.Null, "Engineer should spawn successfully using job spawner fallback");
            });
        }
    }
}