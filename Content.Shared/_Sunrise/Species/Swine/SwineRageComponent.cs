using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Species.Swine;

/// <summary>
/// Component that manages the swine rage system
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SwineRageComponent : Component
{
    /// <summary>
    /// Current number of rage stacks (0-5)
    /// </summary>
    [DataField, AutoNetworkedField]
    public int RageStacks = 0;
    
    /// <summary>
    /// Maximum rage stacks allowed
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxRageStacks = 5;
    
    /// <summary>
    /// Health threshold below which rage can be triggered
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RageHealthThreshold = 75f;
    
    /// <summary>
    /// How long rage stacks last without activity (in seconds)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RageDecayTime = 10f;
    
    /// <summary>
    /// Time when each rage stack was gained (for decay)
    /// </summary>
    [DataField, AutoNetworkedField]
    public Queue<TimeSpan> RageStackTimes = new();
    
    /// <summary>
    /// Whether rage is currently active
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RageActive = false;
    
    /// <summary>
    /// Number of times rage has been used this round
    /// </summary>
    [DataField, AutoNetworkedField]
    public int RageUsesThisRound = 0;
    
    /// <summary>
    /// Maximum number of rage activations per round
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxRageUsesPerRound = 2;
    
    /// <summary>
    /// Damage multiplier per rage stack
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DamageMultiplierPerStack = 0.05f;
    
    /// <summary>
    /// Resistance multiplier per rage stack
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ResistanceMultiplierPerStack = 0.05f;
}