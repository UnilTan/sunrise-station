using System.Diagnostics.Metrics;
using System.Linq;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Store.Components;
using Content.Shared.Tag;
using Robust.Server.DataMetrics;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Analytics;

/// <summary>
/// System for tracking uplink purchases and exporting metrics to Prometheus/Grafana
/// </summary>
public sealed class UplinkPurchaseMetricsSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IMetricsManager _metrics = default!;
    [Dependency] private readonly IMeterFactory _meterFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private ISawmill _sawmill = default!;

    private readonly Dictionary<string, long> _purchaseCounts = new();
    private readonly Dictionary<string, double> _purchaseCosts = new();

    // Tag mappings for different uplink types
    private readonly Dictionary<string, string> _uplinkTags = new()
    {
        { "SunriseUplink", "sponsor" },
        { "AssaultOpsUplink", "erd" },
        { "NukeOpsUplink", "nuclear" },
        { "SyndieAgentUplink", "traitor" },
        { "LoneOpsUplink", "traitor" },
        { "FugitiveUplink", "fugitive" }
    };

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("uplink.purchases");

        // Subscribe to purchase events from the store system
        SubscribeLocalEvent<StoreComponent, ItemPurchasedEvent>(OnItemPurchased);
        SubscribeLocalEvent<StoreComponent, SubtractCashEvent>(OnCashSubtracted);

        InitializeMetrics();
    }

    private void InitializeMetrics()
    {
        _metrics.UpdateMetrics += UpdateMetrics;

        var meter = _meterFactory.Create("SS14.UplinkPurchases");

        // Counter for number of purchases
        meter.CreateObservableCounter(
            "uplink_purchases_total",
            () => GetPurchaseCountMeasurements(),
            null,
            "Total number of uplink purchases");

        // Gauge for total currency spent
        meter.CreateObservableGauge(
            "uplink_currency_spent_total",
            () => GetCurrencySpentMeasurements(),
            null,
            "Total currency spent on uplink purchases");
    }

    private void OnItemPurchased(EntityUid uid, StoreComponent component, ref ItemPurchasedEvent args)
    {
        var uplinkType = GetUplinkType(uid);
        if (uplinkType == null)
            return;

        // Get item prototype name for better tracking
        var itemName = "unknown";
        if (TryComp<MetaDataComponent>(args.Purchaser, out var metaData))
        {
            itemName = metaData.EntityPrototype?.ID ?? "unknown";
        }

        var key = $"{uplinkType}|{itemName}";
        _purchaseCounts.TryGetValue(key, out var currentCount);
        _purchaseCounts[key] = currentCount + 1;

        _sawmill.Info($"Uplink purchase tracked: {uplinkType} - {itemName} (Total: {_purchaseCounts[key]})");
    }

    private void OnCashSubtracted(EntityUid uid, StoreComponent component, ref SubtractCashEvent args)
    {
        var uplinkType = GetUplinkType(uid);
        if (uplinkType == null)
            return;

        var key = $"{uplinkType}|{args.Currency}";
        _purchaseCosts.TryGetValue(key, out var currentCost);
        _purchaseCosts[key] = currentCost + (double)args.Cost;

        _sawmill.Info($"Uplink currency spent tracked: {uplinkType} - {args.Currency}: {args.Cost} (Total: {_purchaseCosts[key]})");
    }

    private string? GetUplinkType(EntityUid storeEntity)
    {
        // Check if this store entity has any of our known uplink tags
        foreach (var (tag, type) in _uplinkTags)
        {
            if (_tagSystem.HasTag(storeEntity, tag))
            {
                return type;
            }
        }

        // For traitor uplinks that might not have specific tags,
        // check if it's a regular traitor uplink by checking for telecrystal currency
        if (TryComp<StoreComponent>(storeEntity, out var store))
        {
            if (store.CurrencyWhitelist.Any(c => c.Id == "Telecrystal") && 
                !_uplinkTags.Keys.Any(tag => _tagSystem.HasTag(storeEntity, tag)))
            {
                return "traitor";
            }
        }

        // Check for cult uplinks by checking for specific cult currencies
        if (TryComp<StoreComponent>(storeEntity, out var cultStore))
        {
            if (cultStore.CurrencyWhitelist.Any(c => c.Id.Contains("Cult") || c.Id.Contains("Soul")))
            {
                return "cult";
            }
        }

        return null; // Not an uplink we're tracking
    }

    private void UpdateMetrics()
    {
        // This method is called periodically by the metrics manager
        // The actual measurements are provided by the observable meters
    }

    private IEnumerable<Measurement<long>> GetPurchaseCountMeasurements()
    {
        foreach (var (key, count) in _purchaseCounts)
        {
            var parts = key.Split('|');
            if (parts.Length != 2) continue;

            var uplinkType = parts[0];
            var itemName = parts[1];

            yield return new Measurement<long>(
                count,
                new KeyValuePair<string, object?>("uplink_type", uplinkType),
                new KeyValuePair<string, object?>("item", itemName));
        }
    }

    private IEnumerable<Measurement<double>> GetCurrencySpentMeasurements()
    {
        foreach (var (key, cost) in _purchaseCosts)
        {
            var parts = key.Split('|');
            if (parts.Length != 2) continue;

            var uplinkType = parts[0];
            var currency = parts[1];

            yield return new Measurement<double>(
                cost,
                new KeyValuePair<string, object?>("uplink_type", uplinkType),
                new KeyValuePair<string, object?>("currency", currency));
        }
    }
}