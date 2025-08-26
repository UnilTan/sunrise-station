using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.MusicConsole
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
    public sealed partial class MusicConsoleComponent : Component
    {
        public const string TapeSlotId = "tape";

        [DataField(required: true)]
        public ItemSlot TapeSlot = new();

        [DataField, AutoNetworkedField]
        public EntityUid? InsertedMusicTape;

        [DataField, AutoNetworkedField]
        public bool IsPlayingMusic = false;

        [DataField, AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float MusicVolume = 0.5f;

        [DataField, AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float MusicMaxDistance = 30f;

        [DataField, AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite)]
        public float MusicRolloffFactor = 1f;

        [DataField, AutoNetworkedField]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool MusicLoop = true;

        [DataField, AutoNetworkedField]
        public TimeSpan MusicStartTime = TimeSpan.Zero;

        [DataField, AutoNetworkedField]
        public bool MusicWasPlayingBeforePowerLoss = false;

        [DataField]
        public string Title = "comms-console-music-title";

        [DataField]
        public Color Color = Color.Gold;

        [DataField]
        public SoundSpecifier? ButtonSound = new SoundPathSpecifier("/Audio/_Sunrise/TapePlayer/switch.ogg");

        [DataField]
        public float UIUpdateAccumulator = 0f;

        [DataField]
        public Dictionary<EntityUid, EntityUid> ActiveSpeakers = new();
    }

    [Serializable, NetSerializable]
    public sealed class MusicConsoleInterfaceState : BoundUserInterfaceState
    {
        public readonly bool HasMusicTape;
        public readonly bool IsPlayingMusic;
        public readonly string? CurrentSongName;
        public readonly float MusicVolume;
        public readonly bool CanControlMusic;

        public MusicConsoleInterfaceState(bool hasMusicTape, bool isPlayingMusic, string? currentSongName, float musicVolume, bool canControlMusic)
        {
            HasMusicTape = hasMusicTape;
            IsPlayingMusic = isPlayingMusic;
            CurrentSongName = currentSongName;
            MusicVolume = musicVolume;
            CanControlMusic = canControlMusic;
        }
    }

    [Serializable, NetSerializable]
    public sealed class MusicConsolePlayMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class MusicConsolePauseMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class MusicConsoleStopMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class MusicConsoleSetVolumeMessage : BoundUserInterfaceMessage
    {
        public readonly float Volume;

        public MusicConsoleSetVolumeMessage(float volume)
        {
            Volume = volume;
        }
    }

    [Serializable, NetSerializable]
    public enum MusicConsoleUiKey
    {
        Key
    }
}
