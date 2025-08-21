using Content.Client.UserInterface.Controls;
using Content.Shared.CodeLock;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using System.Numerics;

namespace Content.Client.CodeLock.UI;

/// <summary>
/// Window for the code lock keypad interface.
/// </summary>
public sealed partial class CodeLockWindow : DefaultWindow
{
    private readonly GridContainer _keypadGrid;
    private readonly Label _displayLabel;
    private readonly Label _statusLabel;
    private readonly Button _clearButton;
    private readonly Button _enterButton;

    public CodeLockWindow()
    {
        Title = Loc.GetString("code-lock-window-title");
        SetSize = new Vector2(300, 400);

        var vbox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(10),
            SeparationOverride = 5
        };

        // Status label
        _statusLabel = new Label
        {
            Text = Loc.GetString("code-lock-status-locked"),
            HorizontalAlignment = Control.HAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };
        vbox.AddChild(_statusLabel);

        // Code display
        _displayLabel = new Label
        {
            Text = "****",
            HorizontalAlignment = Control.HAlignment.Center,
            StyleClasses = { "CodeLockDisplay" },
            Margin = new Thickness(0, 0, 0, 15)
        };
        vbox.AddChild(_displayLabel);

        // Keypad grid
        _keypadGrid = new GridContainer
        {
            Columns = 3,
            HorizontalAlignment = Control.HAlignment.Center,
            VerticalAlignment = Control.VAlignment.Center
        };

        // Add number buttons (1-9)
        for (int i = 1; i <= 9; i++)
        {
            var button = new Button
            {
                Text = i.ToString(),
                SetSize = new Vector2(50, 50),
                Name = $"Keypad{i}"
            };
            
            var value = i; // Capture for closure
            button.OnPressed += _ => OnKeypadPressed(value);
            _keypadGrid.AddChild(button);
        }

        // Add bottom row: Clear, 0, Enter
        _clearButton = new Button
        {
            Text = Loc.GetString("code-lock-clear"),
            SetSize = new Vector2(50, 50)
        };
        _clearButton.OnPressed += _ => OnClearPressed();
        _keypadGrid.AddChild(_clearButton);

        var zeroButton = new Button
        {
            Text = "0",
            SetSize = new Vector2(50, 50)
        };
        zeroButton.OnPressed += _ => OnKeypadPressed(0);
        _keypadGrid.AddChild(zeroButton);

        _enterButton = new Button
        {
            Text = Loc.GetString("code-lock-enter"),
            SetSize = new Vector2(50, 50)
        };
        _enterButton.OnPressed += _ => OnEnterPressed();
        _keypadGrid.AddChild(_enterButton);

        vbox.AddChild(_keypadGrid);

        Contents.AddChild(vbox);
    }

    public event Action<int>? KeypadPressed;
    public event Action? ClearPressed;
    public event Action? EnterPressed;

    private void OnKeypadPressed(int value)
    {
        KeypadPressed?.Invoke(value);
    }

    private void OnClearPressed()
    {
        ClearPressed?.Invoke();
    }

    private void OnEnterPressed()
    {
        EnterPressed?.Invoke();
    }

    public void UpdateState(CodeLockUserInterfaceState state)
    {
        // Update display
        var displayText = "";
        for (int i = 0; i < state.EnteredCodeLength; i++)
        {
            displayText += "*";
        }
        for (int i = state.EnteredCodeLength; i < state.MaxCodeLength; i++)
        {
            displayText += "_";
        }
        _displayLabel.Text = displayText;

        // Update status
        if (state.IsLockedOut)
        {
            _statusLabel.Text = Loc.GetString("code-lock-status-locked-out", ("time", state.RemainingLockoutTime));
            _statusLabel.StyleClasses.Clear();
            _statusLabel.StyleClasses.Add("CodeLockStatusError");
        }
        else if (state.IsUnlocked)
        {
            _statusLabel.Text = Loc.GetString("code-lock-status-unlocked");
            _statusLabel.StyleClasses.Clear();
            _statusLabel.StyleClasses.Add("CodeLockStatusSuccess");
        }
        else
        {
            _statusLabel.Text = Loc.GetString("code-lock-status-locked");
            _statusLabel.StyleClasses.Clear();
            _statusLabel.StyleClasses.Add("CodeLockStatusNormal");
        }

        // Disable buttons when locked out
        var buttonsEnabled = !state.IsLockedOut;
        foreach (var child in _keypadGrid.Children)
        {
            if (child is Button button)
            {
                button.Disabled = !buttonsEnabled;
            }
        }
    }
}