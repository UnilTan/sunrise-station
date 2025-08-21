using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceNetwork;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Popups;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Collections.Generic;

namespace Content.Server.Teleportation;

/// <summary>
/// Handles research teleporter functionality for transmitting, receiving, and bidirectional teleportation.
/// </summary>
public sealed class ResearchTeleporterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPortalSystem _portal = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly LinkedEntitySystem _linkedEntity = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchTeleporterComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<ResearchTeleporterComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }

    private void OnInteractHand(EntityUid uid, ResearchTeleporterComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        // Only transmitters and bidirectional teleporters can be manually activated
        if (component.Mode == TeleporterMode.Receive)
            return;

        if (!_power.IsPowered(uid))
        {
            _popup.PopupEntity(Loc.GetString("teleporter-no-power"), uid, args.User);
            return;
        }

        if (_timing.CurTime < component.NextUseTime)
        {
            var remainingTime = (component.NextUseTime - _timing.CurTime).TotalSeconds;
            _popup.PopupEntity(Loc.GetString("teleporter-cooldown", ("time", $"{remainingTime:F1}")), uid, args.User);
            return;
        }

        TryTeleport(uid, component, args.User);
        args.Handled = true;
    }

    private void OnPacketReceived(EntityUid uid, ResearchTeleporterComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out var cmd))
            return;

        if (cmd.ToString() != "TeleportActivate")
            return;

        // Only transmitters and bidirectional teleporters respond to network activation
        if (component.Mode == TeleporterMode.Receive)
            return;

        if (!_power.IsPowered(uid))
            return;

        if (_timing.CurTime < component.NextUseTime)
            return;

        // Try to get the entity to teleport from the packet data
        EntityUid? targetEntity = null;
        if (args.Data.TryGetValue("TargetEntity", out var entityData) && entityData is EntityUid entity)
        {
            targetEntity = entity;
        }

        TryTeleport(uid, component, targetEntity);
    }

    private void TryTeleport(EntityUid teleporter, ResearchTeleporterComponent component, EntityUid? targetEntity = null)
    {
        // Find linked entities (receivers or bidirectional teleporters)
        if (!_linkedEntity.GetLink(teleporter, out var linkedEntity))
        {
            if (targetEntity.HasValue)
                _popup.PopupEntity(Loc.GetString("teleporter-no-linked-receiver"), teleporter, targetEntity.Value);
            return;
        }

        if (!TryComp<ResearchTeleporterComponent>(linkedEntity.Value, out var linkedComponent))
            return;

        // Verify the linked entity can receive teleports
        if (linkedComponent.Mode == TeleporterMode.Transmit)
        {
            if (targetEntity.HasValue)
                _popup.PopupEntity(Loc.GetString("teleporter-invalid-receiver"), teleporter, targetEntity.Value);
            return;
        }

        if (!_power.IsPowered(linkedEntity.Value))
        {
            if (targetEntity.HasValue)
                _popup.PopupEntity(Loc.GetString("teleporter-receiver-no-power"), teleporter, targetEntity.Value);
            return;
        }

        // Determine what to teleport
        var teleportTargets = new List<EntityUid>();
        if (targetEntity.HasValue)
        {
            teleportTargets.Add(targetEntity.Value);
        }
        else
        {
            // Teleport everything on the teleporter pad
            var teleporterPos = Transform(teleporter).Coordinates;
            var entitiesOnPad = _lookup.GetEntitiesInRange(teleporterPos, 0.5f);
            
            foreach (var entity in entitiesOnPad)
            {
                if (entity == teleporter)
                    continue;
                    
                if (HasComp<MobStateComponent>(entity) || HasComp<ItemComponent>(entity))
                    teleportTargets.Add(entity);
            }
        }

        if (teleportTargets.Count == 0)
        {
            if (targetEntity.HasValue)
                _popup.PopupEntity(Loc.GetString("teleporter-nothing-to-teleport"), teleporter, targetEntity.Value);
            return;
        }

        // Consume power
        if (!_power.TryUseActivePower(teleporter, component.TeleportPowerCost))
        {
            if (targetEntity.HasValue)
                _popup.PopupEntity(Loc.GetString("teleporter-insufficient-power"), teleporter, targetEntity.Value);
            return;
        }

        // Teleport entities
        var receiverPos = Transform(linkedEntity.Value).Coordinates;
        foreach (var entity in teleportTargets)
        {
            _portal.TeleportEntity(entity, receiverPos);
        }

        // Set cooldown
        component.NextUseTime = _timing.CurTime + TimeSpan.FromSeconds(component.CooldownTime);

        // Send network message to linked entities
        var netData = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = "TeleportComplete",
            ["SourceEntity"] = teleporter,
            ["TeleportedCount"] = teleportTargets.Count
        };
        _deviceNetwork.QueuePacket(teleporter, linkedEntity.Value, netData);

        if (targetEntity.HasValue)
            _popup.PopupEntity(Loc.GetString("teleporter-success", ("count", teleportTargets.Count)), teleporter, targetEntity.Value);
    }
}