# Godot 4.x Rhythm Game Project

## Quick Start

1. Open Godot 4.2 or later
2. Import the project from `E:\case\rhythmX-Godot`
3. Wait for C# compilation to complete
4. Press F5 to run

## Project Structure

```
rhythmX-Godot/
├── project.godot          # Project settings
├── RhythmX.csproj         # C# project
├── icon.svg               # App icon
│
├── scenes/                # UI Scenes (auto-generated)
│   ├── MainMenu.tscn      # Main menu
│   ├── SongSelection.tscn # Song selection
│   ├── Gameplay.tscn      # Gameplay
│   ├── Results.tscn       # Results screen
│   ├── Settings.tscn      # Settings
│   ├── Achievements.tscn  # Achievements
│   └── ChartEditor.tscn   # Chart editor
│
├── scripts/
│   ├── autoload/          # Global managers
│   │   ├── GameManager.cs
│   │   ├── AudioManager.cs
│   │   ├── ScoreManager.cs
│   │   ├── SongLibrary.cs
│   │   ├── AchievementManager.cs
│   │   ├── CloudManager.cs
│   │   ├── EffectManager.cs
│   │   └── StatisticsManager.cs
│   │
│   ├── data/
│   │   └── GameData.cs    # Data structures
│   │
│   ├── audio/
│   │   └── AudioAnalysis.cs # BPM detection
│   │
│   └── ui/                # UI controllers
│       ├── MainMenuUI.cs
│       ├── SongSelectionUI.cs
│       ├── GameplayUI.cs
│       ├── ResultsUI.cs
│       ├── SettingsUI.cs
│       ├── AchievementsUI.cs
│       └── ChartEditorUI.cs
│
└── resources/
    └── theme/
        └── MainTheme.tres  # Global theme
```

## Features

- ✅ 4-track rhythm gameplay
- ✅ Tap, Hold, Swipe note types
- ✅ Perfect/Great/Good/Miss judgment
- ✅ Score and combo system
- ✅ S+/S/A/B/C/D grades
- ✅ 10 mock songs with 4 difficulties
- ✅ Achievement system (13 regular + 5 hidden)
- ✅ Chart editor
- ✅ Local player data persistence
- ✅ Mock cloud sync API
- ✅ Statistics tracking

## Controls

- **D / F / J / K** - Track 1/2/3/4
- **Touch** - Tap on tracks
- **ESC** - Pause

## Adding Custom Songs

Place audio files in `user://imported_songs/` (on Windows: `%APPDATA%/Godot/app_userdata/RhythmX/imported_songs/`)

## Building

1. Android: Project → Export → Android
2. iOS: Project → Export → iOS
3. Windows/Linux/Mac: Project → Export → Desktop

## License

MIT