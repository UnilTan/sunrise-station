using Robust.Shared.Serialization;

namespace Content.Shared.Light.Components;

[Serializable, NetSerializable]
public sealed class ConfigurableFlashlightSetColorMessage : BoundUserInterfaceMessage
{
    public Color Color { get; }

    public ConfigurableFlashlightSetColorMessage(Color color)
    {
        Color = color;
    }
}

[Serializable, NetSerializable]
public sealed class ConfigurableFlashlightResetMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ConfigurableFlashlightBuiState : BoundUserInterfaceState
{
    public Color? CustomColor { get; }
    public Color? OriginalColor { get; }

    public ConfigurableFlashlightBuiState(Color? customColor, Color? originalColor)
    {
        CustomColor = customColor;
        OriginalColor = originalColor;
    }
}

public enum ConfigurableFlashlightUiKey : byte
{
    Key
}