using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Flip
{
    [NetworkedComponent, RegisterComponent]
    public sealed partial class FlipComponent : Component
    {
        public Dictionary<string, int> OriginalCollisionLayers { get; } = new();

        /// <summary>
        /// If true, this flip will not cause neck breaking. Used for weapon attacks.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool PreventNeckBreaking = false;
    }
}
