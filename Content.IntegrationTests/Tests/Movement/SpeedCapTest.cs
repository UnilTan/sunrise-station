using Content.IntegrationTests.Tests.Movement;
using Content.Shared.Chemistry.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Movement;

[TestFixture]
public sealed class SpeedCapTest : MovementTest
{
    /// <summary>
    /// Test that speed caps prevent stimulants from exceeding the maximum speed.
    /// </summary>
    [Test]
    public async Task TestSpeedCapBlocksStimulants()
    {
        var entMan = Server.ResolveDependency<IEntityManager>();
        var movementSystem = entMan.System<MovementSpeedModifierSystem>();
        var inventorySystem = entMan.System<InventorySystem>();

        // Give the player galoshes with speed caps
        var galoshes = await SpawnEntity("ClothingShoesGaloshes", Player);
        await Server.WaitPost(() => inventorySystem.TryEquip(Player, galoshes, "shoes"));

        // Get initial speed with galoshes (should be capped)
        var moveComp = entMan.GetComponent<MovementSpeedModifierComponent>(Player);
        var cappedWalkSpeed = moveComp.CurrentWalkSpeed;
        var cappedSprintSpeed = moveComp.CurrentSprintSpeed;

        // Add a speed-boosting metabolism component (simulating stimulants)
        var metabolismComp = entMan.AddComponent<MovespeedModifierMetabolismComponent>(Player);
        metabolismComp.WalkSpeedModifier = 2.0f; // 200% speed
        metabolismComp.SprintSpeedModifier = 2.0f; // 200% speed
        metabolismComp.ModifierTimer = Timing.CurTime + TimeSpan.FromMinutes(1);

        // Refresh movement speed
        await Server.WaitPost(() => movementSystem.RefreshMovementSpeedModifiers(Player));

        // Speed should still be capped despite the speed boost
        var boostedWalkSpeed = moveComp.CurrentWalkSpeed;
        var boostedSprintSpeed = moveComp.CurrentSprintSpeed;

        // Assert that speeds are still at the cap (should not have increased)
        Assert.That(boostedWalkSpeed, Is.EqualTo(cappedWalkSpeed).Within(0.01f), 
            "Walk speed should be capped despite stimulants");
        Assert.That(boostedSprintSpeed, Is.EqualTo(cappedSprintSpeed).Within(0.01f), 
            "Sprint speed should be capped despite stimulants");

        // Expected cap values for galoshes (2.5 * 0.9 = 2.25, 4.5 * 0.9 = 4.05)
        Assert.That(cappedWalkSpeed, Is.EqualTo(2.25f).Within(0.01f), 
            "Galoshes should cap walk speed to 2.25 m/s");
        Assert.That(cappedSprintSpeed, Is.EqualTo(4.05f).Within(0.01f), 
            "Galoshes should cap sprint speed to 4.05 m/s");
    }

    /// <summary>
    /// Test that removing capped clothing allows stimulants to work.
    /// </summary>
    [Test]
    public async Task TestRemovingCapAllowsSpeedBoost()
    {
        var entMan = Server.ResolveDependency<IEntityManager>();
        var movementSystem = entMan.System<MovementSpeedModifierSystem>();
        var inventorySystem = entMan.System<InventorySystem>();

        // Get base speed without any items
        var moveComp = entMan.GetComponent<MovementSpeedModifierComponent>(Player);
        var baseWalkSpeed = moveComp.CurrentWalkSpeed;
        var baseSprintSpeed = moveComp.CurrentSprintSpeed;

        // Add stimulants
        var metabolismComp = entMan.AddComponent<MovespeedModifierMetabolismComponent>(Player);
        metabolismComp.WalkSpeedModifier = 1.5f; // 150% speed
        metabolismComp.SprintSpeedModifier = 1.5f; // 150% speed
        metabolismComp.ModifierTimer = Timing.CurTime + TimeSpan.FromMinutes(1);

        await Server.WaitPost(() => movementSystem.RefreshMovementSpeedModifiers(Player));

        // Speed should be boosted without caps
        var boostedWalkSpeed = moveComp.CurrentWalkSpeed;
        var boostedSprintSpeed = moveComp.CurrentSprintSpeed;

        Assert.That(boostedWalkSpeed, Is.GreaterThan(baseWalkSpeed), 
            "Walk speed should be boosted without caps");
        Assert.That(boostedSprintSpeed, Is.GreaterThan(baseSprintSpeed), 
            "Sprint speed should be boosted without caps");

        // Verify the boost is approximately correct (1.5x)
        Assert.That(boostedWalkSpeed, Is.EqualTo(baseWalkSpeed * 1.5f).Within(0.01f), 
            "Walk speed boost should be approximately 1.5x");
        Assert.That(boostedSprintSpeed, Is.EqualTo(baseSprintSpeed * 1.5f).Within(0.01f), 
            "Sprint speed boost should be approximately 1.5x");
    }
}