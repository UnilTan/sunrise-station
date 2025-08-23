using Content.Server.Atmos.EntitySystems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Examine;
using Content.Shared._Sunrise.Brewing;
using Content.Shared._Sunrise.Brewing.Prototypes;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Brewing
{
    public sealed class BrewingBarrelSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BrewingBarrelComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<BrewingBarrelComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<BrewingBarrelComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
            SubscribeLocalEvent<BrewingBarrelComponent, ExaminedEvent>(OnExamined);
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<BrewingBarrelComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var barrel, out var xform))
            {
                if (barrel.IsFermenting && barrel.FermentationStartTime.HasValue)
                {
                    var elapsed = _timing.CurTime - barrel.FermentationStartTime.Value;
                    var requiredTime = TimeSpan.FromMinutes(barrel.FermentationTime);

                    if (elapsed >= requiredTime)
                    {
                        CompleteFermentation(uid, barrel);
                    }
                    else if (barrel.SensesAtmosphere)
                    {
                        UpdateAtmosphericConditions(uid, barrel, xform);
                    }
                }
            }
        }

        private void OnComponentInit(EntityUid uid, BrewingBarrelComponent component, ComponentInit args)
        {
            // Create solution containers for the barrel
            _solutionContainer.EnsureSolution(uid, component.WortContainerId, 100, out _);
            _solutionContainer.EnsureSolution(uid, component.WaterContainerId, 100, out _);
            _solutionContainer.EnsureSolution(uid, component.OutputContainerId, 200, out _);
        }

        private void OnInteractHand(EntityUid uid, BrewingBarrelComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = TryToggleBarrel(uid, component, args.User);
        }

        private void OnGetVerbs(EntityUid uid, BrewingBarrelComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            var user = args.User;

            // Examine fermentation verb
            AlternativeVerb examineVerb = new()
            {
                Act = () => ExamineFermentation(uid, component, user),
                Text = Loc.GetString("brewing-barrel-examine-fermentation"),
                Priority = 1
            };
            args.Verbs.Add(examineVerb);

            // Toggle barrel verb
            AlternativeVerb toggleVerb = new()
            {
                Act = () => TryToggleBarrel(uid, component, user),
                Text = component.IsOpen ? Loc.GetString("brewing-barrel-close") : Loc.GetString("brewing-barrel-open"),
                Priority = 0
            };
            args.Verbs.Add(toggleVerb);
        }

        private void OnExamined(EntityUid uid, BrewingBarrelComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            if (component.IsOpen)
            {
                args.PushMarkup(Loc.GetString("brewing-barrel-examine-open"));
            }
            else if (component.IsFermenting)
            {
                args.PushMarkup(Loc.GetString("brewing-barrel-examine-fermenting"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("brewing-barrel-examine-closed"));
            }
        }

        private bool TryToggleBarrel(EntityUid uid, BrewingBarrelComponent component, EntityUid user)
        {
            if (component.IsOpen)
            {
                return TryCloseBarrel(uid, component, user);
            }
            else
            {
                return TryOpenBarrel(uid, component, user);
            }
        }

        private bool TryOpenBarrel(EntityUid uid, BrewingBarrelComponent component, EntityUid user)
        {
            if (component.IsOpen)
                return false;

            if (component.IsFermenting)
            {
                _popup.PopupEntity(Loc.GetString("brewing-barrel-cant-open-fermenting"), uid, user);
                return false;
            }

            component.IsOpen = true;
            component.IsFermenting = false;
            _audio.PlayPvs(component.OpenSound, uid);
            _popup.PopupEntity(Loc.GetString("brewing-barrel-opened", ("barrel", uid)), uid, user);

            Dirty(uid, component);
            return true;
        }

        private bool TryCloseBarrel(EntityUid uid, BrewingBarrelComponent component, EntityUid user)
        {
            if (!component.IsOpen)
                return false;

            // Check if we have wort and water to start fermentation
            if (CanStartFermentation(uid, component))
            {
                component.IsOpen = false;
                StartFermentation(uid, component);
                _audio.PlayPvs(component.CloseSound, uid);
                _popup.PopupEntity(Loc.GetString("brewing-barrel-closed-fermentation-started", ("barrel", uid)), uid, user);
            }
            else
            {
                component.IsOpen = false;
                _audio.PlayPvs(component.CloseSound, uid);
                _popup.PopupEntity(Loc.GetString("brewing-barrel-closed", ("barrel", uid)), uid, user);
            }

            Dirty(uid, component);
            return true;
        }

        private bool CanStartFermentation(EntityUid uid, BrewingBarrelComponent component)
        {
            // Check if we have wort
            if (!_solution.TryGetSolution(uid, component.WortContainerId, out var wortSolution) ||
                wortSolution.Value.Comp.Solution.Volume <= 0)
                return false;

            // Check if we have water
            if (!_solution.TryGetSolution(uid, component.WaterContainerId, out var waterSolution) ||
                waterSolution.Value.Comp.Solution.Volume <= 0)
                return false;

            // Check if wort contains any valid wort reagents
            var hasValidWort = false;
            foreach (var reagent in wortSolution.Value.Comp.Solution.Contents)
            {
                if (reagent.Reagent.Prototype.EndsWith("Wort"))
                {
                    hasValidWort = true;
                    break;
                }
            }

            return hasValidWort;
        }

        private void StartFermentation(EntityUid uid, BrewingBarrelComponent component)
        {
            component.IsFermenting = true;
            component.FermentationStartTime = _timing.CurTime;
            component.FermentationQuality = 1.0f; // Start with optimal quality

            Dirty(uid, component);
        }

        private void CompleteFermentation(EntityUid uid, BrewingBarrelComponent component)
        {
            // Move wort to output and convert to fermented variants
            if (_solution.TryGetSolution(uid, component.WortContainerId, out var wortSolution) &&
                _solution.TryGetSolution(uid, component.OutputContainerId, out var outputSolution))
            {
                foreach (var reagent in wortSolution.Value.Comp.Solution.Contents)
                {
                    var fermentedVariant = GetFermentedVariant(reagent.Reagent.Prototype);
                    if (fermentedVariant != null)
                    {
                        var amount = reagent.Quantity * component.FermentationQuality;
                        _solution.TryAddReagent(outputSolution.Value, fermentedVariant, amount, out _);
                    }
                }

                // Clear the wort container
                _solution.RemoveAllSolution(wortSolution.Value);
            }

            // Clear water container
            if (_solution.TryGetSolution(uid, component.WaterContainerId, out var waterSolution))
            {
                _solution.RemoveAllSolution(waterSolution.Value);
            }

            component.IsFermenting = false;
            component.FermentationStartTime = null;

            _popup.PopupEntity(Loc.GetString("brewing-barrel-fermentation-complete", ("barrel", uid)), uid);
            Dirty(uid, component);
        }

        private string? GetFermentedVariant(string wortType)
        {
            // Look for a brewing recipe that produces this wort type and return its fermented result
            foreach (var recipe in _proto.EnumeratePrototypes<BrewingRecipePrototype>())
            {
                if (recipe.Result == wortType)
                    return recipe.FermentedResult;
            }
            return null;
        }

        private void UpdateAtmosphericConditions(EntityUid uid, BrewingBarrelComponent component, TransformComponent xform)
        {
            if (xform.GridUid == null)
                return;

            var position = _transform.GetGridTilePositionOrDefault((uid, xform));
            var tileMix = _atmosphere.GetTileMixture(xform.GridUid, xform.MapUid, position, true);

            if (tileMix == null)
                return;

            var temperature = tileMix.Temperature;
            var pressure = tileMix.Pressure;

            // Calculate fermentation quality based on temperature and pressure
            var tempQuality = 1.0f;
            if (temperature < component.OptimalMinTemp)
            {
                tempQuality = Math.Max(0.1f, (temperature - 200f) / (component.OptimalMinTemp - 200f));
            }
            else if (temperature > component.OptimalMaxTemp)
            {
                tempQuality = Math.Max(0.1f, 1.0f - (temperature - component.OptimalMaxTemp) / 100f);
            }

            // Pressure affects fermentation too (optimal around 1 atm)
            var pressureQuality = 1.0f;
            var optimalPressure = Atmospherics.OneAtmosphere;
            if (pressure < optimalPressure * 0.5f || pressure > optimalPressure * 2.0f)
            {
                pressureQuality = 0.7f;
            }

            component.FermentationQuality = Math.Min(1.5f, tempQuality * pressureQuality);
            Dirty(uid, component);
        }

        private void ExamineFermentation(EntityUid uid, BrewingBarrelComponent component, EntityUid user)
        {
            if (!component.IsFermenting || !component.FermentationStartTime.HasValue)
            {
                _popup.PopupEntity(Loc.GetString("brewing-barrel-not-fermenting"), uid, user);
                return;
            }

            var elapsed = _timing.CurTime - component.FermentationStartTime.Value;
            var required = TimeSpan.FromMinutes(component.FermentationTime);
            var progress = (float)(elapsed.TotalSeconds / required.TotalSeconds) * 100f;

            var qualityDesc = component.FermentationQuality switch
            {
                >= 1.3f => Loc.GetString("brewing-barrel-quality-excellent"),
                >= 1.1f => Loc.GetString("brewing-barrel-quality-good"),
                >= 0.9f => Loc.GetString("brewing-barrel-quality-average"),
                >= 0.7f => Loc.GetString("brewing-barrel-quality-poor"),
                _ => Loc.GetString("brewing-barrel-quality-terrible")
            };

            var message = Loc.GetString("brewing-barrel-fermentation-status",
                ("progress", progress.ToString("F1")),
                ("quality", qualityDesc));

            _popup.PopupEntity(message, uid, user);
        }
    }
}