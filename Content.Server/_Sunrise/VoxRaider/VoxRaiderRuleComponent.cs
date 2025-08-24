using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.VoxRaider;

/// <summary>
/// Stores data for VoxRaiderRuleSystem.
/// </summary>
[RegisterComponent, Access(typeof(VoxRaiderRuleSystem))]
public sealed partial class VoxRaiderRuleComponent : Component
{
    [DataField]
    public ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";

    [DataField]
    public ProtoId<NpcFactionPrototype> VoxRaiderFaction = "VoxRaider";
}