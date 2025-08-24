using Content.Shared._Sunrise.Salvage;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.Salvage.UI;

[UsedImplicitly]
public sealed class SalvageQuotaConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SalvageQuotaConsoleWindow? _window;

    public SalvageQuotaConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindowCentered<SalvageQuotaConsoleWindow>();
        _window.Title = Loc.GetString("salvage-quota-console-window-title");
        
        _window.OnMissionTypeSelected += OnMissionTypeSelected;
        _window.OnActivateShuttle += OnActivateShuttle;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SalvageQuotaConsoleState current || _window == null)
            return;

        _window.UpdateState(current);
    }

    private void OnMissionTypeSelected(SalvageMissionType missionType)
    {
        SendMessage(new SelectSalvageMissionTypeMessage(missionType));
    }

    private void OnActivateShuttle()
    {
        SendMessage(new ActivateSalvageShuttleMessage());
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