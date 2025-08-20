using System.Globalization;
using Content.Shared.Light.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Light.UI;

public sealed class ConfigurableFlashlightWindow : DefaultWindow
{
    private readonly LineEdit _hexInput;
    private readonly ColorPreview _colorPreview;
    private readonly Button _resetButton;
    private readonly Button _applyButton;

    public event Action<Color>? OnColorChanged;
    public event Action? OnResetPressed;

    public ConfigurableFlashlightWindow()
    {
        MinSize = (300, 150);
        Title = Loc.GetString("configurable-flashlight-window-title");

        var vbox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5
        };

        // HEX input section
        var hexSection = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 10
        };

        var hexLabel = new Label
        {
            Text = Loc.GetString("configurable-flashlight-hex-label"),
            MinSize = (80, 0)
        };

        _hexInput = new LineEdit
        {
            PlaceholderText = Loc.GetString("configurable-flashlight-hex-placeholder"),
            MinSize = (120, 0)
        };

        _hexInput.OnTextChanged += OnHexInputChanged;

        hexSection.AddChild(hexLabel);
        hexSection.AddChild(_hexInput);

        // Color preview
        _colorPreview = new ColorPreview
        {
            MinSize = (60, 30),
            Color = Color.White
        };

        hexSection.AddChild(_colorPreview);

        // Buttons section
        var buttonSection = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 10
        };

        _resetButton = new Button
        {
            Text = Loc.GetString("configurable-flashlight-reset-button")
        };

        _applyButton = new Button
        {
            Text = Loc.GetString("configurable-flashlight-apply-button")
        };

        _resetButton.OnPressed += OnResetButtonPressed;
        _applyButton.OnPressed += OnApplyButtonPressed;

        buttonSection.AddChild(_resetButton);
        buttonSection.AddChild(_applyButton);

        vbox.AddChild(hexSection);
        vbox.AddChild(buttonSection);

        Contents.AddChild(vbox);
    }

    public void UpdateState(ConfigurableFlashlightBuiState state)
    {
        var currentColor = state.CustomColor ?? state.OriginalColor ?? Color.White;
        _colorPreview.Color = currentColor;
        _hexInput.Text = ColorToHex(currentColor);
    }

    private void OnHexInputChanged(LineEdit.LineEditEventArgs args)
    {
        if (TryParseHex(args.Text, out var color))
        {
            _colorPreview.Color = color;
            _hexInput.ModulateSelfOverride = null; // Reset to normal color
        }
        else
        {
            _hexInput.ModulateSelfOverride = Color.LightCoral; // Show error
        }
    }

    private void OnApplyButtonPressed(Button.ButtonEventArgs args)
    {
        if (TryParseHex(_hexInput.Text, out var color))
        {
            OnColorChanged?.Invoke(color);
        }
    }

    private void OnResetButtonPressed(Button.ButtonEventArgs args)
    {
        OnResetPressed?.Invoke();
    }

    private bool TryParseHex(string hex, out Color color)
    {
        color = Color.White;

        if (string.IsNullOrWhiteSpace(hex))
            return false;

        // Remove # if present
        hex = hex.TrimStart('#');

        // Support both 6-digit (RGB) and 8-digit (ARGB) hex
        if (hex.Length == 6)
            hex = "FF" + hex; // Add full alpha

        if (hex.Length != 8)
            return false;

        if (!uint.TryParse(hex, NumberStyles.HexNumber, null, out var value))
            return false;

        var a = (byte)((value >> 24) & 0xFF);
        var r = (byte)((value >> 16) & 0xFF);
        var g = (byte)((value >> 8) & 0xFF);
        var b = (byte)(value & 0xFF);

        color = Color.FromSrgb(new ColorByte(r, g, b, a));
        return true;
    }

    private string ColorToHex(Color color)
    {
        var srgb = color.ToSrgb();
        return $"#{srgb.R:X2}{srgb.G:X2}{srgb.B:X2}";
    }

    private sealed class ColorPreview : Control
    {
        public Color Color { get; set; } = Color.White;

        protected override void Draw(DrawingHandleScreen handle)
        {
            handle.DrawRect(PixelSizeBox, Color);
            handle.DrawRect(PixelSizeBox, Color.Black, filled: false);
        }
    }
}