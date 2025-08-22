# Guidebook Localization Guide for Sunrise Station

This document describes the proper approach for localizing guidebooks to avoid merge conflicts with upstream Space Station 14.

## Problem

Previously, guidebook XML files were directly modified with Russian text in the main `/Resources/ServerInfo/Guidebook/` directory. This created merge conflicts every time upstream made changes to these files.

## Solution

Create separate Russian localized guidebooks in a dedicated Sunrise directory structure while keeping the original English files intact for upstream compatibility.

## Directory Structure

```
Resources/
├── ServerInfo/
│   └── Guidebook/
│       ├── _Sunrise/                    # Russian localized guidebooks
│       │   ├── NewPlayer/
│       │   │   ├── NewPlayer.xml
│       │   │   ├── CharacterCreation.xml
│       │   │   └── Controls/
│       │   │       ├── Controls.xml
│       │   │       └── Radio.xml
│       │   ├── Medical/
│       │   ├── Engineering/
│       │   └── ... (other sections)
│       └── ... (original English files)
└── Prototypes/
    └── Guidebook/
        ├── newplayer.yml               # Modified to point to Russian versions
        ├── medical.yml                 # Modified to point to Russian versions
        └── ... (other prototype files)
```

## Implementation Steps

### 1. Create Russian Localized Content

1. Create directory structure under `/Resources/ServerInfo/Guidebook/_Sunrise/`
2. Copy Russian content from existing localized files to new location
3. Create proper XML structure for each localized guidebook

### 2. Restore Original English Files

1. Replace Russian content in original files with proper English versions
2. Ensure original files match upstream format and content

### 3. Update Prototypes

1. Modify existing prototype files in `/Resources/Prototypes/Guidebook/`
2. Change `text:` paths to point to `_Sunrise` versions for localized content
3. Keep original paths for non-localized content

### 4. Validate

1. Build the project to ensure no prototype conflicts
2. Test server startup to verify guidebooks load correctly
3. Check that Russian content displays properly in-game

## Example

### Before (Problematic):
```yaml
# Resources/Prototypes/Guidebook/newplayer.yml
- type: guideEntry
  id: NewPlayer
  text: "/ServerInfo/Guidebook/NewPlayer/NewPlayer.xml"  # Contains Russian text
```

### After (Proper):
```yaml
# Resources/Prototypes/Guidebook/newplayer.yml  
- type: guideEntry
  id: NewPlayer
  text: "/ServerInfo/Guidebook/_Sunrise/NewPlayer/NewPlayer.xml"  # Points to Russian version
```

```xml
<!-- Resources/ServerInfo/Guidebook/NewPlayer/NewPlayer.xml (restored English) -->
<Document>
# Welcome to Space Station 14!
You've just started your shift aboard a Nanotrasen space station...
</Document>
```

```xml
<!-- Resources/ServerInfo/Guidebook/_Sunrise/NewPlayer/NewPlayer.xml (Russian) -->
<Document>
# Добро пожаловать в Space Station 14!
Вы только что заступили на смену на борту космической станции Nanotrasen...
</Document>
```

## Benefits

1. **No Merge Conflicts**: Original English files can be updated from upstream without conflicts
2. **Clean Separation**: Russian content is clearly separated and organized
3. **Maintainable**: Easy to update either Russian translations or incorporate upstream changes
4. **Backwards Compatible**: Existing guidebook system continues to work
5. **Scalable**: Pattern can be applied to any guidebook section

## Files That Need Migration

Based on analysis, the following guidebook files contain Russian content and need migration:

- NewPlayer section (✅ Completed)
- Medical section (MedicalDoctor.xml, Chemist.xml, Cryogenics.xml, etc.)
- Engineering section
- Cargo section
- Service section
- Security section
- Science section
- Various other specialized guides

## Migration Script

For each section:

1. Identify Russian-localized XML files
2. Create corresponding directory structure in `_Sunrise`
3. Move Russian content to new location
4. Restore English content in original location
5. Update prototype files to point to Russian versions
6. Test and validate

This approach ensures that Sunrise Station can maintain its Russian localization while staying compatible with upstream Space Station 14 updates.