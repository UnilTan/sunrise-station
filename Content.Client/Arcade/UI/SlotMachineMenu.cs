using System;
using System.Numerics;
using Content.Client.Arcade.UI;
using Content.Shared.Arcade;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;

namespace Content.Client.Arcade.UI;

public sealed class SlotMachineMenu : DefaultWindow
{
    private readonly SlotMachineBoundUserInterface _owner;
    
    private readonly Label _creditsLabel;
    private readonly Label _lastBetLabel;
    private readonly Label _statusLabel;
    
    private readonly FloatSpinBox _betSpinBox;
    private readonly Button _spinButton;
    private readonly Button _cashOutButton;
    
    private readonly Label[] _slotLabels = new Label[3];
    
    private SlotMachineMessages.SlotMachineState _currentState = SlotMachineMessages.SlotMachineState.Idle;
    private int _currentCredits = 0;

    public SlotMachineMenu(SlotMachineBoundUserInterface owner)
    {
        _owner = owner;
        
        MinSize = SetSize = new Vector2(400, 300);
        Title = Loc.GetString("slot-machine-window-title");
        
        var grid = new GridContainer { Columns = 1 };
        
        // Credits info
        var creditsGrid = new GridContainer { Columns = 2 };
        creditsGrid.AddChild(new Label { Text = Loc.GetString("slot-machine-credits-label") });
        _creditsLabel = new Label { Text = "0" };
        creditsGrid.AddChild(_creditsLabel);
        
        creditsGrid.AddChild(new Label { Text = Loc.GetString("slot-machine-last-bet-label") });
        _lastBetLabel = new Label { Text = "0" };
        creditsGrid.AddChild(_lastBetLabel);
        
        grid.AddChild(creditsGrid);
        
        // Status
        _statusLabel = new Label 
        { 
            Text = Loc.GetString("slot-machine-status-idle"),
            Align = Label.AlignMode.Center 
        };
        grid.AddChild(_statusLabel);
        
        // Slot display
        var slotsGrid = new GridContainer { Columns = 3 };
        for (int i = 0; i < 3; i++)
        {
            _slotLabels[i] = new Label 
            { 
                Text = "🍒", 
                Align = Label.AlignMode.Center,
                StyleClasses = { "LabelBig" }
            };
            slotsGrid.AddChild(_slotLabels[i]);
        }
        
        var slotsCenterContainer = new CenterContainer();
        slotsCenterContainer.AddChild(slotsGrid);
        grid.AddChild(slotsCenterContainer);
        
        // Bet controls
        var betGrid = new GridContainer { Columns = 2 };
        betGrid.AddChild(new Label { Text = Loc.GetString("slot-machine-bet-label") });
        _betSpinBox = new FloatSpinBox { Value = 1 };
        betGrid.AddChild(_betSpinBox);
        grid.AddChild(betGrid);
        
        // Action buttons
        var buttonGrid = new GridContainer { Columns = 2 };
        
        _spinButton = new Button { Text = Loc.GetString("slot-machine-spin-button") };
        _spinButton.OnPressed += OnSpinPressed;
        buttonGrid.AddChild(_spinButton);
        
        _cashOutButton = new Button { Text = Loc.GetString("slot-machine-cash-out-button") };
        _cashOutButton.OnPressed += OnCashOutPressed;
        buttonGrid.AddChild(_cashOutButton);
        
        var buttonCenterContainer = new CenterContainer();
        buttonCenterContainer.AddChild(buttonGrid);
        grid.AddChild(buttonCenterContainer);
        
        // Help text
        grid.AddChild(new Label
        {
            Text = Loc.GetString("slot-machine-insert-money-hint"),
            Align = Label.AlignMode.Center,
            StyleClasses = { "LabelSubText" }
        });
        
        Contents.AddChild(grid);
        
        UpdateUI();
    }

    private void OnSpinPressed(BaseButton.ButtonEventArgs args)
    {
        if (_currentState != SlotMachineMessages.SlotMachineState.Idle || _currentCredits < (int)_betSpinBox.Value)
            return;
            
        _owner.SendMessage(new SlotMachineMessages.SlotMachinePlayerActionMessage(
            SlotMachineMessages.SlotMachinePlayerAction.Spin, (int)_betSpinBox.Value));
    }

    private void OnCashOutPressed(BaseButton.ButtonEventArgs args)
    {
        if (_currentCredits <= 0)
            return;
            
        _owner.SendMessage(new SlotMachineMessages.SlotMachinePlayerActionMessage(
            SlotMachineMessages.SlotMachinePlayerAction.CashOut));
    }

    public void UpdateState(SlotMachineMessages.SlotMachineUpdateStateMessage message)
    {
        _currentState = message.State;
        _currentCredits = message.PlayerCredits;
        
        _creditsLabel.Text = message.PlayerCredits.ToString();
        _lastBetLabel.Text = message.LastBet.ToString();
        
        _statusLabel.Text = _currentState switch
        {
            SlotMachineMessages.SlotMachineState.Idle => Loc.GetString("slot-machine-status-idle"),
            SlotMachineMessages.SlotMachineState.Spinning => Loc.GetString("slot-machine-status-spinning"),
            SlotMachineMessages.SlotMachineState.ShowingResult => Loc.GetString("slot-machine-status-result"),
            SlotMachineMessages.SlotMachineState.OutOfOrder => Loc.GetString("slot-machine-status-broken"),
            SlotMachineMessages.SlotMachineState.Hacked => Loc.GetString("slot-machine-status-hacked"),
            _ => "Unknown"
        };
        
        UpdateUI();
    }

    public void UpdateSpinResult(SlotMachineMessages.SlotMachineSpinResultMessage message)
    {
        for (int i = 0; i < _slotLabels.Length && i < message.Symbols.Length; i++)
        {
            _slotLabels[i].Text = GetSymbolText(message.Symbols[i]);
        }
        
        if (message.Payout > 0)
        {
            _statusLabel.Text = message.IsJackpot 
                ? Loc.GetString("slot-machine-jackpot", ("payout", message.Payout))
                : Loc.GetString("slot-machine-win", ("payout", message.Payout));
        }
        else
        {
            _statusLabel.Text = Loc.GetString("slot-machine-lose");
        }
    }

    private void UpdateUI()
    {
        var canSpin = _currentState == SlotMachineMessages.SlotMachineState.Idle && _currentCredits >= (int)_betSpinBox.Value;
        var canCashOut = _currentCredits > 0;
        
        _spinButton.Disabled = !canSpin;
        _cashOutButton.Disabled = !canCashOut;
        
        // Animate slots when spinning
        if (_currentState == SlotMachineMessages.SlotMachineState.Spinning)
        {
            // Simple animation by cycling through symbols
            var symbols = new[] { "🍒", "🍋", "🍊", "🔔", "⬜", "7️⃣", "💎", "💰" };
            var currentTime = DateTime.Now.Millisecond / 100;
            
            for (int i = 0; i < _slotLabels.Length; i++)
            {
                _slotLabels[i].Text = symbols[(currentTime + i) % symbols.Length];
            }
        }
    }

    private string GetSymbolText(SlotMachineMessages.SlotSymbol symbol)
    {
        return symbol switch
        {
            SlotMachineMessages.SlotSymbol.Cherry => "🍒",
            SlotMachineMessages.SlotSymbol.Lemon => "🍋",
            SlotMachineMessages.SlotSymbol.Orange => "🍊",
            SlotMachineMessages.SlotSymbol.Bell => "🔔",
            SlotMachineMessages.SlotSymbol.Bar => "⬜",
            SlotMachineMessages.SlotSymbol.Seven => "7️⃣",
            SlotMachineMessages.SlotSymbol.Diamond => "💎",
            SlotMachineMessages.SlotSymbol.Jackpot => "💰",
            _ => "?"
        };
    }
}