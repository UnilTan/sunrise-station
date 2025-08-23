using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.PaintMixer;

[Serializable, NetSerializable]
public sealed class PaintMixerUpdateState : BoundUserInterfaceState
{
    public Color SelectedColor;
    public bool IsMixing;
    public bool CanMix;

    public PaintMixerUpdateState(Color selectedColor, bool isMixing, bool canMix)
    {
        SelectedColor = selectedColor;
        IsMixing = isMixing;
        CanMix = canMix;
    }
}

/// <summary>
/// Message sent when the player changes the selected color
/// </summary>
[Serializable, NetSerializable]
public sealed class PaintMixerSetColorMessage : BoundUserInterfaceMessage
{
    public Color Color;

    public PaintMixerSetColorMessage(Color color)
    {
        Color = color;
    }
}

/// <summary>
/// Message sent when the player wants to start mixing paint
/// </summary>
[Serializable, NetSerializable]
public sealed class PaintMixerStartMixingMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Request to sync the current state
/// </summary>
[Serializable, NetSerializable]
public sealed class PaintMixerSyncRequestMessage : BoundUserInterfaceMessage
{
}

[NetSerializable, Serializable]
public enum PaintMixerUiKey
{
    Key
}