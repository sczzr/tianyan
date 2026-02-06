# AGENTS.md - 天衍峰：修仙商铺 (TianYanShop)

This file provides guidelines for AI agents working on this codebase.

## Build Commands

```bash
# Compile and build the project
dotnet build TianYanShop.csproj

# Build for release (Godot export)
godot --export-release "Windows Desktop" build/TianYanShop.exe

# Clean and rebuild
dotnet clean && dotnet build TianYanShop.csproj
```

## Code Style Guidelines

### General Principles
- Use C# 12 language features
- Enable nullable reference types (`<Nullable>enable</Nullable>`)
- Prefer Rider for IDE; VS Code is also supported
- Use Godot's logging: `GD.Print()`, `GD.PrintErr()`, `GD.PrintRich()`

### Naming Conventions
- **Namespaces**: `TianYanShop.{Module}` (e.g., `TianYanShop.Core`, `TianYanShop.Entity`)
- **Classes/Types**: PascalCase (e.g., `GameManager`, `InventoryManager`)
- **Methods**: PascalCase with verb-first naming (e.g., `InitializeGame()`, `TryAddItem()`)
- **Properties/Fields**: PascalCase for public, camelCase for private
- **Constants**: UPPER_SNAKE_CASE for compile-time constants
- **Signals**: PascalCase with `EventHandler` suffix (e.g., `SpiritStonesChangedEventHandler`)
- **Godot Nodes**: PascalCase (e.g., `Control`, `Label`, `Button`)
- **Item/NPC IDs**: snake_case (e.g., `spirit_grass`, `npc_001`)

### File Structure
- **Scripts/Core/**: Core managers (GameManager, DataManager, TimeManager)
- **Scripts/Entity/**: Game entity systems (InventoryManager, TianZhuanManager, CraftingSystem)
- **Scripts/NPC/**: NPC and dialogue systems
- **Scripts/UI/**: UI components and controllers
- **Scripts/Data/**: Data definitions and configurations
- **Scenes/**: Godot scene files (.tscn)

### Code Organization
- Put `[Signal]` delegates at top of class (after `using` statements)
- Use region blocks (`#region`, `#endregion`) for grouping related methods
- Use XML documentation comments for public APIs (`/// <summary>...`)
- Initialize collections inline: `public Dictionary<string, int> Items { get; } = new();`

### Imports
```csharp
using Godot;
using System;
using System.Collections.Generic;

using TianYanShop.Core;  // Use explicit namespace imports
```

Order: Godot → System → Project namespaces

### Godot-Specific Patterns
- Use singleton pattern via `public static Instance { get; private set; }`
- Initialize in `_Ready()` with null check pattern
- Use `CallDeferred()` for cross-frame initialization
- Connect signals in code (not in editor)
- Use `ProcessMode = ProcessModeEnum.Always` for managers
- Use `[Export]` for editor-exposed properties

### Error Handling
- Use `GD.PrintErr()` for errors, not exceptions
- Return early for invalid states instead of nesting
- Validate inputs with guard clauses
- Never swallow exceptions silently
- Use `TryX` pattern for operations that might fail (e.g., `TryAddItem()`)

### Types
- Prefer `Godot.Collections.Dictionary` and `Godot.Collections.Array` for Godot interop
- Use `System.Collections.Generic` for pure C# collections
- Use `int` for most numeric types (Godot uses int internally)
- Use `bool` for flags and states
- Use `string` for text (IDs stored in snake_case)

### Async/Concurrency
- Use Godot's `await` pattern for async operations
- Avoid `async void`; use `async Task` for coroutines
- Use `GDSignal` patterns with `await` for waiting

### Git Workflow
- Commit frequently with descriptive messages
- Keep commits atomic (one feature/fix per commit)
- Never commit secrets or .env files
- Use feature branches for new features

### Testing
- No formal test framework configured yet
- Test manually via Godot editor or exported builds
- Consider adding unit tests for core managers

## Key Project Files
- `TianYanShop.csproj`: Project configuration
- `project.godot`: Godot project settings
- `Scripts/Core/GameManager.cs`: Main singleton
- `Scripts/Core/DataManager.cs`: JSON data loading
- `Scripts/Entity/InventoryManager.cs`: Item management

---

# Map Generation System - Fantasy Map Generator C# Port

## Overview

移植 Azgaar's Fantasy Map Generator 到 Godot 4.x (C#)，支持 1024×1024 单元格地图的高性能生成、渲染、编辑。

## Technical Stack

| Component | Technology |
|-----------|------------|
| Language | C# 12 + .NET 8 |
| Engine | Godot 4.x |
| Rendering | Shader + MultiMesh + ImageTexture |
| Serialization | JSON (config) + Binary (map data) |
| PRNG | Alea PRNG (ported from JS) |
| Triangulation | Delaunator C# port |
| Name Libraries | `.tres` resource files |
| UI | Custom themed Control widgets |

## Directory Structure

```
TianYanShop/
├── Scripts/
│   ├── Core/
│   │
│   ├── MapGeneration/                    # 地图生成系统
│   │   ├── Core/
│   │   │   ├── RandomManager.cs         # Alea PRNG 随机数
│   │   │   ├── MapConstants.cs          # 常量定义
│   │   │   └── MapStatistics.cs         # 统计数据
│   │   │
│   │   ├── Data/
│   │   │   ├── Types/
│   │   │   │   ├── Vector2i.cs         # 整型向量
│   │   │   │   ├── CellData.cs         # 单元格
│   │   │   │   ├── VertexData.cs       # 顶点
│   │   │   │   └── TypedArray.cs       # 类型化数组
│   │   │   │
│   │   │   ├── Entities/
│   │   │   │   ├── Feature.cs          # 地理特征
│   │   │   │   ├── RiverData.cs       # 河流
│   │   │   │   ├── LakeData.cs        # 湖泊
│   │   │   │   ├── BurgData.cs        # 城市
│   │   │   │   ├── StateData.cs       # 国家
│   │   │   │   ├── CultureData.cs     # 文化
│   │   │   │   ├── ReligionData.cs    # 宗教
│   │   │   │   ├── RouteData.cs       # 道路
│   │   │   │   └── ProvinceData.cs    # 省份
│   │   │   │
│   │   │   ├── VoronoiGraph.cs        # Voronoi图
│   │   │   ├── BiomeConfig.cs         # 生物群系配置
│   │   │   └── NameBase.cs            # 名称库（.tres）
│   │   │
│   │   ├── Math/
│   │   │   ├── Delaunator.cs         # Delaunay三角剖分
│   │   │   ├── VoronoiBuilder.cs     # Voronoi构建
│   │   │   └── PolygonClipper.cs     # 多边形裁剪
│   │   │
│   │   ├── Generation/
│   │   │   ├── MapGenerator.cs         # 生成主控制器
│   │   │   ├── HeightmapGenerator.cs  # 高度图
│   │   │   ├── FeatureGenerator.cs    # 特征标记
│   │   │   ├── BiomeGenerator.cs      # 生物群系
│   │   │   ├── LakeGenerator.cs      # 湖泊
│   │   │   ├── RiverGenerator.cs     # 河流
│   │   │   ├── CultureGenerator.cs   # 文化
│   │   │   ├── StateGenerator.cs     # 国家
│   │   │   ├── BurgGenerator.cs     # 城市
│   │   │   ├── ReligionGenerator.cs  # 宗教
│   │   │   ├── RouteGenerator.cs    # 道路
│   │   │   └── NameGenerator.cs     # 名称生成
│   │   │
│   │   ├── Rendering/
│   │   │   ├── MapRenderer.cs        # 主渲染器
│   │   │   ├── BiomeRenderer.cs      # 生物群系
│   │   │   ├── RiverRenderer.cs      # 河流
│   │   │   ├── CoastRenderer.cs      # 海岸线
│   │   │   ├── BorderRenderer.cs    # 国境线
│   │   │   ├── BurgIconRenderer.cs  # 城市图标
│   │   │   ├── LabelRenderer.cs     # 标签
│   │   │   └── Shaders/
│   │   │       ├── biome.gdshader
│   │   │       ├── coastline.gdshader
│   │   │       └── river.gdshader
│   │   │
│   │   ├── Editor/
│   │   │   ├── MapEditor.cs         # 编辑器主控制器
│   │   │   ├── EditorState.cs      # 编辑器状态
│   │   │   ├── SelectionManager.cs  # 选择管理
│   │   │   ├── UndoRedoSystem.cs   # 撤销/重做
│   │   │   ├── HeightmapEditor.cs  # 高度图编辑
│   │   │   ├── BurgEditor.cs       # 城市编辑
│   │   │   └── Tools/
│   │   │       ├── EditorTool.cs     # 工具基类
│   │   │       ├── HillBrush.cs     # 山丘笔刷
│   │   │       ├── PitBrush.cs     # 坑洼笔刷
│   │   │       ├── RangeBrush.cs    # 山脉笔刷
│   │   │       ├── TroughBrush.cs   # 河谷笔刷
│   │   │       ├── StraitBrush.cs   # 海峡笔刷
│   │   │       ├── SmoothBrush.cs   # 平滑笔刷
│   │   │       └── MaskBrush.cs     # 掩码笔刷
│   │   │
│   │   ├── UI/
│   │   │   ├── Editor/
│   │   │   │   ├── MapEditorPanel.cs    # 编辑器面板
│   │   │   │   ├── LayerControl.cs      # 图层控制
│   │   │   │   ├── ToolSelector.cs      # 工具选择器
│   │   │   │   ├── SettingsPanel.cs    # 设置面板
│   │   │   │   └── MiniMap.cs          # 小地图
│   │   │   └── Styles/
│   │   │       └── MapEditorTheme.tres  # 自定义主题
│   │   │
│   │   └── Export/
│   │       ├── MapExporter.cs      # 导出基类
│   │       ├── SceneExporter.cs    # Godot Scene
│   │       ├── BinaryExporter.cs   # 二进制
│   │       ├── JsonExporter.cs     # JSON
│   │       └── MapImporter.cs      # 导入
│   │
│   ├── UI/
│   │
│   ├── Entity/
│   │
│   └── TianYanShop.cs
│
├── Scenes/
│   ├── Core/
│   │
│   ├── MapGeneration/
│   │   ├── Editor/
│   │   │   ├── MapEditor.tscn
│   │   │   ├── EditorPanel.tscn
│   │   │   └── MiniMap.tscn
│   │   │
│   │   └── Runtime/
│   │       ├── WorldMap.tscn
│   │       └── MapLayer.tscn
│   │
│   └── UI/
│
├── Resources/
│   ├── Core/
│   │
│   └── MapGeneration/
│       ├── BiomeColors.tres
│       ├── HeightmapTemplates.tres
│       ├── Shaders/
│       │   ├── biome.gdshader
│       │   ├── coastline.gdshader
│       │   └── river.gdshader
│       │
│       └── NameBases/
│           ├── EuropeanNames.tres
│           ├── OrientalNames.tres
│           ├── HighFantasyNames.tres
│           ├── DarkFantasyNames.tres
│           └── ...
│
└── project.godot
```

## Implementation Phases

### Phase 1: Core Foundation (1-2 weeks)

| Task | Output | Dependencies |
|------|--------|--------------|
| RandomManager.cs | PRNG system | - |
| TypedArray.cs | Typed array wrappers | - |
| Vector2i.cs, CellData.cs, VertexData.cs | Basic data structures | - |
| VoronoiGraph.cs | Graph structure | CellData, VertexData |
| Delaunator.cs | Delaunay algorithm | - |
| VoronoiBuilder.cs | Voronoi construction | Delaunator |

### Phase 2: Map Generation (2-3 weeks)

| Task | Output | Dependencies |
|------|--------|--------------|
| HeightmapGenerator.cs | Heightmap | RandomManager |
| FeatureGenerator.cs | Ocean/Lake/Island detection | HeightmapGenerator |
| BiomeGenerator.cs | Biome assignment | FeatureGenerator |
| LakeGenerator.cs | Lake processing | FeatureGenerator |
| RiverGenerator.cs | River system | LakeGenerator |
| CultureGenerator.cs | Culture generation | BiomeGenerator |
| StateGenerator.cs | State generation | CultureGenerator |
| BurgGenerator.cs | Burg generation | StateGenerator |
| ReligionGenerator.cs | Religion generation | CultureGenerator |
| RouteGenerator.cs | Roads/Sea routes | BurgGenerator |
| NameGenerator.cs | Markov name generation | NameBase.tres |

### Phase 3: Rendering System (1 week)

| Task | Output | Dependencies |
|------|--------|--------------|
| biome.gdshader | Biome shader | BiomeConfig.tres |
| coastline.gdshader | Coastline shader | FeatureGenerator |
| river.gdshader | River shader | RiverGenerator |
| BiomeRenderer.cs | Biome texture | Shader |
| CoastRenderer.cs | Coastline rendering | Shader |
| RiverRenderer.cs | River rendering | Shader |
| BorderRenderer.cs | Border rendering | StateGenerator |
| BurgIconRenderer.cs | Burg icons | BurgGenerator |
| MapRenderer.cs | Complete pipeline | All Renderers |

### Phase 4: Editor (2 weeks)

| Task | Output | Dependencies |
|------|--------|--------------|
| EditorState.cs | Editor state | - |
| SelectionManager.cs | Selection management | VoronoiGraph |
| UndoRedoSystem.cs | Undo/Redo | - |
| EditorTool.cs | Tool base class | - |
| HillBrush/PitBrush/RangeBrush | Brush tools | EditorTool |
| HeightmapEditor.cs | Heightmap editing | Tools, SelectionManager |
| BurgEditor.cs | Burg add/delete | Tools, SelectionManager |
| MapEditor.cs | Complete editor | All Editor components |

### Phase 5: UI & Theme (1 week)

| Task | Output | Dependencies |
|------|--------|--------------|
| MapEditorTheme.tres | Custom theme | - |
| MapEditorPanel.cs | Main panel | MapEditor |
| LayerControl.cs | Layer control | - |
| ToolSelector.cs | Tool selector | EditorTools |
| SettingsPanel.cs | Settings panel | - |
| MiniMap.cs | Mini-map | MapRenderer |

### Phase 6: Export & Import (1 week)

| Task | Output | Dependencies |
|------|--------|--------------|
| SceneExporter.cs | Godot Scene export | MapRenderer |
| BinaryExporter.cs | Binary export | VoronoiGraph |
| JsonExporter.cs | JSON export | - |
| MapImporter.cs | Map import | All Exporters |

## Core Data Structures

### VoronoiGraph

```csharp
public class VoronoiGraph
{
    // Cell data
    public int[] Indices;                    // i: cell indices
    public int[][] Neighbors;               // c: adjacent cells
    public int[][] VertexNeighbors;         // v: adjacent vertices
    public Vector2[] Points;               // p: polygon points
    public bool[] IsBorder;                 // b: border mark
    
    // Property arrays
    public byte[] Heights;                  // h: height
    public byte[] Terrains;                 // t: terrain type
    public ushort[] Rivers;                 // r: river id
    public ushort[] Features;               // f: feature id
    public ushort[] Biomes;                 // biome: biome id
    public ushort[] Cultures;               // culture: culture id
    public ushort[] States;                 // state: state id
    public ushort[] Burgs;                 // burg: burg id
    public ushort[] Religions;             // religion: religion id
    
    // Geographic elements
    public List<Feature> Features;          // geographic features
    public List<RiverData> Rivers;          // rivers
    public List<LakeData> Lakes;            // lakes
    public List<BurgData> Burgs;            // burgs
    public List<StateData> States;          // states
    public List<CultureData> Cultures;      // cultures
    public List<ReligionData> Religions;   // religions
}
```

### RandomManager (Alea PRNG)

```csharp
public class RandomManager
{
    public void Seed(string seed);
    public float NextFloat();
    public int NextInt(int min, int max);
    public bool NextBool(float probability);
    public float NextGaussian(float mean, float deviation);
    public int NextChoice(object[] weights);
    public int NextBiased(int min, int max, float exponent);
}
```

## Resource Files

| File | Type | Description |
|------|------|-------------|
| `BiomeColors.tres` | Resource | 13 biome color configuration |
| `HeightmapTemplates.tres` | Resource | Heightmap generation templates |
| `MapEditorTheme.tres` | Theme | Custom editor theme |
| `EuropeanNames.tres` | Resource | European-style names |
| `OrientalNames.tres` | Resource | Oriental-style names |
| `HighFantasyNames.tres` | Resource | High fantasy names |
| `DarkFantasyNames.tres` | Resource | Dark fantasy names |
| `biome.gdshader` | Shader | Biome rendering |
| `coastline.gdshader` | Shader | Coastline rendering |
| `river.gdshader` | Shader | River rendering |

## API Usage

### Runtime Generation

```csharp
// Create generator
var settings = new MapSettings {
    Width = 1024,
    Height = 1024,
    Seed = "my_seed",
    CellsCount = 8000,
    StatesNumber = 10,
    BurgsNumber = 50
};

var generator = new MapGenerator();
var map = generator.Generate(settings);

// Render
var renderer = new MapRenderer();
renderer.Initialize();
renderer.Render(map);

// Add to scene
AddChild(renderer);

// Access data
var cell = map.GetCellAt(500, 300);
```

### Editor Usage

```csharp
// Open editor
var editor = new MapEditor();
editor.LoadMap("res://maps/my_map.bin");

// Select tool
editor.SelectTool<ToolType>(ToolType.HillBrush);
editor.BrushRadius = 10;
editor.BrushStrength = 0.5f;

// Apply editing
editor.ApplyBrush(screenPosition);

// Save
editor.SaveMap("res://maps/edited_map.bin");
editor.ExportScene("res://maps/world_map.tscn");
```

## Timeline

| Phase | Weeks | Cumulative |
|-------|-------|------------|
| Phase 1: Core | 1-2 | 1-2 |
| Phase 2: Generation | 2-3 | 3-5 |
| Phase 3: Rendering | 1 | 4-6 |
| Phase 4: Editor | 2 | 6-8 |
| Phase 5: UI & Theme | 1 | 7-9 |
| Phase 6: Export | 1 | 8-10 |

**Total: 8-10 weeks**

## Key Decisions

| Question | Decision |
|----------|----------|
| Name library format | `.tres` resource files |
| UI style | Custom theme |
| Map size | 1024×1024 |
| Platform | PC |
| Editor features | Heightmap + Burg add/delete |
| Preview/Edit | Yes, editor required |
| Import mode | Pre-generate + Runtime generate |
