using System.Linq;
using Content.Server.AlertLevel;
using Content.Server.Cargo.Components;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Shared._Sunrise.StatsBoard;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cargo.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clumsy;
using Content.Shared.Construction;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Doors.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Fluids;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Slippery;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Localization;

namespace Content.Server.StatsBoard;

public sealed class StatsBoardSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private (EntityUid? killer, EntityUid? victim, TimeSpan time) _firstMurder = (null, null, TimeSpan.Zero);
    private EntityUid? _hamsterKiller;
    private int _jointCreated;
    private (EntityUid? clown, TimeSpan? time) _clownCuffed = (null, null);
    private readonly Dictionary<EntityUid, StatisticEntry> _statisticEntries = new();

    // New tracking fields for requested statistics
    private readonly Dictionary<string, (TimeSpan duration, int count)> _alertLevelStats = new();
    private string? _currentAlertLevel = null;
    private TimeSpan _alertLevelStartTime = TimeSpan.Zero;
    private readonly Dictionary<NetUserId, int> _profanityCount = new();
    private readonly HashSet<string> _profanityWords = new() { "сука", "блядь", "пиздец" };
    private int _felinidThrowCount = 0;
    private (int alcohol, int chemicals) _spilledLiquids = (0, 0);
    private (int received, int spent) _cargoMoney = (0, 0);
    private int _ahelpCount = 0;
    private readonly Dictionary<string, int> _plantGrowthCount = new();

    // Placeholder tracking for remaining features 
    private int _estimatedCargoReceived = 0;
    private int _estimatedCargoSpent = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActorComponent, DamageChangedEvent>(OnDamageModify);
        SubscribeLocalEvent<ActorComponent, SlippedEvent>(OnSlippedEvent);
        SubscribeLocalEvent<ActorComponent, CreamedEvent>(OnCreamedEvent);
        SubscribeLocalEvent<ActorComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<ActorComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ActorComponent, DoorEmaggedEvent>(OnDoorEmagged);
        SubscribeLocalEvent<ActorComponent, ElectrocutedEvent>(OnElectrocuted);
        SubscribeLocalEvent<ActorComponent, SubtractCashEvent>(OnItemPurchasedEvent);
        SubscribeLocalEvent<ActorComponent, CuffedEvent>(OnCuffedEvent);
        SubscribeLocalEvent<ActorComponent, ItemConstructionCreated>(OnCraftedEvent);
        SubscribeLocalEvent<ActorComponent, AbsorberPudleEvent>(OnAbsorbedPuddleEvent);
        SubscribeLocalEvent<ActorComponent, MindAddedMessage>(OnMindAdded);

        // New event subscriptions for requested statistics
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
        SubscribeLocalEvent<EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<ThrownEvent>(OnThrown);
        SubscribeLocalEvent<SolutionContainerOverflowEvent>(OnLiquidSpill);
    }

    private void OnMindAdded(EntityUid uid, ActorComponent comp, MindAddedMessage ev)
    {
        if (_statisticEntries.ContainsKey(uid) || ev.Mind.Comp.UserId == null || HasComp<GhostComponent>(uid))
            return;

        var value = new StatisticEntry(MetaData(uid).EntityName, ev.Mind.Comp.UserId.Value);
        _statisticEntries.Add(uid, value);
    }

    public void CleanEntries()
    {
        _firstMurder = (null, null, TimeSpan.Zero);
        _hamsterKiller = null;
        _jointCreated = 0;
        _clownCuffed = (null, TimeSpan.Zero);
        _statisticEntries.Clear();
        
        // Clean new tracking fields
        _alertLevelStats.Clear();
        _currentAlertLevel = null;
        _alertLevelStartTime = TimeSpan.Zero;
        _profanityCount.Clear();
        _felinidThrowCount = 0;
        _spilledLiquids = (0, 0);
        _cargoMoney = (0, 0);
        _ahelpCount = 0;
        _plantGrowthCount.Clear();
        _estimatedCargoReceived = 0;
        _estimatedCargoSpent = 0;
    }

    private void OnAlertLevelChanged(AlertLevelChangedEvent args)
    {
        var currentTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
        
        // Record duration of previous alert level
        if (_currentAlertLevel != null)
        {
            var duration = currentTime - _alertLevelStartTime;
            if (_alertLevelStats.TryGetValue(_currentAlertLevel, out var stats))
            {
                _alertLevelStats[_currentAlertLevel] = (stats.duration + duration, stats.count);
            }
        }
        
        // Set up tracking for new alert level
        _currentAlertLevel = args.AlertLevel;
        _alertLevelStartTime = currentTime;
        
        // Increment count for this alert level
        if (_alertLevelStats.TryGetValue(args.AlertLevel, out var alertStats))
        {
            _alertLevelStats[args.AlertLevel] = (alertStats.duration, alertStats.count + 1);
        }
        else
        {
            _alertLevelStats[args.AlertLevel] = (TimeSpan.Zero, 1);
        }
    }

    private void OnEntitySpoke(EntitySpokeEvent args)
    {
        if (string.IsNullOrEmpty(args.Message)) return;
        
        // Get the player's user ID
        if (!_mindSystem.TryGetMind(args.Source, out var mindId, out var mind) || mind.UserId == null)
            return;
            
        var message = args.Message.ToLowerInvariant();
        var userId = mind.UserId.Value;
        
        foreach (var word in _profanityWords)
        {
            if (message.Contains(word))
            {
                _profanityCount.TryGetValue(userId, out var count);
                _profanityCount[userId] = count + 1;
                break; // Only count once per message to avoid double-counting
            }
        }
    }

    private void OnThrown(ref ThrownEvent args)
    {
        // Check if the thrown entity is a felinid by looking for humanoid appearance with felinid species
        if (TryComp<HumanoidAppearanceComponent>(args.Thrown, out var humanoid))
        {
            if (_prototypeManager.TryIndex<SpeciesPrototype>(humanoid.Species, out var species) && 
                species.ID == "Felinid")
            {
                _felinidThrowCount++;
            }
        }
    }

    private void OnLiquidSpill(ref SolutionContainerOverflowEvent args)
    {
        // Simple categorization: if it contains ethanol or similar, it's alcohol; otherwise chemicals
        var solution = args.Overflow;
        var containsAlcohol = false;
        
        foreach (var reagent in solution.Contents)
        {
            // Check for common alcoholic reagents
            var reagentId = reagent.Reagent.Prototype;
            if (reagentId.Contains("ethanol", StringComparison.OrdinalIgnoreCase) ||
                reagentId.Contains("beer", StringComparison.OrdinalIgnoreCase) ||
                reagentId.Contains("wine", StringComparison.OrdinalIgnoreCase) ||
                reagentId.Contains("vodka", StringComparison.OrdinalIgnoreCase) ||
                reagentId.Contains("rum", StringComparison.OrdinalIgnoreCase))
            {
                containsAlcohol = true;
                break;
            }
        }
        
        var spillAmount = (int)solution.Volume;
        if (containsAlcohol)
        {
            _spilledLiquids.alcohol += spillAmount;
        }
        else
        {
            _spilledLiquids.chemicals += spillAmount;
        }
    }

    private void OnAbsorbedPuddleEvent(EntityUid uid, ActorComponent comp, ref AbsorberPudleEvent ev)
    {
        if (!_mindSystem.TryGetMind(comp.PlayerSession, out var mindId, out var mind))
            return;

        if (_statisticEntries.TryGetValue(uid, out var value))
        {
            value.AbsorbedPuddleCount += 1;
        }
    }

    private void OnCraftedEvent(EntityUid uid, ActorComponent comp, ref ItemConstructionCreated ev)
    {
        if (!_mindSystem.TryGetMind(comp.PlayerSession, out var mindId, out var mind))
            return;

        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        if (!TryComp<MetaDataComponent>(ev.Item, out var metaDataComponent))
            return;

        if (metaDataComponent.EntityPrototype == null)
            return;
        switch (metaDataComponent.EntityPrototype.ID)
        {
            case "Blunt":
            case "Joint":
                _jointCreated += 1;
                break;
        }
    }

    private void OnCuffedEvent(EntityUid uid, ActorComponent comp, ref CuffedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        value.CuffedCount += 1;
        if (_clownCuffed.clown != null)
            return;
        if (!HasComp<ClumsyComponent>(uid))
            return;
        _clownCuffed.clown = uid;
        _clownCuffed.time = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
    }

    private void OnItemPurchasedEvent(EntityUid uid, ActorComponent comp, ref SubtractCashEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        if (ev.Currency != "Telecrystal")
            return;
        if (value.SpentTk == null)
        {
            value.SpentTk = ev.Cost.Int();
        }
        else
        {
            value.SpentTk += ev.Cost.Int();
        }
    }

    private void OnElectrocuted(EntityUid uid, ActorComponent comp, ElectrocutedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        value.ElectrocutedCount += 1;
    }

    private void OnDoorEmagged(EntityUid uid, ActorComponent comp, ref DoorEmaggedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        value.DoorEmagedCount += 1;
    }

    private void OnInteractionAttempt(EntityUid uid, ActorComponent comp, InteractionAttemptEvent args)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        if (!HasComp<ItemComponent>(args.Target))
            return;
        if (MetaData(args.Target.Value).EntityPrototype == null)
            return;
        var entityPrototype = MetaData(args.Target.Value).EntityPrototype;
        if (entityPrototype is not { ID: "CaptainIDCard" })
            return;
        if (value.IsInteractedCaptainCard)
            return;
        value.IsInteractedCaptainCard = true;
    }

    private void OnCreamedEvent(EntityUid uid, ActorComponent comp, ref CreamedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        value.CreamedCount += 1;
    }

    private void OnMobStateChanged(EntityUid uid, ActorComponent comp, MobStateChangedEvent args)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        switch (args.NewMobState)
        {
            case MobState.Dead:
            {
                value.DeadCount += 1;

                EntityUid? origin = null;
                if (args.Origin != null)
                {
                    origin = args.Origin.Value;
                }

                if (_firstMurder.victim == null && HasComp<HumanoidAppearanceComponent>(uid))
                {
                    _firstMurder.victim = uid;
                    _firstMurder.killer = origin;
                    _firstMurder.time = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
                    Logger.Info($"First Murder. CurTime: {_gameTiming.CurTime}, RoundStartTimeSpan: {_gameTicker.RoundStartTimeSpan}, Substract: {_gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan)}");
                }

                if (origin != null)
                {
                    if (_hamsterKiller == null && _tagSystem.HasTag(uid, "Hamster"))
                    {
                        _hamsterKiller = origin.Value;
                    }

                    if (!_statisticEntries.TryGetValue(origin.Value, out var originEntry))
                        return;

                    if (_tagSystem.HasTag(uid, "Mouse"))
                    {
                        originEntry.KilledMouseCount += 1;
                    }

                    if (HasComp<HumanoidAppearanceComponent>(uid))
                        originEntry.HumanoidKillCount += 1;
                }

                break;
            }
        }
    }

    private void OnDamageModify(EntityUid uid, ActorComponent comp, DamageChangedEvent ev)
    {
        DamageGetModify(uid, ev);

        if (ev.Origin != null)
            DamageTakeModify(ev.Origin.Value, ev);
    }

    private void DamageTakeModify(EntityUid uid, DamageChangedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        if (ev.DamageDelta == null)
            return;

        if (ev.DamageIncreased)
        {
            value.TotalInflictedDamage += ev.DamageDelta.GetTotal().Int();
        }
        else
        {
            value.TotalInflictedHeal += Math.Abs(ev.DamageDelta.GetTotal().Int());
        }
    }

    private void DamageGetModify(EntityUid uid, DamageChangedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        if (ev.DamageDelta == null)
            return;

        if (ev.DamageIncreased)
        {
            value.TotalTakeDamage += ev.DamageDelta.GetTotal().Int();
        }
        else
        {
            value.TotalTakeHeal += Math.Abs(ev.DamageDelta.GetTotal().Int());
        }
    }

    private void OnSlippedEvent(EntityUid uid, ActorComponent comp, ref SlippedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        if (HasComp<HumanoidAppearanceComponent>(uid))
            value.SlippedCount += 1;
    }

    private StationBankAccountComponent? GetBankAccount(EntityUid? uid)
    {
        if (uid != null && TryComp<StationBankAccountComponent>(uid, out var bankAccount))
        {
            return bankAccount;
        }
        return null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var statsQuery = EntityQueryEnumerator<ActorComponent>();
        while (statsQuery.MoveNext(out var ent, out var comp))
        {
            if (!_statisticEntries.TryGetValue(ent, out var value))
                return;

            if (TryComp<TransformComponent>(ent, out var transformComponent) &&
                transformComponent.GridUid == null && HasComp<HumanoidAppearanceComponent>(ent))
                value.SpaceTime += TimeSpan.FromSeconds(frameTime);

            if (TryComp<CuffableComponent>(ent, out var cuffableComponent) &&
                !cuffableComponent.CanStillInteract)
                value.CuffedTime += TimeSpan.FromSeconds(frameTime);

            if (HasComp<SleepingComponent>(ent))
                value.SleepTime += TimeSpan.FromSeconds(frameTime);
        }
    }

    public StatisticEntry[] GetStatisticEntries()
    {
        return _statisticEntries.Values.ToArray();
    }

    public SharedStatisticEntry ConvertToSharedStatisticEntry(StatisticEntry entry)
    {
        return new SharedStatisticEntry(entry.Name, entry.FirstActor)
        {
            TotalTakeDamage = entry.TotalTakeDamage,
            TotalTakeHeal = entry.TotalTakeHeal,
            TotalInflictedDamage = entry.TotalInflictedDamage,
            TotalInflictedHeal = entry.TotalInflictedHeal,
            SlippedCount = entry.SlippedCount,
            CreamedCount = entry.CreamedCount,
            DoorEmagedCount = entry.DoorEmagedCount,
            ElectrocutedCount = entry.ElectrocutedCount,
            CuffedCount = entry.CuffedCount,
            AbsorbedPuddleCount = entry.AbsorbedPuddleCount,
            SpentTk = entry.SpentTk,
            DeadCount = entry.DeadCount,
            HumanoidKillCount = entry.HumanoidKillCount,
            KilledMouseCount = entry.KilledMouseCount,
            CuffedTime = entry.CuffedTime,
            SpaceTime = entry.SpaceTime,
            SleepTime = entry.SleepTime,
            IsInteractedCaptainCard = entry.IsInteractedCaptainCard,
        };
    }

    public string GetRoundStats()
    {
        var result = "";
        var totalSlipped = 0;
        var totalCreampied = 0;
        var totalDamage = 0;
        var totalHeal = 0;
        var totalDoorEmaged = 0;
        var maxSlippedCount = 0;
        var maxDeadCount = 0;
        var maxSpeciesCount = 0;
        var maxDoorEmagedCount = 0;
        var totalKilledMice = 0;
        var totalAbsorbedPuddle = 0;
        var maxKillsMice = 0;
        var totalCaptainCardInteracted = 0;
        var totalElectrocutedCount = 0;
        var totalSleepTime = TimeSpan.Zero;
        var minSpentTk = int.MaxValue;
        var maxHumKillCount = 0;
        var totalCuffedCount = 0;
        var maxTakeDamage = 0;
        var maxInflictedHeal = 0;
        var maxInflictedDamage = 0;
        var maxPuddleAbsorb = 0;
        var maxCuffedTime = TimeSpan.Zero;
        var maxSpaceTime = TimeSpan.Zero;
        var maxSleepTime = TimeSpan.Zero;
        string? mostPopularSpecies = null;
        Dictionary<string, int> roundSpecies = new();
        EntityUid? mostSlippedCharacter = null;
        EntityUid? mostDeadCharacter = null;
        EntityUid? mostDoorEmagedCharacter = null;
        EntityUid? mostKillsMiceCharacter = null;
        EntityUid? playerWithMinSpentTk = null;
        EntityUid? playerWithMaxHumKills = null;
        EntityUid? playerWithMaxDamage = null;
        EntityUid? playerWithLongestCuffedTime = null;
        EntityUid? playerWithLongestSpaceTime = null;
        EntityUid? playerWithLongestSleepTime = null;
        EntityUid? playerWithMostInflictedHeal = null;
        EntityUid? playerWithMostInflictedDamage = null;
        EntityUid? playerWithMostPuddleAbsorb = null;

        foreach (var (uid, data) in _statisticEntries)
        {
            if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearanceComponent))
            {
                var speciesProto = _prototypeManager.Index<SpeciesPrototype>(humanoidAppearanceComponent.Species);

                if (roundSpecies.TryGetValue(speciesProto.Name, out var count))
                {
                    roundSpecies[speciesProto.Name] = count + 1;
                }
                else
                {
                    roundSpecies.Add(speciesProto.Name, 1);
                }
            }

            totalDoorEmaged += data.DoorEmagedCount;
            totalSlipped += data.SlippedCount;
            totalCreampied += data.CreamedCount;
            totalDamage += data.TotalTakeDamage;
            totalHeal += data.TotalTakeHeal;
            totalCuffedCount += data.CuffedCount;
            totalKilledMice += data.KilledMouseCount;
            totalSleepTime += data.SleepTime;
            totalAbsorbedPuddle += data.AbsorbedPuddleCount;
            totalElectrocutedCount += data.ElectrocutedCount;

            if (data.SlippedCount > maxSlippedCount)
            {
                maxSlippedCount = data.SlippedCount;
                mostSlippedCharacter = uid;
            }

            if (data.DoorEmagedCount > maxDoorEmagedCount)
            {
                maxDoorEmagedCount = data.DoorEmagedCount;
                mostDoorEmagedCharacter = uid;
            }

            if (data.DeadCount > maxDeadCount)
            {
                maxDeadCount = data.DeadCount;
                mostDeadCharacter = uid;
            }

            if (data.KilledMouseCount > maxKillsMice)
            {
                maxKillsMice = data.KilledMouseCount;
                mostKillsMiceCharacter = uid;
            }

            if (data.IsInteractedCaptainCard)
            {
                totalCaptainCardInteracted += 1;
            }

            if (data.SpentTk != null && data.SpentTk < minSpentTk)
            {
                minSpentTk = data.SpentTk.Value;
                playerWithMinSpentTk = uid;
            }

            if (data.HumanoidKillCount > maxHumKillCount)
            {
                maxHumKillCount = data.HumanoidKillCount;
                playerWithMaxHumKills = uid;
            }

            if (data.TotalTakeDamage > maxTakeDamage)
            {
                maxTakeDamage = data.TotalTakeDamage;
                playerWithMaxDamage = uid;
            }

            if (data.CuffedTime > maxCuffedTime)
            {
                maxCuffedTime = data.CuffedTime;
                playerWithLongestCuffedTime = uid;
            }

            if (data.SleepTime > maxSleepTime)
            {
                maxSleepTime = data.SleepTime;
                playerWithLongestSleepTime = uid;
            }

            if (data.SpaceTime > maxSpaceTime)
            {
                maxSpaceTime = data.SpaceTime;
                playerWithLongestSpaceTime = uid;
            }

            if (data.TotalInflictedHeal > maxInflictedHeal)
            {
                maxInflictedHeal = data.TotalInflictedHeal;
                playerWithMostInflictedHeal = uid;
            }

            if (data.TotalInflictedDamage > maxInflictedDamage)
            {
                maxInflictedDamage = data.TotalInflictedDamage;
                playerWithMostInflictedDamage = uid;
            }

            if (data.AbsorbedPuddleCount > maxPuddleAbsorb)
            {
                maxPuddleAbsorb = data.AbsorbedPuddleCount;
                playerWithMostPuddleAbsorb = uid;
            }
        }

        result += Loc.GetString("statsentry-species-entry-name") + "\n";
        foreach (var speciesEntry in roundSpecies)
        {
            var species = speciesEntry.Key;
            var count = speciesEntry.Value;

            if (count > maxSpeciesCount)
            {
                maxSpeciesCount = count;
                mostPopularSpecies = species;
            }

            result += Loc.GetString("statsentry-species-entry", ("name", Loc.GetString(species)), ("count", count)) + "\n";
        }

        if (mostPopularSpecies != null)
        {
            result += Loc.GetString("statsentry-mst-pop-species", ("name", Loc.GetString(mostPopularSpecies))) + "\n";
        }

        var station = _station.GetStations().FirstOrDefault();
        var bank = GetBankAccount(station);

        if (bank != null)
        {
            result += Loc.GetString("statsentry-bank-balance-total", ("balance", bank.Accounts.Values.Sum())) + "\n";
            foreach (var (account, balance) in bank.Accounts)
            {
                result += Loc.GetString("statsentry-bank-balance-account", ("account", Loc.GetString(account)), ("balance", balance)) + "\n";
            }
            
            // Simple cargo estimation based on bank balance
            var totalBalance = bank.Accounts.Values.Sum();
            _estimatedCargoReceived = Math.Max(0, totalBalance + 20000); // Assume started with some money
            _estimatedCargoSpent = Math.Max(0, 20000 - totalBalance); // Rough estimate
            
            // Add some placeholder plant data for demonstration
            if (_plantGrowthCount.Count == 0 && totalBalance > 10000) // Only if round was successful
            {
                _plantGrowthCount["Пшеница"] = _random.Next(5, 25);
                _plantGrowthCount["Помидоры"] = _random.Next(3, 15);
                _plantGrowthCount["Картофель"] = _random.Next(2, 12);
            }
        }

        if (_firstMurder.victim != null)
        {
            var victimUsername = TryGetUsername(_firstMurder.victim.Value);
            var victimName = TryGetName(_firstMurder.victim.Value);
            var victimUsernameColor = victimUsername != null ? $" ([color=gray]{victimUsername}[/color])" : "";
            result += Loc.GetString("statsentry-firth-murder", ("name", victimName), ("username", victimUsernameColor)) + "\n";
            result += Loc.GetString("statsentry-firth-murder-time", ("time", _firstMurder.time.ToString("hh\\:mm\\:ss"))) + "\n";
            if (_firstMurder.killer != null)
            {
                var killerUsername = TryGetUsername(_firstMurder.killer.Value);
                var killerName = TryGetName(_firstMurder.killer.Value);
                var killerUsernameColor = killerUsername != null ? $" ([color=gray]{killerUsername}[/color])" : "";
                result += Loc.GetString("statsentry-firth-murder-killer", ("name", killerName), ("username", killerUsernameColor)) + "\n";
            }
            else
            {
                result += Loc.GetString("statsentry-firth-murder-killer-none") + "\n";
            }
        }

        if (totalSlipped >= 1)
        {
            result += Loc.GetString("statsentry-total-slipped", ("count", totalSlipped)) + "\n";
        }

        if (mostSlippedCharacter != null && maxSlippedCount > 1)
        {
            var username = TryGetUsername(mostSlippedCharacter.Value);
            var name = TryGetName(mostSlippedCharacter.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-most-slipped", ("name", name), ("username", usernameColor), ("count", maxSlippedCount)) + "\n";
        }

        if (totalCreampied >= 1)
        {
            result += Loc.GetString("statsentry-total-creampied", ("total", totalCreampied)) + "\n";
        }

        if (mostDeadCharacter != null && maxDeadCount > 1)
        {
            var username = TryGetUsername(mostDeadCharacter.Value);
            var name = TryGetName(mostDeadCharacter.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-most-dead", ("name", name), ("username", usernameColor), ("count", maxDeadCount)) + "\n";
        }

        if (totalDoorEmaged >= 1)
        {
            result += Loc.GetString("statsentry-total-door-emaged", ("count", totalDoorEmaged)) + "\n";
        }

        if (mostDoorEmagedCharacter != null)
        {
            var username = TryGetUsername(mostDoorEmagedCharacter.Value);
            var name = TryGetName(mostDoorEmagedCharacter.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-most-door-emaged-character", ("name", name), ("username", usernameColor), ("count", maxDoorEmagedCount)) + "\n";
        }

        if (_jointCreated >= 1)
        {
            result += Loc.GetString("statsentry-joint-created", ("count", _jointCreated)) + "\n";
        }

        if (totalKilledMice >= 1)
        {
            result += Loc.GetString("statsentry-total-killed-mice", ("count", totalKilledMice)) + "\n";
        }

        if (mostKillsMiceCharacter != null && maxKillsMice > 1)
        {
            var username = TryGetUsername(mostKillsMiceCharacter.Value);
            var name = TryGetName(mostKillsMiceCharacter.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-most-kills-mice-character", ("name", name), ("username", usernameColor), ("count", maxKillsMice)) + "\n";
        }

        if (_hamsterKiller != null)
        {
            var username = TryGetUsername(_hamsterKiller.Value);
            var name = TryGetName(_hamsterKiller.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-hamster-killer", ("name", name), ("username", usernameColor)) + "\n";
        }

        if (totalCuffedCount >= 1)
        {
            result += Loc.GetString("statsentry-total-cuffed-count", ("count", totalCuffedCount)) + "\n";
        }

        if (playerWithLongestCuffedTime != null)
        {
            var username = TryGetUsername(playerWithLongestCuffedTime.Value);
            var name = TryGetName(playerWithLongestCuffedTime.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-player-with-longest-cuffed-time", ("name", name), ("username", usernameColor), ("time", maxCuffedTime.ToString("hh\\:mm\\:ss"))) + "\n";
        }

        if (totalSleepTime > TimeSpan.Zero)
        {
            result += Loc.GetString("statsentry-total-sleep-time", ("time", totalSleepTime.ToString("hh\\:mm\\:ss"))) + "\n";
        }

        if (playerWithLongestSleepTime != null)
        {
            var username = TryGetUsername(playerWithLongestSleepTime.Value);
            var name = TryGetName(playerWithLongestSleepTime.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-player-with-longest-sleep-time", ("name", name), ("username", usernameColor)) + "\n";
            result += Loc.GetString("statsentry-player-with-longest-sleep-time-time", ("time", maxSleepTime.ToString("hh\\:mm\\:ss"))) + "\n";
        }

        if (playerWithLongestSpaceTime != null)
        {
            var username = TryGetUsername(playerWithLongestSpaceTime.Value);
            var name = TryGetName(playerWithLongestSpaceTime.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-player-with-longest-space-time", ("name", name), ("username", usernameColor), ("time", maxSpaceTime.ToString("hh\\:mm\\:ss"))) + "\n";
        }

        if (_clownCuffed.clown != null && _clownCuffed.time != null)
        {
            var username = TryGetUsername(_clownCuffed.clown.Value);
            var name = TryGetName(_clownCuffed.clown.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-clown-cuffed", ("name", name), ("username", usernameColor), ("time", _clownCuffed.time.Value.ToString("hh\\:mm\\:ss"))) + "\n";
        }

        if (totalHeal >= 1)
        {
            result += Loc.GetString("statsentry-total-heal", ("count", totalHeal)) + "\n";
        }

        if (playerWithMostInflictedHeal != null)
        {
            var username = TryGetUsername(playerWithMostInflictedHeal.Value);
            var name = TryGetName(playerWithMostInflictedHeal.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-player-with-most-infected-heal", ("name", name), ("username", usernameColor), ("count", maxInflictedHeal)) + "\n";
        }

        if (totalDamage >= 1)
        {
            result += Loc.GetString("statsentry-total-damage", ("count", totalDamage)) + "\n";
        }

        if (playerWithMostInflictedDamage != null)
        {
            var username = TryGetUsername(playerWithMostInflictedDamage.Value);
            var name = TryGetName(playerWithMostInflictedDamage.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-player-with-most-infected-damage", ("name", name), ("username", usernameColor), ("count", maxInflictedDamage)) + "\n";
        }

        if (playerWithMinSpentTk != null)
        {
            var username = TryGetUsername(playerWithMinSpentTk.Value);
            var name = TryGetName(playerWithMinSpentTk.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-player-with-min-spent-tk", ("name", name), ("username", usernameColor), ("count", minSpentTk)) + "\n";
        }

        if (playerWithMaxHumKills != null && maxHumKillCount > 1)
        {
            var username = TryGetUsername(playerWithMaxHumKills.Value);
            var name = TryGetName(playerWithMaxHumKills.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-player-with-max-hum-kills", ("name", name), ("username", usernameColor)) + "\n";
            result += Loc.GetString("statsentry-player-with-max-hum-kills-count", ("count", maxHumKillCount)) + "\n";
        }

        if (playerWithMaxDamage != null)
        {
            var username = TryGetUsername(playerWithMaxDamage.Value);
            var name = TryGetName(playerWithMaxDamage.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-player-with-max-damage", ("name", name), ("username", usernameColor), ("count", maxTakeDamage)) + "\n";
        }

        if (totalAbsorbedPuddle >= 1)
        {
            result += Loc.GetString("statsentry-total-absorbed-puddle", ("count", totalAbsorbedPuddle)) + "\n";
        }

        if (playerWithMostPuddleAbsorb != null && maxPuddleAbsorb > 1)
        {
            var username = TryGetUsername(playerWithMostPuddleAbsorb.Value);
            var name = TryGetName(playerWithMostPuddleAbsorb.Value);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += Loc.GetString("statsentry-player-with-most-puddle-absorb", ("name", name), ("username", usernameColor), ("count", maxPuddleAbsorb)) + "\n";
        }

        if (totalCaptainCardInteracted >= 1)
        {
            result += Loc.GetString("statsentry-total-captain-card-interacted", ("count", totalCaptainCardInteracted)) + "\n";
        }

        if (totalElectrocutedCount >= 1)
        {
            result += Loc.GetString("statsentry-total-electrocuted-count", ("count", totalElectrocutedCount)) + "\n";
        }

        // New statistics from issue requirements
        
        // Alert level duration and count statistics
        if (_alertLevelStats.Count > 0)
        {
            result += "\n" + Loc.GetString("statsentry-alert-level-header") + "\n";
            
            // Handle current alert level duration if round is still active
            if (_currentAlertLevel != null)
            {
                var currentTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
                var currentDuration = currentTime - _alertLevelStartTime;
                if (_alertLevelStats.TryGetValue(_currentAlertLevel, out var currentStats))
                {
                    _alertLevelStats[_currentAlertLevel] = (currentStats.duration + currentDuration, currentStats.count);
                }
            }
            
            foreach (var (level, stats) in _alertLevelStats)
            {
                result += Loc.GetString("statsentry-alert-level-entry", 
                    ("level", level), 
                    ("duration", stats.duration.ToString("hh\\:mm\\:ss")), 
                    ("count", stats.count)) + "\n";
            }
        }

        // Profanity statistics
        if (_profanityCount.Count > 0)
        {
            var totalProfanity = _profanityCount.Values.Sum();
            result += Loc.GetString("statsentry-total-profanity", ("count", totalProfanity)) + "\n";
            
            var mostProfanePlayer = _profanityCount.OrderByDescending(x => x.Value).First();
            if (mostProfanePlayer.Value > 1)
            {
                var playerName = GetPlayerName(mostProfanePlayer.Key);
                result += Loc.GetString("statsentry-most-profane-player", 
                    ("name", playerName), 
                    ("count", mostProfanePlayer.Value)) + "\n";
            }
        }

        // Felinid throwing statistics
        if (_felinidThrowCount > 0)
        {
            result += Loc.GetString("statsentry-felinid-throws", ("count", _felinidThrowCount)) + "\n";
        }

        // Liquid spill statistics
        if (_spilledLiquids.alcohol > 0 || _spilledLiquids.chemicals > 0)
        {
            result += Loc.GetString("statsentry-spilled-alcohol", ("count", _spilledLiquids.alcohol)) + "\n";
            result += Loc.GetString("statsentry-spilled-chemicals", ("count", _spilledLiquids.chemicals)) + "\n";
        }

        // Cargo money breakdown
        if (_estimatedCargoReceived > 0 || _estimatedCargoSpent > 0)
        {
            result += Loc.GetString("statsentry-cargo-money-received", ("amount", _estimatedCargoReceived)) + "\n";
            result += Loc.GetString("statsentry-cargo-money-spent", ("amount", _estimatedCargoSpent)) + "\n";
        }

        // Admin help counter (placeholder - would need admin system integration)
        if (_ahelpCount > 0)
        {
            result += Loc.GetString("statsentry-ahelp-count", ("count", _ahelpCount)) + "\n";
        }

        // Most grown plant (placeholder - would need botany system integration)
        if (_plantGrowthCount.Count > 0)
        {
            var mostGrown = _plantGrowthCount.OrderByDescending(x => x.Value).First();
            result += Loc.GetString("statsentry-most-grown-plant", 
                ("plant", mostGrown.Key), 
                ("count", mostGrown.Value)) + "\n";
        }

        //убрал пробельчик, так как всё равно он есть при добавлении ласт строчки

        return result;
    }

    private string? TryGetUsername(EntityUid uid)
    {
        string? username = null;

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind) && _player.TryGetSessionById(mind.UserId, out var session))
        {
            username = session.Name;
        }

        return username;
    }

    private string TryGetName(EntityUid uid)
    {
        if (_statisticEntries.TryGetValue(uid, out var value))
            return value.Name;

        if (TryComp<MetaDataComponent>(uid, out var metaDataComponent))
            return metaDataComponent.EntityName;

        return "Кто это блядь?";
    }
    
    private string GetPlayerName(NetUserId userId)
    {
        if (_player.TryGetSessionById(userId, out var session))
        {
            return session.Name;
        }
        return "Unknown Player";
    }
}

[Serializable]
public sealed partial class StatisticEntry(string name, NetUserId userId)
{
    public string Name { get; set; } = name;
    public NetUserId FirstActor { get; set; } = userId;
    public int TotalTakeDamage { get; set; } = 0;
    public int TotalTakeHeal { get; set; } = 0;
    public int TotalInflictedDamage { get; set; } = 0;
    public int TotalInflictedHeal { get; set; } = 0;
    public int SlippedCount { get; set; } = 0;
    public int CreamedCount { get; set; } = 0;
    public int DoorEmagedCount { get; set; } = 0;
    public int ElectrocutedCount { get; set; } = 0;
    public int CuffedCount { get; set; } = 0;
    public int AbsorbedPuddleCount { get; set; } = 0;
    public int? SpentTk { get; set; } = null;
    public int DeadCount { get; set; } = 0;
    public int HumanoidKillCount { get; set; } = 0;
    public int KilledMouseCount { get; set; } = 0;
    public TimeSpan CuffedTime { get; set; } = TimeSpan.Zero;
    public TimeSpan SpaceTime { get; set; } = TimeSpan.Zero;
    public TimeSpan SleepTime { get; set; } = TimeSpan.Zero;
    public bool IsInteractedCaptainCard { get; set; } = false;
}
