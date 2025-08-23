using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Brewing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BrewingBarrelComponent : Component
{
    /// <summary>
    /// Whether the barrel is open or closed
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsOpen = false;

    /// <summary>
    /// Whether fermentation is currently in progress
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsFermenting = false;

    /// <summary>
    /// Time when fermentation started
    /// </summary>
    [DataField]
    public TimeSpan? FermentationStartTime;

    /// <summary>
    /// How long fermentation takes in minutes
    /// </summary>
    [DataField]
    public float FermentationTime = 10f; // 10 minutes by default

    /// <summary>
    /// The wort input container ID
    /// </summary>
    [DataField]
    public string WortContainerId = "barrel_wort_container";

    /// <summary>
    /// The water input container ID
    /// </summary>
    [DataField]
    public string WaterContainerId = "barrel_water_container";

    /// <summary>
    /// The output container ID for finished products
    /// </summary>
    [DataField]
    public string OutputContainerId = "barrel_output_container";

    /// <summary>
    /// Sound when opening the barrel
    /// </summary>
    [DataField]
    public SoundSpecifier? OpenSound;

    /// <summary>
    /// Sound when closing the barrel
    /// </summary>
    [DataField]
    public SoundSpecifier? CloseSound;

    /// <summary>
    /// Minimum temperature for optimal fermentation (Kelvin)
    /// </summary>
    [DataField]
    public float OptimalMinTemp = 288.15f; // 15°C

    /// <summary>
    /// Maximum temperature for optimal fermentation (Kelvin)
    /// </summary>
    [DataField]
    public float OptimalMaxTemp = 298.15f; // 25°C

    /// <summary>
    /// Current fermentation quality based on temperature and atmosphere
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FermentationQuality = 1.0f;

    /// <summary>
    /// Whether the barrel is currently sensing atmosphere
    /// </summary>
    [DataField]
    public bool SensesAtmosphere = true;
}