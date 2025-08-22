using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.Abilities;

[RegisterComponent]
public sealed partial class BorgStealthComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite),
     DataField("stealthActionId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string StealthActionId = "BorgStealth";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("stealthTime")]
    public float StealthTime = 1f;

    [ViewVariables(VVAccess.ReadWrite),
     DataField("isStealthed")]
    public bool IsStealthed = false;

    /// <summary>
    /// Minimum visibility when stealthed (-1 = fully invisible)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),
     DataField("minVisibility")]
    public float MinVisibility = 0.15f;

    /// <summary>
    /// Maximum visibility when visible
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),
     DataField("maxVisibility")]
    public float MaxVisibility = 1f;
}

public sealed partial class BorgStealthActionEvent : InstantActionEvent
{

}

[Serializable, NetSerializable]
public sealed partial class BorgStealthDoAfterEvent : SimpleDoAfterEvent
{
    public bool Enable = false;

    public BorgStealthDoAfterEvent(bool enable)
    {
        Enable = enable;
    }
}