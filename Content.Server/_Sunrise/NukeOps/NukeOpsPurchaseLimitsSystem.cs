using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using System.Linq;

namespace Content.Server._Sunrise.NukeOps;

/// <summary>
/// System that handles purchase limits for nuclear operative uplinks
/// </summary>
public sealed class NukeOpsPurchaseLimitsSystem : EntitySystem
{
    [Dependency] private readonly StoreSystem _store = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeOpsPurchaseLimitsComponent, StoreBuyListingMessage>(OnBuyListingMessage);
    }

    private void OnBuyListingMessage(EntityUid uid, NukeOpsPurchaseLimitsComponent component, StoreBuyListingMessage msg)
    {
        // Find the listing being purchased
        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        var listing = store.FullListingsCatalog.FirstOrDefault(x => x.ID.Equals(msg.Listing.Id));
        if (listing == null)
            return;

        var listingProto = listing.ID;
        
        // Check current purchases
        var currentPurchases = component.PurchasedItems.GetValueOrDefault(listingProto, 0);
        var limit = component.ItemLimits.GetValueOrDefault(listingProto, component.DefaultLimit);

        if (currentPurchases >= limit)
        {
            // Cancel the purchase by not allowing it to proceed
            // We'll send a popup message to the buyer
            // TODO: Add popup system integration
            return;
        }

        // Increment purchase count for tracking
        if (component.PurchasedItems.ContainsKey(listingProto))
        {
            component.PurchasedItems[listingProto]++;
        }
        else
        {
            component.PurchasedItems[listingProto] = 1;
        }
    }
}