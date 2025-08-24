using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Server._Sunrise.EnergyShield;
using Content.Shared.Damage;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Examine;
using Content.Shared.Timing;
using Content.Shared.IdentityManagement;
using Content.Shared.PowerCell.Components;
using Content.Shared.Inventory;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Containers;

namespace Content.Server._Sunrise.EnergyShield;

public sealed class EnergyShieldSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EnergyShieldComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<EnergyShieldComponent, ItemToggleActivateAttemptEvent>(OnToggleAttempt);
        
        // Listen for when entities are inserted into containers to auto-deactivate shields
        SubscribeLocalEvent<EntInsertedIntoContainerMessage>(OnEntityInsertedIntoContainer);
    }

    private void OnDamage(Entity<EnergyShieldComponent> ent, ref DamageChangedEvent args)
    {
        if (!_itemToggle.IsActivated(ent.Owner))
            return;

        if (args.DamageDelta == null)
            return;

        if (!TryComp<BatteryComponent>(ent, out var battery))
            return;

        var totalDamage = args.DamageDelta.GetTotal();
        if (totalDamage <= 0)
            return;

        var cost = totalDamage.Float() * ent.Comp.EnergyCostPerDamage;
        _battery.UseCharge(ent, cost, battery);
        _audio.PlayPvs(ent.Comp.AbsorbSound, ent);

        if (battery.CurrentCharge <= 0)
        {
            _itemToggle.Toggle(ent.Owner);
            _audio.PlayPvs(ent.Comp.ShutdownSound, ent);
        }
    }

    private void OnToggleAttempt(Entity<EnergyShieldComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        // Prevent activation while inside a container (e.g., mech, locker)
        if (Exists(args.User) && _container.IsEntityInContainer(args.User.Value))
        {
            _popup.PopupEntity(
                Loc.GetString("energy-shield-cant-activate-in-container"),
                args.User.Value,
                args.User.Value,
                PopupType.Medium
            );
            args.Cancelled = true;
            return;
        }

        if (TryComp<BatteryComponent>(ent, out var battery) &&
            battery.CurrentCharge >= battery.MaxCharge * ent.Comp.MinChargeFractionForActivation)
        {
            return;
        }

        if (Exists(args.User))
        {
            _popup.PopupEntity(
                Loc.GetString("stunbaton-component-low-charge"),
                args.User.Value,
                args.User.Value,
                PopupType.Small
            );
        }

        args.Cancelled = true;
    }

    private void OnEntityInsertedIntoContainer(EntInsertedIntoContainerMessage args)
    {
        // Check if the entity being inserted into a container has any active energy shields
        var entity = args.Entity;
        
        // Get all equipped energy shields on the entity
        if (!TryComp<InventoryComponent>(entity, out var inventory))
            return;

        // Check all inventory slots for active energy shields
        if (_inventory.TryGetContainerSlotEnumerator(entity, out var slotEnumerator))
        {
            while (slotEnumerator.NextItem(out var itemEntity, out var slot))
            {
                if (!TryComp<EnergyShieldComponent>(itemEntity, out var shieldComp))
                    continue;

                // If the shield is active, deactivate it
                if (_itemToggle.IsActivated(itemEntity))
                {
                    _itemToggle.Toggle(itemEntity);
                    _popup.PopupEntity(
                        Loc.GetString("energy-shield-deactivated-in-container"),
                        entity,
                        entity,
                        PopupType.Medium
                    );
                    _audio.PlayPvs(shieldComp.ShutdownSound, itemEntity);
                }
            }
        }
    }
}
