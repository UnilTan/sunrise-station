using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class HasVisualLayerCondition : IAppearCondition, IInteractCondition
{
    [DataField]
    public bool CheckInitiator { get; private set; }

    [DataField]
    public bool CheckTarget { get; private set; } = true;

    [DataField(required: true)]
    public HumanoidVisualLayers Layer { get; private set; }

    public bool IsMet(EntityUid initiator, EntityUid target, EntityManager entMan)
    {
        if (CheckInitiator && !CheckLayer(initiator, entMan))
            return false;

        if (CheckTarget && !CheckLayer(target, entMan))
            return false;

        return true;
    }

    private bool CheckLayer(EntityUid uid, EntityManager entMan)
    {
        if (!entMan.TryGetComponent<HumanoidAppearanceComponent>(uid, out var appearance))
            return false;

        var hasLayer = appearance.CustomBaseLayers.ContainsKey(Layer)
                       || appearance.BaseLayers.ContainsKey(Layer);

        if (!hasLayer)
            return false;

        return !appearance.PermanentlyHidden.Contains(Layer);
    }
}

