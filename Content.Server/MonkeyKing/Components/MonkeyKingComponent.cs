using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.MonkeyKing.Components;

[RegisterComponent]
public sealed partial class MonkeyKingComponent : Component
{
    /// <summary>
    /// List of summoned monkeys under the king's control
    /// </summary>
    [DataField("summonedMonkeys")]
    public List<EntityUid> SummonedMonkeys = new();

    /// <summary>
    /// Maximum number of monkeys the king can have at once
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxMonkeys")]
    public int MaxMonkeys = 12;

    [DataField("bananaThrowAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BananaThrowAction = "ActionMonkeyKingBananaThrow";

    [DataField("bananaThrowActionEntity")]
    public EntityUid? BananaThrowActionEntity;

    [DataField("battleCryAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BattleCryAction = "ActionMonkeyKingBattleCry";

    [DataField("battleCryActionEntity")]
    public EntityUid? BattleCryActionEntity;

    [DataField("callToArmsAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CallToArmsAction = "ActionMonkeyKingCallToArms";

    [DataField("callToArmsActionEntity")]
    public EntityUid? CallToArmsActionEntity;

    [DataField("liberationAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string LiberationAction = "ActionMonkeyKingLiberation";

    [DataField("liberationActionEntity")]
    public EntityUid? LiberationActionEntity;

    [ViewVariables(VVAccess.ReadWrite), DataField("bananaProjectilePrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BananaProjectilePrototype = "MonkeyKingBananaProjectile";

    [ViewVariables(VVAccess.ReadWrite), DataField("summonedMonkeyPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SummonedMonkeyPrototype = "MobMonkeyArmed";

    /// <summary>
    /// Random weapons that can be given to summoned monkeys
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("monkeyWeapons")]
    public List<string> MonkeyWeapons = new()
    {
        "KitchenKnife",
        "CombatKnife", 
        "BaseBallBat",
        "Spear",
        "SpearBone"
    };

    [ViewVariables(VVAccess.ReadWrite), DataField("soundRoar")]
    public SoundSpecifier? SoundRoar = new SoundPathSpecifier("/Audio/Animals/monkey_scream.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("soundBattleCry")]
    public SoundSpecifier? SoundBattleCry = new SoundPathSpecifier("/Audio/Animals/monkey_scream.ogg");

    /// <summary>
    /// NPC faction to ensure summoned monkeys are allied
    /// </summary>
    [DataField]
    public ProtoId<NpcFactionPrototype> Faction = "MonkeyKing";

    [DataField]
    public ProtoId<NpcFactionPrototype> HostileFaction = "SimpleHostile";
}