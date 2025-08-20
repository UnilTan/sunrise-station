# Speech Bubble Storage Hiding - Implementation Documentation

## Overview
This implementation addresses GitHub issue #2522 to hide speech bubbles when players are inside storage containers (lockers, cabinets, etc.).

## Changes Made

### Primary Change: `Content.Client/UserInterface/Systems/Chat/ChatUIController.cs`

1. **Added import**: `using Content.Shared.Storage.Components;`
2. **Modified `AddSpeechBubble()` method** to check for `InsideEntityStorageComponent`

```csharp
private void AddSpeechBubble(ChatMessage msg, SpeechBubble.SpeechType speechType)
{
    var ent = EntityManager.GetEntity(msg.SenderEntity);

    if (!EntityManager.EntityExists(ent))
    {
        _sawmill.Debug("Got local chat message with invalid sender entity: {0}", msg.SenderEntity);
        return;
    }

    // Don't show speech bubbles for entities inside storage (lockers, cabinets, etc.)
    if (EntityManager.HasComponent<InsideEntityStorageComponent>(ent))
    {
        return;
    }

    EnqueueSpeechBubble(ent, msg, speechType);
}
```

## How It Works

1. **Normal Flow**: When a player speaks, the chat system calls `AddSpeechBubble()`
2. **Storage Check**: The method now checks if the speaking entity has `InsideEntityStorageComponent`
3. **Bubble Prevention**: If the entity is inside storage, the method returns early, preventing bubble creation
4. **Chat Preservation**: Chat messages and TTS continue to work normally through other systems

## Benefits

- **Minimal Change**: Only 4 lines of code added
- **Surgical Approach**: No modification to core chat/TTS functionality
- **Robust**: Uses existing component system to detect storage containment
- **Future-Proof**: Works with any entity storage that uses `InsideEntityStorageComponent`

## Testing

Due to .NET version requirements in the project, manual testing is recommended:

1. **Setup**: Have two players, one inside a locker
2. **Test Speech**: Player inside locker speaks
3. **Expected Result**: 
   - Chat message appears in chat window
   - TTS plays if enabled
   - No speech bubble appears above the locker
4. **Comparison**: Player outside locker speaks normally with speech bubble

## Edge Cases Handled

- **Component Presence**: Safely checks for component existence before proceeding
- **Entity Validity**: Maintains existing entity validation logic
- **Debug Logging**: Preserves existing error logging for invalid entities

## Potential Extensions (Not Implemented)

The issue mentioned optional vision limitation for players inside storage. This would require:
- Modifications to vision/FOV systems
- More complex changes beyond the minimal scope
- Should be considered as a separate feature request

## Files Modified

- `Content.Client/UserInterface/Systems/Chat/ChatUIController.cs` - Primary implementation
- `Content.Tests/Client/Chat/SpeechBubbleStorageTest.cs` - Test validation (created)