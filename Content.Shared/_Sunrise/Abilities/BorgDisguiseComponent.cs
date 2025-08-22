using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.Abilities;

[RegisterComponent]
public sealed partial class BorgDisguiseComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite),
     DataField("disguiseActionId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DisguiseActionId = "BorgDisguise";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("disguiseTime")]
    public float DisguiseTime = 2f;

    /// <summary>
    /// Available disguise sprite states for different borg types
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),
     DataField("availableDisguises")]
    public List<string> AvailableDisguises = new()
    {
        "sec",
        "peace", 
        "clown",
        "engi",
        "medical",
        "service",
        "spider"
    };

    /// <summary>
    /// Current disguised state. Empty string means no disguise.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),
     DataField("currentDisguise")]
    public string CurrentDisguise = "";

    /// <summary>
    /// Original sprite state before disguising
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),
     DataField("originalState")]
    public string OriginalState = "spider";
}

public sealed partial class BorgDisguiseActionEvent : InstantActionEvent
{

}

[Serializable, NetSerializable]
public sealed partial class BorgDisguiseDoAfterEvent : SimpleDoAfterEvent
{
    public string DisguiseState = "";

    public BorgDisguiseDoAfterEvent(string disguiseState)
    {
        DisguiseState = disguiseState;
    }
}