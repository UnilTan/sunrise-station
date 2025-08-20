using Content.Server.Ninja.Events;
using Content.Server.Power.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Server system for ninja equipment that draws power from the ninja suit's battery.
/// Handles the actual power draw logic using the ninja suit's battery.
/// </summary>
public sealed class NinjaSuitDrawSystem : SharedNinjaSuitDrawSystem
{
    [Dependency] private readonly SpaceNinjaSystem _ninja = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitDrawComponent, NinjaBatteryChangedEvent>(OnBatteryChanged);
        SubscribeLocalEvent<ToggleNinjaSuitDrawComponent, NinjaSuitPowerEmptyEvent>(OnPowerEmpty);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NinjaSuitDrawComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Enabled)
                continue;

            if (Timing.CurTime < comp.NextUpdateTime)
                continue;

            comp.NextUpdateTime += comp.Delay;

            // Get the user wearing this equipment
            var user = Transform(uid).ParentUid;
            if (!_ninja.IsNinja(user))
                continue;

            // Try to draw power from the ninja suit
            if (_ninja.GetNinjaBattery(user, out var batteryEnt, out var battery))
            {
                var powerToDraw = comp.DrawRate * (float)comp.Delay.TotalSeconds;
                _ninja.TryUseCharge(user, powerToDraw);
            }
            else
            {
                // No ninja suit battery available
                var ev = new NinjaSuitPowerEmptyEvent();
                RaiseLocalEvent(uid, ref ev);
            }
        }
    }

    private void OnBatteryChanged(Entity<NinjaSuitDrawComponent> ent, ref NinjaBatteryChangedEvent args)
    {
        UpdatePowerStatus(ent);
    }

    private void OnPowerEmpty(Entity<ToggleNinjaSuitDrawComponent> ent, ref NinjaSuitPowerEmptyEvent args)
    {
        _toggle.TryDeactivate(ent.Owner);
    }

    /// <summary>
    /// Update the power status based on current battery state.
    /// </summary>
    private void UpdatePowerStatus(Entity<NinjaSuitDrawComponent> ent)
    {
        var user = Transform(ent).ParentUid;
        if (!_ninja.IsNinja(user))
        {
            SetPowerStatus(ent, false, false);
            return;
        }

        if (_ninja.GetNinjaBattery(user, out _, out var battery))
        {
            var canUse = ent.Comp.UseRate <= 0f || battery.CurrentCharge >= ent.Comp.UseRate;
            var canDraw = ent.Comp.DrawRate <= 0f || battery.CurrentCharge > 0f;
            SetPowerStatus(ent, canDraw, canUse);
        }
        else
        {
            SetPowerStatus(ent, false, false);
        }
    }

    /// <summary>
    /// Set the power status flags and dirty the component if changed.
    /// </summary>
    private void SetPowerStatus(Entity<NinjaSuitDrawComponent> ent, bool canDraw, bool canUse)
    {
        if (ent.Comp.CanDraw != canDraw || ent.Comp.CanUse != canUse)
        {
            ent.Comp.CanDraw = canDraw;
            ent.Comp.CanUse = canUse;
            Dirty(ent, ent.Comp);
        }
    }

    public override bool CanDrawPower(Entity<NinjaSuitDrawComponent> ent)
    {
        var user = Transform(ent).ParentUid;
        if (!_ninja.IsNinja(user))
            return false;

        return _ninja.GetNinjaBattery(user, out _, out var battery) && battery.CurrentCharge > 0f;
    }

    public override bool CanUse(Entity<NinjaSuitDrawComponent> ent)
    {
        var user = Transform(ent).ParentUid;
        if (!_ninja.IsNinja(user))
            return false;

        return _ninja.GetNinjaBattery(user, out _, out var battery) && 
               (ent.Comp.UseRate <= 0f || battery.CurrentCharge >= ent.Comp.UseRate);
    }
}

/// <summary>
/// Event raised when ninja suit power is empty for equipment that depends on it.
/// </summary>
public sealed record struct NinjaSuitPowerEmptyEvent;