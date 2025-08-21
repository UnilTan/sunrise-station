using Content.Server.Communications;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Station.Components;
using Robust.Server.Audio;

namespace Content.Server._Sunrise.NukeOps;

/// <summary>
/// System that handles nuclear operative communications jammer functionality
/// </summary>
public sealed class NukeOpsCommJammerSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NukeOpsCommJammerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NukeOpsCommJammerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<NukeOpsCommJammerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<NukeOpsCommJammerComponent, NewLinkEvent>(OnDeviceLink);

        // Subscribe to communication events to block them
        SubscribeLocalEvent<CommunicationConsoleCallShuttleAttemptEvent>(OnShuttleCallAttempt);
    }

    private void OnInit(EntityUid uid, NukeOpsCommJammerComponent component, ComponentInit args)
    {
        // Ensure the jammer starts as inactive
        component.Active = false;
        UpdateJammerAppearance(uid, component);
    }

    private void OnActivate(EntityUid uid, NukeOpsCommJammerComponent component, ActivateInWorldEvent args)
    {
        // Toggle jammer state
        ToggleJammer(uid, component, args.User);
    }

    private void OnPowerChanged(EntityUid uid, NukeOpsCommJammerComponent component, ref PowerChangedEvent args)
    {
        // Deactivate jammer if power is lost
        if (!args.Powered && component.Active)
        {
            DeactivateJammer(uid, component);
        }
    }

    private void OnDeviceLink(EntityUid uid, NukeOpsCommJammerComponent component, NewLinkEvent args)
    {
        // Allow device linking for remote activation
    }

    private void ToggleJammer(EntityUid uid, NukeOpsCommJammerComponent component, EntityUid user)
    {
        if (component.Active)
        {
            DeactivateJammer(uid, component);
        }
        else
        {
            ActivateJammer(uid, component);
        }
    }

    private void ActivateJammer(EntityUid uid, NukeOpsCommJammerComponent component)
    {
        // Check power requirements
        if (!_power.IsPowered(uid))
            return;

        // Check if on station (if required)
        if (component.RequiresStationPlacement && !IsOnStation(uid))
            return;

        component.Active = true;
        UpdateJammerAppearance(uid, component);

        if (component.ActivationSound != null)
            _audio.PlayPvs(component.ActivationSound, uid);

        // TODO: Add station-wide announcement that communications are being jammed
    }

    private void DeactivateJammer(EntityUid uid, NukeOpsCommJammerComponent component)
    {
        component.Active = false;
        UpdateJammerAppearance(uid, component);

        if (component.DeactivationSound != null)
            _audio.PlayPvs(component.DeactivationSound, uid);

        // TODO: Add station-wide announcement that communications are restored
    }

    private void UpdateJammerAppearance(EntityUid uid, NukeOpsCommJammerComponent component)
    {
        // TODO: Update visual appearance based on active state
        // This would involve sprite layer changes, light colors, etc.
    }

    private bool IsOnStation(EntityUid uid)
    {
        var transform = Transform(uid);
        if (transform.GridUid == null)
            return false;

        return HasComp<StationMemberComponent>(transform.GridUid.Value);
    }

    private void OnShuttleCallAttempt(ref CommunicationConsoleCallShuttleAttemptEvent args)
    {
        // Check if there's an active jammer blocking emergency shuttle calls
        if (IsJammed(args.Uid))
        {
            args.Cancelled = true;
            args.Reason = Loc.GetString("comms-jammer-shuttle-call-blocked");
        }
    }

    /// <summary>
    /// Check if an entity is affected by an active communications jammer
    /// </summary>
    public bool IsJammed(EntityUid? entity)
    {
        if (entity == null)
            return false;

        // Check if there are any active jammers affecting this entity
        var query = EntityQueryEnumerator<NukeOpsCommJammerComponent, TransformComponent>();
        while (query.MoveNext(out var jammerUid, out var jammer, out var jammerTransform))
        {
            if (!jammer.Active)
                continue;

            // Check power
            if (!_power.IsPowered(jammerUid))
                continue;

            // If jammer has no range limit, it affects everything on the station
            if (jammer.Range <= 0)
            {
                if (jammer.RequiresStationPlacement)
                {
                    // Jammer affects the entire station it's on
                    var entityTransform = Transform(entity.Value);
                    if (jammerTransform.GridUid == entityTransform.GridUid)
                        return true;
                }
                else
                {
                    // Global jammer
                    return true;
                }
            }
            else
            {
                // Range-based jamming (not implemented in this basic version)
                // TODO: Implement distance-based jamming
            }
        }

        return false;
    }
}