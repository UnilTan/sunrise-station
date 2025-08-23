using Content.Shared.Materials;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Brewing.Prototypes;

[Prototype]
public sealed partial class BrewingRecipePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Human readable name for the recipe. If not specified, entity name is used.
    /// </summary>
    [DataField]
    public string? Name;

    /// <summary>
    /// Human readable description for the recipe. If not specified, entity description is used.
    /// </summary>
    [DataField]
    public string? Description;

    /// <summary>
    /// Reagent id that is produced when the recipe is brewed.
    /// </summary>
    [DataField]
    public string? Result;

    /// <summary>
    /// The amount of the reagent produced.
    /// </summary>
    [DataField]
    public int ResultReagentAmount = 10;

    /// <summary>
    /// The amount of time it takes to brew this recipe.
    /// </summary>
    [DataField]
    public TimeSpan CompleteTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The materials required to brew this recipe.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MaterialPrototype>, int> Materials = new();

    /// <summary>
    /// Whether this recipe's material cost should be reduced by the material use multiplier.
    /// </summary>
    [DataField]
    public bool ApplyMaterialDiscount = true;

    /// <summary>
    /// The category of this recipe for sorting in the UI.
    /// </summary>
    [DataField]
    public ProtoId<BrewingRecipeCategoryPrototype>? Category;

    /// <summary>
    /// Whether this recipe requires industrial brewing apparatus.
    /// </summary>
    [DataField]
    public bool RequiresIndustrial = false;

    /// <summary>
    /// The reagent id that is produced when the wort from this recipe is fermented.
    /// </summary>
    [DataField]
    public string? FermentedResult;
}