using Content.Server.Shuttles.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared._Sunrise.VoxRaider;

namespace Content.Server._Sunrise.VoxRaider.Objectives;

public sealed class VoxRaiderSurvivalConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoxRaiderSurvivalConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, VoxRaiderSurvivalConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.MindId, args.Mind);
    }

    private float GetProgress(EntityUid mindId, MindComponent mind)
    {
        // For now, just check if the player is alive
        // TODO: Check if they're on their shuttle/outpost when implementing the shuttle
        if (mind.OwnedEntity == null || _mind.IsCharacterDeadIc(mind))
            return 0f;

        // If alive, consider it 50% progress
        // Full progress will require being on the Vox shuttle
        return 0.5f;
    }
}