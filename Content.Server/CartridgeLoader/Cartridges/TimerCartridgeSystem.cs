using Content.Server.Administration.Logs;
using Content.Server.Audio;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class TimerCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TimerCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<TimerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        // Check for expired timers and send notifications
        var query = EntityQueryEnumerator<TimerCartridgeComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            CheckExpiredTimers(uid, component);
        }
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(EntityUid uid, TimerCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    /// <summary>
    /// The ui messages received here get wrapped by a CartridgeMessageEvent and are relayed from the <see cref="CartridgeLoaderSystem"/>
    /// </summary>
    private void OnUiMessage(EntityUid uid, TimerCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not TimerUiMessageEvent message)
            return;

        var currentTime = _timing.CurTime;

        switch (message.Action)
        {
            case TimerUiAction.Create:
                if (!string.IsNullOrWhiteSpace(message.TimerName) && message.Duration > 0)
                {
                    var existingTimer = component.Timers.FirstOrDefault(t => t.Name == message.TimerName);
                    if (existingTimer != null)
                    {
                        existingTimer.DurationSeconds = message.Duration;
                        existingTimer.OriginalDurationSeconds = message.Duration;
                        existingTimer.IsRunning = false;
                        existingTimer.StartTime = null;
                    }
                    else
                    {
                        component.Timers.Add(new TimerEntry(message.TimerName, message.Duration));
                    }
                    
                    _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
                        $"{ToPrettyString(args.Actor)} created/updated timer '{message.TimerName}' with duration {message.Duration}s on: {ToPrettyString(uid)}");
                }
                break;

            case TimerUiAction.Remove:
                var timerToRemove = component.Timers.FirstOrDefault(t => t.Name == message.TimerName);
                if (timerToRemove != null)
                {
                    component.Timers.Remove(timerToRemove);
                    _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
                        $"{ToPrettyString(args.Actor)} removed timer '{message.TimerName}' from: {ToPrettyString(uid)}");
                }
                break;

            case TimerUiAction.Start:
                var timerToStart = component.Timers.FirstOrDefault(t => t.Name == message.TimerName);
                if (timerToStart != null && !timerToStart.IsRunning)
                {
                    timerToStart.IsRunning = true;
                    timerToStart.StartTime = currentTime;
                    _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
                        $"{ToPrettyString(args.Actor)} started timer '{message.TimerName}' on: {ToPrettyString(uid)}");
                }
                break;

            case TimerUiAction.Stop:
                var timerToStop = component.Timers.FirstOrDefault(t => t.Name == message.TimerName);
                if (timerToStop != null && timerToStop.IsRunning)
                {
                    timerToStop.IsRunning = false;
                    var remaining = timerToStop.GetRemainingSeconds(currentTime);
                    timerToStop.DurationSeconds = remaining;
                    timerToStop.StartTime = null;
                    _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
                        $"{ToPrettyString(args.Actor)} stopped timer '{message.TimerName}' on: {ToPrettyString(uid)}");
                }
                break;

            case TimerUiAction.Reset:
                var timerToReset = component.Timers.FirstOrDefault(t => t.Name == message.TimerName);
                if (timerToReset != null)
                {
                    timerToReset.IsRunning = false;
                    timerToReset.StartTime = null;
                    timerToReset.DurationSeconds = timerToReset.OriginalDurationSeconds;
                    _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
                        $"{ToPrettyString(args.Actor)} reset timer '{message.TimerName}' on: {ToPrettyString(uid)}");
                }
                break;
        }

        UpdateUiState(uid, GetEntity(args.LoaderUid), component);
    }

    private void CheckExpiredTimers(EntityUid uid, TimerCartridgeComponent component)
    {
        var currentTime = _timing.CurTime;
        var expiredTimers = component.Timers.Where(t => t.IsExpired(currentTime)).ToList();

        foreach (var timer in expiredTimers)
        {
            // Stop the timer
            timer.IsRunning = false;
            timer.StartTime = null;
            timer.DurationSeconds = 0;

            // Play notification sound
            _audio.PlayPvs("/Audio/Machines/timer.ogg", uid);
            
            _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
                $"Timer '{timer.Name}' expired on: {ToPrettyString(uid)}");
        }

        if (expiredTimers.Any())
        {
            // Update UI to reflect expired timers - we need to find the loader entity
            // For now, let's use a simpler approach and just force an update
            Dirty(uid, component);
        }
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, TimerCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new TimerUiState(component.Timers);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }
}