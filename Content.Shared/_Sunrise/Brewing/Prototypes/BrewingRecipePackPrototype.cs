using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Sunrise.Brewing.Prototypes;

/// <summary>
/// A pack of brewing recipes that one or more brewing apparatus can use.
/// Packs will inherit the parents recipes when using inheritance, so you don't need to copy paste them.
/// </summary>
[Prototype]
public sealed partial class BrewingRecipePackPrototype : IPrototype, IInheritingPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<BrewingRecipePackPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// The brewing recipes contained by this pack.
    /// </summary>
    [DataField(required: true)]
    [AlwaysPushInheritance]
    public HashSet<ProtoId<BrewingRecipePrototype>> Recipes = new();
}