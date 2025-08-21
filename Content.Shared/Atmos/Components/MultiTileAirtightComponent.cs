using Content.Shared.Atmos;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components
{
    /// <summary>
    /// Component that defines additional tiles that need airtight blocking for multi-tile entities like wide doors.
    /// This component works in conjunction with the standard AirtightComponent to ensure all tiles spanned by
    /// a multi-tile entity have proper atmosphere blocking.
    /// </summary>
    [RegisterComponent]
    public sealed partial class MultiTileAirtightComponent : Component
    {
        /// <summary>
        /// List of tile offsets relative to the entity's position that should have airtight blocking.
        /// These are in addition to the tile where the entity is placed.
        /// </summary>
        [DataField("additionalTiles")]
        public List<Vector2i> AdditionalTiles { get; set; } = new();

        /// <summary>
        /// The directions in which the additional tiles should block airflow, relative to the entity's reference frame.
        /// If null, uses the same air blocked direction as the main airtight component.
        /// </summary>
        [DataField("airBlockedDirection")]
        public AtmosDirection? AirBlockedDirection { get; set; }

        /// <summary>
        /// Whether the additional airtight tiles should fix vacuum when unblocked.
        /// If null, uses the same setting as the main airtight component.
        /// </summary>
        [DataField("fixVacuum")]
        public bool? FixVacuum { get; set; }

        /// <summary>
        /// Whether the additional tiles should have no air when fully air blocked.
        /// If null, uses the same setting as the main airtight component.
        /// </summary>
        [DataField("noAirWhenFullyAirBlocked")]
        public bool? NoAirWhenFullyAirBlocked { get; set; }

        /// <summary>
        /// List of helper entities spawned on additional tiles to provide airtight blocking.
        /// This is managed by the MultiTileAirtightSystem.
        /// </summary>
        [ViewVariables]
        public List<EntityUid> HelperEntities { get; set; } = new();
    }
}