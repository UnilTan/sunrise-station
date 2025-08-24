using Content.Server.Station.Systems;
using Content.Server.UserInterface;
using Content.Shared._Sunrise.Salvage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Sunrise.Salvage;

/// <summary>
/// System for managing salvage quota console
/// </summary>
public sealed class SalvageQuotaConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<SalvageQuotaConsoleComponent, ComponentInit>(OnComponentInit);
        
        Subs.BuiEvents<SalvageQuotaConsoleComponent>(SalvageQuotaConsoleUiKey.Key, subs =>
        {
            subs.Event<SelectSalvageMissionTypeMessage>(OnSelectMissionType);
            subs.Event<ActivateSalvageShuttleMessage>(OnActivateShuttle);
        });
    }

    private void OnComponentInit(EntityUid uid, SalvageQuotaConsoleComponent component, ComponentInit args)
    {
        UpdateUI(uid, component);
    }

    private void OnSelectMissionType(EntityUid uid, SalvageQuotaConsoleComponent component, SelectSalvageMissionTypeMessage args)
    {
        // Here we would store the selected mission type and update the UI
        if (component.ActivationSound != null)
        {
            _audio.PlayPvs(component.ActivationSound, uid);
        }
        
        UpdateUI(uid, component);
    }

    private void OnActivateShuttle(EntityUid uid, SalvageQuotaConsoleComponent component, ActivateSalvageShuttleMessage args)
    {
        // Here we would activate the salvage shuttle
        if (component.ActivationSound != null)
        {
            _audio.PlayPvs(component.ActivationSound, uid);
        }
        
        UpdateUI(uid, component);
    }

    private void UpdateUI(EntityUid uid, SalvageQuotaConsoleComponent component)
    {
        var state = new SalvageQuotaConsoleState(
            SalvageMissionType.Money, // Default mission type
            false, // Mission active
            true   // Shuttle available
        );

        _ui.SetUiState(uid, SalvageQuotaConsoleUiKey.Key, state);
    }
}