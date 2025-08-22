using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Server.Silicons.Borgs;

/// <summary>
/// This system handles energy consumption for borg-held weapons and equipment.
/// It disables self-recharging batteries when weapons are held by borgs and
/// routes energy consumption to the borg's main battery instead.
/// </summary>
public sealed class BorgEnergySystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        // Listen for when weapons are fired to handle energy consumption
        // We listen on any entity that has a battery, then check if it's borg-held
        SubscribeLocalEvent<BatteryComponent, GunShotEvent>(OnWeaponFired);
        SubscribeLocalEvent<BatteryComponent, OnEmptyGunShotEvent>(OnWeaponEmptyFired);
        
        // Listen for module changes to manage battery behavior
        SubscribeLocalEvent<ItemBorgModuleComponent, BorgModuleSelectedEvent>(OnBorgModuleSelected);
        SubscribeLocalEvent<ItemBorgModuleComponent, BorgModuleUnselectedEvent>(OnBorgModuleUnselected);
    }

    /// <summary>
    /// When a weapon with a battery is fired, check if it's borg-held and consume borg energy
    /// </summary>
    private void OnWeaponFired(EntityUid weaponUid, BatteryComponent weaponBattery, ref GunShotEvent args)
    {
        if (!TryGetBorgForWeapon(weaponUid, out var borgUid))
            return;

        // Restore the weapon's battery charge and consume from borg instead
        TryConsumeBorgEnergyForWeapon(borgUid, weaponUid, weaponBattery);
    }

    /// <summary>
    /// Handle empty shot events similarly
    /// </summary>
    private void OnWeaponEmptyFired(EntityUid weaponUid, BatteryComponent weaponBattery, ref OnEmptyGunShotEvent args)
    {
        if (!TryGetBorgForWeapon(weaponUid, out var borgUid))
            return;

        // Still try to charge weapon battery from borg for next shot
        TryConsumeBorgEnergyForWeapon(borgUid, weaponUid, weaponBattery);
    }

    /// <summary>
    /// When a borg module is selected, disable self-recharging for any weapons in that module
    /// </summary>
    private void OnBorgModuleSelected(EntityUid moduleUid, ItemBorgModuleComponent component, ref BorgModuleSelectedEvent args)
    {
        DisableSelfRechargingForModuleWeapons(moduleUid, component, disable: true);
    }

    /// <summary>
    /// When a borg module is unselected, re-enable self-recharging for weapons
    /// </summary>
    private void OnBorgModuleUnselected(EntityUid moduleUid, ItemBorgModuleComponent component, ref BorgModuleUnselectedEvent args)
    {
        DisableSelfRechargingForModuleWeapons(moduleUid, component, disable: false);
    }

    /// <summary>
    /// Try to get the borg that owns this weapon
    /// </summary>
    private bool TryGetBorgForWeapon(EntityUid weaponUid, out EntityUid borgUid)
    {
        borgUid = EntityUid.Invalid;

        // Check if this weapon is being held by the borg
        if (!_container.TryGetOuterContainer(weaponUid, Transform(weaponUid), out var container))
            return false;

        // Walk up the container hierarchy to see if it leads to a borg
        var current = container.Owner;
        while (current != EntityUid.Invalid)
        {
            if (HasComp<BorgChassisComponent>(current))
            {
                borgUid = current;
                return true;
            }

            if (!_container.TryGetOuterContainer(current, Transform(current), out container))
                break;
            
            current = container.Owner;
        }

        return false;
    }

    /// <summary>
    /// Consume energy from the borg's battery to recharge a weapon
    /// </summary>
    private void TryConsumeBorgEnergyForWeapon(EntityUid borgUid, EntityUid weaponUid, BatteryComponent weaponBattery)
    {
        if (!_powerCell.TryGetBatteryFromSlot(borgUid, out var borgBatteryUid, out var borgBattery))
            return;

        // Calculate how much charge the weapon needs
        var chargeDelta = weaponBattery.MaxCharge - weaponBattery.CurrentCharge;
        
        if (chargeDelta <= 0)
            return;

        // Try to consume this energy from the borg's battery
        if (_battery.TryUseCharge(borgBatteryUid.Value, chargeDelta, borgBattery))
        {
            // Successfully consumed from borg battery, restore weapon's charge
            _battery.SetCharge(weaponUid, weaponBattery.MaxCharge, weaponBattery);
        }
        // If borg doesn't have enough energy, leave weapon battery as-is (empty)
    }

    /// <summary>
    /// Enable or disable self-recharging for weapons in a borg module
    /// </summary>
    private void DisableSelfRechargingForModuleWeapons(EntityUid moduleUid, ItemBorgModuleComponent component, bool disable)
    {
        if (!_container.TryGetContainer(moduleUid, component.HoldingContainer, out var container))
            return;

        foreach (var containedUid in container.ContainedEntities)
        {
            if (TryComp<BatterySelfRechargerComponent>(containedUid, out var selfRecharger))
            {
                // Store original auto-recharge state if we're disabling for the first time
                if (disable && selfRecharger.AutoRecharge)
                {
                    // Disable auto-recharge for borg-held weapons
                    selfRecharger.AutoRecharge = false;
                }
                else if (!disable)
                {
                    // Re-enable auto-recharge when weapon is no longer borg-held
                    // Note: This is a simple approach. In practice, we might want to store
                    // the original state more carefully
                    selfRecharger.AutoRecharge = true;
                }
            }
        }
    }
}