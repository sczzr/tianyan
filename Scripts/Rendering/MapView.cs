using Godot;
using System.Collections.Generic;
using FantasyMapGenerator.Scripts.Core;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Map.Heightmap;

namespace FantasyMapGenerator.Scripts.Rendering;

/// <summary>
/// 地图视图节点，负责渲染地图
/// </summary>
public partial class MapView : Node2D
{
    [Export]
    public int CellCount
    {
        get => _cellCount;
        set
        {
            _cellCount = value;
            if (_mapGenerator != null)
            {
                GenerateMap();
                QueueRedraw();
            }
        }
    }

    [Export]
    public bool AutoRegenerate { get; set; } = false;

    [Export]
    public bool ShowRivers { get; set; } = true;

    [Export]
    public bool ShowOceanLayers { get; set; } = true;

    [Export]
    public bool UseBiomeColors { get; set; } = true;

    private int _cellCount = 500;
    private MapGenerator _mapGenerator;
    private bool _isGenerating = false;

    // 渲染颜色
    private static readonly Color RiverColor = new Color(0.2f, 0.4f, 0.9f, 0.9f);
    private static readonly Color OceanLayerColor = new Color(0.9f, 0.95f, 1.0f, 0.15f);

    public override void _Ready()
    {
        _mapGenerator = new MapGenerator();
        GenerateMap();
        GD.Print("MapView initialized with ", CellCount, " cells");
    }

    public void GenerateMap()
    {
        if (_isGenerating) return;
        _isGenerating = true;

        _mapGenerator.GenerateWithNewSeed(_cellCount);
        QueueRedraw();

        _isGenerating = false;
    }

    public void GenerateMapWithSeed(string seed)
    {
        if (_isGenerating) return;
        _isGenerating = true;

        _mapGenerator.Generate(seed, _cellCount);
        QueueRedraw();

        _isGenerating = false;
    }

    public void GenerateMapWithSeed(int seed)
    {
        GenerateMapWithSeed(seed.ToString());
    }

    public void SetWaterLevel(float level)
    {
        if (_mapGenerator?.Data != null)
        {
            var heightmap = _mapGenerator.Data.Heightmap;
            var cells = _mapGenerator.Data.Cells;
            int width = (int)_mapGenerator.Data.MapSize.X;
            int height = (int)_mapGenerator.Data.MapSize.Y;

            var processor = new HeightmapProcessor(_mapGenerator.PRNG);
            processor.WaterLevel = level;
            processor.ApplyToCells(cells, heightmap, width, height);
            processor.AssignColors(cells);

            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_mapGenerator?.Data == null) return;

        // 1. 绘制基础地形（Cell多边形）
        DrawCells();

        // 2. 绘制海洋分层效果
        if (ShowOceanLayers)
        {
            DrawOceanLayers();
        }

        // 3. 绘制河流
        if (ShowRivers)
        {
            DrawRivers();
        }
    }

    /// <summary>
    /// 绘制Cell多边形
    /// </summary>
    private void DrawCells()
    {
        var cells = _mapGenerator.Data.Cells;

        foreach (var cell in cells)
        {
            if (cell.Vertices != null && cell.Vertices.Count >= 3)
            {
                var points = new Vector2[cell.Vertices.Count];
                for (int i = 0; i < cell.Vertices.Count; i++)
                {
                    points[i] = cell.Vertices[i];
                }

                Color color = UseBiomeColors ? BiomeData.GetColor(cell.BiomeId) : cell.RenderColor;
                var colors = new Color[cell.Vertices.Count];
                for (int i = 0; i < cell.Vertices.Count; i++)
                {
                    colors[i] = color;
                }
                DrawPolygon(points, colors);
            }
            else if (cell.Vertices != null && cell.Vertices.Count == 2)
            {
                Color color = UseBiomeColors ? BiomeData.GetColor(cell.BiomeId) : cell.RenderColor;
                DrawLine(cell.Vertices[0], cell.Vertices[1], color, 1f);
            }
        }
    }

    /// <summary>
    /// 绘制海洋分层效果
    /// </summary>
    private void DrawOceanLayers()
    {
        var cells = _mapGenerator.Data.Cells;

        // 绘制不同深度的海洋层
        sbyte[] depths = { -2, -4, -6, -8 };
        float baseOpacity = 0.1f;

        foreach (sbyte depth in depths)
        {
            float opacity = baseOpacity * (1 + Mathf.Abs(depth) * 0.1f);
            var layerColor = new Color(OceanLayerColor.R, OceanLayerColor.G, OceanLayerColor.B, opacity);

            foreach (var cell in cells)
            {
                if (cell.DistanceField <= depth && cell.Vertices != null && cell.Vertices.Count >= 3)
                {
                    var points = new Vector2[cell.Vertices.Count];
                    for (int i = 0; i < cell.Vertices.Count; i++)
                    {
                        points[i] = cell.Vertices[i];
                    }
                    var colors = new Color[cell.Vertices.Count];
                    for (int i = 0; i < cell.Vertices.Count; i++)
                    {
                        colors[i] = layerColor;
                    }
                    DrawPolygon(points, colors);
                }
            }
        }
    }

    /// <summary>
    /// 绘制河流
    /// </summary>
    private void DrawRivers()
    {
        var rivers = _mapGenerator.Data.Rivers;
        if (rivers == null) return;

        foreach (var river in rivers)
        {
            if (river.MeanderedPoints == null || river.MeanderedPoints.Count < 2)
                continue;

            // 绘制河流为线条（简化版本）
            for (int i = 1; i < river.MeanderedPoints.Count; i++)
            {
                var p1 = new Vector2(river.MeanderedPoints[i - 1].X, river.MeanderedPoints[i - 1].Y);
                var p2 = new Vector2(river.MeanderedPoints[i].X, river.MeanderedPoints[i].Y);

                // 根据位置计算宽度
                float flux = river.MeanderedPoints[i].Z;
                float width = Mathf.Max(0.5f, Mathf.Sqrt(flux) / 10f);

                DrawLine(p1, p2, RiverColor, width);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
        {
            GenerateMap();
        }
    }

    public MapData GetMapData()
    {
        return _mapGenerator?.Data;
    }
}
