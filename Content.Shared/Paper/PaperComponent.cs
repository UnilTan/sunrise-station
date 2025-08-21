using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Paper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaperComponent : Component
{
    public PaperAction Mode;
    [DataField("content"), AutoNetworkedField]
    public string Content { get; set; } = "";

    [DataField("contentSize")]
    public int ContentSize { get; set; } = 10000;

    // Sunrise start
    [DataField, AutoNetworkedField]
    public Color DefaultColor = new(25, 25, 25);
    // Sunrise-end

    [DataField("stampedBy"), AutoNetworkedField]
    public List<StampDisplayInfo> StampedBy { get; set; } = new();

    // Sunrise-Start: Signature system
    [DataField("signedBy"), AutoNetworkedField]
    public List<SignatureDisplayInfo> SignedBy { get; set; } = new();
    // Sunrise-End

    /// <summary>
    ///     Stamp to be displayed on the paper, state from bureaucracy.rsi
    /// </summary>
    [DataField("stampState"), AutoNetworkedField]
    public string? StampState { get; set; }

    [DataField, AutoNetworkedField]
    public bool EditingDisabled;

    /// <summary>
    /// Sound played after writing to the paper.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound { get; private set; } = new SoundCollectionSpecifier("PaperScribbles", AudioParams.Default.WithVariation(0.1f));

    // Sunrise-Start
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? ImageContent { get; set; }
    [DataField, AutoNetworkedField]
    public Vector2? ImageScale { get; set; }
    // Sunrise-End

    [Serializable, NetSerializable]
    public sealed class PaperBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string Text;
        public readonly List<StampDisplayInfo> StampedBy;
        public readonly PaperAction Mode;
        // Sunrise-Start
        public readonly Color DefaultColor;
        public readonly SpriteSpecifier? ImageContent; // Sunrise-edit
        public readonly Vector2? ImageScale; // Sunrise-edit
        public readonly List<SignatureDisplayInfo> SignedBy; // Sunrise-edit
        // Sunrise-End

        public PaperBoundUserInterfaceState(string text, Color defaultColor, List<StampDisplayInfo> stampedBy, PaperAction mode = PaperAction.Read, SpriteSpecifier? imageContent = null, Vector2? imageScale = null, List<SignatureDisplayInfo>? signedBy = null) // Sunrise-edit
        {
            Text = text;
            StampedBy = stampedBy;
            Mode = mode;
            // Sunrise-Start
            DefaultColor = defaultColor;
            ImageContent = imageContent;
            ImageScale = imageScale;
            SignedBy = signedBy ?? new List<SignatureDisplayInfo>();
            // Sunrise-End
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperInputTextMessage : BoundUserInterfaceMessage
    {
        public readonly string Text;

        public PaperInputTextMessage(string text)
        {
            Text = text;
        }
    }

    [Serializable, NetSerializable]
    public enum PaperUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum PaperAction
    {
        Read,
        Write,
    }

    [Serializable, NetSerializable]
    public enum PaperVisuals : byte
    {
        Status,
        Stamp
    }

    [Serializable, NetSerializable]
    public enum PaperStatus : byte
    {
        Blank,
        Written
    }
}
