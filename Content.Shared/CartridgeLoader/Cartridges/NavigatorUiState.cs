using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class NavigatorUiState : BoundUserInterfaceState
{
    public NetEntity? MapUid;
    public string StationName;
    public NetCoordinates? OwnerCoordinates;

    public NavigatorUiState(NetEntity? mapUid, string stationName, NetCoordinates? ownerCoordinates = null)
    {
        MapUid = mapUid;
        StationName = stationName;
        OwnerCoordinates = ownerCoordinates;
    }
}