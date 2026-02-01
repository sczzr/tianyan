# 天衍峰：修仙商铺 (TianYanShop)

## 项目概述

一款使用 Godot 4.5 + C# 开发的修仙题材商铺经营游戏。玩家扮演修仙界商铺掌柜，经营店铺、炼制丹药法器、与各路修士交易。

## 技术栈

- **引擎**: Godot 4.5 (Mobile 渲染器)
- **语言**: C# (.NET 8.0)
- **IDE**: 支持 Rider/VS Code
- **版本控制**: Git

## 项目结构

```
├── Scripts/           # C# 脚本
│   ├── GameManager.cs      # 游戏主管理器 - 协调各系统
│   ├── DataManager.cs      # 数据管理器 - 加载所有JSON数据
│   ├── TimeManager.cs      # 时间管理系统 - 游戏时间、离线计算
│   ├── InventoryManager.cs # 背包管理器 - 物品存储与交易
│   ├── TianZhuanManager.cs # 天篆管理系统 - 核心力量体系
│   ├── CraftingSystem.cs   # 炼制系统 - 制作物品
│   ├── DialogueSystem.cs   # 对话系统 - NPC交互
│   ├── UIShopMain.cs       # 商铺主界面
│   ├── ItemData.cs         # 物品数据定义
│   └── NPCData.cs          # NPC 数据定义
├── Scenes/            # Godot 场景 (.tscn)
│   ├── MainGame.tscn
│   ├── ShopMainUI.tscn
│   ├── CraftingUI.tscn
│   └── DialogueUI.tscn
├── 游戏设计/          # 详细设计文档
│   ├── 游戏背景故事/  # 世界观、法则体系、历史
│   └── ...
├── project.godot      # Godot 项目配置
└── TianYanShop.csproj # C# 项目文件
```

## 核心系统

### 1. 商铺经营系统
- 商品采购、定价、销售
- 库存管理
- 店铺升级与装修

### 2. 炼制系统 (CraftingSystem)
- 丹药炼制
- 法器打造
- 符箓绘制
- 配方研究与解锁

### 3. 对话系统 (DialogueSystem)
- 与 NPC 交互
- 任务接取与交付
- 剧情推进

### 4. 数据管理器 (DataManager)
- 从 JSON 文件加载所有静态数据
- 管理物品、天篆、生物、材料、掉落表等数据
- 支持 Mod 扩展机制

### 5. 时间管理系统 (TimeManager)
- 游戏时间的流逝与流速控制
- 小时/天事件广播
- 离线时间计算与收益处理
- 快进功能（休息、冥想）

### 6. 背包管理器 (InventoryManager)
- 可堆叠物品和唯一实例物品管理
- 灵石/货币管理
- 物品添加、移除、查询接口
- 物品词条（Affix）系统

### 7. 天篆管理系统 (TianZhuanManager) ⭐ 核心系统
- 天篆数据的加载与管理
- **天篆组合与和谐度计算**（核心玩法）
- 道纹音律谐振频率表
- 组合效果预测（用于衍化创法）
- 与其他系统（战斗、炼制、创造）深度集成

### 8. 数据定义
- **ItemData**: 物品基础数据（名称、类型、品质、价格等）
- **NPCData**: NPC 数据（名称、修为、性格、喜好等）
- **TianZhuanData**: 天篆数据（核心力量体系）
- **RecipeData**: 炼制配方数据

## 游戏设计文档

项目位于 `游戏设计/` 目录，包含完整的世界观设定：

- **法则体系**: 七大本源、二十二道枢
- **修士体系**: 境界划分、修炼方式
- **文明历史**: 五大历史纪元
- **各类子系统**: 法宝、符箓、阵法、丹药等详细设定

## 开发规范

### 代码风格
- 使用 C# 12 特性
- 启用可空引用类型 (`<Nullable>enable</Nullable>`)
- 命名空间: `TianYanShop`
- 优先使用 Rider 的代码分析功能

### Godot 规范
- 节点命名使用 PascalCase
- 信号连接使用代码方式（而非编辑器）
- 场景文件 (.tscn) 使用版本控制

### 资源命名
- 纹理: `icon_sword.png`, `bg_shop.png`
- 音频: `sfx_click.wav`, `bgm_main.ogg`
- 数据: `items.json`, `recipes.json`

## 构建与运行

```bash
# 使用 Godot CLI 导出
godot --export-release "Windows Desktop" build/TianYanShop.exe

# 或使用 .NET CLI 编译 (用于检查)
dotnet build TianYanShop.csproj
```

## 待办事项 (TODO)

- [x] 创建 DataManager 数据管理器
- [x] 创建 TimeManager 时间管理系统
- [x] 创建 InventoryManager 背包管理器
- [x] 创建 TianZhuanManager 天篆管理系统
- [ ] 实现完整的商品交易系统
- [ ] 添加更多炼制配方
- [ ] 完善 NPC AI 与事件系统
- [ ] 设计主界面 UI
- [ ] 添加音效与背景音乐

---

*项目创建日期: 2026-01-31*
*作者: Shawn*
