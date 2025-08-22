using Content.Shared._Sunrise.Abilities;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Abilities;

public sealed class BorgDisguiseSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BorgDisguiseComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BorgDisguiseComponent, BorgDisguiseActionEvent>(OnDisguise);
        SubscribeLocalEvent<BorgDisguiseComponent, BorgDisguiseDoAfterEvent>(OnDisguiseDoAfter);
        SubscribeLocalEvent<BorgDisguiseComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInit(EntityUid uid, BorgDisguiseComponent component, ComponentInit args)
    {
        _actions.AddAction(uid, component.DisguiseActionId);
    }

    private void OnDisguise(EntityUid uid, BorgDisguiseComponent component, BorgDisguiseActionEvent args)
    {
        if (args.Handled)
            return;

        // Cycle through available disguises
        var currentIndex = component.AvailableDisguises.IndexOf(component.CurrentDisguise);
        var nextIndex = (currentIndex + 1) % component.AvailableDisguises.Count;
        var nextDisguise = component.AvailableDisguises[nextIndex];

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.DisguiseTime,
            new BorgDisguiseDoAfterEvent(nextDisguise), uid, target: uid, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true
        });

        args.Handled = true;
    }

    private void OnDisguiseDoAfter(EntityUid uid, BorgDisguiseComponent component, BorgDisguiseDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<BorgChassisComponent>(uid, out var borgChassis))
            return;

        // Update the disguise state
        component.CurrentDisguise = args.DisguiseState;
        
        // Update borg chassis state
        if (args.DisguiseState == component.OriginalState)
        {
            // Return to original appearance
            borgChassis.HasMindState = $"{component.OriginalState}_e";
            borgChassis.NoMindState = $"{component.OriginalState}_e_r";
            _popup.PopupEntity("You return to your original appearance.", uid, uid);
        }
        else
        {
            // Apply disguise
            borgChassis.HasMindState = $"{args.DisguiseState}_e";
            borgChassis.NoMindState = $"{args.DisguiseState}_e_r";
            _popup.PopupEntity($"You disguise yourself as a {args.DisguiseState} cyborg.", uid, uid);
        }

        // Update name if we have a transponder
        if (TryComp<BorgTransponderComponent>(uid, out var transponder))
        {
            if (args.DisguiseState == component.OriginalState)
            {
                transponder.Name = "syndicate's cyborg-saboteur";
            }
            else
            {
                transponder.Name = $"{args.DisguiseState} cyborg";
            }
        }
    }

    private void OnExamined(EntityUid uid, BorgDisguiseComponent component, ExaminedEvent args)
    {
        if (component.CurrentDisguise != component.OriginalState && !string.IsNullOrEmpty(component.CurrentDisguise))
        {
            args.PushMarkup("[color=yellow]It appears to be disguised.[/color]");
        }
    }
}