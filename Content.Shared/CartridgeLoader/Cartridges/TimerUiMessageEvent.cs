using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class TimerUiMessageEvent : CartridgeMessageEvent
{
    public readonly TimerUiAction Action;
    public readonly string TimerName;
    public readonly int Duration; // Duration in seconds

    public TimerUiMessageEvent(TimerUiAction action, string timerName, int duration = 0)
    {
        Action = action;
        TimerName = timerName;
        Duration = duration;
    }
}

[Serializable, NetSerializable]
public enum TimerUiAction
{
    Create,
    Remove,
    Start,
    Stop,
    Reset
}