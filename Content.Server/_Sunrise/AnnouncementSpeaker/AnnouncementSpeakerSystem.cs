using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.AnnouncementSpeaker.Components;
using Content.Shared._Sunrise.AnnouncementSpeaker.Events;
using Content.Shared.Power;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Sunrise.AnnouncementSpeaker;

/// <summary>
/// System that manages announcement speakers distributed across stations.
/// Replaces global announcements with spatial audio from speaker networks.
/// </summary>
public sealed class AnnouncementSpeakerSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnnouncementSpeakerEvent>(OnAnnouncementSpeaker);
        SubscribeLocalEvent<AnnouncementSpeakerComponent, SpeakerPlayAnnouncementEvent>(OnSpeakerPlayAnnouncement);
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

        // Send the announcement to each speaker
        var speakerEvent = new SpeakerPlayAnnouncementEvent(ev.Message, ev.AnnouncementSound, ev.AnnounceVoice);
        foreach (var speaker in speakers)
        {
            RaiseLocalEvent(speaker, ref speakerEvent);
        }
    }

    /// <summary>
    /// Handles playing announcements on individual speakers.
    /// </summary>
    private void OnSpeakerPlayAnnouncement(EntityUid uid, AnnouncementSpeakerComponent component, ref SpeakerPlayAnnouncementEvent ev)
    {
        // Check if speaker is enabled
        if (!component.Enabled)
            return;

        // Check if speaker has power (if required)
        if (component.RequiresPower && TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver) && !powerReceiver.Powered)
            return;

        // Play the announcement sound with spatial audio
        if (ev.AnnouncementSound != null)
        {
            var audioParams = AudioParams.Default.WithVolume(-2f * component.VolumeModifier).WithMaxDistance(component.Range);
            _audioSystem.PlayPvs(ev.AnnouncementSound, uid, audioParams);
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
    public void DispatchAnnouncementToSpeakers(EntityUid station, string message, SoundSpecifier? announcementSound = null, string? announceVoice = null)
    {
        var resolvedSound = announcementSound != null ? _audioSystem.ResolveSound(announcementSound) : null;
        var ev = new AnnouncementSpeakerEvent(station, message, resolvedSound, announceVoice);
        RaiseLocalEvent(ref ev);
    }

    /// <summary>
    /// Dispatches an announcement to speakers on all stations.
    /// Used for server-wide announcements like round start/end.
    /// </summary>
    public void DispatchAnnouncementToAllStations(string message, SoundSpecifier? announcementSound = null, string? announceVoice = null)
    {
        var resolvedSound = announcementSound != null ? _audioSystem.ResolveSound(announcementSound) : null;
        var stationQuery = EntityQueryEnumerator<StationDataComponent>();
        while (stationQuery.MoveNext(out var stationUid, out var stationData))
        {
            var ev = new AnnouncementSpeakerEvent(stationUid, message, resolvedSound, announceVoice);
            RaiseLocalEvent(ref ev);
        }
    }
}