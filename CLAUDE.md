# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a **Godot 4.6 C# procedural map generation engine** - a remake of the Fantasy-Map-Generator project. It generates fantasy worlds using Perlin noise, Delaunay triangulation, and Voronoi tessellation algorithms. The project includes a complete UI system with localization support and is structured as a foundation for a larger fantasy game.

**Technology Stack:**
- Godot 4.6 (GL Compatibility renderer)
- C# 11+
- .NET 6+
- Target: Windows/Multi-platform (1920x1080)
- Repository: https://github.com/sczzr/tianyan.git

## Build and Development Commands

```bash
# Build the project
dotnet build

# Release build
dotnet build -c Release

# Rebuild
dotnet rebuild

# Run the project (from Godot editor)
# - Use F5 shortcut in Godot
# - Or run: godot --path "D:\Files\tianyan"

# Run specific scene
godot --path "D:\Files\tianyan" "res://Scenes/UI/MainMenu.tscn"

# Run tests
godot --path "D:\Files\tianyan" --test
```

**Important Build Notes:**
- When multiple `.csproj`/`.sln` files exist, specify the explicit project: `dotnet build "FantasyMapGenerator.csproj"`
- Build artifacts and DLLs are git-ignored
- Main entry point: `res://Scenes/UI/MainMenu.tscn`

## Project Structure

```
Scripts/
├── Core/              # Game systems (GameManager, MapGenerator, Game controller)
├── Data/              # Data structures (MapData, Cell, Triangle classes)
├── Map/
│   ├── Heightmap/     # Perlin noise & height-to-color conversion
│   └── Voronoi/       # Delaunay triangulation & Voronoi cell generation
├── Rendering/         # MapView (Node2D polygon renderer)
├── UI/                # Main menu, settings, load game controllers
├── Utils/             # Alea PRNG, localization system
└── World/Entity/NPC/  # Placeholder directories for future systems

Scenes/
├── UI/                # MainMenu.tscn, Settings.tscn, LoadGame.tscn
└── Game/              # Game.tscn, ModManager.tscn
```

## C# Code Conventions

**Naming:**
- Namespaces: `FantasyMapGenerator.Scripts.[Module]`
- Classes: `PascalCase` (e.g., `MapGenerator`)
- Private fields: `_camelCase` (e.g., `_mapView`, `_cellCount`)
- Public properties: `PascalCase`
- Local variables: `camelCase`
- Constants: `PascalCase` (e.g., `DEFAULT_CELL_COUNT`)

**Godot-Specific Patterns:**
- Use `[Export]` for inspector properties
- Use `GetNode<T>("path")` in `_Ready()` for child node references (NOT `[Export("path")]` - this syntax is unsupported in Godot C#)
- Override lifecycle methods: `_Ready()`, `_Process()`, `_Input()`
- Use signals for events (e.g., `LanguageChanged` in TranslationManager)
- Use `Error` enum for return values

**File Organization:**
- One class per file
- File names match class names
- File structure mirrors namespace hierarchy
- Import order: System → Collections → Godot → FantasyMapGenerator

## Key Architecture Points

**Map Generation Pipeline:**
1. Perlin noise generates height values with fractal octaves
2. Delaunay triangulation tessellates the space (Bowyer-Watson algorithm)
3. Voronoi polygons are generated from Delaunay triangles
4. Heights are normalized and converted to terrain colors
5. MapView renders polygons with per-cell colors

**Localization System:**
- Full Chinese (zh-CN) and English (en) support
- Managed by TranslationManager
- Persistent language preference storage
- Dynamic UI text updates via signals

**Rendering:**
- Node2D-based polygon rendering in MapView
- Left-click regenerates map
- Real-time water level adjustment

**Seeded RNG:**
- Alea PRNG for reproducible deterministic generation
- Seed-based map regeneration

## Git Workflow

- **Current Branch:** `Dev` (development)
- **Main Branch:** `main` (production)
- Create feature branches from `Dev`
- Merge tested features back to `Dev`

## Planned Features (from README)

Future systems to implement:
- River generation
- Town/settlement placement
- National boundaries
- Name generation
- Biome system
- Road/shipping lanes
