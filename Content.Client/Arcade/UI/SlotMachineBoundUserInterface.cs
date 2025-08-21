using Content.Shared.Arcade;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

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

        _menu = new SlotMachineMenu(this);
        _menu.OnClose += Close;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is SlotMachineMessages.SlotMachineUpdateStateMessage updateMsg)
        {
            _menu?.UpdateState(updateMsg);
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
        }
    }
}