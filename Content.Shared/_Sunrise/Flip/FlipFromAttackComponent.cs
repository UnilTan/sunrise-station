using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Flip
{
    /// <summary>
    /// Temporary component to track that a flip is happening from a weapon attack.
    /// This is used to prevent neck breaking from weapon-triggered flips.
    /// </summary>
    [NetworkedComponent, RegisterComponent]
    public sealed partial class FlipFromAttackComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField]
        public bool PreventNeckBreaking = false;
    }
}