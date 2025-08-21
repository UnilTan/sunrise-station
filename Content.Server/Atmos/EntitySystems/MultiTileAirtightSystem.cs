using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class MultiTileAirtightSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly AirtightSystem _airtightSystem = default!;
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;

        /// <summary>
        /// Prototype ID for the helper entity that provides airtight blocking on additional tiles.
        /// </summary>
        private const string AirtightHelperPrototype = "AirtightHelper";

        public override void Initialize()
        {
            SubscribeLocalEvent<MultiTileAirtightComponent, ComponentInit>(OnMultiTileAirtightInit);
            SubscribeLocalEvent<MultiTileAirtightComponent, ComponentShutdown>(OnMultiTileAirtightShutdown);
            SubscribeLocalEvent<MultiTileAirtightComponent, AnchorStateChangedEvent>(OnMultiTileAirtightAnchorChanged);
            SubscribeLocalEvent<MultiTileAirtightComponent, MoveEvent>(OnMultiTileAirtightMoved);
            
            // Listen for airtight changes on the main entity to update helpers
            SubscribeLocalEvent<AirtightComponent, AirtightChanged>(OnMainAirtightChanged);
        }

        private void OnMultiTileAirtightInit(Entity<MultiTileAirtightComponent> entity, ref ComponentInit args)
        {
            UpdateHelperEntities(entity);
        }

        private void OnMultiTileAirtightShutdown(Entity<MultiTileAirtightComponent> entity, ref ComponentShutdown args)
        {
            CleanupHelperEntities(entity);
        }

        private void OnMultiTileAirtightAnchorChanged(Entity<MultiTileAirtightComponent> entity, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                UpdateHelperEntities(entity);
            }
            else
            {
                CleanupHelperEntities(entity);
            }
        }

        private void OnMultiTileAirtightMoved(Entity<MultiTileAirtightComponent> entity, ref MoveEvent args)
        {
            // Clean up old helper entities and create new ones at the new position
            CleanupHelperEntities(entity);
            UpdateHelperEntities(entity);
        }

        private void OnMainAirtightChanged(EntityUid uid, AirtightComponent mainAirtight, ref AirtightChanged args)
        {
            // If the main entity has a MultiTileAirtightComponent, update its helpers
            if (TryComp<MultiTileAirtightComponent>(uid, out var multiTileComp))
            {
                UpdateHelperAirtightStates((uid, multiTileComp), mainAirtight);
            }
        }

        /// <summary>
        /// Updates or creates helper entities for additional tiles that need airtight blocking.
        /// </summary>
        private void UpdateHelperEntities(Entity<MultiTileAirtightComponent> entity)
        {
            var (uid, comp) = entity;
            var xform = Transform(uid);

            // Only create helpers if the entity is anchored and on a grid
            if (!xform.Anchored || xform.GridUid == null)
                return;

            // Get the main airtight component to copy settings from
            if (!TryComp<AirtightComponent>(uid, out var mainAirtight))
                return;

            if (!TryComp<MapGridComponent>(xform.GridUid.Value, out var grid))
                return;

            CleanupHelperEntities(entity);

            foreach (var offset in comp.AdditionalTiles)
            {
                // Rotate the offset based on the entity's current rotation
                var rotatedOffset = RotateOffset(offset, xform.LocalRotation);
                
                // Calculate the world position for this additional tile
                var currentTilePos = _transform.GetGridTilePositionOrDefault((uid, xform), grid);
                var targetTilePos = currentTilePos + rotatedOffset;

                // Check if there's already an airtight entity on this tile
                if (HasAirtightEntityOnTile(xform.GridUid.Value, targetTilePos))
                    continue;

                // Spawn helper entity
                var helperPos = _mapSystem.GridTileToLocal(xform.GridUid.Value, grid, targetTilePos);
                var helper = EntityManager.SpawnEntity(AirtightHelperPrototype, helperPos);

                // Configure the helper's airtight component
                if (TryComp<AirtightComponent>(helper, out var helperAirtight))
                {
                    // Copy settings from main airtight component or use overrides from MultiTileAirtightComponent
                    helperAirtight.AirBlocked = mainAirtight.AirBlocked;
                    helperAirtight.CurrentAirBlockedDirection = (int)(comp.AirBlockedDirection ?? (AtmosDirection)mainAirtight.CurrentAirBlockedDirection);
                    helperAirtight.InitialAirBlockedDirection = (int)(comp.AirBlockedDirection ?? (AtmosDirection)mainAirtight.InitialAirBlockedDirection);
                    helperAirtight.FixVacuum = comp.FixVacuum ?? mainAirtight.FixVacuum;
                    helperAirtight.NoAirWhenFullyAirBlocked = comp.NoAirWhenFullyAirBlocked ?? mainAirtight.NoAirWhenFullyAirBlocked;

                    // Update the airtight system
                    _airtightSystem.UpdatePosition((helper, helperAirtight));
                }

                // Add to our tracking list
                comp.HelperEntities.Add(helper);
            }
        }

        /// <summary>
        /// Updates the airtight state of all helper entities to match the main entity.
        /// </summary>
        private void UpdateHelperAirtightStates(Entity<MultiTileAirtightComponent> entity, AirtightComponent mainAirtight)
        {
            var (uid, comp) = entity;

            foreach (var helperUid in comp.HelperEntities)
            {
                if (!TryComp<AirtightComponent>(helperUid, out var helperAirtight))
                    continue;

                // Update air blocked state to match main entity
                _airtightSystem.SetAirblocked((helperUid, helperAirtight), mainAirtight.AirBlocked);
            }
        }

        /// <summary>
        /// Removes all helper entities for this multi-tile airtight component.
        /// </summary>
        private void CleanupHelperEntities(Entity<MultiTileAirtightComponent> entity)
        {
            var (uid, comp) = entity;

            foreach (var helperUid in comp.HelperEntities)
            {
                if (EntityManager.EntityExists(helperUid))
                {
                    EntityManager.DeleteEntity(helperUid);
                }
            }

            comp.HelperEntities.Clear();
        }

        /// <summary>
        /// Checks if there's already an airtight entity on the specified tile.
        /// </summary>
        private bool HasAirtightEntityOnTile(EntityUid gridUid, Vector2i tilePos)
        {
            var query = GetEntityQuery<AirtightComponent>();
            var anchored = _mapSystem.GetAnchoredEntitiesEnumerator(gridUid, tilePos);

            while (anchored.MoveNext())
            {
                if (query.HasComponent(anchored.Current))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Rotates a tile offset based on the given angle.
        /// </summary>
        private Vector2i RotateOffset(Vector2i offset, Angle angle)
        {
            if (angle == Angle.Zero)
                return offset;

            // Convert to floating point for rotation
            var rotatedVector = angle.RotateVec(new Vector2(offset.X, offset.Y));
            
            // Round to nearest integer and convert back to Vector2i
            return new Vector2i((int)Math.Round(rotatedVector.X), (int)Math.Round(rotatedVector.Y));
        }
    }
}