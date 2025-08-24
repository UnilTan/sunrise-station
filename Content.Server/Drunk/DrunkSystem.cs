using Content.Shared.Drunk;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Log;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Drunk;

public sealed class DrunkSystem : SharedDrunkSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    private TimeSpan _nextFallCheck = TimeSpan.Zero;
    private readonly TimeSpan _fallCheckInterval = TimeSpan.FromSeconds(5); // Check every 5 seconds
    private readonly float _fallChance = 0.15f; // 15% chance to fall per check

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextFallCheck)
            return;

        _nextFallCheck = _timing.CurTime + _fallCheckInterval;

        // Find all entities with status effect containers
        var query = EntityQueryEnumerator<StatusEffectContainerComponent>();
        while (query.MoveNext(out var uid, out var container))
        {
            // Check if this entity has the drunk status effect
            if (!_statusEffects.HasStatusEffect(uid, Drunk))
                continue;

            // Check if entity can fall (has standing state and is currently standing)
            if (!TryComp<StandingStateComponent>(uid, out var standing) || !standing.Standing)
                continue;

            // Random chance to fall
            if (_random.Prob(_fallChance))
            {
                Log.Debug($"Drunk entity {ToPrettyString(uid)} is falling down due to drunkenness");
                _standing.Down(uid, playSound: true, dropHeldItems: true);
            }
        }
    }
}
