using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
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
    [Dependency] private readonly StationSystem _stationSystem = default!;

    /// <summary>
    /// Минимальное время битвы перед возможностью завершения раунда
    /// </summary>
    private readonly TimeSpan _minBattleTime = TimeSpan.FromMinutes(10);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceBattleRuleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<SpaceBattleRuleComponent, RuleLoadedGridsEvent>(OnRuleLoadedGrids);
    }

    protected override void Started(EntityUid uid, SpaceBattleRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        StartSpaceBattle(uid, component);
    }

    private void OnRuleLoadedGrids(EntityUid uid, SpaceBattleRuleComponent component, RuleLoadedGridsEvent args)
    {
        // Ищем материнские корабли и другие суда на загруженной карте
        foreach (var grid in args.Grids)
        {
            if (!TryComp<MetaDataComponent>(grid, out var metaData))
                continue;

            var gridName = metaData.EntityName;

            // Определяем тип корабля по названию
            if (gridName.Contains("NT_Mothership") || gridName.Contains("NanoTrasen_Mothership"))
            {
                component.NTMothership = grid;
                Log.Info($"Найден материнский корабль NT: {gridName}");
            }
            else if (gridName.Contains("Syndicate_Mothership") || gridName.Contains("Syn_Mothership"))
            {
                component.SyndicateMothership = grid;
                Log.Info($"Найден материнский корабль Синдиката: {gridName}");
            }
            else if (gridName.Contains("NT_Assault") || gridName.Contains("NanoTrasen_Assault"))
            {
                component.NTAssaultShuttles.Add(grid);
                Log.Info($"Найден десантный корабль NT: {gridName}");
            }
            else if (gridName.Contains("Syndicate_Assault") || gridName.Contains("Syn_Assault"))
            {
                component.SyndicateAssaultShuttles.Add(grid);
                Log.Info($"Найден десантный корабль Синдиката: {gridName}");
            }
            else if (gridName.Contains("NT_Fighter") || gridName.Contains("NanoTrasen_Fighter"))
            {
                component.NTFighters.Add(grid);
                Log.Info($"Найден истребитель NT: {gridName}");
            }
            else if (gridName.Contains("Syndicate_Fighter") || gridName.Contains("Syn_Fighter"))
            {
                component.SyndicateFighters.Add(grid);
                Log.Info($"Найден истребитель Синдиката: {gridName}");
            }
        }

        // Проверяем, что у нас есть минимально необходимые корабли
        if (component.NTMothership == null)
            Log.Error("Не найден материнский корабль NT!");
        
        if (component.SyndicateMothership == null)
            Log.Error("Не найден материнский корабль Синдиката!");
    }

    private void OnComponentInit(EntityUid uid, SpaceBattleRuleComponent component, ComponentInit args)
    {
        component.BattleStartTime = _timing.CurTime;
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

        // Ищем точки спавна на материнском корабле
        var spawners = new List<EntityUid>();
        
        // Простой поиск любых маркеров на корабле для спавна
        var transformQuery = EntityQueryEnumerator<TransformComponent>();
        while (transformQuery.MoveNext(out var entityUid, out var xform))
        {
            if (xform.GridUid == mothership)
            {
                spawners.Add(entityUid);
            }
        }

        if (spawners.Count == 0)
        {
            Log.Warning($"Не найдены точки спавна на материнском корабле фракции {faction}");
            return;
        }

        foreach (var player in players)
        {
            var role = roles[roleIndex % roles.Count];
            var spawner = _random.Pick(spawners);
            SpawnPlayerWithRole(player, spawner, role, faction);
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

    private void SpawnPlayerWithRole(ICommonSession player, EntityUid spawner, string role, SpaceBattleFaction faction)
    {
        if (!TryComp<TransformComponent>(spawner, out var spawnerXform))
        {
            Log.Error($"Неверный spawner для игрока {player.Name}");
            return;
        }

        // Спавним игрока на позиции spawner'а
        var spawned = EntityManager.SpawnEntity("MobHumanSpaceBattle", spawnerXform.Coordinates);
        if (spawned == EntityUid.Invalid)
        {
            Log.Error($"Не удалось заспавнить игрока {player.Name}");
            return;
        }

        // Присваиваем игрока заспавненной сущности
        player.AttachToEntity(spawned);

        // Добавляем роль антагониста
        if (_antag.TryGetNextAvailableDefinition(role, out var antagDef))
        {
            _antag.ForceMakeAntag(player, antagDef);
        }

        // Устанавливаем фракцию
        var factionId = faction == SpaceBattleFaction.NanoTrasen ? "NanoTrasen" : "Syndicate";
        _npcFaction.AddFaction(spawned, factionId);

        // Добавляем компонент для отслеживания участников битвы
        var spaceBattleComp = EnsureComp<SpaceBattleParticipantComponent>(spawned);
        spaceBattleComp.Faction = faction;
        spaceBattleComp.Role = role;

        Log.Info($"Заспавнен игрок {player.Name} как {role} фракции {faction}");
    }

    private void SetupAutoDocking(SpaceBattleRuleComponent component)
    {
        // Автоматически стыкуем малые корабли к материнским
        
        // Стыкуем истребители NT к материнскому кораблю NT
        if (component.NTMothership != null)
        {
            foreach (var fighter in component.NTFighters)
            {
                AttemptAutoDocking(fighter, component.NTMothership.Value, "NT");
            }
            
            foreach (var assault in component.NTAssaultShuttles)
            {
                AttemptAutoDocking(assault, component.NTMothership.Value, "NT");
            }
        }

        // Стыкуем истребители Синдиката к материнскому кораблю Синдиката
        if (component.SyndicateMothership != null)
        {
            foreach (var fighter in component.SyndicateFighters)
            {
                AttemptAutoDocking(fighter, component.SyndicateMothership.Value, "Syndicate");
            }
            
            foreach (var assault in component.SyndicateAssaultShuttles)
            {
                AttemptAutoDocking(assault, component.SyndicateMothership.Value, "Syndicate");
            }
        }

        Log.Info("Настройка автодокинга завершена");
    }

    private void AttemptAutoDocking(EntityUid smallShip, EntityUid mothership, string faction)
    {
        try
        {
            // Ищем доки на обоих кораблях
            var mothershipDocks = FindDockingPorts(mothership);
            var smallShipDocks = FindDockingPorts(smallShip);

            if (mothershipDocks.Count == 0 || smallShipDocks.Count == 0)
            {
                Log.Warning($"Не найдены стыковочные порты для автодокинга {faction}");
                return;
            }

            // Выбираем первые доступные порты
            var mothershipDock = mothershipDocks[0];
            var smallShipDock = smallShipDocks[0];

            // Пытаемся состыковать корабли
            _shuttle.TryFTLDock(smallShip, mothership, smallShipDock, mothershipDock);
            
            Log.Info($"Автодокинг выполнен для фракции {faction}");
        }
        catch (Exception ex)
        {
            Log.Error($"Ошибка автодокинга для фракции {faction}: {ex.Message}");
        }
    }

    private List<EntityUid> FindDockingPorts(EntityUid grid)
    {
        var docks = new List<EntityUid>();
        
        // Поиск стыковочных портов на сетке
        var dockQuery = EntityQueryEnumerator<DockingComponent, TransformComponent>();
        while (dockQuery.MoveNext(out var dockUid, out var dock, out var xform))
        {
            if (xform.GridUid == grid)
            {
                docks.Add(dockUid);
            }
        }

        return docks;
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

        // Подсчитываем общий урон корабля на основе поврежденных структур
        var totalStructures = 0;
        var damagedStructures = 0;

        var structureQuery = EntityQueryEnumerator<DamageableComponent, TransformComponent>();
        while (structureQuery.MoveNext(out var structureUid, out var structureDamage, out var xform))
        {
            if (xform.GridUid != mothership)
                continue;

            totalStructures++;
            
            // Считаем структуру поврежденной, если она получила больше 50% урона
            if (structureDamage.TotalDamage > structureDamage.DamageContainerData.Prototype.TotalHealth * 0.5f)
            {
                damagedStructures++;
            }
        }

        if (totalStructures == 0)
            return;

        var damageRatio = (float)damagedStructures / totalStructures;
        
        if (damageRatio >= component.MothershipDefeatThreshold)
        {
            // Материнский корабль критически поврежден
            var opposingFaction = faction == SpaceBattleFaction.NanoTrasen 
                ? SpaceBattleFaction.Syndicate 
                : SpaceBattleFaction.NanoTrasen;

            var factionName = faction == SpaceBattleFaction.NanoTrasen ? "NanoTrasen" : "Синдиката";
            var message = $"Материнский корабль {factionName} критически поврежден! ({damageRatio:P1} структур уничтожено)";
            
            Log.Info(message);
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
        
        foreach (var (uid, mobState, participant) in EntityQuery<MobStateComponent, SpaceBattleParticipantComponent>())
        {
            if (mobState.CurrentState == MobState.Alive && participant.Faction == faction)
            {
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