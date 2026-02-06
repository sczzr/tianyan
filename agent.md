# Fantasy Map Generator - 代码规范指南

## 项目概述

本项目是基于 TypeScript 项目 [Fantasy-Map-Generator](https://github.com/Azgaar/Fantasy-Map-Generator) 的 Godot 4.x C# 重写版本。

## 目录结构

```
E:\2_Personal\tianyan\
├── agent.md                          # 本规范文档
├── project.godot                     # Godot 项目配置
├── FantasyMapGenerator.csproj        # .NET 项目文件
├── icon.svg                          # 项目图标
├── .gitignore                         # Git 忽略配置
├── README.md                         # 项目说明
│
├── Assets/                           # 资源文件
│   ├── Textures/                     # 图片纹理
│   │   ├── UI/                       # UI 相关
│   │   └── Map/                      # 地图相关
│   ├── Audio/                        # 音频文件
│   │   ├── Music/                    # 背景音乐
│   │   └── SFX/                      # 音效
│   ├── Fonts/                        # 字体文件
│   └── Shaders/                      # 着色器文件
│
├── Scenes/                           # 场景文件 (.tscn)
│   ├── UI/                           # UI 场景
│   │   ├── MainMenu.tscn             # 主菜单
│   │   ├── Settings.tscn             # 设置界面
│   │   ├── LoadGame.tscn             # 读取存档
│   │   ├── ModManager.tscn           # Mod 管理
│   │   ├── PauseMenu.tscn            # 暂停菜单
│   │   └── Dialogs/                  # 对话框
│   │       ├── YesNoDialog.tscn
│   │       └── InputDialog.tscn
│   │
│   ├── Game/                         # 游戏场景
│   │   ├── Game.tscn                 # 游戏主界面
│   │   ├── MapView.tscn              # 地图视图
│   │   ├── MapEditor.tscn            # 地图编辑器
│   │   └── Overlays/                 # 覆盖层
│   │       ├── MiniMap.tscn
│   │       ├── Tooltip.tscn
│   │       └── InfoPanel.tscn
│   │
│   └── Transitions/                  # 转场场景
│       ├── Fade.tscn
│       └── Slide.tscn
│
├── Scripts/                          # C# 脚本
│   ├── Core/                         # 核心系统
│   │   ├── GameManager.cs           # 游戏管理器
│   │   ├── SaveManager.cs           # 存档管理
│   │   ├── EventManager.cs          # 事件系统
│   │   └── ConfigManager.cs         # 配置管理
│   │
│   ├── Map/                          # 地图生成模块
│   │   ├── MapGenerator.cs          # 主生成器
│   │   ├── Heightmap/
│   │   │   ├── HeightmapGenerator.cs
│   │   │   ├── PerlinNoise.cs
│   │   │   └── HeightmapProcessor.cs
│   │   ├── Voronoi/
│   │   │   ├── VoronoiGenerator.cs
│   │   │   ├── Delaunay.cs
│   │   │   └── Cell.cs
│   │   ├── Features/
│   │   │   ├── FeatureDetector.cs
│   │   │   └── FeatureTypes.cs
│   │   ├── Rivers/
│   │   │   ├── RiverGenerator.cs
│   │   │   └── RiverTypes.cs
│   │   ├── Biomes/
│   │   │   ├── BiomeGenerator.cs
│   │   │   └── BiomeTypes.cs
│   │   └── Regions/
│   │       ├── StateGenerator.cs
│   │       ├── CultureGenerator.cs
│   │       └── ProvinceGenerator.cs
│   │
│   ├── World/                        # 世界内容模块
│   │   ├── Burgs/
│   │   │   ├── BurgGenerator.cs
│   │   │   └── BurgTypes.cs
│   │   ├── Names/
│   │   │   ├── NameGenerator.cs
│   │   │   ├── NameDatabase.cs
│   │   │   └── NameGenerators/
│   │   │       ├── EuropeanNames.cs
│   │   │       ├── AsianNames.cs
│   │   │       └── FantasyNames.cs
│   │   ├── Religions/
│   │   │   ├── ReligionGenerator.cs
│   │   │   └── ReligionTypes.cs
│   │   └── Routes/
│   │       ├── RouteGenerator.cs
│   │       └── RouteTypes.cs
│   │
│   ├── Rendering/                    # 渲染模块
│   │   ├── MapRenderer.cs           # 地图渲染器
│   │   ├── CellRenderer.cs          # 单元格渲染
│   │   ├── LabelPlacer.cs           # 标签放置
│   │   ├── ColorSchemes/            # 配色方案
│   │   │   ├── DefaultColors.cs
│   │   │   ├── DarkColors.cs
│   │   │   └── AncientColors.cs
│   │   └── Exporters/               # 导出器
│   │       ├── SvgExporter.cs
│   │       ├── JsonExporter.cs
│   │       └── PngExporter.cs
│   │
│   ├── UI/                           # UI 组件
│   │   ├── MainMenu.cs
│   │   ├── SettingsMenu.cs
│   │   ├── LoadGameMenu.cs
│   │   ├── ModManagerMenu.cs
│   │   ├── Components/              # 可复用组件
│   │   │   ├── MenuButton.cs
│   │   │   ├── SliderControl.cs
│   │   │   ├── ToggleSwitch.cs
│   │   │   ├── ListItem.cs
│   │   │   └── Tooltip.cs
│   │   └── Styles/                  # UI 样式
│   │       ├── ButtonStyles.cs
│   │       ├── PanelStyles.cs
│   │       └── LabelStyles.cs
│   │
│   ├── Data/                         # 数据模型
│   │   ├── MapData.cs
│   │   ├── Cell.cs
│   │   ├── Vertex.cs
│   │   ├── River.cs
│   │   ├── Feature.cs
│   │   ├── Burg.cs
│   │   ├── State.cs
│   │   ├── Culture.cs
│   │   ├── Province.cs
│   │   ├── Religion.cs
│   │   └── Route.cs
│   │
│   ├── Utils/                        # 工具类
│   │   ├── MathUtils.cs
│   │   ├── RandomUtils.cs
│   │   ├── ArrayUtils.cs
│   │   ├── ColorUtils.cs
│   │   ├── FileUtils.cs
│   │   └── JsonUtils.cs
│   │
│   └── ThirdParty/                   # 第三方库
│       ├── AleaPRNG.cs              # 随机数生成
│       └── Delaunator.cs           # Delaunay 库
│
├── Resources/                        # Godot 资源
│   ├── MapPresets/                  # 地图预设
│   │   ├── island_1.tres
│   │   ├── continent_1.tres
│   │   └── custom_1.tres
│   │
│   ├── ColorSchemes/                 # 配色资源
│   │   ├── default_colors.tres
│   │   ├── dark_colors.tres
│   │   └── ancient_colors.tres
│   │
│   ├── Heightmaps/                  # 高度图模板
│   │   ├── default.png
│   │   └── templates/
│   │
│   └── Themes/                      # UI 主题
│       ├── main_theme.tres
│       └── dark_theme.tres
│
├── Mods/                             # Mod 目录
│   ├── SampleMod/                    # 示例 Mod
│   │   ├── mod.json                 # Mod 配置
│   │   ├── Scripts/                 # Mod 脚本
│   │   ├── Resources/               # Mod 资源
│   │   └── Data/                    # Mod 数据
│   └── built-in/                    # 内置 Mod
│
└── Saves/                           # 存档目录
	├── save_001.json
	├── save_002.json
	└── autosave.json
```

## 命名规范

### 文件命名

| 类型 | 前缀 | 示例 |
|------|------|------|
| 脚本 | 无 | `MapGenerator.cs` |
| 场景 | 无 | `MainMenu.tscn` |
| UI 组件 | 组件名 | `MenuButton.cs` |
| 数据类 | 无 | `MapData.cs` |
| 接口 | I | `IMapGenerator.cs` |
| 枚举 | 无 | `BiomeType.cs` |

### 类命名

```csharp
// 核心类
public class MapGenerator { }
public class GameManager { }

// 数据类
public struct CellData { }
public enum FeatureType { }

// 接口
public interface IMapRenderer { }
public interface ISaveable { }
```

### 变量命名

```csharp
// 公开属性 - PascalCase
public int CellCount { get; set; }
public Vector2 MapSize { get; private set; }

// 私有字段 - camelCase
private MapGenerator _mapGenerator;
private List<Cell> _cells;

// 局部变量 - camelCase
var cellCount = 500;
var randomPoints = new Vector2[count];
```

### 常量命名

```csharp
public const int DEFAULT_CELL_COUNT = 500;
public const float MIN_WATER_LEVEL = 0.1f;
```

## 代码结构规范

### 单文件原则

- **一个文件一个类**（除内部类外）
- 文件名与类名一致
- 嵌套类放在父类文件中

### 类结构顺序

```csharp
namespace FantasyMapGenerator.Scripts.Map;

// 1. using 语句
using System;
using Godot;

// 2. 命名空间
namespace FantasyMapGenerator.Scripts.Map.Heightmap;

// 3. 类定义
public partial class HeightmapGenerator : Node
{
	// 4. 信号声明
	[Signal]
	public delegate void GenerationProgressChangedEventHandler(float progress);

	// 5. 常量
	private const int DEFAULT_WIDTH = 512;

	// 6. 导出属性
	[Export]
	public int Width { get; set; } = 512;

	// 7. 公开属性
	public float WaterLevel { get; set; } = 0.35f;

	// 8. 私有字段
	private AleaPRNG _prng;
	private float[] _heightmap;

	// 9. 构造函数
	public HeightmapGenerator()
	{
		_prng = new AleaPRNG();
	}

	// 10. 生命周期方法
	public override void _Ready()
	{
	}

	// 11. 公开方法
	public float[] Generate()
	{
		return _heightmap;
	}

	// 12. 私有方法
	private float[] CreateNoiseMap()
	{
		// ...
	}
}
```

## 场景组织规范

### 场景树结构

```
Main (Control)
├── Background (TextureRect)
├── MenuPanel (PanelContainer)
│   └── VBoxContainer
│       ├── Title (Label)
│       ├── Separator (HSeparator)
│       ├── NewGameButton (Button)
│       ├── LoadGameButton (Button)
│       ├── SettingsButton (Button)
│       └── QuitButton (Button)
└── VersionLabel (Label)
```

### 节点命名规范

| 节点类型 | 命名示例 |
|----------|----------|
| Panel | `MenuPanel`, `SettingsPanel` |
| Button | `NewGameButton`, `QuitButton` |
| Label | `TitleLabel`, `VersionLabel` |
| Slider | `VolumeSlider`, `CellCountSlider` |
| TextureRect | `BackgroundImage` |
| Container | `ButtonContainer`, `ListContainer` |

## Git 提交规范

### 分支命名

```
main              # 主分支
develop           # 开发分支
feature/*         # 功能分支
bugfix/*          # 修复分支
hotfix/*          # 紧急修复
release/*         # 发布分支
```

### 提交信息格式

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Type 类型

```
feat:     新功能
fix:      Bug 修复
docs:     文档更新
style:    代码格式调整
refactor: 重构
test:     测试相关
chore:    构建/工具相关
```

### 示例

```
feat(map): 添加河流生成系统

- 实现基于流量累积的河流检测
- 添加河流下切侵蚀效果
- 支持多条河流交汇

Closes #123
```

## 性能优化指南

### 避免的操作

```csharp
// ❌ 避免：每帧创建新数组
for (int i = 0; i < 1000; i++)
{
	var array = new float[1000]; // 避免
}

// ✅ 正确：复用数组
private float[] _cachedArray = new float[1000];
```

### 使用合适的数据结构

```csharp
// ✅ 高频查询使用 HashSet
private HashSet<int> _landCellIds = new HashSet<int>();

// ✅ 位置查找使用 Dictionary
private Dictionary<int, Cell> _cellsById = new Dictionary<int, Cell>();
```

### 异步处理

```csharp
public async void GenerateLargeMapAsync()
{
	await Task.Run(() =>
	{
		// 地图生成逻辑
	});
	// 完成后的 UI 更新
}
```

## 测试规范

### 单元测试

```
Tests/
├── Map/
│   ├── HeightmapTests.cs
│   ├── VoronoiTests.cs
│   └── RiverTests.cs
├── Utils/
│   ├── MathUtilsTests.cs
│   └── RandomUtilsTests.cs
└── Data/
	└── CellTests.cs
```

## 文档规范

### 类注释

```csharp
/// <summary>
/// 高度图生成器，负责生成和处理地形高度数据
/// </summary>
/// <remarks>
/// 使用 Perlin 噪声生成自然的地形变化，
/// 支持多倍频和边缘遮罩处理
/// </remarks>
public class HeightmapGenerator : Node
{
}
```

### 方法注释

```csharp>
/// <summary>
/// 生成指定尺寸的高度图
/// </summary>
/// <param name="width">地图宽度</param>
/// <param name="height">地图高度</param>
/// <param name="seed">随机种子</param>
/// <returns>归一化的高度图数组</returns>
public float[] GenerateHeightmap(int width, int height, string seed)
```

## 多语言支持规范

### 翻译管理器使用

项目使用 `TranslationManager` 单例类管理多语言支持，默认语言为中文 (`zh-CN`)。

```csharp
// 获取翻译实例
using FantasyMapGenerator.Scripts.Utils;

// 在 _Ready 中初始化
public override void _Ready()
{
	_translationManager = TranslationManager.Instance;
	_translationManager.LanguageChanged += OnLanguageChanged;
}

// 翻译文本
var text = _translationManager.Tr("key_name");
```

### 翻译键值定义

在 `TranslationManager.cs` 的 `LoadTranslations()` 方法中定义翻译键值：

```csharp
_translations["zh-CN"] = new Dictionary<string, string>
{
	["app_title"] = "奇幻地图生成器",
	["new_game"] = "新游戏",
	["settings"] = "设置",
	["back"] = "返回"
};

_translations["en"] = new Dictionary<string, string>
{
	["app_title"] = "Fantasy Map Generator",
	["new_game"] = "New Game",
	["settings"] = "Settings",
	["back"] = "Back"
};
```

### 场景 UI 文本翻译

在场景脚本中实现 `LanguageChanged` 事件处理和 `UpdateUIText()` 方法：

```csharp
private void OnLanguageChanged(string language)
{
	UpdateUIText();
}

private void UpdateUIText()
{
	var tm = TranslationManager.Instance;
	if (_titleLabel != null)
	{
		_titleLabel.Text = tm.Tr("app_title");
	}
	if (_newGameButton != null)
	{
		_newGameButton.Text = tm.Tr("new_game");
	}
}
```

### 语言切换下拉框

在设置界面使用 `OptionButton` 实现语言选择：

```csharp
[Export] private OptionButton _languageSelector;

private void SetupLanguageOptions()
{
	_languageSelector.Clear();
	_languageSelector.AddItem("中文", 0);
	_languageSelector.AddItem("English", 1);
}

private void OnLanguageSelected(long index)
{
	if (index == 0)
	{
		TranslationManager.Instance.CurrentLanguage = "zh-CN";
	}
	else
	{
		TranslationManager.Instance.CurrentLanguage = "en";
	}
}
```

## UI 场景与脚本交互规范

### 节点获取与事件绑定

场景脚本应同时支持从 `.tscn` 文件获取节点和动态创建节点：

```csharp
public override void _Ready()
{
	SetupMenuPanel();
	SetupMenuItems();
}

private void SetupMenuItems()
{
	// 先尝试从场景文件获取节点
	if (_newGameButton == null)
	{
		_newGameButton = GetNode<Button>("MenuPanel/MenuVBox/NewGameButton");
	}
	
	// 绑定事件
	if (_newGameButton != null)
	{
		_newGameButton.Pressed += OnNewGamePressed;
	}
}

private void OnNewGamePressed()
{
	SwitchToScene("res://Scenes/Game/Game.tscn");
}
```

### 场景切换方法

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

### 场景文件组织

```
Scenes/
├── MainMenu.tscn              # 主菜单（主场景）
├── UI/
│   ├── Settings.tscn          # 设置界面
│   ├── LoadGame.tscn          # 加载存档
│   └── ModManager.tscn        # Mod 管理
└── Game/
	├── Game.tscn              # 游戏主界面
	└── ModManager.tscn        # Mod 管理
```

### 按钮命名规范

| 功能 | 命名示例 |
|------|----------|
| 新游戏 | `NewGameButton` |
| 加载存档 | `LoadGameButton` |
| 模组管理 | `ModManagerButton` |
| 设置 | `SettingsButton` |
| 退出游戏 | `QuitButton` |
| 返回 | `BackButton` |
| 菜单 | `MenuButton` |
