using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared._Sunrise.Medical.ALV;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Medical.ALV;

/// <summary>
/// Handles artificial lung ventilation (ALV) procedures
/// Система искусственной вентиляции легких (ИВЛ)
/// </summary>
public sealed class ALVSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, GetVerbsEvent<InteractionVerb>>(AddALVVerb);
        SubscribeLocalEvent<ALVComponent, ALVDoAfterEvent>(OnALVDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ALVComponent>();
        
        while (query.MoveNext(out var uid, out var alvComp))
        {
            if (alvComp.Performer == null || alvComp.Target == null)
            {
                RemCompDeferred<ALVComponent>(uid);
                continue;
            }

            // Check if target still needs ALV
            if (!NeedsALV(alvComp.Target.Value))
            {
                StopALV(uid, alvComp);
                continue;
            }

            // Check if performer is still valid
            if (!CanPerformALV(alvComp.Performer.Value, alvComp.Target.Value))
            {
                StopALV(uid, alvComp);
                continue;
            }

            // Apply healing and damage effects at intervals
            if (currentTime >= alvComp.LastEffectTime + TimeSpan.FromSeconds(alvComp.EffectInterval))
            {
                ApplyALVEffects(alvComp.Target.Value, alvComp);
                alvComp.LastEffectTime = currentTime;
            }
        }
    }

    private void AddALVVerb(EntityUid uid, MobStateComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (args.User == uid) // Can't perform ALV on yourself
            return;

        if (!NeedsALV(uid))
            return;

        if (!CanPerformALV(args.User, uid))
            return;

        InteractionVerb verb = new()
        {
            Act = () => TryStartALV(args.User, uid),
            Text = Loc.GetString("alv-verb-start"),
            Category = VerbCategory.Interaction,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/heal.svg.192dpi.png")),
            Priority = 2
        };

        args.Verbs.Add(verb);
    }

    private bool NeedsALV(EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var damageable))
            return false;

        // Check if target has asphyxiation damage
        return damageable.Damage.DamageDict.TryGetValue("Asphyxiation", out var asphyxiationDamage) 
               && asphyxiationDamage > 0;
    }

    private bool CanPerformALV(EntityUid performer, EntityUid target)
    {
        // Check if target is not wearing masks or helmets
        return !_inventory.TryGetSlotEntity(target, "head", out _) &&
               !_inventory.TryGetSlotEntity(target, "mask", out _);
    }

    private void TryStartALV(EntityUid performer, EntityUid target)
    {
        if (HasComp<ALVComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("alv-already-being-performed"), target, performer);
            return;
        }

        if (!CanPerformALV(performer, target))
        {
            _popup.PopupEntity(Loc.GetString("alv-blocked-by-equipment"), target, performer);
            return;
        }

        var alvComp = AddComp<ALVComponent>(target);
        alvComp.Performer = performer;
        alvComp.Target = target;
        alvComp.LastEffectTime = _timing.CurTime;

        _popup.PopupEntity(Loc.GetString("alv-start-self", ("target", target)), performer, performer);
        _popup.PopupEntity(Loc.GetString("alv-start-others", ("performer", performer), ("target", target)), 
                          performer, Filter.PvsExcept(performer), true);

        // Start a long DoAfter that represents the ALV procedure
        var doAfterArgs = new DoAfterArgs(EntityManager, performer, TimeSpan.FromSeconds(30.0f), 
            new ALVDoAfterEvent(), target, target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            CancelDuplicate = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

        if (alvComp.ALVSound != null)
        {
            _audio.PlayPvs(alvComp.ALVSound, target);
        }
    }

    private void OnALVDoAfter(EntityUid uid, ALVComponent component, ALVDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
        {
            StopALV(uid, component);
            return;
        }

        args.Handled = true;

        if (component.Performer == null || component.Target == null)
        {
            StopALV(uid, component);
            return;
        }

        // ALV procedure completed normally, check if we need to continue
        if (NeedsALV(component.Target.Value) && CanPerformALV(component.Performer.Value, component.Target.Value))
        {
            // Restart ALV procedure if still needed
            var doAfterArgs = new DoAfterArgs(EntityManager, component.Performer.Value, TimeSpan.FromSeconds(30.0f), 
                new ALVDoAfterEvent(), component.Target.Value, target: component.Target.Value)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
                CancelDuplicate = false,
            };

            _doAfter.TryStartDoAfter(doAfterArgs);
        }
        else
        {
            StopALV(uid, component);
        }
    }

    private void ApplyALVEffects(EntityUid target, ALVComponent component)
    {
        if (!TryComp<DamageableComponent>(target, out var damageable))
            return;

        // Calculate random healing and damage within specified ranges
        var healAmount = _random.NextFloat(1.2f, 2.0f);
        var damageAmount = _random.NextFloat(0.2f, 0.5f);

        // Heal asphyxiation damage
        var healSpec = new DamageSpecifier();
        healSpec.DamageDict.Add("Asphyxiation", -healAmount);
        _damageableSystem.TryChangeDamage(target, healSpec, ignoreResistances: false);

        // Deal mechanical damage
        var damageSpec = new DamageSpecifier();
        damageSpec.DamageDict.Add("Blunt", damageAmount);
        _damageableSystem.TryChangeDamage(target, damageSpec, ignoreResistances: false);

        // Play breathing sound occasionally
        if (_random.Prob(0.3f) && component.ALVSound != null)
        {
            _audio.PlayPvs(component.ALVSound, target);
        }
    }

    private void StopALV(EntityUid uid, ALVComponent component)
    {
        if (component.Performer != null)
        {
            _popup.PopupEntity(Loc.GetString("alv-stopped"), uid, component.Performer.Value);
        }

        RemCompDeferred<ALVComponent>(uid);
    }
}