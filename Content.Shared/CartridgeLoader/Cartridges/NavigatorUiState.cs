using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NavigatorUiState : BoundUserInterfaceState
{
    public EntityUid? MapUid;
    public string StationName;

    public NavigatorUiState(EntityUid? mapUid, string stationName)
    {
        MapUid = mapUid;
        StationName = stationName;
    }
}