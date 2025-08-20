using System.Numerics;
using Content.Server._Sunrise.BloodCult.Items.Systems;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.Items;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Sunrise.BloodCult;

[TestFixture]
[TestOf(typeof(VoidTeleportSystem))]
public sealed class VoidTeleportSystemTest
{
    [Test]
    public async Task VoidTeleport_BlockedLocation_DoesNotCrash()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Dirty = true
        });
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var entityManager = server.ResolveDependency<IEntityManager>();
            var popupSystem = entityManager.EntitySysManager.GetEntitySystem<SharedPopupSystem>();
            var voidTeleportSystem = entityManager.EntitySysManager.GetEntitySystem<VoidTeleportSystem>();

            // Create a cultist user
            var user = entityManager.SpawnEntity("MobHuman", map.GridCoords);
            entityManager.AddComponent<BloodCultistComponent>(user);

            // Create a void teleport staff using the proper prototype
            var staff = entityManager.SpawnEntity("CultVeilShifter", map.GridCoords);
            var voidTeleportComponent = entityManager.GetComponent<VoidTeleportComponent>(staff);

            // Set up a scenario where no valid teleportation location can be found
            // by setting a very small range and surrounding the entity with walls
            voidTeleportComponent.MinRange = 1;
            voidTeleportComponent.MaxRange = 3;

            // Surround the user with walls to make all nearby teleportation spots blocked
            var mapSystem = entityManager.System<SharedMapSystem>();
            var turfSystem = entityManager.System<TurfSystem>();
            var userTransform = entityManager.GetComponent<TransformComponent>(user);
            var userPos = userTransform.Coordinates;

            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                {
                    var coords = userPos.Offset(new Vector2(x, y));
                    server.System<SharedMapSystem>().SetTile(map.Grid.Owner, map.Grid.Comp, coords, map.Tile.Tile);
                }
            }

            // Create walls around the user in a small radius
            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                {
                    if (x == 0 && y == 0) continue; // Don't put wall on user
                    var wallPos = userPos.Offset(new Vector2(x, y));
                    entityManager.SpawnEntity("WallSolid", wallPos);
                }
            }

            // Simulate using the staff in hand by raising the event
            var useInHandEvent = new UseInHandEvent(user);

            // This should not crash the server, but instead show a popup and return early
            Assert.DoesNotThrow(() =>
            {
                entityManager.EventBus.RaiseLocalEvent(staff, useInHandEvent);
            });

            // Verify that the teleportation was blocked (user should still be in original position)
            var currentTransform = entityManager.GetComponent<TransformComponent>(user);
            Assert.That(currentTransform.Coordinates.Position.X, Is.EqualTo(userPos.Position.X).Within(0.1f));
            Assert.That(currentTransform.Coordinates.Position.Y, Is.EqualTo(userPos.Position.Y).Within(0.1f));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task VoidTeleport_ValidLocation_Succeeds()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            Dirty = true
        });
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var entityManager = server.ResolveDependency<IEntityManager>();
            var voidTeleportSystem = entityManager.EntitySysManager.GetEntitySystem<VoidTeleportSystem>();

            // Create a cultist user
            var user = entityManager.SpawnEntity("MobHuman", map.GridCoords);
            entityManager.AddComponent<BloodCultistComponent>(user);

            // Create a void teleport staff using the proper prototype
            var staff = entityManager.SpawnEntity("CultVeilShifter", map.GridCoords);
            var voidTeleportComponent = entityManager.GetComponent<VoidTeleportComponent>(staff);

            var initialUses = voidTeleportComponent.UsesLeft;
            var userTransform = entityManager.GetComponent<TransformComponent>(user);
            var userPos = userTransform.Coordinates;
            var originalPos = userTransform.Coordinates.Position;
            voidTeleportComponent.MinRange = 1;
            voidTeleportComponent.MaxRange = 3;

            for (int x = -3; x <= 3; x++)
            {
                for (int y = -3; y <= 3; y++)
                {
                    var coords = userPos.Offset(new Vector2(x, y));
                    server.System<SharedMapSystem>().SetTile(map.Grid.Owner, map.Grid.Comp, coords, map.Tile.Tile);
                }
            }

            // Simulate using the staff in hand by raising the event
            var useInHandEvent = new UseInHandEvent(user);

            // This should succeed and teleport the user
            Assert.DoesNotThrow(() =>
            {
                entityManager.EventBus.RaiseLocalEvent(staff, useInHandEvent);
            });

            // Verify that the user was teleported (position should have changed)
            var currentTransform = entityManager.GetComponent<TransformComponent>(user);
            var currentPos = currentTransform.Coordinates.Position;

            // Position should be different from original
            Assert.That(currentPos.X, Is.Not.EqualTo(originalPos.X).Within(0.1f));
            Assert.That(currentPos.Y, Is.Not.EqualTo(originalPos.Y).Within(0.1f));
            // Uses should be decremented
            Assert.That(voidTeleportComponent.UsesLeft, Is.EqualTo(initialUses - 1)); // Should decrement by 1
        });

        await pair.CleanReturnAsync();
    }
}
