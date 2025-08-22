using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.TTS;

/// <summary>
/// Component for cyborgs that allows them to change their TTS voice.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BorgVoiceComponent : Component
{
    /// <summary>
    /// The currently selected voice prototype ID.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    [DataField("selectedVoice", customTypeSerializer: typeof(PrototypeIdSerializer<TTSVoicePrototype>))]
    public string? SelectedVoiceId { get; set; }
}