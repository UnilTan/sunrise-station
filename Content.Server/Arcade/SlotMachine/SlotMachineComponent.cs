using Content.Shared.Arcade;
using Robust.Shared.GameStates;

namespace Content.Server.Arcade.SlotMachine;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlotMachineComponent : Component
{
    /// <summary>
    /// Current state of the slot machine
    /// </summary>
    [DataField]
    public SlotMachineMessages.SlotMachineState State = SlotMachineMessages.SlotMachineState.Idle;

    /// <summary>
    /// Player currently using the slot machine
    /// </summary>
    public EntityUid? CurrentPlayer = null;

    /// <summary>
    /// Current credits inserted by the player
    /// </summary>
    [DataField]
    public int CurrentCredits = 0;

    /// <summary>
    /// Amount of the last bet
    /// </summary>
    [DataField]
    public int LastBet = 0;

    /// <summary>
    /// Time when spinning started (for animation timing)
    /// </summary>
    public TimeSpan SpinStartTime = TimeSpan.Zero;

    /// <summary>
    /// Duration of spin animation
    /// </summary>
    [DataField]
    public TimeSpan SpinDuration = TimeSpan.FromSeconds(3.0);

    /// <summary>
    /// Last spin result symbols
    /// </summary>
    [DataField]
    public SlotMachineMessages.SlotSymbol[] LastSpinResult = new SlotMachineMessages.SlotSymbol[3];

    /// <summary>
    /// Minimum bet amount
    /// </summary>
    [DataField]
    public int MinBet = 1;

    /// <summary>
    /// Maximum bet amount
    /// </summary>
    [DataField]
    public int MaxBet = 100;

    /// <summary>
    /// Jackpot amount
    /// </summary>
    [DataField]
    public int JackpotAmount = 1000;

    /// <summary>
    /// Chance of jackpot (0.0 to 1.0)
    /// </summary>
    [DataField]
    public float JackpotChance = 0.001f;

    /// <summary>
    /// Base payout multipliers for different symbol combinations
    /// </summary>
    [DataField]
    public Dictionary<SlotMachineMessages.SlotSymbol, int> BasePayouts = new()
    {
        { SlotMachineMessages.SlotSymbol.Cherry, 2 },
        { SlotMachineMessages.SlotSymbol.Lemon, 3 },
        { SlotMachineMessages.SlotSymbol.Orange, 4 },
        { SlotMachineMessages.SlotSymbol.Bell, 5 },
        { SlotMachineMessages.SlotSymbol.Bar, 10 },
        { SlotMachineMessages.SlotSymbol.Seven, 25 },
        { SlotMachineMessages.SlotSymbol.Diamond, 50 },
        { SlotMachineMessages.SlotSymbol.Jackpot, 100 }
    };

    /// <summary>
    /// Symbol weights for random generation (higher = more common)
    /// </summary>
    [DataField]
    public Dictionary<SlotMachineMessages.SlotSymbol, float> SymbolWeights = new()
    {
        { SlotMachineMessages.SlotSymbol.Cherry, 20.0f },
        { SlotMachineMessages.SlotSymbol.Lemon, 18.0f },
        { SlotMachineMessages.SlotSymbol.Orange, 16.0f },
        { SlotMachineMessages.SlotSymbol.Bell, 14.0f },
        { SlotMachineMessages.SlotSymbol.Bar, 10.0f },
        { SlotMachineMessages.SlotSymbol.Seven, 5.0f },
        { SlotMachineMessages.SlotSymbol.Diamond, 2.0f },
        { SlotMachineMessages.SlotSymbol.Jackpot, 1.0f }
    };
}