using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// Handles charged teleporters that can teleport users to random locations within a limited range.
/// </summary>
public sealed class ChargedTeleporterSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const int MaxTeleportAttempts = 20;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ChargedTeleporterComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ChargedTeleporterComponent, ChargedTeleporterDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ChargedTeleporterComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_netMan.IsServer)
            return;

        var query = EntityQueryEnumerator<ChargedTeleporterComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Recharge logic
            if (comp.Charges < comp.MaxCharges && _timing.CurTime >= comp.NextChargeTime)
            {
                comp.Charges++;
                Dirty(uid, comp);
                
                if (comp.Charges < comp.MaxCharges)
                {
                    comp.NextChargeTime = _timing.CurTime + TimeSpan.FromSeconds(comp.RechargeTime);
                }
            }
        }
    }

    private void OnExamined(EntityUid uid, ChargedTeleporterComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("charged-teleporter-examine-charges", 
            ("charges", component.Charges), ("maxCharges", component.MaxCharges)));
        
        if (component.Charges < component.MaxCharges)
        {
            var timeLeft = component.NextChargeTime - _timing.CurTime;
            if (timeLeft > TimeSpan.Zero)
            {
                args.PushMarkup(Loc.GetString("charged-teleporter-examine-recharge-time", 
                    ("time", (int)timeLeft.TotalSeconds)));
            }
        }
    }

    private void OnUseInHand(EntityUid uid, ChargedTeleporterComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (component.Charges <= 0)
        {
            _popup.PopupClient(Loc.GetString("charged-teleporter-no-charges"), uid, args.User);
            _audio.PlayPredicted(component.NoChargeSound, uid, args.User);
            args.Handled = true;
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, 1.0f, new ChargedTeleporterDoAfterEvent(), uid, used: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.5f,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, ChargedTeleporterComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (component.Charges <= 0)
        {
            _popup.PopupClient(Loc.GetString("charged-teleporter-no-charges"), uid, args.Args.User);
            args.Handled = true;
            return;
        }

        TeleportRandomly(uid, component, args.Args.User);
        args.Handled = true;
    }

    private void TeleportRandomly(EntityUid teleporter, ChargedTeleporterComponent component, EntityUid user)
    {
        if (!_netMan.IsServer)
            return;

        var userTransform = Transform(user);
        var coords = userTransform.Coordinates;

        // Find a valid teleport location
        EntityCoordinates newCoords = coords;
        for (var i = 0; i < MaxTeleportAttempts; i++)
        {
            var randVector = _random.NextVector2(component.TeleportRange);
            var testCoords = coords.Offset(randVector);
            
            if (!_lookup.AnyEntitiesIntersecting(_transform.ToMapCoordinates(testCoords), LookupFlags.Static))
            {
                newCoords = testCoords;
                break;
            }
        }

        // Consume charge
        component.Charges--;
        if (component.Charges < component.MaxCharges && component.NextChargeTime <= _timing.CurTime)
        {
            component.NextChargeTime = _timing.CurTime + TimeSpan.FromSeconds(component.RechargeTime);
        }
        Dirty(teleporter, component);

        // Log teleportation
        _adminLogger.Add(LogType.Teleport, LogImpact.Low, 
            $"{ToPrettyString(user):player} used charged teleporter {ToPrettyString(teleporter)} to teleport from {coords} to {newCoords}");

        // Check for bluespace interference before teleporting
        var portalSystem = System<SharedPortalSystem>();
        if (portalSystem.HasBluespaceItems(user))
        {
            portalSystem.KillEntityFromBluespaceIntolerance(user);
            _popup.PopupEntity(Loc.GetString("bluespace-teleport-death-message"), user, 
                Filter.Pvs(user, entityMan: EntityManager), true);
            return;
        }

        // Perform teleportation
        _transform.SetCoordinates(user, newCoords);
        _audio.PlayPvs(component.TeleportSound, user);

        _popup.PopupEntity(Loc.GetString("charged-teleporter-teleported"), user, user);
    }
}