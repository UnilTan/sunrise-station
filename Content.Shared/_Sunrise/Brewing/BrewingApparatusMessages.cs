using Content.Shared._Sunrise.Brewing.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Brewing;

[Serializable, NetSerializable]
public sealed class BrewingApparatusUpdateState : BoundUserInterfaceState
{
    public BrewingApparatusUpdateState(List<ProtoId<BrewingRecipePrototype>> recipes, ProtoId<BrewingRecipePrototype>[] queue, ProtoId<BrewingRecipePrototype>? currentRecipe)
    {
        Recipes = recipes;
        Queue = queue;
        CurrentRecipe = currentRecipe;
    }

    public List<ProtoId<BrewingRecipePrototype>> Recipes;
    public ProtoId<BrewingRecipePrototype>[] Queue;
    public ProtoId<BrewingRecipePrototype>? CurrentRecipe;
}

[Serializable, NetSerializable]
public sealed class BrewingApparatusQueueRecipeMessage : BoundUserInterfaceMessage
{
    public BrewingApparatusQueueRecipeMessage(ProtoId<BrewingRecipePrototype> id, int quantity)
    {
        ID = id;
        Quantity = quantity;
    }

    public ProtoId<BrewingRecipePrototype> ID;
    public int Quantity;
}

[Serializable, NetSerializable]
public sealed class BrewingApparatusSyncRequestMessage : BoundUserInterfaceMessage
{
    public BrewingApparatusSyncRequestMessage()
    {
    }
}

[Serializable, NetSerializable]
public enum BrewingApparatusUiKey
{
    Key,
}