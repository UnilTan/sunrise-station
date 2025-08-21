using Content.Shared.Arcade;
using Robust.Client.UserInterface;

namespace Content.Client.Arcade.UI;

public sealed class SlotMachineBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SlotMachineMenu? _menu;

    public SlotMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SlotMachineMenu>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case SlotMachineMessages.SlotMachineUpdateStateMessage updateMsg:
                _menu?.UpdateState(updateMsg);
                break;
        }
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        switch (message)
        {
            case SlotMachineMessages.SlotMachineSpinResultMessage resultMsg:
                _menu?.UpdateSpinResult(resultMsg);
                break;
            case SlotMachineMessages.SlotMachineUpdateStateMessage updateMsg:
                _menu?.UpdateState(updateMsg);
                break;
        }
    }
}