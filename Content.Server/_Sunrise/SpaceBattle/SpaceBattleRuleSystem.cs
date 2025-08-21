using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Shared.Damage;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.SpaceBattle;

/// <summary>
/// Система космической битвы между двумя фракциями
/// </summary>
public sealed class SpaceBattleRuleSystem : GameRuleSystem<SpaceBattleRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergency = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;

    /// <summary>
    /// Минимальное время битвы перед возможностью завершения раунда
    /// </summary>
    private readonly TimeSpan _minBattleTime = TimeSpan.FromMinutes(10);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceBattleRuleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DamageChangedEvent>(OnDamageChanged);
    }

    private void OnComponentInit(EntityUid uid, SpaceBattleRuleComponent component, ComponentInit args)
    {
        component.BattleStartTime = _timing.CurTime;
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        foreach (var (uid, rule, gameRule) in EntityQuery<SpaceBattleRuleComponent, GameRuleComponent>())
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            StartSpaceBattle(uid, rule);
        }
    }

    private void StartSpaceBattle(EntityUid uid, SpaceBattleRuleComponent component)
    {
        if (component.BattleStarted)
            return;

        component.BattleStarted = true;
        component.BattleStartTime = _timing.CurTime;

        // Получаем всех подключенных игроков
        var eligiblePlayers = new List<ICommonSession>();
        foreach (var session in Filter.GetAllPlayers())
        {
            if (session.AttachedEntity != null)
                continue;

            eligiblePlayers.Add(session);
        }

        if (eligiblePlayers.Count < component.MinPlayers)
        {
            Log.Warning($"Недостаточно игроков для космической битвы: {eligiblePlayers.Count}/{component.MinPlayers}");
            return;
        }

        // Перемешиваем и делим игроков между фракциями
        _random.Shuffle(eligiblePlayers);
        
        var ntPlayers = new List<ICommonSession>();
        var syndicatePlayers = new List<ICommonSession>();

        for (var i = 0; i < eligiblePlayers.Count && 
             ntPlayers.Count < component.MaxPlayersPerFaction && 
             syndicatePlayers.Count < component.MaxPlayersPerFaction; i++)
        {
            if (i % 2 == 0)
                ntPlayers.Add(eligiblePlayers[i]);
            else
                syndicatePlayers.Add(eligiblePlayers[i]);
        }

        // Спавним игроков для каждой фракции
        SpawnFactionPlayers(component, ntPlayers, SpaceBattleFaction.NanoTrasen);
        SpawnFactionPlayers(component, syndicatePlayers, SpaceBattleFaction.Syndicate);

        // Запускаем автодокинг малых кораблей
        SetupAutoDocking(component);

        Log.Info($"Космическая битва начата! NT: {ntPlayers.Count}, Синдикат: {syndicatePlayers.Count}");
    }

    private void SpawnFactionPlayers(SpaceBattleRuleComponent component, List<ICommonSession> players, SpaceBattleFaction faction)
    {
        if (players.Count == 0)
            return;

        // Получаем материнский корабль фракции
        var mothership = faction == SpaceBattleFaction.NanoTrasen 
            ? component.NTMothership 
            : component.SyndicateMothership;

        if (mothership == null)
        {
            Log.Error($"Материнский корабль фракции {faction} не найден!");
            return;
        }

        // Определяем роли для фракции
        var roles = GetFactionRoles(faction);
        var roleIndex = 0;

        foreach (var player in players)
        {
            var role = roles[roleIndex % roles.Count];
            SpawnPlayerWithRole(player, mothership.Value, role, faction);
            roleIndex++;
        }
    }

    private List<string> GetFactionRoles(SpaceBattleFaction faction)
    {
        // Базовые роли для космической битвы
        return new List<string>
        {
            "SpaceBattleCaptain",     // Капитан
            "SpaceBattleGunner",      // Стрелок
            "SpaceBattleEngineer",    // Инженер
            "SpaceBattlePilot",       // Пилот
            "SpaceBattleMedical",     // Врач
            "SpaceBattleMarine"       // Десантник
        };
    }

    private void SpawnPlayerWithRole(ICommonSession player, EntityUid mothership, string role, SpaceBattleFaction faction)
    {
        // TODO: Реализовать спавн игрока с определенной ролью на материнском корабле
        // Пока что это заглушка - нужно найти подходящую точку спавна на корабле
        Log.Info($"Спавн игрока {player.Name} как {role} на корабле фракции {faction}");
    }

    private void SetupAutoDocking(SpaceBattleRuleComponent component)
    {
        // TODO: Реализовать автоматическую стыковку малых кораблей к материнским
        // Пока что заглушка
        Log.Info("Настройка автодокинга малых кораблей");
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        // Проверяем состояние экипажей для определения победы
        CheckForVictoryConditions();
    }

    private void OnDamageChanged(DamageChangedEvent ev)
    {
        // Проверяем повреждения материнских кораблей
        CheckMothershipDamage(ev.Entity);
    }

    private void CheckMothershipDamage(EntityUid entity)
    {
        foreach (var (uid, component) in EntityQuery<SpaceBattleRuleComponent>())
        {
            if (entity == component.NTMothership)
            {
                CheckMothershipDefeat(entity, SpaceBattleFaction.NanoTrasen, component);
            }
            else if (entity == component.SyndicateMothership)
            {
                CheckMothershipDefeat(entity, SpaceBattleFaction.Syndicate, component);
            }
        }
    }

    private void CheckMothershipDefeat(EntityUid mothership, SpaceBattleFaction faction, SpaceBattleRuleComponent component)
    {
        if (!TryComp<DamageableComponent>(mothership, out var damageable))
            return;

        var damageRatio = damageable.TotalDamage / damageable.DamageContainerData.Prototype.TotalHealth;
        
        if (damageRatio >= component.MothershipDefeatThreshold)
        {
            // Материнский корабль критически поврежден
            var opposingFaction = faction == SpaceBattleFaction.NanoTrasen 
                ? SpaceBattleFaction.Syndicate 
                : SpaceBattleFaction.NanoTrasen;

            EndBattleWithVictory(component, opposingFaction);
        }
    }

    private void CheckForVictoryConditions()
    {
        foreach (var (uid, component) in EntityQuery<SpaceBattleRuleComponent>())
        {
            if (component.WinType != SpaceBattleWinType.Neutral)
                continue;

            // Проверяем минимальное время битвы
            if (_timing.CurTime - component.BattleStartTime < _minBattleTime)
                continue;

            // Подсчитываем живых игроков каждой фракции
            var ntAlive = CountAliveFactionMembers(SpaceBattleFaction.NanoTrasen);
            var syndicateAlive = CountAliveFactionMembers(SpaceBattleFaction.Syndicate);

            if (ntAlive == 0 && syndicateAlive > 0)
            {
                EndBattleWithVictory(component, SpaceBattleFaction.Syndicate);
            }
            else if (syndicateAlive == 0 && ntAlive > 0)
            {
                EndBattleWithVictory(component, SpaceBattleFaction.NanoTrasen);
            }
        }
    }

    private int CountAliveFactionMembers(SpaceBattleFaction faction)
    {
        var count = 0;
        
        foreach (var (uid, mobState, _) in EntityQuery<MobStateComponent, AntagRoleComponent>())
        {
            if (mobState.CurrentState == MobState.Alive)
            {
                // TODO: Проверить принадлежность к фракции
                count++;
            }
        }

        return count;
    }

    private void EndBattleWithVictory(SpaceBattleRuleComponent component, SpaceBattleFaction winner)
    {
        component.WinningFaction = winner;
        component.WinType = winner == SpaceBattleFaction.NanoTrasen 
            ? SpaceBattleWinType.NTVictory 
            : SpaceBattleWinType.SyndicateVictory;

        var message = winner == SpaceBattleFaction.NanoTrasen
            ? "Победа NanoTrasen! Синдикат повержен!"
            : "Победа Синдиката! NanoTrasen повержен!";

        _roundEndSystem.EndRound();
        
        Log.Info($"Космическая битва завершена: {message}");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (uid, component) in EntityQuery<SpaceBattleRuleComponent>())
        {
            if (!component.BattleStarted)
                continue;

            // Проверяем условия победы каждую секунду
            CheckForVictoryConditions();
        }
    }
}