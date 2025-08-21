using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Logic component that displays the received signal value on screen.
/// Based on the "Текстовый экран" component from the issue description.
/// </summary>
[RegisterComponent, Access(typeof(TextScreenSystem))]
public sealed partial class TextScreenComponent : Component
{
    /// <summary>
    /// Name of the input port.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "Input";

    /// <summary>
    /// The current displayed text.
    /// Doesn't change when receiving empty signals.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string DisplayText = "";

    /// <summary>
    /// Maximum number of characters to display.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxDisplayLength = 256;
}