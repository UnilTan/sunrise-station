using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.HandToHand;

/// <summary>
/// DoAfter event for hand-to-hand item transfers between players.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HandToHandTransferDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// The item being transferred.
    /// </summary>
    public NetEntity ItemToTransfer;

    /// <summary>
    /// The entity receiving the item.
    /// </summary>
    public NetEntity Receiver;

    public HandToHandTransferDoAfterEvent(NetEntity itemToTransfer, NetEntity receiver)
    {
        ItemToTransfer = itemToTransfer;
        Receiver = receiver;
    }

    public override DoAfterEvent Clone()
    {
        return new HandToHandTransferDoAfterEvent(ItemToTransfer, Receiver);
    }

    public override bool IsDuplicate(DoAfterEvent other)
    {
        return other is HandToHandTransferDoAfterEvent otherEvent
               && otherEvent.ItemToTransfer == ItemToTransfer
               && otherEvent.Receiver == Receiver;
    }
}