using Content.Server.Antag.Components;
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Antag;

/// <summary>
/// Modified objectives system specifically for traitors that limits kill objectives to 1 per traitor.
/// </summary>
public sealed class TraitorObjectivesSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitorObjectivesComponent, AfterAntagEntitySelectedEvent>(OnTraitorSelected);
    }

    private void OnTraitorSelected(Entity<TraitorObjectivesComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mind.TryGetMind(args.Session, out var mindId, out var mind))
        {
            Log.Error($"Traitor {ToPrettyString(args.EntityUid):player} was selected but had no mind attached!");
            return;
        }

        var difficulty = 0f;
        var hasKillObjective = false;

        foreach (var set in ent.Comp.Sets)
        {
            if (!_random.Prob(set.Prob))
                continue;

            for (var pick = 0; pick < set.MaxPicks && ent.Comp.MaxDifficulty > difficulty; pick++)
            {
                var remainingDifficulty = ent.Comp.MaxDifficulty - difficulty;
                
                // For kill objectives, skip if we already have one
                if (IsKillObjectiveGroup(set.Groups) && hasKillObjective)
                {
                    Log.Debug($"Skipping kill objective for {ToPrettyString(args.EntityUid):player} - already has one");
                    continue;
                }

                if (_objectives.GetRandomObjective(mindId, mind, set.Groups, remainingDifficulty) is not { } objective)
                    continue;

                // Check if this is a kill objective
                var isKillObj = HasKillCondition(objective);
                if (isKillObj && hasKillObjective)
                {
                    // Skip this objective and delete it
                    QueueDel(objective);
                    continue;
                }

                _mind.AddObjective(mindId, mind, objective);
                var adding = Comp<ObjectiveComponent>(objective).Difficulty;
                difficulty += adding;

                if (isKillObj)
                    hasKillObjective = true;

                Log.Debug($"Added objective {ToPrettyString(objective):objective} to {ToPrettyString(args.EntityUid):player} with {adding} difficulty (kill: {isKillObj})");
            }
        }
    }

    private bool IsKillObjectiveGroup(ProtoId<WeightedRandomPrototype> groupId)
    {
        return groupId == "TraitorObjectiveGroupKill";
    }

    private bool HasKillCondition(EntityUid objective)
    {
        return HasComp<KillPersonConditionComponent>(objective);
    }
}