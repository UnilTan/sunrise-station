using Content.Server.DoAfter;
using Content.Server.Materials;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared._Sunrise.Paint;
using Content.Shared._Sunrise.PaintMixer;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.PaintMixer;

/// <summary>
/// Server-side system for the paint mixer machine
/// </summary>
public sealed class PaintMixerSystem : SharedPaintMixerSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintMixerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PaintMixerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PaintMixerComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<PaintMixerComponent, PaintMixerSetColorMessage>(OnSetColor);
        SubscribeLocalEvent<PaintMixerComponent, PaintMixerStartMixingMessage>(OnStartMixing);
        SubscribeLocalEvent<PaintMixerComponent, PaintMixerSyncRequestMessage>(OnSyncRequest);
        SubscribeLocalEvent<PaintMixerComponent, PaintMixerDoAfterEvent>(OnMixingComplete);
    }

    private void OnComponentInit(EntityUid uid, PaintMixerComponent component, ComponentInit args)
    {
        _ui.SetUi(uid, PaintMixerUiKey.Key, 
            new InterfaceData("Content.Client._Sunrise.PaintMixer.PaintMixerBoundUserInterface"));
    }

    private void OnInteractUsing(EntityUid uid, PaintMixerComponent component, InteractUsingEvent args)
    {
        // Try to insert materials
        if (args.Handled)
            return;

        // Try to insert materials
        if (TryComp<MaterialComponent>(args.Used, out var materialComp))
        {
            if (TryComp<MaterialStorageComponent>(uid, out var storage))
            {
                if (_materialStorage.TryInsertMaterialEntity(args.User, args.Used, uid, storage))
                {
                    _popup.PopupEntity(
                        Loc.GetString("paint-mixer-insert-material-success"),
                        uid, args.User, PopupType.Medium);
                    args.Handled = true;
                    UpdateUI(uid, component);
                    return;
                }
            }
        }
    }

    private void OnActivateInWorld(EntityUid uid, PaintMixerComponent component, ActivateInWorldEvent args)
    {
        if (!_ui.TryToggleUi(uid, PaintMixerUiKey.Key, args.User))
            return;

        UpdateUI(uid, component);
    }

    private void OnSetColor(EntityUid uid, PaintMixerComponent component, PaintMixerSetColorMessage args)
    {
        component.SelectedColor = args.Color;
        Dirty(uid, component);
        UpdateUI(uid, component);
    }

    private void OnStartMixing(EntityUid uid, PaintMixerComponent component, PaintMixerStartMixingMessage args)
    {
        var user = args.Actor;

        if (!CanMix(uid, component))
        {
            _popup.PopupEntity(
                Loc.GetString("paint-mixer-insufficient-materials"),
                uid, user, PopupType.Medium);
            return;
        }

        if (!_power.IsPowered(uid))
        {
            _popup.PopupEntity(
                Loc.GetString("paint-mixer-no-power"),
                uid, user, PopupType.Medium);
            return;
        }

        component.IsMixing = true;
        Dirty(uid, component);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, component.MixingTime, 
            new PaintMixerDoAfterEvent(), uid, target: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        _audio.PlayPvs(component.MixingSound, uid);

        _popup.PopupEntity(
            Loc.GetString("paint-mixer-start-mixing"),
            uid, user, PopupType.Medium);

        UpdateUI(uid, component);
    }

    private void OnSyncRequest(EntityUid uid, PaintMixerComponent component, PaintMixerSyncRequestMessage args)
    {
        UpdateUI(uid, component);
    }

    private void OnMixingComplete(EntityUid uid, PaintMixerComponent component, PaintMixerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
        {
            component.IsMixing = false;
            Dirty(uid, component);
            UpdateUI(uid, component);
            return;
        }

        if (!CanMix(uid, component))
        {
            component.IsMixing = false;
            Dirty(uid, component);
            _popup.PopupEntity(
                Loc.GetString("paint-mixer-insufficient-materials"),
                uid, args.User, PopupType.Medium);
            UpdateUI(uid, component);
            return;
        }

        // Consume materials
        if (TryComp<MaterialStorageComponent>(uid, out var storage))
        {
            foreach (var (material, amount) in component.RequiredMaterials)
            {
                _materialStorage.TryChangeMaterialAmount(uid, material, -amount, storage);
            }
        }

        // Create the spray paint can
        var sprayPaint = Spawn(component.SprayPaintPrototype, Transform(uid).Coordinates);

        // Apply the selected color
        if (TryComp<PaintComponent>(sprayPaint, out var paintComp))
        {
            paintComp.Color = component.SelectedColor;
        }

        component.IsMixing = false;
        Dirty(uid, component);

        _popup.PopupEntity(
            Loc.GetString("paint-mixer-mixing-complete"),
            uid, args.User, PopupType.Medium);

        UpdateUI(uid, component);
        args.Handled = true;
    }

    private bool CanMix(EntityUid uid, PaintMixerComponent component)
    {
        // Check materials
        if (TryComp<MaterialStorageComponent>(uid, out var storage))
        {
            foreach (var (material, required) in component.RequiredMaterials)
            {
                var available = _materialStorage.GetMaterialAmount(uid, material, storage);
                if (available < required)
                    return false;
            }
        }
        else
        {
            return false;
        }

        return true;
    }

    private void UpdateUI(EntityUid uid, PaintMixerComponent component)
    {
        var canMix = CanMix(uid, component);
        var state = new PaintMixerUpdateState(component.SelectedColor, component.IsMixing, canMix);
        _ui.SetUiState(uid, PaintMixerUiKey.Key, state);
    }
}

/// <summary>
/// DoAfter event for paint mixing
/// </summary>
[Serializable]
public sealed partial class PaintMixerDoAfterEvent : SimpleDoAfterEvent
{
}