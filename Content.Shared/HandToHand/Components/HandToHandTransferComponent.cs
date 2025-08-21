using Robust.Shared.GameStates;

namespace Content.Shared.HandToHand.Components;

/// <summary>
/// Component for entities that can participate in hand-to-hand item transfers.
/// This component allows the entity to both give items to and receive items from other entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HandToHandTransferComponent : Component
{
    /// <summary>
    /// Whether this entity can give items to other entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanGiveItems = true;

    /// <summary>
    /// Whether this entity can receive items from other entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanReceiveItems = true;

    /// <summary>
    /// The maximum range at which hand-to-hand transfers can occur.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TransferRange = 1.5f;
}