using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.MonkeyKing.Components;
using Content.Shared.Actions;
using Content.Shared.MonkeyKing;
using Content.Shared.Maps;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.MonkeyKing;

public sealed partial class MonkeyKingSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedProjectileSystem _projectileSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MonkeyKingComponent, ComponentInit>(OnMonkeyKingInit);
        SubscribeLocalEvent<MonkeyKingComponent, ComponentShutdown>(OnMonkeyKingShutdown);

        SubscribeLocalEvent<MonkeyKingBananaThrowActionEvent>(OnBananaThrow);
        SubscribeLocalEvent<MonkeyKingBattleCryActionEvent>(OnBattleCry);
        SubscribeLocalEvent<MonkeyKingCallToArmsActionEvent>(OnCallToArms);
        SubscribeLocalEvent<MonkeyKingLiberationActionEvent>(OnLiberation);
    }

    private void OnMonkeyKingInit(EntityUid uid, MonkeyKingComponent component, ComponentInit args)
    {
        // Add actions to the monkey king
        _actions.AddAction(uid, ref component.BananaThrowActionEntity, component.BananaThrowAction);
        _actions.AddAction(uid, ref component.BattleCryActionEntity, component.BattleCryAction);
        _actions.AddAction(uid, ref component.CallToArmsActionEntity, component.CallToArmsAction);
        _actions.AddAction(uid, ref component.LiberationActionEntity, component.LiberationAction);
    }

    private void OnMonkeyKingShutdown(EntityUid uid, MonkeyKingComponent component, ComponentShutdown args)
    {
        // Clean up summoned monkeys when king dies/leaves
        foreach (var monkey in component.SummonedMonkeys.ToList())
        {
            if (EntityManager.EntityExists(monkey))
                EntityManager.DeleteEntity(monkey);
        }
        component.SummonedMonkeys.Clear();
    }

    private void OnBananaThrow(MonkeyKingBananaThrowActionEvent args)
    {
        if (!TryComp<MonkeyKingComponent>(args.Performer, out var component))
            return;

        var performerTransform = Transform(args.Performer);
        var targetCoords = args.Target;

        // Create and fire banana projectile
        var bananaEnt = EntityManager.SpawnEntity(component.BananaProjectilePrototype, performerTransform.Coordinates);
        
        if (TryComp<PhysicsComponent>(bananaEnt, out var projectilePhysics))
        {
            var direction = (targetCoords.ToMapPos(EntityManager, _transform) - performerTransform.WorldPosition).Normalized();
            var velocity = direction * 15f; // Banana speed
            _physics.SetLinearVelocity(bananaEnt, velocity);
        }

        // Play throw sound
        if (component.SoundRoar != null)
            _audio.PlayPvs(component.SoundRoar, args.Performer);

        _popup.PopupEntity("Король обезьян швыряет банан!", args.Performer);
        args.Handled = true;
    }

    private void OnBattleCry(MonkeyKingBattleCryActionEvent args)
    {
        if (!TryComp<MonkeyKingComponent>(args.Performer, out var component))
            return;

        var performerTransform = Transform(args.Performer);
        
        // Find nearby monkeys to buff
        var nearbyEntities = _lookup.GetEntitiesInRange(performerTransform.Coordinates, 8f);
        var buffedCount = 0;

        foreach (var entity in nearbyEntities)
        {
            // Check if entity is a monkey (has monkey faction or is summoned monkey)
            if (component.SummonedMonkeys.Contains(entity) || HasComp<MonkeyKingComponent>(entity))
            {
                // Apply speed buff and healing effect
                // TODO: Implement proper buff system
                buffedCount++;
            }
        }

        // Play battle cry sound
        if (component.SoundBattleCry != null)
            _audio.PlayPvs(component.SoundBattleCry, args.Performer);

        _popup.PopupEntity($"УК-УК-УК! Король обезьян воодушевляет {buffedCount} обезьян!", args.Performer);
        args.Handled = true;
    }

    private void OnCallToArms(MonkeyKingCallToArmsActionEvent args)
    {
        if (!TryComp<MonkeyKingComponent>(args.Performer, out var component))
            return;

        if (component.SummonedMonkeys.Count >= component.MaxMonkeys)
        {
            _popup.PopupEntity("Слишком много обезьян уже призвано!", args.Performer);
            return;
        }

        var performerTransform = Transform(args.Performer);
        
        // Find a valid spawn position near the king
        var spawnPos = FindValidSpawnPosition(performerTransform.Coordinates);

        // Spawn armed monkey
        var monkey = EntityManager.SpawnEntity(component.SummonedMonkeyPrototype, spawnPos);
        component.SummonedMonkeys.Add(monkey);

        // Set faction
        if (TryComp<NpcFactionMemberComponent>(monkey, out var factionComp))
        {
            _faction.RemoveFaction(monkey, "SimpleHostile", false);
            _faction.AddFaction(monkey, component.Faction);
        }

        _popup.PopupEntity("Король обезьян призывает вооруженную обезьяну!", args.Performer);
        args.Handled = true;
    }

    private void OnLiberation(MonkeyKingLiberationActionEvent args)
    {
        if (!TryComp<MonkeyKingComponent>(args.Performer, out var component))
            return;

        var target = args.Target;
        
        // Check if target is valid for liberation (animal, non-sentient)
        if (!HasComp<MobStateComponent>(target))
        {
            _popup.PopupEntity("Эту цель нельзя освободить!", args.Performer);
            return;
        }

        if (HasComp<MindContainerComponent>(target))
        {
            _popup.PopupEntity("Эта цель уже разумна!", args.Performer);
            return;
        }

        // TODO: Inject cognizine or make sentient directly
        // For now, just add mind container component to make sentient
        var mindContainer = EntityManager.AddComponent<MindContainerComponent>(target);
        
        _popup.PopupEntity("Король обезьян освобождает разум существа!", args.Performer);
        _popup.PopupEntity("Вы чувствуете, как сознание проясняется!", target);
        
        args.Handled = true;
    }

    private EntityCoordinates FindValidSpawnPosition(EntityCoordinates center)
    {
        // Try to find a valid spawn position within 3 tiles of the center
        for (var i = 0; i < 10; i++)
        {
            var randomOffset = _random.NextVector2(3f);
            var testPos = center.Offset(randomOffset);
            
            // For now, just return any position (we can add more validation later)
            return testPos;
        }
        
        // Fall back to the original position if nothing found
        return center;
    }
}