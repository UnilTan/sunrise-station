using Content.Shared._Sunrise.Abilities;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;

namespace Content.Server._Sunrise.Abilities;

public sealed class BorgStealthSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStealthSystem _stealthSystem = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BorgStealthComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BorgStealthComponent, BorgStealthActionEvent>(OnStealth);
        SubscribeLocalEvent<BorgStealthComponent, BorgStealthDoAfterEvent>(OnStealthDoAfter);
    }

    private void OnInit(EntityUid uid, BorgStealthComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, component.StealthActionId);
    }

    private void OnStealth(EntityUid uid, BorgStealthComponent component, BorgStealthActionEvent args)
    {
        if (args.Handled)
            return;

        var enabling = !component.IsStealthed;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.StealthTime,
            new BorgStealthDoAfterEvent(enabling), uid, target: uid, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true
        });

        args.Handled = true;
    }

    private void OnStealthDoAfter(EntityUid uid, BorgStealthComponent component, BorgStealthDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        component.IsStealthed = args.Enable;

        if (args.Enable)
        {
            // Enable stealth
            var stealthComp = EnsureComp<StealthComponent>(uid);
            _stealthSystem.SetEnabled(uid, true, stealthComp);
            _popup.PopupEntity("You activate your cloaking device.", uid, uid);
        }
        else
        {
            // Disable stealth
            if (TryComp<StealthComponent>(uid, out var stealthComp))
            {
                _stealthSystem.SetEnabled(uid, false, stealthComp);
            }

            _popup.PopupEntity("You deactivate your cloaking device.", uid, uid);
        }

        // Update action visual state
        // TODO: Update action toggle state when API becomes available
        // if (_actions.TryGetActionData(uid, component.StealthActionId, out var actionData))
        // {
        //     _actions.SetToggled(actionData.ActionEntity, component.IsStealthed);
        // }
    }
}
