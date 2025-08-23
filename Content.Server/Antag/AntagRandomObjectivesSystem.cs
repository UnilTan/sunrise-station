using Content.Server.Antag.Components;
using Content.Server.Objectives;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Random;

namespace Content.Server.Antag;

/// <summary>
/// Adds fixed objectives to an antag made with <c>AntagRandomObjectivesComponent</c>.
/// </summary>
public sealed class AntagRandomObjectivesSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagRandomObjectivesComponent, AfterAntagEntitySelectedEvent>(OnAntagSelected);
    }

    private void OnAntagSelected(Entity<AntagRandomObjectivesComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mind.TryGetMind(args.Session, out var mindId, out var mind))
        {
            Log.Error($"Antag {ToPrettyString(args.EntityUid):player} was selected by {ToPrettyString(ent):rule} but had no mind attached!");
            return;
        }

        var difficulty = 0f;
        var killObjectiveCount = 0;
        foreach (var set in ent.Comp.Sets)
        {
            if (!_random.Prob(set.Prob))
                continue;

            for (var pick = 0; pick < set.MaxPicks && ent.Comp.MaxDifficulty > difficulty; pick++)
            {
                var remainingDifficulty = ent.Comp.MaxDifficulty - difficulty;
                if (_objectives.GetRandomObjective(mindId, mind, set.Groups, remainingDifficulty) is not { } objective)
                    continue;

                // Check if this is a kill objective and if we've reached the limit
                var isKillObjective = HasComp<KillPersonConditionComponent>(objective);
                if (isKillObjective && ent.Comp.MaxKillObjectives.HasValue && killObjectiveCount >= ent.Comp.MaxKillObjectives.Value)
                {
                    // Skip this objective and delete it
                    QueueDel(objective);
                    Log.Debug($"Skipping kill objective for {ToPrettyString(args.EntityUid):player} - already has {killObjectiveCount} kill objectives (limit: {ent.Comp.MaxKillObjectives.Value})");
                    continue;
                }

                _mind.AddObjective(mindId, mind, objective);
                var adding = Comp<ObjectiveComponent>(objective).Difficulty;
                difficulty += adding;

                if (isKillObjective)
                    killObjectiveCount++;

                Log.Debug($"Added objective {ToPrettyString(objective):objective} to {ToPrettyString(args.EntityUid):player} with {adding} difficulty (kill: {isKillObjective})");
            }
        }
    }
}
