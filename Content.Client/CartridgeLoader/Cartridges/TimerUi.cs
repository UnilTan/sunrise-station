using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class TimerUi : UIFragment
{
    private TimerUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new TimerUiFragment();
        _fragment.OnTimerAction += (action, timerName, duration) => SendTimerMessage(action, timerName, duration, userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not TimerUiState timerState)
            return;

        _fragment?.UpdateState(timerState.Timers);
    }

    private void SendTimerMessage(TimerUiAction action, string timerName, int duration, BoundUserInterface userInterface)
    {
        var timerMessage = new TimerUiMessageEvent(action, timerName, duration);
        var message = new CartridgeUiMessage(timerMessage);
        userInterface.SendMessage(message);
    }
}