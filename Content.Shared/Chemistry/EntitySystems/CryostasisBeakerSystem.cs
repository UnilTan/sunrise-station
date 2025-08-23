using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components.SolutionManager;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// System that prevents solutions in cryostasis beakers from being heated above room temperature.
/// </summary>
public sealed class CryostasisBeakerSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CryostasisBeakerComponent, SolutionChangedEvent>(OnSolutionChanged);
    }

    private void OnSolutionChanged(EntityUid uid, CryostasisBeakerComponent component, ref SolutionChangedEvent args)
    {
        // Check if the solution belongs to this cryostasis beaker
        if (!TryComp<SolutionContainerManagerComponent>(uid, out var containerManager))
            return;

        // Find all solutions in this cryostasis beaker and enforce temperature limits
        foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((uid, containerManager)))
        {
            var solution = soln.Comp.Solution;
            
            // If the solution temperature is above the maximum allowed, cool it down
            if (solution.Temperature > component.MaxTemperature)
            {
                solution.Temperature = component.MaxTemperature;
            }
        }
    }
}