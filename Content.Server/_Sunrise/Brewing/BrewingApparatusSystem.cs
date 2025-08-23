using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Materials;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Stack;
using Content.Shared._Sunrise.Economy;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.UserInterface;
using Content.Shared.Database;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared._Sunrise.Brewing;
using Content.Shared._Sunrise.Brewing.Prototypes;
using Content.Shared.Localizations;
using Content.Shared.Materials;
using Content.Shared.Power;
using Content.Shared.ReagentSpeed;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Brewing
{
    [UsedImplicitly]
    public sealed class BrewingApparatusSystem : SharedBrewingApparatusSystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly EmagSystem _emag = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
        [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;
        [Dependency] private readonly ReagentSpeedSystem _reagentSpeed = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
        [Dependency] private readonly StackSystem _stack = default!;
        [Dependency] private readonly TransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BrewingApparatusComponent, GetMaterialWhitelistEvent>(OnGetWhitelist);
            SubscribeLocalEvent<BrewingApparatusComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<BrewingApparatusComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<BrewingApparatusComponent, TechnologyDatabaseModifiedEvent>(OnDatabaseModified);
            SubscribeLocalEvent<BrewingApparatusComponent, ResearchRegistrationChangedEvent>(OnResearchRegistrationChanged);

            SubscribeLocalEvent<BrewingApparatusComponent, BrewingApparatusQueueRecipeMessage>(OnBrewingApparatusQueueRecipeMessage);
            SubscribeLocalEvent<BrewingApparatusComponent, BrewingApparatusSyncRequestMessage>(OnBrewingApparatusSyncRequestMessage);

            SubscribeLocalEvent<BrewingApparatusComponent, BeforeActivatableUIOpenEvent>((u, c, _) => UpdateUserInterfaceState(u, c));
            SubscribeLocalEvent<BrewingApparatusComponent, MaterialAmountChangedEvent>(OnMaterialAmountChanged);
            SubscribeLocalEvent<TechnologyDatabaseComponent, BrewingApparatusGetRecipesEvent>(OnGetRecipes);
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<BrewingApparatusProducingComponent, BrewingApparatusComponent>();
            while (query.MoveNext(out var uid, out var comp, out var brewing))
            {
                if (brewing.CurrentRecipe == null)
                    continue;

                if (_timing.CurTime - comp.StartTime >= comp.ProductionLength)
                    FinishBrewing(uid, brewing);
            }
        }

        private void OnGetWhitelist(EntityUid uid, BrewingApparatusComponent component, ref GetMaterialWhitelistEvent args)
        {
            if (args.Storage != uid)
                return;
            var materialWhitelist = new List<ProtoId<MaterialPrototype>>();
            var recipes = GetAvailableRecipes(uid, component, true);
            foreach (var id in recipes)
            {
                if (!_proto.TryIndex(id, out var proto))
                    continue;
                foreach (var (mat, _) in proto.Materials)
                {
                    if (!materialWhitelist.Contains(mat))
                    {
                        materialWhitelist.Add(mat);
                    }
                }
            }

            var combined = args.Whitelist.Union(materialWhitelist).ToList();
            args.Whitelist = combined;
        }

        [PublicAPI]
        public bool TryGetAvailableRecipes(EntityUid uid, [NotNullWhen(true)] out List<ProtoId<BrewingRecipePrototype>>? recipes, [NotNullWhen(true)] BrewingApparatusComponent? component = null, bool getUnavailable = false)
        {
            recipes = null;
            if (!Resolve(uid, ref component))
                return false;
            recipes = GetAvailableRecipes(uid, component, getUnavailable);
            return true;
        }

        public List<ProtoId<BrewingRecipePrototype>> GetAvailableRecipes(EntityUid uid, BrewingApparatusComponent component, bool getUnavailable = false)
        {
            var ev = new BrewingApparatusGetRecipesEvent((uid, component), getUnavailable);
            AddRecipesFromPacks(ev.Recipes, component.StaticPacks);
            RaiseLocalEvent(uid, ev);
            return ev.Recipes.ToList();
        }

        public bool TryAddToQueue(EntityUid uid, BrewingRecipePrototype recipe, BrewingApparatusComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (!CanProduce(uid, recipe, 1, component))
                return false;

            // Consume materials
            foreach (var (mat, amount) in recipe.Materials)
            {
                var adjustedAmount = recipe.ApplyMaterialDiscount
                    ? (int) (-amount * component.MaterialUseMultiplier)
                    : -amount;

                _materialStorage.TryChangeMaterialAmount(uid, mat, adjustedAmount);
            }

            // Consume reagents
            if (_solution.TryGetSolution(uid, "reagents", out var solution, out var solutionComp))
            {
                foreach (var (reagent, amount) in recipe.Reagents)
                {
                    _solution.RemoveReagent(solution.Value, new ReagentId(reagent.Id, null), amount);
                }
            }

            component.Queue.Enqueue(recipe);

            return true;
        }

        public bool TryStartBrewing(EntityUid uid, BrewingApparatusComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;
            if (component.CurrentRecipe != null || component.Queue.Count <= 0 || !this.IsPowered(uid, EntityManager))
                return false;

            var recipeProto = component.Queue.Dequeue();
            var recipe = _proto.Index(recipeProto);

            var time = _reagentSpeed.ApplySpeed(uid, recipe.CompleteTime) * component.TimeMultiplier;

            var brewingComp = EnsureComp<BrewingApparatusProducingComponent>(uid);
            brewingComp.StartTime = _timing.CurTime;
            brewingComp.ProductionLength = time;
            component.CurrentRecipe = recipe;

            var ev = new BrewingApparatusStartBrewingEvent(recipe);
            RaiseLocalEvent(uid, ref ev);

            _audio.PlayPvs(component.BrewingSound, uid);
            UpdateRunningAppearance(uid, true);
            UpdateUserInterfaceState(uid, component);

            if (time == TimeSpan.Zero)
            {
                FinishBrewing(uid, component, brewingComp);
            }
            return true;
        }

        public void FinishBrewing(EntityUid uid, BrewingApparatusComponent? comp = null, BrewingApparatusProducingComponent? prodComp = null)
        {
            if (!Resolve(uid, ref comp, ref prodComp, false))
                return;

            if (comp.CurrentRecipe != null)
            {
                var currentRecipe = _proto.Index(comp.CurrentRecipe.Value);
                
                if (currentRecipe.Result is { } resultReagent &&
                    comp.WortOutputSlotId is { } slotId)
                {
                    var toAdd = new Solution(new[]
                    {
                        new ReagentQuantity(resultReagent, currentRecipe.ResultReagentAmount, null)
                    });

                    // dispense it in the container if we have it and dump it if we don't
                    if (_container.TryGetContainer(uid, slotId, out var container) &&
                        container.ContainedEntities.Count == 1 &&
                        _solution.TryGetFitsInDispenser(container.ContainedEntities.First(), out var solution, out _))
                    {
                        _solution.AddSolution(solution.Value, toAdd);
                    }
                    else
                    {
                        _popup.PopupEntity(Loc.GetString("brewing-apparatus-wort-dispense-no-container", ("name", uid)), uid);
                        _puddle.TrySpillAt(uid, toAdd, out _);
                    }
                }
            }

            comp.CurrentRecipe = null;
            prodComp.StartTime = _timing.CurTime;

            if (!TryStartBrewing(uid, comp))
            {
                RemCompDeferred(uid, prodComp);
                UpdateUserInterfaceState(uid, comp);
                UpdateRunningAppearance(uid, false);
            }
        }

        public void UpdateUserInterfaceState(EntityUid uid, BrewingApparatusComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var producing = component.CurrentRecipe;
            if (producing == null && component.Queue.TryPeek(out var next))
                producing = next;

            var state = new BrewingApparatusUpdateState(GetAvailableRecipes(uid, component), component.Queue.ToArray(), producing);
            _uiSys.SetUiState(uid, BrewingApparatusUiKey.Key, state);
        }

        /// <summary>
        /// Adds every unlocked recipe from each pack to the recipes list.
        /// </summary>
        public void AddRecipesFromDynamicPacks(ref BrewingApparatusGetRecipesEvent args, TechnologyDatabaseComponent database, IEnumerable<ProtoId<BrewingRecipePackPrototype>> packs)
        {
            foreach (var id in packs)
            {
                var pack = _proto.Index(id);
                foreach (var recipe in pack.Recipes)
                {
                    // For now, just add all recipes - research integration can be improved later
                    args.Recipes.Add(recipe);
                }
            }
        }

        private void OnGetRecipes(EntityUid uid, TechnologyDatabaseComponent component, BrewingApparatusGetRecipesEvent args)
        {
            if (uid == args.BrewingApparatus)
                AddRecipesFromDynamicPacks(ref args, component, args.Comp.DynamicPacks);
        }

        private void OnMaterialAmountChanged(EntityUid uid, BrewingApparatusComponent component, ref MaterialAmountChangedEvent args)
        {
            UpdateUserInterfaceState(uid, component);
        }

        /// <summary>
        /// Initialize the UI and appearance.
        /// Appearance requires initialization or the layers break
        /// </summary>
        private void OnMapInit(EntityUid uid, BrewingApparatusComponent component, MapInitEvent args)
        {
            _appearance.SetData(uid, BrewingApparatusVisuals.IsInserting, false);
            _appearance.SetData(uid, BrewingApparatusVisuals.IsRunning, false);

            _materialStorage.UpdateMaterialWhitelist(uid);
        }

        /// <summary>
        /// Sets the machine sprite to either play the running animation
        /// or stop.
        /// </summary>
        private void UpdateRunningAppearance(EntityUid uid, bool isRunning)
        {
            _appearance.SetData(uid, BrewingApparatusVisuals.IsRunning, isRunning);
        }

        private void OnPowerChanged(EntityUid uid, BrewingApparatusComponent component, ref PowerChangedEvent args)
        {
            if (!args.Powered)
            {
                RemComp<BrewingApparatusProducingComponent>(uid);
                UpdateRunningAppearance(uid, false);
            }
            else if (component.CurrentRecipe != null)
            {
                EnsureComp<BrewingApparatusProducingComponent>(uid);
                TryStartBrewing(uid, component);
            }
        }

        private void OnDatabaseModified(EntityUid uid, BrewingApparatusComponent component, ref TechnologyDatabaseModifiedEvent args)
        {
            UpdateUserInterfaceState(uid, component);
        }

        private void OnResearchRegistrationChanged(EntityUid uid, BrewingApparatusComponent component, ref ResearchRegistrationChangedEvent args)
        {
            UpdateUserInterfaceState(uid, component);
        }

        protected override bool HasRecipe(EntityUid uid, BrewingRecipePrototype recipe, BrewingApparatusComponent component)
        {
            return GetAvailableRecipes(uid, component).Contains(recipe.ID);
        }

        #region UI Messages

        private void OnBrewingApparatusQueueRecipeMessage(EntityUid uid, BrewingApparatusComponent component, BrewingApparatusQueueRecipeMessage args)
        {
            if (_proto.TryIndex(args.ID, out BrewingRecipePrototype? recipe))
            {
                var count = 0;
                for (var i = 0; i < args.Quantity; i++)
                {
                    if (TryAddToQueue(uid, recipe, component))
                        count++;
                    else
                        break;
                }
                if (count > 0)
                {
                    _adminLogger.Add(LogType.Action,
                        LogImpact.Low,
                        $"{ToPrettyString(args.Actor):player} queued {count} {GetRecipeName(recipe)} at {ToPrettyString(uid):brewing_apparatus}");
                }
            }
            TryStartBrewing(uid, component);
            UpdateUserInterfaceState(uid, component);
        }

        private void OnBrewingApparatusSyncRequestMessage(EntityUid uid, BrewingApparatusComponent component, BrewingApparatusSyncRequestMessage args)
        {
            UpdateUserInterfaceState(uid, component);
        }
        #endregion
    }

    [RegisterComponent]
    public sealed partial class BrewingApparatusProducingComponent : Component
    {
        /// <summary>
        /// When the current production started.
        /// </summary>
        [DataField]
        public TimeSpan StartTime;

        /// <summary>
        /// How long the current production will take.
        /// </summary>
        [DataField]
        public TimeSpan ProductionLength;
    }
}