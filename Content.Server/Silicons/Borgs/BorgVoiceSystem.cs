using System.Linq;
using Content.Server._Sunrise.TTS;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Silicons.Borgs;

/// <summary>
/// System that handles cyborg voice changing functionality.
/// </summary>
public sealed class BorgVoiceSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgVoiceComponent, BorgVoiceChangeActionEvent>(OnBorgVoiceChangeAction);
        SubscribeLocalEvent<BorgVoiceComponent, BorgVoiceChangeMessage>(OnBorgVoiceChangeMessage);
        SubscribeLocalEvent<BorgVoiceComponent, ComponentStartup>(OnBorgVoiceStartup);
        
        // Subscribe to TTS voice transformation
        SubscribeLocalEvent<BorgVoiceComponent, TransformSpeakerVoiceEvent>(OnTransformSpeakerVoice);
    }

    private void OnBorgVoiceChangeAction(EntityUid uid, BorgVoiceComponent component, BorgVoiceChangeActionEvent args)
    {
        if (!TryComp<BorgChassisComponent>(uid, out _))
            return;

        // Open the voice selection UI
        if (!_uiSystem.HasUi(uid, BorgVoiceUiKey.Key))
            return;

        var state = CreateVoiceChangeState(uid, component, args.Performer);
        _uiSystem.ServerSendUiMessage(uid, BorgVoiceUiKey.Key, state, args.Performer);
        _uiSystem.OpenUi(uid, BorgVoiceUiKey.Key, args.Performer);
    }

    private void OnBorgVoiceChangeMessage(EntityUid uid, BorgVoiceComponent component, BorgVoiceChangeMessage args)
    {
        if (!TryComp<BorgChassisComponent>(uid, out _))
            return;

        // Validate the voice prototype exists
        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(args.VoiceId, out var voicePrototype))
        {
            _popup.PopupEntity("Invalid voice selected!", uid, args.Actor, PopupType.MediumCaution);
            return;
        }

        // Check if player can use sponsor-only voices
        if (voicePrototype.SponsorOnly && !CanUseSponsorVoice(args.Actor))
        {
            _popup.PopupEntity("This voice is only available to sponsors!", uid, args.Actor, PopupType.MediumCaution);
            return;
        }

        // Set the new voice
        component.SelectedVoiceId = args.VoiceId;
        Dirty(uid, component);

        _popup.PopupEntity($"Voice changed to {voicePrototype.Name}!", uid, args.Actor, PopupType.Medium);

        // Update UI
        var state = CreateVoiceChangeState(uid, component, args.Actor);
        _uiSystem.ServerSendUiMessage(uid, BorgVoiceUiKey.Key, state, args.Actor);
    }

    private void OnBorgVoiceStartup(EntityUid uid, BorgVoiceComponent component, ComponentStartup args)
    {
        // Set default voice if not already set
        if (component.SelectedVoiceId == null)
        {
            var defaultVoice = _prototypeManager
                .EnumeratePrototypes<TTSVoicePrototype>()
                .Where(v => v.RoundStart && !v.SponsorOnly)
                .FirstOrDefault();

            if (defaultVoice != null)
            {
                component.SelectedVoiceId = defaultVoice.ID;
                Dirty(uid, component);
            }
        }
    }

    private void OnTransformSpeakerVoice(EntityUid uid, BorgVoiceComponent component, TransformSpeakerVoiceEvent args)
    {
        // Use the borg's selected voice instead of the default
        if (component.SelectedVoiceId != null)
        {
            args.VoiceId = component.SelectedVoiceId;
        }
    }

    private BorgVoiceChangeState CreateVoiceChangeState(EntityUid uid, BorgVoiceComponent component, ICommonSession player)
    {
        var availableVoices = _prototypeManager
            .EnumeratePrototypes<TTSVoicePrototype>()
            .Where(v => v.RoundStart)
            .Select(v => v.ID)
            .ToList();

        return new BorgVoiceChangeState(component.SelectedVoiceId, availableVoices);
    }

    private bool CanUseSponsorVoice(ICommonSession player)
    {
        // TODO: Implement proper sponsor checking
        // For now, allow all voices for cyborgs since they can speak with any voice
        return true;
    }
}