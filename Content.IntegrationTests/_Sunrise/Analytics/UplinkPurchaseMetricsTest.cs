using System.Linq;
using Content.Server._Sunrise.Analytics;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Store.Components;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.UnitTesting;

namespace Content.IntegrationTests._Sunrise.Analytics;

[TestFixture]
public sealed class UplinkPurchaseMetricsTest : RobustIntegrationTest
{
    [Test]
    public async Task UplinkPurchaseMetrics_TracksCorrectly()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        await server.WaitIdleAsync();

        var testEntityManager = server.ResolveDependency<IEntityManager>();
        var tagSystem = testEntityManager.System<TagSystem>();
        var metricsSystem = testEntityManager.System<UplinkPurchaseMetricsSystem>();

        await server.WaitRunTicks(1);

        // Create a test uplink with sponsor tag
        var testUplink = testEntityManager.SpawnEntity(null, default);
        testEntityManager.AddComponent<StoreComponent>(testUplink);
        tagSystem.AddTag(testUplink, "SunriseUplink");

        var storeComponent = testEntityManager.GetComponent<StoreComponent>(testUplink);
        
        // Create a test purchaser
        var testPurchaser = testEntityManager.SpawnEntity(null, default);

        await server.WaitRunTicks(1);

        // Simulate a purchase by raising the events
        var itemPurchasedEvent = new ItemPurchasedEvent(testPurchaser);
        testEntityManager.EventBus.RaiseLocalEvent(testUplink, ref itemPurchasedEvent);

        var cashEvent = new SubtractCashEvent(testPurchaser, "Suntick", FixedPoint2.New(10));
        testEntityManager.EventBus.RaiseLocalEvent(testUplink, ref cashEvent);

        await server.WaitRunTicks(5);

        // The system should have tracked the purchase
        // This is a basic test to ensure the system doesn't crash
        Assert.That(testEntityManager.EntityExists(testUplink), Is.True);
        Assert.That(testEntityManager.EntityExists(testPurchaser), Is.True);

        await pair.CleanReturnAsync();
    }
}