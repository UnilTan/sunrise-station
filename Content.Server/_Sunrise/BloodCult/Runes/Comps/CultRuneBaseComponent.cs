namespace Content.Server._Sunrise.BloodCult.Runes.Comps;

[RegisterComponent]
public sealed partial class CultRuneBaseComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("cultistGatheringRange")]
    public float CultistGatheringRange = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("gatherInvokers")]
    public bool GatherInvokers = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("invokePhrase")]
    public string InvokePhrase = "";

    [ViewVariables(VVAccess.ReadWrite), DataField("invokersMinCount")]
    public uint InvokersMinCount = 1;

    /// <summary>
    /// DNA of the entity that created this rune
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("creatorDna")]
    public string? CreatorDna;

    /// <summary>
    /// Fingerprints of the entity that created this rune
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("creatorFingerprint")]
    public string? CreatorFingerprint;

    /// <summary>
    /// Blood type and other blood-related data from the creator
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("creatorBloodData")]
    public string? CreatorBloodData;
}
