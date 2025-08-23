using Content.Shared.Construction.Prototypes;
using Content.Shared._Sunrise.Brewing.Prototypes;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Brewing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BrewingApparatusComponent : Component
{
    /// <summary>
    /// Static recipe packs that the brewing apparatus has by default
    /// </summary>
    [DataField]
    public List<ProtoId<BrewingRecipePackPrototype>> StaticPacks = new();

    /// <summary>
    /// Dynamic recipe packs that can be researched
    /// </summary>
    [DataField]
    public List<ProtoId<BrewingRecipePackPrototype>> DynamicPacks = new();

    /// <summary>
    /// The brewing queue
    /// </summary>
    [DataField]
    public Queue<ProtoId<BrewingRecipePrototype>> Queue = new();

    /// <summary>
    /// The sound that plays when brewing
    /// </summary>
    [DataField]
    public SoundSpecifier? BrewingSound;

    /// <summary>
    /// The slot for wort output
    /// </summary>
    [DataField]
    public string? WortOutputSlotId;

    /// <summary>
    /// Default production amount for UI
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DefaultProductionAmount = 1;

    /// <summary>
    /// Type of brewing apparatus - moonshine or industrial
    /// </summary>
    [DataField]
    public BrewingApparatusType Type = BrewingApparatusType.Moonshine;

    #region Visualizer info
    [DataField]
    public string? IdleState;

    [DataField]
    public string? BrewingState;

    [DataField]
    public string? UnlitIdleState;

    [DataField]
    public string? UnlitBrewingState;
    #endregion

    /// <summary>
    /// The recipe currently being brewed
    /// </summary>
    [ViewVariables]
    public ProtoId<BrewingRecipePrototype>? CurrentRecipe;

    #region MachineUpgrading
    /// <summary>
    /// A modifier that changes how long it takes to brew a recipe
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TimeMultiplier = 1;

    /// <summary>
    /// A modifier that changes how much of a material is needed to brew a recipe
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float MaterialUseMultiplier = 1;
    #endregion
}

public enum BrewingApparatusType
{
    Moonshine,
    Industrial
}

public sealed class BrewingApparatusGetRecipesEvent : EntityEventArgs
{
    public readonly EntityUid BrewingApparatus;
    public readonly BrewingApparatusComponent Comp;

    public bool GetUnavailable;

    public HashSet<ProtoId<BrewingRecipePrototype>> Recipes = new();

    public BrewingApparatusGetRecipesEvent(Entity<BrewingApparatusComponent> brewingApparatus, bool forced)
    {
        (BrewingApparatus, Comp) = brewingApparatus;
        GetUnavailable = forced;
    }
}

/// <summary>
/// Event raised on a brewing apparatus when it starts brewing a recipe.
/// </summary>
[ByRefEvent]
public readonly record struct BrewingApparatusStartBrewingEvent(BrewingRecipePrototype Recipe);