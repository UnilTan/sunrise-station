using Content.Shared.FixedPoint;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.SpaceBattle;

/// <summary>
/// Компонент правила космической битвы между двумя фракциями
/// </summary>
[RegisterComponent, Access(typeof(SpaceBattleRuleSystem))]
public sealed partial class SpaceBattleRuleComponent : Component
{
    /// <summary>
    /// Фракция NT
    /// </summary>
    [DataField]
    public ProtoId<NpcFactionPrototype> NTFaction = "NanoTrasen";

    /// <summary>
    /// Фракция Синдиката
    /// </summary>
    [DataField]
    public ProtoId<NpcFactionPrototype> SyndicateFaction = "Syndicate";

    /// <summary>
    /// Минимальное количество игроков для запуска режима
    /// </summary>
    [DataField]
    public int MinPlayers = 20;

    /// <summary>
    /// Максимальное количество игроков на каждую фракцию
    /// </summary>
    [DataField]
    public int MaxPlayersPerFaction = 15;

    /// <summary>
    /// Материнский корабль NT
    /// </summary>
    [DataField]
    public EntityUid? NTMothership;

    /// <summary>
    /// Материнский корабль Синдиката
    /// </summary>
    [DataField]
    public EntityUid? SyndicateMothership;

    /// <summary>
    /// Список средних десантных кораблей NT
    /// </summary>
    [DataField]
    public List<EntityUid> NTAssaultShuttles = new();

    /// <summary>
    /// Список средних десантных кораблей Синдиката
    /// </summary>
    [DataField]
    public List<EntityUid> SyndicateAssaultShuttles = new();

    /// <summary>
    /// Список малых истребителей NT
    /// </summary>
    [DataField]
    public List<EntityUid> NTFighters = new();

    /// <summary>
    /// Список малых истребителей Синдиката
    /// </summary>
    [DataField]
    public List<EntityUid> SyndicateFighters = new();

    /// <summary>
    /// Процент повреждений материнского корабля для поражения
    /// </summary>
    [DataField]
    public float MothershipDefeatThreshold = 0.7f;

    /// <summary>
    /// Тип победы
    /// </summary>
    [DataField]
    public SpaceBattleWinType WinType = SpaceBattleWinType.Neutral;

    /// <summary>
    /// Победившая фракция
    /// </summary>
    [DataField]
    public SpaceBattleFaction? WinningFaction;

    /// <summary>
    /// Звук начала режима
    /// </summary>
    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/nukeops_start.ogg");

    /// <summary>
    /// Была ли битва начата
    /// </summary>
    [DataField]
    public bool BattleStarted;

    /// <summary>
    /// Время начала битвы
    /// </summary>
    [DataField]
    public TimeSpan BattleStartTime;
}

/// <summary>
/// Типы побед в космической битве
/// </summary>
public enum SpaceBattleWinType : byte
{
    /// <summary>
    /// Нейтральная победа (ничья)
    /// </summary>
    Neutral,
    /// <summary>
    /// Победа NT
    /// </summary>
    NTVictory,
    /// <summary>
    /// Победа Синдиката
    /// </summary>
    SyndicateVictory
}

/// <summary>
/// Фракции в космической битве
/// </summary>
public enum SpaceBattleFaction : byte
{
    /// <summary>
    /// NanoTrasen
    /// </summary>
    NanoTrasen,
    /// <summary>
    /// Синдикат
    /// </summary>
    Syndicate
}

/// <summary>
/// Компонент для отслеживания участников космической битвы
/// </summary>
[RegisterComponent]
public sealed partial class SpaceBattleParticipantComponent : Component
{
    /// <summary>
    /// Фракция участника
    /// </summary>
    [DataField]
    public SpaceBattleFaction Faction;

    /// <summary>
    /// Роль участника
    /// </summary>
    [DataField]
    public string Role = string.Empty;
}