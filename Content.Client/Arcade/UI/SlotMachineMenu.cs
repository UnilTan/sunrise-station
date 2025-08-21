using Content.Client.UserInterface.Controls;
using Content.Shared.Arcade;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;

namespace Content.Client.Arcade.UI;

public sealed class SlotMachineMenu : FancyWindow
{
    private readonly SlotMachineBoundUserInterface _owner;
    
    private readonly Label _creditsLabel;
    private readonly Label _lastBetLabel;
    private readonly Label _statusLabel;
    
    private readonly SpinBox _betSpinBox;
    private readonly Button _spinButton;
    private readonly Button _cashOutButton;
    
    private readonly Container _slotsContainer;
    private readonly Label[] _slotLabels = new Label[3];
    
    private SlotMachineMessages.SlotMachineState _currentState = SlotMachineMessages.SlotMachineState.Idle;
    private int _currentCredits = 0;

    public SlotMachineMenu(SlotMachineBoundUserInterface owner)
    {
        _owner = owner;
        
        Title = Loc.GetString("slot-machine-window-title");
        SetSize = (400, 300);
        
        Contents.AddChild(new VBoxContainer
        {
            Children =
            {
                new HBoxContainer
                {
                    Children =
                    {
                        new Label { Text = Loc.GetString("slot-machine-credits-label") },
                        (_creditsLabel = new Label { Text = "0" })
                    }
                },
                new HBoxContainer
                {
                    Children =
                    {
                        new Label { Text = Loc.GetString("slot-machine-last-bet-label") },
                        (_lastBetLabel = new Label { Text = "0" })
                    }
                },
                (_statusLabel = new Label 
                { 
                    Text = Loc.GetString("slot-machine-status-idle"),
                    HorizontalAlignment = Control.HAlignment.Center 
                }),
                new HSeparator(),
                (_slotsContainer = new HBoxContainer
                {
                    HorizontalAlignment = Control.HAlignment.Center,
                    Children =
                    {
                        (_slotLabels[0] = new Label 
                        { 
                            Text = "🍒", 
                            StyleClasses = { "LabelBig" },
                            Margin = new Thickness(10)
                        }),
                        (_slotLabels[1] = new Label 
                        { 
                            Text = "🍒", 
                            StyleClasses = { "LabelBig" },
                            Margin = new Thickness(10)
                        }),
                        (_slotLabels[2] = new Label 
                        { 
                            Text = "🍒", 
                            StyleClasses = { "LabelBig" },
                            Margin = new Thickness(10)
                        })
                    }
                }),
                new HSeparator(),
                new HBoxContainer
                {
                    Children =
                    {
                        new Label { Text = Loc.GetString("slot-machine-bet-label") },
                        (_betSpinBox = new SpinBox
                        {
                            Value = 1,
                            MinValue = 1,
                            MaxValue = 100,
                            Step = 1
                        })
                    }
                },
                new HBoxContainer
                {
                    HorizontalAlignment = Control.HAlignment.Center,
                    Children =
                    {
                        (_spinButton = new Button 
                        { 
                            Text = Loc.GetString("slot-machine-spin-button") 
                        }),
                        (_cashOutButton = new Button 
                        { 
                            Text = Loc.GetString("slot-machine-cash-out-button") 
                        })
                    }
                },
                new Label
                {
                    Text = Loc.GetString("slot-machine-insert-money-hint"),
                    HorizontalAlignment = Control.HAlignment.Center,
                    StyleClasses = { "LabelSubText" }
                }
            }
        });
        
        _spinButton.OnPressed += OnSpinPressed;
        _cashOutButton.OnPressed += OnCashOutPressed;
        
        UpdateUI();
    }

    private void OnSpinPressed(BaseButton.ButtonEventArgs args)
    {
        if (_currentState != SlotMachineMessages.SlotMachineState.Idle || _currentCredits < _betSpinBox.Value)
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
        var canSpin = _currentState == SlotMachineMessages.SlotMachineState.Idle && _currentCredits >= _betSpinBox.Value;
        var canCashOut = _currentCredits > 0;
        
        _spinButton.Disabled = !canSpin;
        _cashOutButton.Disabled = !canCashOut;
        _betSpinBox.Editable = _currentState == SlotMachineMessages.SlotMachineState.Idle;
        
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