using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.NukeOps;

/// <summary>
/// Component that tracks purchase limits for nuclear operative uplinks
/// </summary>
[RegisterComponent]
public sealed partial class NukeOpsPurchaseLimitsComponent : Component
{
    /// <summary>
    /// Dictionary tracking how many of each item has been purchased
    /// Key: prototype ID, Value: quantity purchased
    /// </summary>
    [DataField]
    public Dictionary<string, int> PurchasedItems = new();

    /// <summary>
    /// Maximum allowed purchases per item type
    /// </summary>
    [DataField]
    public Dictionary<string, int> ItemLimits = new()
    {
        // Examples - these should be configured per item
        ["C4"] = 3,
        ["ExGrenade"] = 2,
        ["ReinforcementRadioSyndicate"] = 1,
        ["DonkPocketBoxOfTricks"] = 1,
        ["BorgModuleSyndicate"] = 2
    };

    /// <summary>
    /// Default limit for items not specifically configured
    /// </summary>
    [DataField]
    public int DefaultLimit = 5;
}