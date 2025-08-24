using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing;

public sealed class ClothingSpeedCapSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingSpeedCapComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ClothingSpeedCapComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<ClothingSpeedCapComponent, GetVerbsEvent<ExamineVerb>>(OnClothingVerbExamine);
        SubscribeLocalEvent<ClothingSpeedCapComponent, ItemToggledEvent>(OnToggled);

        // Subscribe to the movement speed refresh event to apply caps after all other modifiers
        SubscribeLocalEvent<MovementSpeedModifierComponent, RefreshMovementSpeedModifiersEvent>(OnApplySpeedCaps);
    }

    private void OnGetState(EntityUid uid, ClothingSpeedCapComponent component, ref ComponentGetState args)
    {
        args.State = new ClothingSpeedCapComponentState(component.MaxWalkSpeed, component.MaxSprintSpeed, component.Standing);
    }

    private void OnHandleState(EntityUid uid, ClothingSpeedCapComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ClothingSpeedCapComponentState state)
            return;

        var diff = component.MaxWalkSpeed != state.MaxWalkSpeed ||
                   component.MaxSprintSpeed != state.MaxSprintSpeed ||
                   component.Standing != state.Standing;

        component.MaxWalkSpeed = state.MaxWalkSpeed;
        component.MaxSprintSpeed = state.MaxSprintSpeed;
        component.Standing = state.Standing;

        // Avoid raising the event for the container if nothing changed.
        if (diff && _container.TryGetContainingContainer((uid, null, null), out var container))
        {
            _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
        }
    }

    private void OnApplySpeedCaps(EntityUid uid, MovementSpeedModifierComponent moveComp, RefreshMovementSpeedModifiersEvent args)
    {
        // Find the lowest speed caps from all equipped clothing
        float? maxWalkSpeed = null;
        float? maxSprintSpeed = null;

        // Check all clothing slots for speed caps
        if (TryComp<InventoryComponent>(uid, out var inventory))
        {
            foreach (var slot in inventory.Containers)
            {
                if (slot.ContainedEntity == null)
                    continue;

                var item = slot.ContainedEntity.Value;
                
                if (!TryComp<ClothingSpeedCapComponent>(item, out var capComp))
                    continue;

                // Check if the item is activated (for toggleable items)
                if (HasComp<ItemToggleComponent>(item) && !_toggle.IsActivated(item))
                    continue;

                // Check standing state requirement
                if (capComp.Standing != null && !_standing.IsMatchingState(uid, capComp.Standing.Value))
                    continue;

                // Apply the most restrictive caps
                if (capComp.MaxWalkSpeed.HasValue)
                    maxWalkSpeed = maxWalkSpeed.HasValue ? Math.Min(maxWalkSpeed.Value, capComp.MaxWalkSpeed.Value) : capComp.MaxWalkSpeed.Value;

                if (capComp.MaxSprintSpeed.HasValue)
                    maxSprintSpeed = maxSprintSpeed.HasValue ? Math.Min(maxSprintSpeed.Value, capComp.MaxSprintSpeed.Value) : capComp.MaxSprintSpeed.Value;
            }
        }

        // Apply speed caps if they would limit the current speed
        if (maxWalkSpeed.HasValue)
        {
            var currentWalkSpeed = args.WalkSpeedModifier * moveComp.BaseWalkSpeed;
            if (currentWalkSpeed > maxWalkSpeed.Value)
            {
                var cappedWalkModifier = maxWalkSpeed.Value / moveComp.BaseWalkSpeed;
                args.ModifySpeed(cappedWalkModifier / args.WalkSpeedModifier, 1.0f);
            }
        }

        if (maxSprintSpeed.HasValue)
        {
            var currentSprintSpeed = args.SprintSpeedModifier * moveComp.BaseSprintSpeed;
            if (currentSprintSpeed > maxSprintSpeed.Value)
            {
                var cappedSprintModifier = maxSprintSpeed.Value / moveComp.BaseSprintSpeed;
                args.ModifySpeed(1.0f, cappedSprintModifier / args.SprintSpeedModifier);
            }
        }
    }

    private void OnClothingVerbExamine(EntityUid uid, ClothingSpeedCapComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (component.MaxWalkSpeed == null && component.MaxSprintSpeed == null)
            return;

        var msg = new FormattedMessage();

        if (component.MaxWalkSpeed.HasValue && component.MaxSprintSpeed.HasValue)
        {
            if (MathHelper.CloseTo(component.MaxWalkSpeed.Value, component.MaxSprintSpeed.Value, 0.1f))
            {
                msg.AddMarkupOrThrow(Loc.GetString("clothing-speed-cap-equal-examine", 
                    ("speed", component.MaxWalkSpeed.Value.ToString("F1"))));
            }
            else
            {
                msg.AddMarkupOrThrow(Loc.GetString("clothing-speed-cap-different-examine", 
                    ("walkSpeed", component.MaxWalkSpeed.Value.ToString("F1")),
                    ("runSpeed", component.MaxSprintSpeed.Value.ToString("F1"))));
            }
        }
        else if (component.MaxWalkSpeed.HasValue)
        {
            msg.AddMarkupOrThrow(Loc.GetString("clothing-speed-cap-walk-examine", 
                ("walkSpeed", component.MaxWalkSpeed.Value.ToString("F1"))));
        }
        else if (component.MaxSprintSpeed.HasValue)
        {
            msg.AddMarkupOrThrow(Loc.GetString("clothing-speed-cap-run-examine", 
                ("runSpeed", component.MaxSprintSpeed.Value.ToString("F1"))));
        }

        _examine.AddDetailedExamineVerb(args, component, msg, 
            Loc.GetString("clothing-speed-cap-examinable-verb-text"), 
            "/Textures/Interface/VerbIcons/outfit.svg.192dpi.png", 
            Loc.GetString("clothing-speed-cap-examinable-verb-message"));
    }

    private void OnToggled(Entity<ClothingSpeedCapComponent> ent, ref ItemToggledEvent args)
    {
        // make sentient boots apply/remove speed caps too
        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        if (_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
        {
            // inventory system will automatically hook into the event raised by this and update accordingly
            _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
        }
    }
}

