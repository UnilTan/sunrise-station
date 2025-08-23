using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Examine;
using Content.Shared._Sunrise.Brewing.Prototypes;
using Content.Shared.Localizations;
using Content.Shared.Materials;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Brewing;

/// <summary>
/// This handles brewing apparatus functionality
/// </summary>
public abstract class SharedBrewingApparatusSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;


    public readonly Dictionary<string, List<BrewingRecipePrototype>> InverseRecipes = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrewingApparatusComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        BuildInverseRecipeDictionary();
    }

    /// <summary>
    /// Get the set of all recipes that a brewing apparatus could possibly ever create (e.g., if all techs were unlocked).
    /// </summary>
    public HashSet<ProtoId<BrewingRecipePrototype>> GetAllPossibleRecipes(BrewingApparatusComponent component)
    {
        var recipes = new HashSet<ProtoId<BrewingRecipePrototype>>();
        foreach (var pack in component.StaticPacks)
        {
            recipes.UnionWith(_proto.Index(pack).Recipes);
        }

        foreach (var pack in component.DynamicPacks)
        {
            recipes.UnionWith(_proto.Index(pack).Recipes);
        }

        return recipes;
    }

    /// <summary>
    /// Add every recipe in the list of recipe packs to a single hashset.
    /// </summary>
    public void AddRecipesFromPacks(HashSet<ProtoId<BrewingRecipePrototype>> recipes, IEnumerable<ProtoId<BrewingRecipePackPrototype>> packs)
    {
        foreach (var id in packs)
        {
            var pack = _proto.Index(id);
            recipes.UnionWith(pack.Recipes);
        }
    }

    private void OnExamined(Entity<BrewingApparatusComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.WortOutputSlotId != null)
            args.PushMarkup(Loc.GetString("brewing-apparatus-examine-wort-slot"));
    }

    [PublicAPI]
    public bool CanProduce(EntityUid uid, string recipe, int amount = 1, BrewingApparatusComponent? component = null)
    {
        return _proto.TryIndex<BrewingRecipePrototype>(recipe, out var proto) && CanProduce(uid, proto, amount, component);
    }

    public bool CanProduce(EntityUid uid, BrewingRecipePrototype recipe, int amount = 1, BrewingApparatusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (!HasRecipe(uid, recipe, component))
            return false;

        // Check if recipe requires industrial apparatus
        if (recipe.RequiresIndustrial && component.Type != BrewingApparatusType.Industrial)
            return false;

        foreach (var (material, needed) in recipe.Materials)
        {
            var adjustedAmount = AdjustMaterial(needed, recipe.ApplyMaterialDiscount, component.MaterialUseMultiplier);

            if (_materialStorage.GetMaterialAmount(uid, material) < adjustedAmount * amount)
                return false;
        }
        return true;
    }

    public static int AdjustMaterial(int original, bool reduce, float multiplier)
        => reduce ? (int) MathF.Ceiling(original * multiplier) : original;

    protected abstract bool HasRecipe(EntityUid uid, BrewingRecipePrototype recipe, BrewingApparatusComponent component);

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (!obj.WasModified<BrewingRecipePrototype>())
            return;
        BuildInverseRecipeDictionary();
    }

    private void BuildInverseRecipeDictionary()
    {
        InverseRecipes.Clear();
        foreach (var brewingRecipe in _proto.EnumeratePrototypes<BrewingRecipePrototype>())
        {
            if (brewingRecipe.Result is not {} result)
                continue;

            InverseRecipes.GetOrNew(result).Add(brewingRecipe);
        }
    }

    public bool TryGetRecipesFromReagent(string prototype, [NotNullWhen(true)] out List<BrewingRecipePrototype>? recipes)
    {
        recipes = new();
        if (InverseRecipes.TryGetValue(prototype, out var r))
            recipes.AddRange(r);
        return recipes.Count != 0;
    }

    public string GetRecipeName(ProtoId<BrewingRecipePrototype> proto)
    {
        return GetRecipeName(_proto.Index(proto));
    }

    public string GetRecipeName(BrewingRecipePrototype proto)
    {
        if (!string.IsNullOrWhiteSpace(proto.Name))
            return Loc.GetString(proto.Name);

        if (proto.Result is {} result)
        {
            return _proto.Index<ReagentPrototype>(result).LocalizedName;
        }

        return string.Empty;
    }

    [PublicAPI]
    public string GetRecipeDescription(ProtoId<BrewingRecipePrototype> proto)
    {
        return GetRecipeDescription(_proto.Index(proto));
    }

    public string GetRecipeDescription(BrewingRecipePrototype proto)
    {
        if (!string.IsNullOrWhiteSpace(proto.Description))
            return Loc.GetString(proto.Description);

        if (proto.Result is {} result)
        {
            return _proto.Index<ReagentPrototype>(result).LocalizedDescription;
        }

        return string.Empty;
    }
}