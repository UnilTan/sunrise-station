using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.PaintMixer;

/// <summary>
/// Component for a machine that can create custom-colored spray paint cans
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaintMixerComponent : Component
{
    /// <summary>
    /// The currently selected color for paint mixing
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color SelectedColor = Color.Red;

    /// <summary>
    /// Whether the machine is currently mixing paint
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsMixing = false;

    /// <summary>
    /// Time it takes to mix paint and create a spray can
    /// </summary>
    [DataField]
    public TimeSpan MixingTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Sound played when mixing paint
    /// </summary>
    [DataField]
    public SoundSpecifier MixingSound = new SoundPathSpecifier("/Audio/Machines/airlock_open.ogg");

    /// <summary>
    /// The entity prototype ID for the base spray paint that will be created
    /// </summary>
    [DataField]
    public string SprayPaintPrototype = "PaintBase";

    /// <summary>
    /// Required materials to create a spray paint can
    /// Steel: 5, Plastic: 1, Paint reagent: 15
    /// </summary>
    [DataField]
    public Dictionary<string, int> RequiredMaterials = new()
    {
        { "Steel", 5 },
        { "Plastic", 1 }
    };

    /// <summary>
    /// Required paint reagent amount
    /// </summary>
    [DataField]
    public int RequiredPaintAmount = 15;

    /// <summary>
    /// Paint reagent ID
    /// </summary>
    [DataField]
    public string PaintReagentId = "SpaceGlue";
}