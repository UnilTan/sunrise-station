using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.PaintMixer;

/// <summary>
/// Shared system for paint mixer functionality
/// </summary>
public abstract class SharedPaintMixerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PaintMixerComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<PaintMixerComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, PaintMixerComponent component, ref ComponentGetState args)
    {
        args.State = new PaintMixerComponentState(component.SelectedColor, component.IsMixing);
    }

    private void OnHandleState(EntityUid uid, PaintMixerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PaintMixerComponentState state)
            return;

        component.SelectedColor = state.SelectedColor;
        component.IsMixing = state.IsMixing;
    }
}

[Serializable, NetSerializable]
public sealed class PaintMixerComponentState : ComponentState
{
    public Color SelectedColor;
    public bool IsMixing;

    public PaintMixerComponentState(Color selectedColor, bool isMixing)
    {
        SelectedColor = selectedColor;
        IsMixing = isMixing;
    }
}