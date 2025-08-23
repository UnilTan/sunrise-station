using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class NavigatorUi : UIFragment
{
    private NavigatorUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new NavigatorUiFragment();
        _fragment.Setup(fragmentOwner);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not NavigatorUiState navigatorState)
            return;

        _fragment?.UpdateState(navigatorState.MapUid, navigatorState.StationName);
    }
}