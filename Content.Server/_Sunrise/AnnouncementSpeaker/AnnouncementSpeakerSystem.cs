using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Server.Station.Systems;
using Content.Server._Sunrise.TTS;
using Content.Server.Power.Components;
using Content.Shared._Sunrise.AnnouncementSpeaker.Components;
using Content.Shared._Sunrise.AnnouncementSpeaker.Events;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.SunriseCCVars;

namespace Content.Server._Sunrise.AnnouncementSpeaker;

/// <summary>
/// System that manages announcement speakers distributed across stations.
/// Replaces global announcements with spatial audio from speaker networks.
/// </summary>
public sealed class AnnouncementSpeakerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private bool _isEnabled;
    private string _defaultAnnounceVoice = "Hanson";

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(SunriseCCVars.TTSEnabled, v => _isEnabled = v, true);
        SubscribeLocalEvent<AnnouncementSpeakerEvent>(OnAnnouncementSpeaker);
        // Note: SpeakerPlayAnnouncementEvent is handled by TTSSystem for the component
    }

    /// <summary>
    /// Handles station-wide announcements by finding all speakers on the station and playing the announcement through them.
    /// </summary>
    private void OnAnnouncementSpeaker(ref AnnouncementSpeakerEvent ev)
    {
        // Find all speakers on the station
        var speakers = GetStationSpeakers(ev.Station);

        if (speakers.Count == 0)
        {
            // Fallback: If no speakers are found, log a warning
            // In the future, this could send to a single communications console or similar
            Logger.Warning($"No announcement speakers found on station {ToPrettyString(ev.Station)}. Announcement not played: {ev.Message}");
            return;
        }

        // Play announcement sound via PVS for each speaker on server side
        if (ev.AnnouncementSound != null)
        {
            foreach (var speaker in speakers)
            {
                if (!TryComp<AnnouncementSpeakerComponent>(speaker, out var speakerComp))
                    continue;

                // Check if speaker is enabled and has power
                if (!speakerComp.Enabled)
                    continue;

                if (speakerComp.RequiresPower)
                {
                    if (!TryComp<ApcPowerReceiverComponent>(speaker, out var powerReceiver) || !powerReceiver.Powered)
                        continue;
                }

                // Play announcement sound via PVS from this speaker
                var audioParams = AudioParams.Default.WithVolume(-2f * speakerComp.VolumeModifier).WithMaxDistance(speakerComp.Range);
                _audioSystem.PlayPvs(ev.AnnouncementSound, speaker, audioParams);
            }
        }

        // Передаём TTS сразу всем динамикам
        var speakerEvent = new SpeakerPlayAnnouncementEvent(ev.Message, ev.AnnouncementSound, ev.AnnounceVoice, ev.TtsData);
        foreach (var speaker in speakers)
        {
            RaiseLocalEvent(speaker, ref speakerEvent);
        }
    }

    /// <summary>
    /// Gets all functional announcement speakers on a station.
    /// </summary>
    private List<EntityUid> GetStationSpeakers(EntityUid station)
    {
        var speakers = new List<EntityUid>();

        if (!TryComp<StationDataComponent>(station, out var stationData))
            return speakers;

        // Look through all grids on the station for speakers
        foreach (var grid in stationData.Grids)
        {
            var query = EntityQueryEnumerator<AnnouncementSpeakerComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var speakerComp, out var xform))
            {
                // Check if the speaker is on this grid
                if (xform.GridUid == grid)
                {
                    speakers.Add(uid);
                }
            }
        }

        return speakers;
    }

    /// <summary>
    /// Dispatches an announcement to all speakers on a station.
    /// This is the main entry point for the announcement speaker system.
    /// </summary>
    public async void DispatchAnnouncementToSpeakers(EntityUid station, string message, SoundSpecifier? announcementSound = null, string? announceVoice = null)
    {
        var resolvedSound = announcementSound != null ? _audioSystem.ResolveSound(announcementSound) : null;
        if (!_isEnabled)
            return;
        if (!GetVoicePrototype(announceVoice ?? _defaultAnnounceVoice, out var protoVoice))
            return;
        var generatedTts = await GenerateTtsForAnnouncement(message, protoVoice);
        var ev = new AnnouncementSpeakerEvent(station, message, resolvedSound, announceVoice, generatedTts);
        RaiseLocalEvent(ref ev);
    }

    /// <summary>
    /// Dispatches an announcement to speakers on all stations.
    /// Used for server-wide announcements like round start/end.
    /// </summary>
    public async void DispatchAnnouncementToAllStations(string message, SoundSpecifier? announcementSound = null, string? announceVoice = null)
    {
        var resolvedSound = announcementSound != null ? _audioSystem.ResolveSound(announcementSound) : null;
        if (!_isEnabled)
            return;
        if (!GetVoicePrototype(announceVoice ?? _defaultAnnounceVoice, out var protoVoice))
            return;
        var generatedTts = await GenerateTtsForAnnouncement(message, protoVoice);
        var stationQuery = EntityQueryEnumerator<StationDataComponent>();
        while (stationQuery.MoveNext(out var stationUid, out var stationData))
        {
            var ev = new AnnouncementSpeakerEvent(stationUid, message, resolvedSound, announceVoice, generatedTts);
            RaiseLocalEvent(ref ev);
        }
    }

    /// <summary>
    /// Gets a voice prototype by ID, with fallback to default voice.
    /// </summary>
    private bool GetVoicePrototype(string voiceId, [NotNullWhen(true)] out TTSVoicePrototype? voicePrototype)
    {
        if (!_prototypeManager.TryIndex(voiceId, out voicePrototype))
        {
            return _prototypeManager.TryIndex("father_grigori", out voicePrototype);
        }
        return true;
    }

    /// <summary>
    /// Generates TTS audio for an announcement with the megaphone effect.
    /// </summary>
    private async Task<byte[]?> GenerateTtsForAnnouncement(string text, TTSVoicePrototype voicePrototype)
    {
        try
        {
            var textSanitized = Sanitize(text);
            if (textSanitized == "") return null;
            if (char.IsLetter(textSanitized[^1]))
                textSanitized += ".";

            // Use TTS manager directly to generate with megaphone effect
            var ttsManager = IoCManager.Resolve<TTSManager>();
            return await ttsManager.ConvertTextToSpeechAnnounce(voicePrototype, textSanitized);
        }
        catch (Exception e)
        {
            Logger.Error($"TTS System error in announcement generation: {e.Message}");
        }
        return null;
    }

    /// <summary>
    /// Sanitizes text for TTS generation.
    /// </summary>
    private string Sanitize(string text)
    {
        return text.Trim();
    }
}
