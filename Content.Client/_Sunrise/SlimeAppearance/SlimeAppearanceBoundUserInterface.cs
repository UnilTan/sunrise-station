using Content.Client.Humanoid;
using Content.Shared._Sunrise.SlimeAppearance;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.SlimeAppearance;

public sealed class SlimeAppearanceBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HumanoidMarkingModifierWindow? _window;

    public SlimeAppearanceBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredLeft<HumanoidMarkingModifierWindow>();
        _window.Title = Loc.GetString("slime-appearance-window-title");
        _window.OnMarkingAdded += SendMarkingSet;
        _window.OnMarkingRemoved += SendMarkingSet;
        _window.OnMarkingColorChange += SendMarkingSetNoResend;
        _window.OnMarkingRankChange += SendMarkingSet;
        _window.OnLayerInfoModified += SendBaseLayer;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not SlimeAppearanceModifierState cast)
        {
            return;
        }

        _window.SetState(cast.MarkingSet, cast.Species, cast.Sex, cast.SkinColor, cast.CustomBaseLayers);
    }

    private void SendMarkingSet(MarkingSet set)
    {
        SendMessage(new SlimeAppearanceModifierMarkingSetMessage(set, true));
    }

    private void SendMarkingSetNoResend(MarkingSet set)
    {
        SendMessage(new SlimeAppearanceModifierMarkingSetMessage(set, false));
    }

    private void SendBaseLayer(HumanoidVisualLayers layer, CustomBaseLayerInfo? info)
    {
        SendMessage(new SlimeAppearanceModifierBaseLayersSetMessage(layer, info, true));
    }
}