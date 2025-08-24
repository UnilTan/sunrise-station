using Content.Shared._Sunrise.Salvage;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.Salvage.UI;

[UsedImplicitly]
public sealed class EnhancedSalvageShuttleControlBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private EnhancedSalvageShuttleControlWindow? _window;

    public EnhancedSalvageShuttleControlBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindowCentered<EnhancedSalvageShuttleControlWindow>();
        _window.Title = Loc.GetString("enhanced-salvage-shuttle-control-window-title");
        
        _window.OnStartMission += OnStartMission;
        _window.OnEndMission += OnEndMission;
        _window.OnActivateEmergency += OnActivateEmergency;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not EnhancedSalvageShuttleControlState current || _window == null)
            return;

        _window.UpdateState(current);
    }

    private void OnStartMission(SalvageMissionType missionType)
    {
        SendMessage(new StartSalvageMissionMessage(missionType));
    }

    private void OnEndMission()
    {
        SendMessage(new EndSalvageMissionMessage());
    }

    private void OnActivateEmergency()
    {
        SendMessage(new ActivateEmergencyModeMessage());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (disposing)
        {
            _window?.Dispose();
        }
    }
}