using Content.Client.UserInterface.Controls;
using Content.Shared._Sunrise.PaintMixer;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client._Sunrise.PaintMixer.UI;

/// <summary>
/// UI window for the paint mixer machine
/// </summary>
public sealed partial class PaintMixerWindow : DefaultWindow
{
    [Dependency] private readonly ILocalizationManager _loc = default!;

    // UI Controls
    private ColorSelectorSliders? _colorSelector;
    private Label? _selectedColorLabel;
    private Button? _mixButton;
    private ProgressBar? _mixingProgress;
    private Label? _statusLabel;

    // Events
    public event Action<Color>? OnColorSelected;
    public event Action? OnMixRequested;

    public PaintMixerWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = _loc.GetString("paint-mixer-window-title");
        
        SetupUI();
    }

    private void SetupUI()
    {
        var vbox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        Contents.AddChild(vbox);

        // Title
        var titleLabel = new Label
        {
            Text = _loc.GetString("paint-mixer-window-title"),
            Margin = new Thickness(0, 0, 0, 10)
        };
        vbox.AddChild(titleLabel);

        // Color selection section
        var colorSection = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        vbox.AddChild(colorSection);

        var colorLabel = new Label { Text = _loc.GetString("paint-mixer-select-color") };
        colorSection.AddChild(colorLabel);

        // Color picker
        _colorSelector = new ColorSelectorSliders
        {
            Margin = new Thickness(0, 5, 0, 5)
        };
        _colorSelector.OnColorChanged += OnColorChanged;
        colorSection.AddChild(_colorSelector);

        // Selected color display
        _selectedColorLabel = new Label
        {
            Text = _loc.GetString("paint-mixer-selected-color", ("color", "#FF0000")),
            Margin = new Thickness(0, 5, 0, 10)
        };
        colorSection.AddChild(_selectedColorLabel);

        // Mix button
        _mixButton = new Button
        {
            Text = _loc.GetString("paint-mixer-mix-button"),
            Margin = new Thickness(0, 10, 0, 5)
        };
        _mixButton.OnPressed += OnMixButtonPressed;
        vbox.AddChild(_mixButton);

        // Status section
        _statusLabel = new Label
        {
            Text = _loc.GetString("paint-mixer-ready"),
            Margin = new Thickness(0, 5, 0, 5)
        };
        vbox.AddChild(_statusLabel);

        // Progress bar
        _mixingProgress = new ProgressBar
        {
            Visible = false,
            Margin = new Thickness(0, 5, 0, 10)
        };
        vbox.AddChild(_mixingProgress);

        SetSize = new Vector2i(400, 450);
    }

    private void OnColorChanged(Color color)
    {
        UpdateSelectedColorDisplay(color);
        OnColorSelected?.Invoke(color);
    }

    private void OnMixButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        OnMixRequested?.Invoke();
    }

    public void UpdateState(PaintMixerUpdateState state)
    {
        if (_colorSelector != null)
            _colorSelector.Color = state.SelectedColor;

        UpdateSelectedColorDisplay(state.SelectedColor);

        if (_mixButton != null)
            _mixButton.Disabled = !state.CanMix || state.IsMixing;

        if (_mixingProgress != null)
            _mixingProgress.Visible = state.IsMixing;

        if (_statusLabel != null)
        {
            if (state.IsMixing)
                _statusLabel.Text = _loc.GetString("paint-mixer-mixing");
            else if (!state.CanMix)
                _statusLabel.Text = _loc.GetString("paint-mixer-insufficient-materials");
            else
                _statusLabel.Text = _loc.GetString("paint-mixer-ready");
        }
    }

    private void UpdateSelectedColorDisplay(Color color)
    {
        if (_selectedColorLabel != null)
        {
            var hexColor = color.ToHex();
            _selectedColorLabel.Text = _loc.GetString("paint-mixer-selected-color", ("color", hexColor));
            _selectedColorLabel.Modulate = color;
        }
    }
}