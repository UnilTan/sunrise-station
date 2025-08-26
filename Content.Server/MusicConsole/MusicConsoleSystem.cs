using Content.Server._Sunrise.AnnouncementSpeaker;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.TapePlayer;
using Content.Shared._Sunrise.AnnouncementSpeaker.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.MusicConsole;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.MusicConsole
{
    public sealed class MusicConsoleSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly AnnouncementSpeakerSystem _announcementSpeakerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

        private const float UIUpdateInterval = 1.0f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MusicConsoleComponent, MapInitEvent>(OnMapInit);

            SubscribeLocalEvent<MusicConsoleComponent, MusicConsolePlayMessage>(OnMusicPlayMessage);
            SubscribeLocalEvent<MusicConsoleComponent, MusicConsolePauseMessage>(OnMusicPauseMessage);
            SubscribeLocalEvent<MusicConsoleComponent, MusicConsoleStopMessage>(OnMusicStopMessage);
            SubscribeLocalEvent<MusicConsoleComponent, MusicConsoleSetVolumeMessage>(OnMusicSetVolumeMessage);

            SubscribeLocalEvent<MusicConsoleComponent, EntInsertedIntoContainerMessage>(OnMusicTapeInserted);
            SubscribeLocalEvent<MusicConsoleComponent, EntRemovedFromContainerMessage>(OnMusicTapeRemoved);

            SubscribeLocalEvent<MusicConsoleComponent, PowerChangedEvent>(OnPowerChanged);

            SubscribeLocalEvent<AnnouncementSpeakerComponent, PowerChangedEvent>(OnSpeakerPowerChanged);
        }

        private void OnMapInit(EntityUid uid, MusicConsoleComponent component, MapInitEvent args)
        {
            _itemSlotsSystem.AddItemSlot(uid, MusicConsoleComponent.TapeSlotId, component.TapeSlot);
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<MusicConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (comp.IsPlayingMusic && !this.IsPowered(uid, EntityManager))
                {
                    comp.MusicWasPlayingBeforePowerLoss = true;
                    StopMusic(uid, comp, announce: false);
                }
                else if (!comp.IsPlayingMusic && comp.MusicWasPlayingBeforePowerLoss && this.IsPowered(uid, EntityManager))
                {
                    comp.MusicWasPlayingBeforePowerLoss = false;
                    if (comp.InsertedMusicTape != null)
                    {
                        StartMusic(uid, comp);
                    }
                }

                if (comp.IsPlayingMusic)
                {
                    CheckActiveSpeakersStatus(uid, comp);
                }

                if (comp.UIUpdateAccumulator < UIUpdateInterval)
                {
                    comp.UIUpdateAccumulator += frameTime;
                    continue;
                }

                comp.UIUpdateAccumulator -= UIUpdateInterval;

                if (_uiSystem.IsUiOpen(uid, MusicConsoleUiKey.Key))
                    UpdateMusicConsoleInterface(uid, comp);
            }

            base.Update(frameTime);
        }

        private void CheckActiveSpeakersStatus(EntityUid consoleUid, MusicConsoleComponent comp)
        {
            var speakersToRemove = new List<EntityUid>();
            var stationUid = _stationSystem.GetOwningStation(consoleUid);

            foreach (var (speakerUid, audioStreamUid) in comp.ActiveSpeakers)
            {
                if (!Exists(speakerUid))
                {
                    speakersToRemove.Add(speakerUid);
                    continue;
                }

                if (!TryComp<AnnouncementSpeakerComponent>(speakerUid, out var speakerComp) || !speakerComp.Enabled)
                {
                    speakersToRemove.Add(speakerUid);
                    continue;
                }

                if (speakerComp.RequiresPower)
                {
                    if (!TryComp<ApcPowerReceiverComponent>(speakerUid, out var powerReceiver) || !powerReceiver.Powered)
                    {
                        speakersToRemove.Add(speakerUid);
                        continue;
                    }
                }
            }

            foreach (var speakerUid in speakersToRemove)
            {
                if (comp.ActiveSpeakers.TryGetValue(speakerUid, out var audioStreamUid))
                {
                    _audioSystem.Stop(audioStreamUid);
                    comp.ActiveSpeakers.Remove(speakerUid);
                }
            }

            if (stationUid != null && comp.InsertedMusicTape != null)
            {

                if (TryComp<MusicTapeComponent>(comp.InsertedMusicTape.Value, out var musicTape))
                    TryConnectNewSpeakers(consoleUid, comp, stationUid.Value, musicTape);
            }

            if (speakersToRemove.Count > 0)
            {
                Dirty(consoleUid, comp);
            }
        }

        private void TryConnectNewSpeakers(EntityUid consoleUid, MusicConsoleComponent comp, EntityUid stationUid, MusicTapeComponent musicTape)
        {
            if (comp.InsertedMusicTape == null)
                return;

            var speakers = _announcementSpeakerSystem.GetStationSpeakers(stationUid);
            var audioParams = CreateAudioParams(comp);

            foreach (var speaker in speakers)
            {
                if (comp.ActiveSpeakers.ContainsKey(speaker))
                    continue;

                if (!TryComp<AnnouncementSpeakerComponent>(speaker, out var speakerComp) || !speakerComp.Enabled)
                    continue;

                if (speakerComp.RequiresPower)
                {
                    if (!TryComp<ApcPowerReceiverComponent>(speaker, out var powerReceiver) || !powerReceiver.Powered)
                        continue;
                }

                var audio = _audioSystem.PlayEntity(musicTape.Sound, Filter.Pvs(speaker), speaker, false, audioParams);
                if (audio != null)
                {
                    comp.ActiveSpeakers[speaker] = audio.Value.Entity;
                }
            }

            if (comp.ActiveSpeakers.Count > 0)
            {
                Dirty(consoleUid, comp);
            }
        }

        private AudioParams CreateAudioParams(MusicConsoleComponent comp)
        {
            return AudioParams.Default
                .WithVolume(comp.MusicVolume)
                .WithMaxDistance(comp.MusicMaxDistance)
                .WithRolloffFactor(comp.MusicRolloffFactor)
                .WithLoop(comp.MusicLoop);
        }

        public void UpdateMusicConsoleInterface(EntityUid uid, MusicConsoleComponent comp)
        {
            var canControlMusic = comp.InsertedMusicTape != null && this.IsPowered(uid, EntityManager);
            var currentSongName = GetCurrentSongName(comp);

            _uiSystem.SetUiState(uid, MusicConsoleUiKey.Key, new MusicConsoleInterfaceState(
                comp.InsertedMusicTape != null,
                comp.IsPlayingMusic,
                currentSongName,
                comp.MusicVolume,
                canControlMusic
            ));
        }

        private void OnMusicPlayMessage(EntityUid uid, MusicConsoleComponent comp, MusicConsolePlayMessage message)
        {
            if (comp.InsertedMusicTape == null || !this.IsPowered(uid, EntityManager))
                return;

            if (comp.IsPlayingMusic)
            {
                ResumeAllSpeakers(uid, comp);
                return;
            }

            StartMusic(uid, comp);
        }

        private void OnMusicPauseMessage(EntityUid uid, MusicConsoleComponent comp, MusicConsolePauseMessage message)
        {
            if (!comp.IsPlayingMusic)
                return;

            PauseAllSpeakers(uid, comp);
        }

        private void OnMusicStopMessage(EntityUid uid, MusicConsoleComponent comp, MusicConsoleStopMessage message)
        {
            if (!comp.IsPlayingMusic)
                return;

            StopMusic(uid, comp, announce: true);
        }

        private void OnMusicSetVolumeMessage(EntityUid uid, MusicConsoleComponent comp, MusicConsoleSetVolumeMessage message)
        {
            comp.MusicVolume = Math.Clamp(message.Volume, 0f, 1f);

            var audioParams = CreateAudioParams(comp);
            foreach (var (speakerUid, audioStreamUid) in comp.ActiveSpeakers)
            {
                _audioSystem.SetVolume(audioStreamUid, comp.MusicVolume);
            }

            Dirty(uid, comp);
            UpdateMusicConsoleInterface(uid, comp);
        }

        private void OnMusicTapeInserted(EntityUid uid, MusicConsoleComponent comp, EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID != MusicConsoleComponent.TapeSlotId)
                return;

            comp.InsertedMusicTape = args.Entity;
            Dirty(uid, comp);
            UpdateMusicConsoleInterface(uid, comp);
        }

        private void OnMusicTapeRemoved(EntityUid uid, MusicConsoleComponent comp, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID != MusicConsoleComponent.TapeSlotId)
                return;

            StopMusic(uid, comp, announce: false);
            comp.InsertedMusicTape = null;
            Dirty(uid, comp);
            UpdateMusicConsoleInterface(uid, comp);
        }

        private void OnPowerChanged(EntityUid uid, MusicConsoleComponent comp, PowerChangedEvent args)
        {
            if (!args.Powered && comp.IsPlayingMusic)
            {
                comp.MusicWasPlayingBeforePowerLoss = true;
                StopMusic(uid, comp, announce: false);
            }
            else if (args.Powered && comp.MusicWasPlayingBeforePowerLoss && comp.InsertedMusicTape != null)
            {
                comp.MusicWasPlayingBeforePowerLoss = false;
                StartMusic(uid, comp);
            }

            UpdateMusicConsoleInterface(uid, comp);
        }

        private void OnSpeakerPowerChanged(EntityUid speakerUid, AnnouncementSpeakerComponent speakerComp, PowerChangedEvent args)
        {
            var musicConsoleQuery = EntityQueryEnumerator<MusicConsoleComponent>();
            while (musicConsoleQuery.MoveNext(out var consoleUid, out var consoleComp))
            {
                if (!consoleComp.IsPlayingMusic)
                    continue;

                if (consoleComp.ActiveSpeakers.TryGetValue(speakerUid, out var audioStreamUid))
                {
                    if (!args.Powered)
                    {
                        _audioSystem.Stop(audioStreamUid);
                        consoleComp.ActiveSpeakers.Remove(speakerUid);
                        Dirty(consoleUid, consoleComp);
                    }
                    else
                    {
                        if (this.IsPowered(consoleUid, EntityManager) && consoleComp.InsertedMusicTape != null)
                        {
                            if (TryComp<MusicTapeComponent>(consoleComp.InsertedMusicTape.Value, out var musicTape))
                            {
                                var audioParams = CreateAudioParams(consoleComp);
                                var audio = _audioSystem.PlayEntity(musicTape.Sound, Filter.Pvs(speakerUid), speakerUid, false, audioParams);
                                if (audio != null)
                                {
                                    consoleComp.ActiveSpeakers[speakerUid] = audio.Value.Entity;
                                    Dirty(consoleUid, consoleComp);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void StartMusic(EntityUid uid, MusicConsoleComponent comp)
        {
            if (comp.InsertedMusicTape == null || !TryComp<MusicTapeComponent>(comp.InsertedMusicTape.Value, out var musicTape))
                return;

            StopAllSpeakers(uid, comp);

            comp.IsPlayingMusic = true;
            comp.MusicStartTime = _gameTiming.CurTime;
            Dirty(uid, comp);

            var stationUid = _stationSystem.GetOwningStation(uid);
            if (stationUid != null)
            {
                PlayMusicThroughStationSpeakers(stationUid.Value, musicTape, comp);
            }

            UpdateMusicConsoleInterface(uid, comp);

            var startText = Loc.GetString("music-console-music-started", ("song", musicTape.SongName));
            var title = Loc.GetString(comp.Title);
            _chatSystem.DispatchStationAnnouncement(uid, startText, sender: title, playDefault: true, playTts: true, colorOverride: comp.Color);
        }

        private void StopMusic(EntityUid uid, MusicConsoleComponent comp, bool announce)
        {
            if (!comp.IsPlayingMusic)
                return;

            comp.IsPlayingMusic = false;
            comp.MusicStartTime = TimeSpan.Zero;

            StopAllSpeakers(uid, comp);

            Dirty(uid, comp);
            UpdateMusicConsoleInterface(uid, comp);

            if (announce)
            {
                var stopText = Loc.GetString("music-console-music-stopped");
                var title = Loc.GetString(comp.Title);
                _chatSystem.DispatchStationAnnouncement(uid, stopText, sender: title, playDefault: true, playTts: true, colorOverride: comp.Color);
            }
        }

        private void StopAllSpeakers(EntityUid uid, MusicConsoleComponent comp)
        {
            foreach (var (speakerUid, audioStreamUid) in comp.ActiveSpeakers)
            {
                _audioSystem.Stop(audioStreamUid);
            }
            comp.ActiveSpeakers.Clear();
        }

        private void PauseAllSpeakers(EntityUid uid, MusicConsoleComponent comp)
        {
            foreach (var (speakerUid, audioStreamUid) in comp.ActiveSpeakers)
            {
                _audioSystem.SetState(audioStreamUid, AudioState.Paused);
            }
        }

        private void ResumeAllSpeakers(EntityUid uid, MusicConsoleComponent comp)
        {
            foreach (var (speakerUid, audioStreamUid) in comp.ActiveSpeakers)
            {
                _audioSystem.SetState(audioStreamUid, AudioState.Playing);
            }
        }

        private void PlayMusicThroughStationSpeakers(EntityUid station, MusicTapeComponent musicTape, MusicConsoleComponent comp)
        {
            var speakers = _announcementSpeakerSystem.GetStationSpeakers(station);
            if (speakers.Count == 0)
                return;

            var audioParams = CreateAudioParams(comp);

            foreach (var speaker in speakers)
            {
                if (!TryComp<AnnouncementSpeakerComponent>(speaker, out var speakerComp))
                    continue;

                if (!speakerComp.Enabled)
                    continue;

                if (speakerComp.RequiresPower)
                {
                    if (!TryComp<ApcPowerReceiverComponent>(speaker, out var powerReceiver) || !powerReceiver.Powered)
                        continue;
                }

                var audio = _audioSystem.PlayEntity(musicTape.Sound, Filter.Pvs(speaker), speaker, false, audioParams);
                if (audio != null)
                {
                    comp.ActiveSpeakers[speaker] = audio.Value.Entity;
                }
            }
        }

        private string? GetCurrentSongName(MusicConsoleComponent comp)
        {
            if (comp.InsertedMusicTape == null || !TryComp<MusicTapeComponent>(comp.InsertedMusicTape.Value, out var musicTape))
                return null;

            return musicTape.SongName;
        }
    }
}
