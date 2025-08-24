using Content.Server.UserInterface;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Communications;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Communications
{
    [RegisterComponent]
    public sealed partial class CommunicationsConsoleComponent : SharedCommunicationsConsoleComponent
    {
        public float UIUpdateAccumulator = 0f;

        /// <summary>
        /// Remaining cooldown between making announcements.
        /// </summary>
        [ViewVariables]
        [DataField]
        public float AnnouncementCooldownRemaining;

        [ViewVariables]
        [DataField]
        public float BroadcastCooldownRemaining;

        /// <summary>
        /// Fluent ID for the announcement title
        /// If a Fluent ID isn't found, just uses the raw string
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField(required: true)]
        public LocId Title = "comms-console-announcement-title-station";

        /// <summary>
        /// Announcement color
        /// </summary>
        [ViewVariables]
        [DataField]
        public Color Color = Color.Gold;

        /// <summary>
        /// Time in seconds between announcement delays on a per-console basis
        /// </summary>
        [ViewVariables]
        [DataField]
        public int Delay = 90;

        /// <summary>
        /// Time in seconds of announcement cooldown when a new console is created on a per-console basis
        /// </summary>
        [ViewVariables]
        [DataField]
        public int InitialDelay = 30;

        /// <summary>
        /// Can call or recall the shuttle
        /// </summary>
        [ViewVariables]
        [DataField]
        public bool CanShuttle = true;

        /// <summary>
        /// Announce on all grids (for nukies)
        /// </summary>
        [DataField]
        public bool Global = false;

        /// <summary>
        /// Announce sound file path
        /// </summary>
        [DataField]
        public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Announcements/announce.ogg");

        /// <summary>
        /// Hides the sender identity (If they even have one).
        /// In practise this removes the "Sent by ScugMcWawa (Slugcat Captain)" at the bottom of the announcement.
        /// </summary>
        [DataField]
        public bool AnnounceSentBy = true;

        // Sunrise-Start
        [DataField("announceVoice", customTypeSerializer:typeof(PrototypeIdSerializer<TTSVoicePrototype>))]
        public string AnnounceVoice = "Hanson";
        // Sunrise-End

        /// <summary>
        /// Whether the intercom functionality is currently enabled
        /// </summary>
        [ViewVariables]
        [DataField]
        public bool IntercomEnabled = false;

        /// <summary>
        /// Remaining time for intercom functionality in seconds
        /// </summary>
        [ViewVariables]
        [DataField]
        public float IntercomTimeRemaining = 0f;

        /// <summary>
        /// Duration of intercom functionality in seconds (1-2 minutes)
        /// </summary>
        [ViewVariables]
        [DataField]
        public float IntercomDuration = 90f; // 1.5 minutes

        /// <summary>
        /// Sound played when intercom is activated
        /// </summary>
        [DataField]
        public SoundSpecifier IntercomActivationSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg");

        /// <summary>
        /// Channel for intercom broadcasts (uses station communication channel)
        /// </summary>
        [ViewVariables]
        [DataField]
        public string IntercomChannel = "Common";
    }
}
