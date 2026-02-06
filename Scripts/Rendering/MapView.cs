using Godot;
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

    private int _cellCount = 500;
    private MapGenerator _mapGenerator;
    private bool _isGenerating = false;

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
                var colors = new Color[cell.Vertices.Count];
                for (int i = 0; i < cell.Vertices.Count; i++)
                {
                    colors[i] = cell.RenderColor;
                }
                DrawPolygon(points, colors);
            }
            else if (cell.Vertices != null && cell.Vertices.Count == 2)
            {
                DrawLine(cell.Vertices[0], cell.Vertices[1], cell.RenderColor, 1f);
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
