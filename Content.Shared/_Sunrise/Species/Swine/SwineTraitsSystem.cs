using Content.Shared.Movement.Events;
using Robust.Shared.Audio;

namespace Content.Shared._Sunrise.Species.Swine;

/// <summary>
/// System that handles swine species traits
/// </summary>
public sealed class SwineTraitsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        
        // Subscribe to footstep events for hoof sounds
        SubscribeLocalEvent<SwineTraitsComponent, GetFootstepSoundEvent>(OnGetFootstepSound);
    }

    private void OnGetFootstepSound(EntityUid uid, SwineTraitsComponent swine, ref GetFootstepSoundEvent args)
    {
        // Use hoof sounds for swines
        args.Sound = new SoundCollectionSpecifier("FootstepHoof");
    }
}