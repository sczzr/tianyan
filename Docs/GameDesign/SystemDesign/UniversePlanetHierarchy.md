## 一、 项目概述

### 1.1 设计目标
本系统旨在替代传统的“新建游戏向导”。在 Fantasy Map Generator 中，创建地图不应只是填写表单，而是一次**扮演造物主（Demiurge）**的体验。
通过引入**“太虚幻境” (The Great Void Interface)**，我们将技术参数包装为世界观概念，利用实时视觉反馈和动态叙事，让玩家在进入地图前就沉浸于自己创造的宇宙中。

### 1.2 美学风格：东方幻想与赛博机枢
本设计采用独特的**“道法/科技”双相美学**：
*   **灵气侧 (Spirit/Magic)**: 水墨晕染、金石铭文、古卷展读、星盘流转。
*   **天工侧 (Artifice/Tech)**: 全息网格、数据流光、晶体界面、深空幽蓝。
*   **交互语言**: 采用半文半白的修辞（如“演化”、“法相”、“界域”），营造史诗感。

---

## 二、 核心体验流程：太虚演化 (Genesis Flow)

不再使用分步向导，而是采用**单屏沉浸式工作台**。

### 2.1 界面布局：太虚幻境 (Genesis Hub)

```text
┌─────────────────────────────────────────────────────────────────────────┐
│  🌌 太虚幻境 (GENESIS HUB)                                  [ 归去 ]    │
├──────────────────────┬──────────────────────────────────────────────────┤
│  [壹] 天道法则       │         ( 中央视口 Viewport )                    │
│      灵气 / 天工     │                                                  │
│      [====☯====]     │        🪐 混元一气 (3D 星球实时预览)             │
│   (影响UI主题与规则) │        (悬浮于混沌星云或深空数据流中)            │
│                      │                                                  │
│  [贰] 星辰法相       │                                                  │
│      五行 / 规模     │        [ ☀ ] 阴阳流转 (调整温度/地貌颜色)        │
│      [====✦====]     │        [ ≋ ] 沧海桑田 (调整海洋/海平面)          │
│   (影响地理生成)     │                                                  │
│                      │                                                  │
│  [叁] 观测垂范       │  ( 天道卷轴 / 终端日志 Narrative Console )       │
│      游历 / 掌道     │  ┌────────────────────────────────────────────┐  │
│      [====👁====]    │  │ > [天道] 检测到灵气复苏 (Magic 85%)...     │  │
│   (影响层级深度)     │  │ > [地脉] 正在生成 "后土" 宜居地貌...       │  │
│                      │  │ > [演化] 文明点火，万邦林立 (Complex)...   │  │
│  [肆] 轮回之骰       │  └────────────────────────────────────────────┘  │
│      [ 🎲 ]         │                                                  │
├──────────────────────┼──────────────────────────────────────────────────┤
│  当前界域摘要:       │  创世决断:                                       │
│  "灵气充盈的         │                                                  │
│   极寒仙域..."       │    [ 🎲 重入轮回 ]    [ ⚡ 开天辟地 (生成) ]   │
└──────────────────────┴──────────────────────────────────────────────────┘
```

---

## 三、 详细功能规格

### 3.1 模块一：天道法则 (Cosmic Laws) - 宇宙层

此模块决定宇宙的物理规则和 UI 的视觉主题。

*   **核心控件**: **“双相命盘” (Dual-Phase Slider)**
    *   **左极 (0%)**: **纯粹灵气 (High Magic)**。UI 背景变为淡墨山水，音效为古琴/风铃。
    *   **右极 (100%)**: **纯粹天工 (High Tech)**。UI 背景变为赛博网格，音效为合成器/脉冲。
    *   **中间 (50%)**: **混沌/蒸汽朋克**。齿轮与符文共存。
*   **辅助参数**:
    *   **星火密度 (Civilization Density)**: 原“文明上限”。通过滑块点亮背景中的星辰数量来表达。
    *   **光阴流速 (Time Flow)**: 控制昼夜更替与历史演进速度。

### 3.2 模块二：星辰法相 (Planet Shaping) - 星球层

此模块通过 Shader 实时渲染 3D 星球，所见即所得。

*   **参数映射**:
    | 参数名称         | 游戏内概念   | 视觉反馈 (3D Preview)                                        |
    | :--------------- | :----------- | :----------------------------------------------------------- |
    | **类型 (Type)**  | **五行属性** | **后土(类地)**: 蓝绿; **祝融(熔岩)**: 红黑; **玄冥(冰)**: 蓝白; **罡风(气态)**: 条纹 |
    | **海洋 (Ocean)** | **沧海桑田** | 调整 Shader 的水位阈值，实时淹没或浮现陆地                   |
    | **温度 (Temp)**  | **阴阳调和** | 调整贴图色相：偏蓝(寒冷) <-> 偏红(炎热)                      |
    | **大气 (Atmos)** | **混沌之气** | 调整星球边缘光晕(Fresnel)的强度和云层厚度                    |

### 3.3 模块三：观测垂范 (Hierarchy Archetypes) - 层级层

将原本枯燥的“层级数量”包装为玩家的**观测身份**。

| 身份代号     | 对应层数 | 风格化名称                | 适用玩法  | 描述文案                                   |
| :----------- | :------- | :------------------------ | :-------- | :----------------------------------------- |
| **Simple**   | 1层      | **【 游历 · 红尘客 】**   | 跑团/故事 | "一剑一酒走江湖，不问庙堂，只记风月。"     |
| **Standard** | 2层      | **【 经略 · 一方霸主 】** | 策略/战棋 | "普天之下莫非王土，率土之滨莫非王臣。"     |
| **Complex**  | 6层      | **【 演化 · 掌道者 】**   | 深度模拟  | "天地为炉，造化为工；万物生灭，皆在掌中。" |
| **Custom**   | 自定义   | **【 虚空 · 观测者 】**   | Mod/极客  | "法则已被重写，此界不入五行。"             |

### 3.4 模块四：轮回之骰 (Quick Start)

*   **功能**: 一键随机生成所有参数。
*   **表现**: 界面上的所有滑块快速滚动（老虎机效果），背景在不同主题间快速切换，最后定格并伴随一声定音鼓（或系统启动音）。

---

## 四、 数据架构设计

### 4.1 宇宙数据 (UniverseData.cs)

```csharp
namespace FantasyMapGenerator.Scripts.Data;

public class UniverseData
{
    public string UniverseId { get; set; }
    public string Name { get; set; } // e.g. "天启位面"
    
    // 核心双相参数 (决定 UI 主题和世界观)
    // 0 = 纯魔幻, 100 = 纯科幻
    public int LawAlignment { get; set; } 
    
    // 视觉风格标签 (由 LawAlignment 导出)
    // "Wuxia" (武侠), "HighFantasy" (西幻), "Cyberpunk" (赛博), "Steampunk" (蒸汽)
    public string AestheticTheme { get; set; }

    public int CivilizationDensity { get; set; } // 星火密度
    public float TimeFlowRate { get; set; }
    
    public HierarchyConfigData HierarchyConfig { get; set; }
    public PlanetData CurrentPlanet { get; set; }
}
```

### 4.2 星球数据 (PlanetData.cs)

```csharp
public enum PlanetElement
{
    Terra,  // 后土 (类地)
    Pyro,   // 祝融 (熔岩)
    Cryo,   // 玄冥 (冰原)
    Aero,   // 罡风 (气态/浮空岛)
}

public class PlanetData
{
    public string PlanetId { get; set; }
    public string Name { get; set; }
    
    public PlanetElement Element { get; set; }
    public PlanetSize Size { get; set; }
    
    // 0.0 - 1.0, 用于 Shader 和 地图生成算法
    public float OceanCoverage { get; set; } 
    public float Temperature { get; set; }
    public float AtmosphereDensity { get; set; }
}
```

### 4.3 叙事配置 (NarrativeConfig)

使用 JSON 存储不同文化和参数下的描述文本。

**narrative_zh.json (示例)**
```json
{
  "theme_desc": {
    "magic": "此界灵气充盈，大道法则显化。凡人亦可窥探天机，御剑乘风。",
    "tech": "星舰航迹划破虚空，硅基与碳基共存。数据洪流取代了古老的信仰。",
    "balance": "旧时代的遗迹与新技术的火光交相辉映，蒸汽与符文共同驱动着世界。"
  },
  "planet_desc": {
    "terra": "后土载物，生生不息。蔚蓝海洋拥抱着翠绿大陆，文明火种在此点燃。",
    "pyro": "地火奔涌，山河破碎。祝融怒火从未平息，这是生命的禁区，能量的宝库。"
  },
  "role_desc": {
    "bard": "你选择了【红尘客】视角。世间繁华，只需一张羊皮纸，一壶浊酒。",
    "demiurge": "你选择了【掌道者】视角。社会结构森严，万物皆在天道注视之下。"
  }
}
```

---

## 五、 技术实现方案

### 5.1 核心控制器 (`GenesisController`)

管理创世流程的状态机，负责协调数据模型、UI 表现和最终的地图生成调用。

```csharp
public class GenesisController : MonoBehaviour
{
    // 依赖注入
    public PlanetPreviewController PlanetPreview;
    public ThemeManager ThemeManager;
    public NarrativeGenerator Narrative;

    private UniverseData _tempUniverse;

    void Start() {
        _tempUniverse = new UniverseData();
        // 初始化默认状态
        OnLawAlignmentChanged(50); 
    }

    // 绑定到滑块事件
    public void OnLawAlignmentChanged(float value) {
        _tempUniverse.LawAlignment = (int)value;
        ThemeManager.UpdateVisuals(_tempUniverse.LawAlignment);
        Narrative.RefreshText(_tempUniverse);
    }

    // 绑定到星球参数
    public void OnPlanetParamsChanged() {
        // 更新数据
        // 更新 3D 预览
        PlanetPreview.UpdateShader(_tempUniverse.CurrentPlanet);
    }

    // 最终生成
    public void IgniteBigBang() {
        GameManager.Instance.GenerateWorld(_tempUniverse);
    }
}
```

### 5.2 动态主题管理器 (`ThemeManager`)

负责 UI 材质的无缝切换。

*   **实现原理**: 使用 `CanvasGroup` 的 Alpha 值在两套 UI 面板（灵气版/天工版）之间进行插值混合。
*   **字体切换**: 当 Alignment > 80% 时，字体从“楷体”渐变为“无衬线体”。

### 5.3 星球预览着色器 (`PlanetPreview.shader`)

一个轻量级的 Shader Graph，用于在 UI 上渲染星球。

*   **Properties**:
    *   `_OceanLevel` (Float): 控制水面高度截断。
    *   `_Temperature` (Float): 控制色相偏移 (Hue Shift) 和极地冰盖范围。
    *   `_CloudSpeed` (Float): 控制噪声纹理的 UV 滚动速度。
    *   `_BaseTexture`: 地貌纹理（根据 Element 类型更换）。

---

## 六、 资源需求清单 (Assets)

### 6.1 UI 美术资源
*   **背景图**:
    *   `bg_ink_mountains.png`: 水墨山水动态图（Loop）。
    *   `bg_cyber_void.png`: 深空网格动态图（Loop）。
*   **图标**:
    *   `icon_yin_yang.png`: 双相滑块手柄。
    *   `icon_roles_atlas.png`: 包含 剑/印章/法典/眼睛 的图集。
*   **边框**:
    *   `frame_bronze.png`: 青铜纹理边框。
    *   `frame_hologram.png`: 发光蓝线边框。

### 6.2 音效资源
*   `BGM_Creation.mp3`: 氛围音乐，包含古琴与合成器的混合轨道（随滑块调整音量配比）。
*   `SFX_Slide_Magic.wav`: 纸张摩擦声/风铃声。
*   `SFX_Slide_Tech.wav`: 机械伺服声/电子音。
*   `SFX_Dice_Roll.wav`: 命运之骰滚动的声音。

---

## 七、 开发路线图

### Phase 1: 数据与逻辑 (3-5 天)
*   实现 `UniverseData`, `PlanetData` 等数据类。
*   完成 `HierarchyConfig` 的 JSON 模板加载系统。
*   实现 `NarrativeGenerator` 的文本拼接逻辑。

### Phase 2: 视觉原型 (5-7 天)
*   编写 `PlanetPreview.shader`，确保性能开销极低。
*   搭建 `Genesis Hub` 的 UI 框架（使用占位资源）。
*   实现 `ThemeManager` 的基础切换逻辑。

### Phase 3: 美术整合与打磨 (5-7 天)
*   替换所有 UI 素材为最终美术资源。
*   调整 Shader 参数，使星球看起来美观。
*   编写并校对中/英/日三语的叙事文本。
*   加入音效反馈。

---

## 八、 验收标准

1.  **沉浸感**: 切换“天道法则”滑块时，背景、音效、字体风格的过渡必须平滑流畅，无突兀感。
2.  **可视化**: 调整“海洋覆盖率”时，预览星球的水位变化必须是实时的，且视觉上清晰可见。
3.  **文化适配**: 中文模式下，必须体现出“修仙/武侠/玄幻”的韵味；英文模式下，体现“D&D/Sci-Fi”风格。
4.  **性能**: 星球预览 Shader 不应导致 UI 帧率低于 60fps。

