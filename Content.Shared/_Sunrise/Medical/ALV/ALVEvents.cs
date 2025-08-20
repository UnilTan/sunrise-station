using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Medical.ALV;

/// <summary>
/// DoAfter event for ALV procedure
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ALVDoAfterEvent : SimpleDoAfterEvent
{
    public ALVDoAfterEvent()
    {
    }
}