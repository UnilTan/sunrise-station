using Content.Server.Antag;
using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Components;

/// <summary>
/// Gives traitor antags a random list of objectives with specific logic to limit kill objectives.
/// </summary>
[RegisterComponent, Access(typeof(TraitorObjectivesSystem))]
public sealed partial class TraitorObjectivesComponent : Component
{
    /// <summary>
    /// Each set of objectives to add.
    /// </summary>
    [DataField(required: true)]
    public List<AntagObjectiveSet> Sets = new();

    /// <summary>
    /// If the total difficulty of the currently given objectives exceeds, no more will be given.
    /// </summary>
    [DataField(required: true)]
    public float MaxDifficulty;

    /// <summary>
    /// Maximum number of kill objectives allowed. Default is 1.
    /// </summary>
    [DataField]
    public int MaxKillObjectives = 1;
}