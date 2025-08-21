using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.SlimeAppearance;

[Serializable, NetSerializable]
public enum SlimeAppearanceModifierKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class SlimeAppearanceModifierMarkingSetMessage : BoundUserInterfaceMessage
{
    public MarkingSet MarkingSet { get; }
    public bool ResendState { get; }

    public SlimeAppearanceModifierMarkingSetMessage(MarkingSet set, bool resendState)
    {
        MarkingSet = set;
        ResendState = resendState;
    }
}

[Serializable, NetSerializable]
public sealed class SlimeAppearanceModifierBaseLayersSetMessage : BoundUserInterfaceMessage
{
    public SlimeAppearanceModifierBaseLayersSetMessage(HumanoidVisualLayers layer, CustomBaseLayerInfo? info, bool resendState)
    {
        Layer = layer;
        Info = info;
        ResendState = resendState;
    }

    public HumanoidVisualLayers Layer { get; }
    public CustomBaseLayerInfo? Info { get; }
    public bool ResendState { get; }
}

[Serializable, NetSerializable]
public sealed class SlimeAppearanceModifierState : BoundUserInterfaceState
{
    public SlimeAppearanceModifierState(
        MarkingSet markingSet,
        string species,
        string bodyType,
        Sex sex,
        Color skinColor,
        Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> customBaseLayers
    )
    {
        MarkingSet = markingSet;
        Species = species;
        BodyType = bodyType;
        Sex = sex;
        SkinColor = skinColor;
        CustomBaseLayers = customBaseLayers;
    }

    public MarkingSet MarkingSet { get; }
    public string Species { get; }
    public string BodyType { get; }
    public Sex Sex { get; }
    public Color SkinColor { get; }
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers { get; }
}