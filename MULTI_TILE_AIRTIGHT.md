# Multi-Tile Airtight System

## Problem
Double and triple airlocks were leaking atmosphere because they span multiple tiles but only had a single `AirtightComponent` on the tile where the entity was placed. This caused atmosphere to pass through the unprotected tiles.

## Solution
Created a `MultiTileAirtightComponent` and `MultiTileAirtightSystem` that:

1. **MultiTileAirtightComponent** - Defines additional tiles that need airtight blocking
   - `AdditionalTiles`: List of tile offsets relative to the entity's position
   - Optional overrides for air blocked direction, vacuum fixing, etc.
   - Tracks helper entities spawned on additional tiles

2. **MultiTileAirtightSystem** - Manages helper entities on additional tiles
   - Spawns invisible `AirtightHelper` entities on additional tiles
   - Synchronizes airtight state between main entity and helpers
   - Handles rotation, movement, anchoring, and cleanup
   - Prevents conflicts with existing airtight entities

3. **AirtightHelper Prototype** - Minimal invisible entity for atmosphere blocking
   - Only has `Transform` (anchored) and `Airtight` components
   - No sprite, physics, or other components to minimize impact

## Implementation Details

### Wide Airlock Prototypes
- **DoubleGlassAirlock**: Spans 2 tiles, has 1 additional tile at `(1,0)`
- **TripleGlassAirlock**: Spans 3 tiles, has 2 additional tiles at `(-1,0)` and `(1,0)`
- **DoubleAirlock**: Non-glass version of double airlock
- **TripleAirlock**: Non-glass version of triple airlock

### Rotation Support
The system handles entity rotation by:
- Rotating tile offsets based on the entity's `LocalRotation`
- Recreating helper entities when the main entity moves or rotates
- Using `angle.RotateVec()` to transform offset vectors

### Event Handling
- **ComponentInit**: Creates initial helper entities
- **ComponentShutdown**: Cleans up helper entities
- **AnchorStateChanged**: Creates/removes helpers based on anchoring
- **MoveEvent**: Recreates helpers at new position with correct rotation
- **AirtightChanged**: Synchronizes airtight state to all helpers

### Testing
Integration tests verify:
- Helper entities are created correctly
- Helper entities are cleaned up on deletion
- Airtight state synchronization works
- Wide airlocks have correct MultiTileAirtight configuration

## Files Added/Modified

### New Files
- `Content.Shared/Atmos/Components/MultiTileAirtightComponent.cs`
- `Content.Server/Atmos/EntitySystems/MultiTileAirtightSystem.cs`
- `Resources/Prototypes/Entities/Structures/airtight_helper.yml`
- `Resources/Prototypes/_Sunrise/Entities/Structures/Doors/Airlocks/wide_airlocks.yml`
- `Content.IntegrationTests/Tests/Atmos/MultiTileAirtightTest.cs`
- `Content.IntegrationTests/Tests/Doors/WideAirlockTest.cs`

### Modified Files
- `Resources/Prototypes/_Sunrise/Entities/Structures/Doors/Airlocks/Glass/airlocks.yml`
- `Resources/Prototypes/RCD/rcd.yml`
- `Resources/Prototypes/Entities/Objects/Tools/tools.yml`

## Usage
Simply add the `MultiTileAirtight` component to any entity that spans multiple tiles:

```yaml
- type: MultiTileAirtight
  additionalTiles:
    - 1,0    # One tile to the right
    - -1,0   # One tile to the left
  # Optional overrides:
  # airBlockedDirection: All
  # fixVacuum: true
  # noAirWhenFullyAirBlocked: false
```

The system will automatically handle the rest, creating and managing helper entities as needed.