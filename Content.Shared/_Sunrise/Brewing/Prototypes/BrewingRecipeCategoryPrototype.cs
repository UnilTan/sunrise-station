using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Brewing.Prototypes;

[Prototype]
public sealed partial class BrewingRecipeCategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Human readable name for the category.
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

    /// <summary>
    /// Sort weight for ordering categories in the UI.
    /// </summary>
    [DataField]
    public int SortWeight = 0;
}