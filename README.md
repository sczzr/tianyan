# Fantasy Map Generator (Godot C#)

基于 [Fantasy-Map-Generator](https://github.com/Azgaar/Fantasy-Map-Generator) 的 C# 重写版本，用于 Godot 4.x。

## 功能

- 基于 Perlin 噪声生成地形高度图
- Voronoi 多边形单元格划分
- 自动识别海洋/陆地
- 可配置的水位线
- JSON 导出

## 项目结构

```
res://
├── Scripts/
│   ├── Types.cs              # 核心数据结构
│   ├── Prng.cs               # Alea 伪随机数生成器
│   ├── PerlinNoise.cs        # Perlin 噪声实现
│   ├── Delaunay.cs           # Delaunay 三角剖分
│   ├── VoronoiGenerator.cs   # Voronoi 多边形生成
│   ├── HeightmapProcessor.cs # 高度图处理
│   ├── MapGenerator.cs       # 主地图生成器
│   └── MapView.cs            # Godot 渲染节点
├── Scenes/
│   └── Main.tscn             # 主场景
├── project.godot             # Godot 项目配置
└── icon.svg                  # 项目图标
```

## 运行方法

1. 安装 Godot 4.x (带 .NET 支持)
2. 打开项目文件夹
3. 点击 "Import" 导入 `project.godot`
4. 运行项目 (F5)

## 控制

- **左键点击地图**: 重新生成地图
- **Cell Count Slider**: 调整单元格数量 (100-2000)
- **Water Level Slider**: 调整水位线
- **Regenerate Button**: 手动重新生成
- **Export JSON**: 导出地图数据

## 后续计划

- [ ] 河流生成系统
- [ ] 城镇生成
- [ ] 国家边界算法
- [ ] 名称生成
- [ ] 生物群落系统
- [ ] 道路/航线生成

## 许可证

MIT License (参考原项目)
