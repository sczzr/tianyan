# AGENTS.md - Fantasy Map Generator

This file provides guidelines for AI agents working on this Godot 4.x C# project.

## Build Commands

```bash
# Build the .NET project
dotnet build

# Build for release
dotnet build -c Release

# Rebuild the project
dotnet rebuild
```

## Run Commands

```bash
# Run from Godot editor (press F5 in editor)
# Or via command line:
godot --path "E:\2_Personal\tianyan"

# Run specific scene
godot --path "E:\2_Personal\tianyan" "res://Scenes/MainMenu.tscn"
```

## Test Commands

This project uses Godot's testing framework. Tests are located in `Tests/` directory.

```bash
# Run all tests (execute from Godot editor: Project > Run Tests)
# Or via godot command line:
godot --path "E:\2_Personal\tianyan" --test

# Run single test class
godot --path "E:\2_Personal\tianyan" --test --suite="Map.HeightmapTests"

# Run single test method
godot --path "E:\2_Personal\tianyan" --test --suite="Map.HeightmapTests.TestNoiseGeneration"
```

## Code Style Guidelines

### Namespace Convention
```csharp
namespace FantasyMapGenerator.Scripts.[Module];  // e.g., Scripts.Map, Scripts.UI
```

### Class Naming (PascalCase)
```csharp
public partial class MapGenerator { }
public class HeightmapGenerator : Node { }
public interface IMapRenderer { }
public enum BiomeType { }
```

### Variable Naming (camelCase)
```csharp
private MapGenerator _mapGenerator;  // Private fields with underscore
private List<Cell> _cells;
public int CellCount { get; set; }   // Public properties PascalCase
var cellCount = 500;                  // Local variables camelCase
```

### Constants (PascalCase)
```csharp
public const int DEFAULT_CELL_COUNT = 500;
public const float MIN_WATER_LEVEL = 0.1f;
```

### File Organization
- One class per file (filename matches class name)
- File path reflects namespace: `Scripts/Map/HeightmapGenerator.cs`

### Import Order
```csharp
using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Utils;
```

### Godot-Specific Patterns
```csharp
// Node lifecycle
public override void _Ready() { }
public override void _Process(double delta) { }

// Export properties
[Export] public int Width { get; set; } = 512;

// Node references
[Export] private Button _startButton;

// Signals
[Signal] public delegate void GenerationProgressChangedEventHandler(float progress);
```

### Error Handling
```csharp
private void SwitchToScene(string scenePath)
{
	var error = GetTree().ChangeSceneToFile(scenePath);
	if (error != Error.Ok)
	{
		GD.PrintErr($"Failed to load scene: {scenePath}");
	}
}
```

### UI Component Patterns
```csharp
// Export UI components
[Export] private Button _newGameButton;
[Export] private OptionButton _languageSelector;

// Bind events in _Ready
public override void _Ready()
{
	if (_newGameButton != null)
	{
		_newGameButton.Pressed += OnNewGamePressed;
	}
}

// Translation support
private TranslationManager _translationManager;
public override void _Ready()
{
	_translationManager = TranslationManager.Instance;
	_translationManager.LanguageChanged += OnLanguageChanged;
}
```

### Scene Paths
- UI scenes: `res://Scenes/UI/[Name].tscn`
- Game scenes: `res://Scenes/Game/[Name].tscn`
- Main entry: `res://Scenes/MainMenu.tscn`

### Node Reference Pattern
**IMPORTANT**: Godot C# `[Export]` attribute does NOT support path strings. Do NOT use:
```csharp
// WRONG - Will cause compilation error CS1503
[Export("_menuPanel/_menuVBox/_newGameButton")]
private Button _newGameButton;
```

**CORRECT**: Use `GetNode<T>()` in `_Ready()`:
```csharp
public override void _Ready()
{
    _newGameButton = GetNode<Button>("menuPanel/menuVBox/newGameButton");
    _loadGameButton = GetNode<Button>("menuPanel/menuVBox/loadGameButton");
    _settingsButton = GetNode<Button>("menuPanel/menuVBox/settingsButton");
    _quitButton = GetNode<Button>("menuPanel/menuVBox/quitButton");

    SetupMenuItems();
}

private void SetupMenuItems()
{
    if (_newGameButton != null)
    {
        _newGameButton.Pressed += OnNewGamePressed;
    }
}
```

### Build with Multiple Project Files
When the directory contains multiple `.csproj` or `.sln` files, specify the project explicitly:
```bash
dotnet build "FantasyMapGenerator.csproj"
```

### C# Version & Compatibility
- Godot 4.6+ with C# 11+ support
- Use .NET 6+ compatible syntax
- GL Compatibility renderer (mobile-friendly)

## Naming Conventions

### Node Naming (PascalCase)

| Node Type | Pattern | Examples |
|-----------|---------|----------|
| Control/Node | PascalCase | `MainMenu`, `GameHUD`, `InventoryPanel` |
| TextureRect | PascalCase | `BackgroundImage`, `PortraitSprite` |
| Button | PascalCase + Button | `NewGameButton`, `SettingsButton`, `BackButton` |
| Label | PascalCase + Label | `TitleLabel`, `ScoreLabel`, `VersionLabel` |
| Panel | PascalCase + Panel | `MenuPanel`, `TooltipPanel` |
| Slider | PascalCase + Slider | `VolumeSlider`, `MusicSlider` |
| Container | PascalCase | `ItemList`, `ButtonContainer`, `SaveList`, `ModList` |
| OptionButton | PascalCase | `LanguageSelector` |
| CheckButton | PascalCase | `FullscreenCheck`, `SampleModCheck` |
| HSeparator | Descriptive | `Separator`, `Separator1` |

**Examples:**
```
MainMenu (Control)
├── BackgroundImage (TextureRect)
├── MenuPanel (PanelContainer)
│   └── MenuVBox (VBoxContainer)
│       ├── TitleLabel (Label)
│       ├── NewGameButton (Button)
│       ├── SettingsButton (Button)
│       └── BackButton (Button)
```

### Script Variable Naming

| Type | Convention | Examples |
|------|------------|----------|
| Private fields | `_` + camelCase | `_player`, `_health`, `_inventory` |
| Public properties | PascalCase | `PlayerName`, `MaxHealth` |
| Local variables | camelCase | `currentHealth`, `itemCount` |
| Constants | PascalCase | `DefaultCellCount`, `MinWaterLevel` |

### Scene File Naming

- **PascalCase**: `MainMenu.tscn`, `Settings.tscn`, `LoadGame.tscn`
- UI scenes: `res://Scenes/UI/[Name].tscn`
- Game scenes: `res://Scenes/Game/[Name].tscn`

### Resource File Naming

| Type | Convention | Examples |
|------|------------|----------|
| Textures | `type_description.ext` | `btn_normal.png`, `icon_sword.png` |
| Audio | `type_description.ext` | `sfx_jump.wav`, `bgm_main.mp3` |
| Animations | PascalCase | `IdleAnimation`, `RunAnimation` |
