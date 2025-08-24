using Content.Shared.Drunk;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Drunk;

public sealed class DrunkSystem : SharedDrunkSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    private float _fallCheckInterval = 3.0f; // Check every 3 seconds
    private float _fallCheckTimer = 0.0f;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _fallCheckTimer += frameTime;
        if (_fallCheckTimer >= _fallCheckInterval)
        {
            _fallCheckTimer = 0.0f;
            CheckDrunkFalling();
        }
    }

    private void CheckDrunkFalling()
    {
        var query = EntityQueryEnumerator<DrunkStatusEffectComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            TryMakeDrunkFall(uid);
        }
    }

    private void TryMakeDrunkFall(EntityUid uid)
    {
        // Get drunk level based on time remaining on drunk effect
        if (!Status.TryGetMaxTime<DrunkStatusEffectComponent>(uid, out var time))
            return;

        // Don't make them fall if they're already down
        if (_standing.IsDown(uid))
            return;

        // Calculate drunkeness scale (similar to slurred system)
        var magic = MagicNumber;
        if (time.EndEffectTime != null)
        {
            var curTime = _timing.CurTime;
            magic = (float)(time.EndEffectTime - curTime).Value.TotalSeconds - 80f;
        }

        var drunkenessScale = Math.Clamp(magic / MagicNumber, 0f, 1f);

        // Only very drunk people fall (scale > 0.6)
        if (drunkenessScale < 0.6f)
            return;

        // Calculate fall chance based on drunkeness level
        // At max drunkeness (scale = 1.0), 10% chance per check
        // At medium drunkeness (scale = 0.6), 2% chance per check
        var fallChance = (drunkenessScale - 0.6f) * 0.25f; // 0 to 0.1 (0% to 10%)

        if (_random.Prob(fallChance))
        {
            _standing.Down(uid, playSound: true, dropHeldItems: false);
        }
    }
}
