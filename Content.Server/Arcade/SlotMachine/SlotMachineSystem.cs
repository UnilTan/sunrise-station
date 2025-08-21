using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared.Arcade;
using Content.Shared.Cargo.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Stacks;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Arcade.SlotMachine;

public sealed class SlotMachineSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlotMachineComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SlotMachineComponent, AfterActivatableUIOpenEvent>(OnAfterUIOpen);
        SubscribeLocalEvent<SlotMachineComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SlotMachineComponent, InteractUsingEvent>(OnInteractUsing);

        Subs.BuiEvents<SlotMachineComponent>(SlotMachineUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnUIClose);
            subs.Event<SlotMachineMessages.SlotMachinePlayerActionMessage>(OnPlayerAction);
        });
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SlotMachineComponent>();
        while (query.MoveNext(out var uid, out var slotMachine))
        {
            if (slotMachine.State == SlotMachineMessages.SlotMachineState.Spinning)
            {
                var elapsed = _timing.CurTime - slotMachine.SpinStartTime;
                if (elapsed >= slotMachine.SpinDuration)
                {
                    CompleteSpinning(uid, slotMachine);
                }
            }
        }
    }

    private void OnComponentInit(EntityUid uid, SlotMachineComponent component, ComponentInit args)
    {
        // Initialize the last spin result with random symbols
        for (int i = 0; i < component.LastSpinResult.Length; i++)
        {
            component.LastSpinResult[i] = GetRandomSymbol(component);
        }
    }

    private void OnAfterUIOpen(EntityUid uid, SlotMachineComponent component, AfterActivatableUIOpenEvent args)
    {
        component.CurrentPlayer = args.Actor;
        UpdateUI(uid, component);
    }

    private void OnUIClose(EntityUid uid, SlotMachineComponent component, BoundUIClosedEvent args)
    {
        if (component.CurrentPlayer == args.Actor)
        {
            // Cash out any remaining credits when player leaves
            if (component.CurrentCredits > 0)
            {
                CashOut(uid, component, args.Actor);
            }
            component.CurrentPlayer = null;
        }
    }

    private void OnPowerChanged(EntityUid uid, SlotMachineComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            _uiSystem.CloseUi(uid, SlotMachineUiKey.Key);
            component.CurrentPlayer = null;
            component.State = SlotMachineMessages.SlotMachineState.OutOfOrder;
        }
        else
        {
            component.State = SlotMachineMessages.SlotMachineState.Idle;
        }
    }

    private void OnInteractUsing(EntityUid uid, SlotMachineComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // Check if player is trying to insert money
        if (TryComp<CashComponent>(args.Used, out _) && TryComp<StackComponent>(args.Used, out var stack))
        {
            if (component.CurrentPlayer != args.User)
            {
                _popup.PopupEntity("You need to activate the slot machine first!", uid, args.User, PopupType.Medium);
                return;
            }

            var amount = stack.Count;
            component.CurrentCredits += amount;

            // Remove the cash from player's hand
            QueueDel(args.Used);

            _popup.PopupEntity($"Inserted {amount} spesos. Credits: {component.CurrentCredits}", uid, args.User, PopupType.Medium);
            
            UpdateUI(uid, component);
            args.Handled = true;
        }
    }

    private void OnPlayerAction(EntityUid uid, SlotMachineComponent component, SlotMachineMessages.SlotMachinePlayerActionMessage msg)
    {
        if (msg.Actor != component.CurrentPlayer)
            return;

        switch (msg.PlayerAction)
        {
            case SlotMachineMessages.SlotMachinePlayerAction.Spin:
                TryStartSpin(uid, component, msg.Actor, msg.BetAmount);
                break;
            case SlotMachineMessages.SlotMachinePlayerAction.CashOut:
                CashOut(uid, component, msg.Actor);
                break;
        }
    }

    private bool TryStartSpin(EntityUid uid, SlotMachineComponent component, EntityUid player, int betAmount)
    {
        if (component.State != SlotMachineMessages.SlotMachineState.Idle)
            return false;

        if (betAmount < component.MinBet || betAmount > component.MaxBet)
        {
            _popup.PopupEntity($"Bet must be between {component.MinBet} and {component.MaxBet} spesos!", uid, player, PopupType.Medium);
            return false;
        }

        if (component.CurrentCredits < betAmount)
        {
            _popup.PopupEntity("Not enough credits!", uid, player, PopupType.Medium);
            return false;
        }

        // Deduct bet from credits
        component.CurrentCredits -= betAmount;
        component.LastBet = betAmount;

        // Start spinning
        component.State = SlotMachineMessages.SlotMachineState.Spinning;
        component.SpinStartTime = _timing.CurTime;

        // Play spinning sound
        //_audio.PlayPvs(new SoundSpecifier.Path("/Audio/Machines/slotmachine_spin.ogg"), uid);

        UpdateUI(uid, component);
        return true;
    }

    private void CompleteSpinning(EntityUid uid, SlotMachineComponent component)
    {
        if (component.CurrentPlayer == null)
            return;

        // Generate spin result
        var symbols = new SlotMachineMessages.SlotSymbol[3];
        for (int i = 0; i < 3; i++)
        {
            symbols[i] = GetRandomSymbol(component);
        }

        component.LastSpinResult = symbols;

        // Calculate payout
        var payout = CalculatePayout(symbols, component.LastBet, component);
        var isJackpot = IsJackpot(symbols);

        if (isJackpot)
        {
            payout += component.JackpotAmount;
            _popup.PopupEntity("JACKPOT!!!", uid, component.CurrentPlayer.Value, PopupType.LargeCaution);
        }

        // Add winnings to credits
        component.CurrentCredits += payout;

        // Update state
        component.State = SlotMachineMessages.SlotMachineState.ShowingResult;

        // Send result to client
        _uiSystem.ServerSendUiMessage(uid, SlotMachineUiKey.Key, 
            new SlotMachineMessages.SlotMachineSpinResultMessage(symbols, payout, isJackpot), 
            component.CurrentPlayer.Value);

        UpdateUI(uid, component);

        // Return to idle after showing result
        Timer.Spawn(TimeSpan.FromSeconds(2), () =>
        {
            if (Exists(uid) && TryComp<SlotMachineComponent>(uid, out var comp))
            {
                comp.State = SlotMachineMessages.SlotMachineState.Idle;
                UpdateUI(uid, comp);
            }
        });
    }

    private SlotMachineMessages.SlotSymbol GetRandomSymbol(SlotMachineComponent component)
    {
        var totalWeight = component.SymbolWeights.Values.Sum();
        var randomValue = _random.NextFloat() * totalWeight;

        float currentWeight = 0;
        foreach (var (symbol, weight) in component.SymbolWeights)
        {
            currentWeight += weight;
            if (randomValue <= currentWeight)
                return symbol;
        }

        return SlotMachineMessages.SlotSymbol.Cherry; // Fallback
    }

    private int CalculatePayout(SlotMachineMessages.SlotSymbol[] symbols, int bet, SlotMachineComponent component)
    {
        // Check for three matching symbols
        if (symbols[0] == symbols[1] && symbols[1] == symbols[2])
        {
            if (component.BasePayouts.TryGetValue(symbols[0], out var multiplier))
            {
                return bet * multiplier;
            }
        }

        // Check for two matching symbols (smaller payout)
        var uniqueSymbols = symbols.Distinct().Count();
        if (uniqueSymbols == 2)
        {
            var mostCommon = symbols.GroupBy(s => s).OrderByDescending(g => g.Count()).First().Key;
            if (component.BasePayouts.TryGetValue(mostCommon, out var multiplier))
            {
                return bet * Math.Max(1, multiplier / 4); // Quarter payout for two matches
            }
        }

        return 0; // No payout
    }

    private bool IsJackpot(SlotMachineMessages.SlotSymbol[] symbols)
    {
        return symbols.All(s => s == SlotMachineMessages.SlotSymbol.Jackpot);
    }

    private void CashOut(EntityUid uid, SlotMachineComponent component, EntityUid player)
    {
        if (component.CurrentCredits <= 0)
        {
            _popup.PopupEntity("No credits to cash out!", uid, player, PopupType.Medium);
            return;
        }

        // Spawn cash and give to player
        var cashEntity = Spawn("SpaceCash", Transform(uid).Coordinates);
        if (TryComp<StackComponent>(cashEntity, out var stack))
        {
            _stackSystem.SetCount(cashEntity, component.CurrentCredits, stack);
        }

        _handsSystem.TryPickupAnyHand(player, cashEntity);

        _popup.PopupEntity($"Cashed out {component.CurrentCredits} spesos!", uid, player, PopupType.Medium);

        component.CurrentCredits = 0;
        UpdateUI(uid, component);
    }

    private void UpdateUI(EntityUid uid, SlotMachineComponent component)
    {
        if (component.CurrentPlayer == null)
            return;

        _uiSystem.ServerSendUiMessage(uid, SlotMachineUiKey.Key,
            new SlotMachineMessages.SlotMachineUpdateStateMessage(component.State, component.CurrentCredits, component.LastBet),
            component.CurrentPlayer.Value);
    }
}