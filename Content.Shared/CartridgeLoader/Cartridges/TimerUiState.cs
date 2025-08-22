using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class TimerUiState : BoundUserInterfaceState
{
    public List<TimerEntry> Timers;

    public TimerUiState(List<TimerEntry> timers)
    {
        Timers = timers;
    }
}

[Serializable, NetSerializable]
public sealed class TimerEntry
{
    public string Name { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public int OriginalDurationSeconds { get; set; }
    public TimeSpan? StartTime { get; set; }
    public bool IsRunning { get; set; }

    public TimerEntry()
    {
    }

    public TimerEntry(string name, int durationSeconds)
    {
        Name = name;
        DurationSeconds = durationSeconds;
        OriginalDurationSeconds = durationSeconds;
        IsRunning = false;
        StartTime = null;
    }

    public int GetRemainingSeconds(TimeSpan currentTime)
    {
        if (!IsRunning || StartTime == null)
            return DurationSeconds;

        var elapsed = (currentTime - StartTime.Value).TotalSeconds;
        var remaining = DurationSeconds - (int)elapsed;
        return Math.Max(0, remaining);
    }

    public bool IsExpired(TimeSpan currentTime)
    {
        return IsRunning && GetRemainingSeconds(currentTime) <= 0;
    }
}