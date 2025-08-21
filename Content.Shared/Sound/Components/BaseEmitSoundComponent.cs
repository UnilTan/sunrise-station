using Robust.Shared.Audio;

namespace Content.Shared.Sound.Components;

/// <summary>
/// Base sound emitter which defines most of the data fields.
/// Accepts both single sounds and sound collections.
/// </summary>
public abstract partial class BaseEmitSoundComponent : Component
{
    /// <summary>
    /// The <see cref="SoundSpecifier"/> to play.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Play the sound at the position instead of parented to the source entity.
    /// Useful if the entity is deleted after.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Positional;

    /// <summary>
    /// Whether this is a loud sound that can be heard through walls.
    /// Loud sounds (gunshots, explosions, screams) use NoOcclusion flag.
    /// Quiet sounds (whispers, interactions) use reduced range and normal occlusion.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool LoudSound;
}
