using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Brewing;

[Serializable, NetSerializable]
public enum BrewingApparatusVisuals : byte
{
    IsInserting,
    IsRunning
}