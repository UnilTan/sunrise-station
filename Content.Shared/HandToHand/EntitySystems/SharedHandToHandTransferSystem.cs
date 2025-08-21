using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.HandToHand.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared.HandToHand.EntitySystems;

/// <summary>
/// System that handles hand-to-hand item transfers between entities.
/// </summary>
public sealed class SharedHandToHandTransferSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandToHandTransferComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<HandToHandTransferComponent, HandToHandTransferDoAfterEvent>(OnTransferDoAfter);
    }

    private void OnGetVerbs(Entity<HandToHandTransferComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        var user = args.User;
        var target = args.Target;

        // Don't show verb if user can't give items
        if (!ent.Comp.CanGiveItems)
            return;

        // Don't show verb if target can't receive items
        if (!TryComp<HandToHandTransferComponent>(target, out var targetComp) || !targetComp.CanReceiveItems)
            return;

        // Check if user has hands and is holding something
        if (!TryComp<HandsComponent>(user, out var userHands) || 
            !_hands.TryGetActiveItem((user, userHands), out var heldItem))
            return;

        // Check if target has hands to receive the item
        if (!TryComp<HandsComponent>(target, out var targetHands))
            return;

        // Check if target has any free hands
        if (_hands.CountFreeHands((target, targetHands)) == 0)
            return;

        // Check if user can interact
        if (!_actionBlocker.CanInteract(user, target))
            return;

        // Check range
        var userTransform = Transform(user);
        var targetTransform = Transform(target);
        if (!userTransform.Coordinates.TryDistance(EntityManager, targetTransform.Coordinates, out var distance) ||
            distance > ent.Comp.TransferRange)
            return;

        // Check if held item can be transferred
        if (!TryComp<ItemComponent>(heldItem, out _))
            return;

        // Create the verb
        var verb = new InteractionVerb
        {
            Text = Loc.GetString("hand-to-hand-transfer-verb"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/in.svg.192dpi.png")),
            Act = () => StartTransfer(user, target, heldItem.Value),
            Priority = 1
        };

        args.Verbs.Add(verb);
    }

    private void StartTransfer(EntityUid giver, EntityUid receiver, EntityUid item)
    {
        // Final validation before starting DoAfter
        if (!CanTransferItem(giver, receiver, item))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, giver, TimeSpan.FromSeconds(1.5), 
            new HandToHandTransferDoAfterEvent(GetNetEntity(item), GetNetEntity(receiver)), 
            giver, receiver, item)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            RequireCanInteract = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

        // Show popup that transfer is starting
        var giverName = Identity.Entity(giver, EntityManager);
        var receiverName = Identity.Entity(receiver, EntityManager);
        var itemName = MetaData(item).EntityName;

        _popup.PopupPredicted(
            Loc.GetString("hand-to-hand-transfer-start-giver", ("receiver", receiverName), ("item", itemName)),
            giver, giver, PopupType.Medium);

        _popup.PopupPredicted(
            Loc.GetString("hand-to-hand-transfer-start-receiver", ("giver", giverName), ("item", itemName)),
            receiver, receiver, PopupType.Medium);
    }

    private void OnTransferDoAfter(Entity<HandToHandTransferComponent> ent, ref HandToHandTransferDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var giver = args.User;
        var receiver = GetEntity(args.Receiver);
        var item = GetEntity(args.ItemToTransfer);

        // Validate the transfer can still happen
        if (!CanTransferItem(giver, receiver, item))
        {
            _popup.PopupEntity(Loc.GetString("hand-to-hand-transfer-failed"), giver, giver, PopupType.Medium);
            return;
        }

        // Check if giver is still holding the item
        if (!TryComp<HandsComponent>(giver, out var giverHands) || 
            !_hands.IsHolding((giver, giverHands), item))
        {
            _popup.PopupEntity(Loc.GetString("hand-to-hand-transfer-no-longer-holding"), giver, giver, PopupType.Medium);
            return;
        }

        // Check if receiver still has free hands
        if (!TryComp<HandsComponent>(receiver, out var receiverHands) || 
            _hands.CountFreeHands((receiver, receiverHands)) == 0)
        {
            var receiverName2 = Identity.Entity(receiver, EntityManager);
            _popup.PopupEntity(Loc.GetString("hand-to-hand-transfer-no-free-hands", ("receiver", receiverName2)), giver, giver, PopupType.Medium);
            return;
        }

        // Perform the transfer
        if (!_hands.TryDrop((giver, giverHands), item))
        {
            _popup.PopupEntity(Loc.GetString("hand-to-hand-transfer-failed"), giver, giver, PopupType.Medium);
            return;
        }

        if (!_hands.TryPickupAnyHand(receiver, item))
        {
            // If pickup failed, try to put the item back in giver's hands
            _hands.TryPickupAnyHand(giver, item);
            _popup.PopupEntity(Loc.GetString("hand-to-hand-transfer-failed"), giver, giver, PopupType.Medium);
            return;
        }

        // Success! Show confirmation popups
        var giverName = Identity.Entity(giver, EntityManager);
        var receiverName = Identity.Entity(receiver, EntityManager);
        var itemName = MetaData(item).EntityName;

        _popup.PopupEntity(
            Loc.GetString("hand-to-hand-transfer-success-giver", ("receiver", receiverName), ("item", itemName)),
            giver, giver, PopupType.Medium);

        _popup.PopupEntity(
            Loc.GetString("hand-to-hand-transfer-success-receiver", ("giver", giverName), ("item", itemName)),
            receiver, receiver, PopupType.Medium);
    }

    private bool CanTransferItem(EntityUid giver, EntityUid receiver, EntityUid item)
    {
        // Check both entities exist
        if (!Exists(giver) || !Exists(receiver) || !Exists(item))
            return false;

        // Check both have HandToHandTransferComponent
        if (!TryComp<HandToHandTransferComponent>(giver, out var giverComp) || !giverComp.CanGiveItems ||
            !TryComp<HandToHandTransferComponent>(receiver, out var receiverComp) || !receiverComp.CanReceiveItems)
            return false;

        // Check both have hands
        if (!HasComp<HandsComponent>(giver) || !HasComp<HandsComponent>(receiver))
            return false;

        // Check if item can be transferred
        if (!TryComp<ItemComponent>(item, out _))
            return false;

        // Check if entities can interact
        if (!_actionBlocker.CanInteract(giver, receiver))
            return false;

        // Check range
        var giverTransform = Transform(giver);
        var receiverTransform = Transform(receiver);
        if (!giverTransform.Coordinates.TryDistance(EntityManager, receiverTransform.Coordinates, out var distance) ||
            distance > giverComp.TransferRange)
            return false;

        return true;
    }
}