using Godot;

using TianYanShop.MapGeneration;
using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Editor;
using TianYanShop.MapGeneration.Rendering;
using TianYanShop.MapGeneration.Generation;

namespace TianYanShop.Scene
{
    public partial class MapEditorScene : Control
    {
        private MapGenerator _mapGenerator;
        private VoronoiGraph _graph;
        private bool _mapGenerated = false;
        
        private Color[] BiomeColors = new Color[13];

        [Export]
        private VBoxContainer ToolButtons { get; set; }

        [Export]
        private Button HillBrushButton { get; set; }

        [Export]
        private Button PitBrushButton { get; set; }

        [Export]
        private Button MountainBrushButton { get; set; }

        [Export]
        private Button SmoothBrushButton { get; set; }

        [Export]
        private Button ValleyBrushButton { get; set; }

        [Export]
        private Button AddBurgButton { get; set; }

        [Export]
        private Button RemoveBurgButton { get; set; }

        [Export]
        private Button SaveButton { get; set; }

        [Export]
        private Button ExportButton { get; set; }

        [Export]
        private Button BackButton { get; set; }

        [Export]
        private Button GenerateButton { get; set; }

        [Export]
        private Slider RadiusSlider { get; set; }

        [Export]
        private Slider StrengthSlider { get; set; }

        [Export]
        private Label RadiusLabel { get; set; }

        [Export]
        private Label StrengthLabel { get; set; }

        [Export]
        private Label StatusLabel { get; set; }

        private EditorToolType _currentTool = EditorToolType.HillBrush;
        private float _brushRadius = 20f;
        private float _brushStrength = 0.5f;
        private bool _isMouseDown = false;
        private int _selectedCell = -1;
        private int _generateCallCount = 0;
        private bool _initialized = false;
        private int _readyCallCount = 0;

        public override void _Ready()
        {
            InitializeBiomeColors();
            
            GD.Print($"=== _Ready() START (call #{++_readyCallCount}) ===");
            
            if (_initialized)
            {
                GD.Print("=== _Ready() ALREADY CALLED, SKIPPING ===");
                return;
            }
            _initialized = true;
            
            GetNodeReferences();
            ConnectSignals();
            GenerateMap();
            GD.Print("=== _Ready() END ===");
        }

        private void InitializeBiomeColors()
        {
            BiomeColors[(int)BiomeType.Ocean] = new Color(0.16f, 0.38f, 0.65f);
            BiomeColors[(int)BiomeType.Lake] = new Color(0.16f, 0.38f, 0.75f);
            BiomeColors[(int)BiomeType.TropicalDesert] = new Color(0.95f, 0.9f, 0.67f);
            BiomeColors[(int)BiomeType.TemperateDesert] = new Color(0.93f, 0.85f, 0.67f);
            BiomeColors[(int)BiomeType.ColdDesert] = new Color(0.8f, 0.78f, 0.7f);
            BiomeColors[(int)BiomeType.TropicalRainforest] = new Color(0.05f, 0.4f, 0.1f);
            BiomeColors[(int)BiomeType.TropicalSeasonalForest] = new Color(0.3f, 0.6f, 0.15f);
            BiomeColors[(int)BiomeType.TemperateSeasonalForest] = new Color(0.3f, 0.55f, 0.25f);
            BiomeColors[(int)BiomeType.TemperateRainforest] = new Color(0.15f, 0.45f, 0.2f);
            BiomeColors[(int)BiomeType.BorealForest] = new Color(0.15f, 0.35f, 0.25f);
            BiomeColors[(int)BiomeType.Tundra] = new Color(0.55f, 0.65f, 0.55f);
            BiomeColors[(int)BiomeType.Snow] = new Color(0.95f, 0.98f, 1f);
            BiomeColors[(int)BiomeType.Mangrove] = new Color(0.1f, 0.45f, 0.35f);
        }

        private void GetNodeReferences()
        {
            GD.Print("Getting node references...");
            
            ToolButtons = GetNode<VBoxContainer>("Toolbar/HBoxContainer/ToolSection/ToolButtons");
            HillBrushButton = GetNode<Button>("Toolbar/HBoxContainer/ToolSection/ToolButtons/HillBrushButton");
            PitBrushButton = GetNode<Button>("Toolbar/HBoxContainer/ToolSection/ToolButtons/PitBrushButton");
            MountainBrushButton = GetNode<Button>("Toolbar/HBoxContainer/ToolSection/ToolButtons/MountainBrushButton");
            SmoothBrushButton = GetNode<Button>("Toolbar/HBoxContainer/ToolSection/ToolButtons/SmoothBrushButton");
            ValleyBrushButton = GetNode<Button>("Toolbar/HBoxContainer/ToolSection/ToolButtons/ValleyBrushButton");
            AddBurgButton = GetNode<Button>("Toolbar/HBoxContainer/ToolSection/ToolButtons/AddBurgButton");
            RemoveBurgButton = GetNode<Button>("Toolbar/HBoxContainer/ToolSection/ToolButtons/RemoveBurgButton");

            RadiusSlider = GetNode<HSlider>("Toolbar/HBoxContainer/BrushSection/RadiusSlider");
            StrengthSlider = GetNode<HSlider>("Toolbar/HBoxContainer/BrushSection/StrengthSlider");
            RadiusLabel = GetNode<Label>("Toolbar/HBoxContainer/BrushSection/RadiusLabel");
            StrengthLabel = GetNode<Label>("Toolbar/HBoxContainer/BrushSection/StrengthLabel");

            SaveButton = GetNode<Button>("Toolbar/HBoxContainer/ButtonSection/SaveButton");
            ExportButton = GetNode<Button>("Toolbar/HBoxContainer/ButtonSection/ExportButton");
            BackButton = GetNode<Button>("Toolbar/HBoxContainer/ButtonSection/BackButton");
            GenerateButton = GetNode<Button>("Toolbar/HBoxContainer/ButtonSection/GenerateButton");

            StatusLabel = GetNode<Label>("Toolbar/HBoxContainer/StatusLabel");
            
            GD.Print("All references acquired");
        }

        private void ConnectSignals()
        {
            HillBrushButton.Pressed += () => OnToolSelected(EditorToolType.HillBrush);
            PitBrushButton.Pressed += () => OnToolSelected(EditorToolType.PitBrush);
            MountainBrushButton.Pressed += () => OnToolSelected(EditorToolType.RangeBrush);
            SmoothBrushButton.Pressed += () => OnToolSelected(EditorToolType.SmoothBrush);
            ValleyBrushButton.Pressed += () => OnToolSelected(EditorToolType.TroughBrush);
            AddBurgButton.Pressed += () => OnToolSelected(EditorToolType.BurgAdd);
            RemoveBurgButton.Pressed += () => OnToolSelected(EditorToolType.BurgRemove);

            RadiusSlider.ValueChanged += (double value) => OnRadiusChanged((float)value);
            StrengthSlider.ValueChanged += (double value) => OnStrengthChanged((float)value);

            SaveButton.Pressed += OnSavePressed;
            ExportButton.Pressed += OnExportPressed;
            BackButton.Pressed += OnBackPressed;
            GenerateButton.Pressed += OnGeneratePressed;
        }

        private void GenerateMap()
        {
            GD.Print($">>> GenerateMap() called (call #{++_generateCallCount})");
            
            _mapGenerator = new MapGenerator("default_seed");
            GD.Print("MapGenerator created");
            
            _graph = _mapGenerator.Generate(new MapSettings
            {
                Width = 1024,
                Height = 1024,
                CellsCount = 1000,
                StatesNumber = 5,
                BurgsNumber = 10,
                AddRivers = true,
                AddLakes = true,
                AddRoads = true,
                AddSeaRoutes = true
            });
            
            GD.Print($"Map generated. Points: {(_graph?.Points?.Length ?? 0)}");
            GD.Print($"BurgsList count: {_graph?.BurgsList?.Count ?? 0}");

            _mapGenerated = true;
            GD.Print($"_mapGenerated set to: {_mapGenerated}");
            GD.Print("Calling QueueRedraw()...");
            QueueRedraw();
            GD.Print("QueueRedraw() called");

            UpdateStatus("地图已生成 - 开始编辑");
        }

        public override void _Draw()
        {
            GD.Print($"_Draw called. _mapGenerated={_mapGenerated}, _graph={(_graph != null ? "not null" : "null")}");
            
            // 绘制测试背景 - 蓝色
            DrawRect(new Rect2(0, 0, 1024, 768), Colors.Blue);
            
            if (!_mapGenerated || _graph == null)
            {
                // 绘制测试文字
                DrawString(GetThemeDefaultFont(), new Vector2(100, 100), "Waiting for map...", HorizontalAlignment.Left, -1, 24, Colors.White);
                return;
            }

            GD.Print($"Drawing map with {_graph.CellsCount} cells");

            int width = _graph.Width;
            int height = _graph.Height;

            // 绘制海洋背景
            DrawRect(new Rect2(0, 0, width, height), BiomeColors[(int)BiomeType.Ocean]);

            int cellSize = System.Math.Max(width, height) / 20;
            
            // 绘制生物群系
            for (int i = 0; i < _graph.CellsCount; i++)
            {
                Vector2 point = _graph.Points[i];
                int biomeId = _graph.Biomes[i];
                Color color = biomeId >= 0 && biomeId < BiomeColors.Length ? BiomeColors[biomeId] : BiomeColors[(int)BiomeType.Ocean];
                
                int px = (int)point.X;
                int py = (int)point.Y;
                int size = System.Math.Max(3, cellSize);
                
                DrawRect(
                    new Rect2(px - size, py - size, size * 2, size * 2), 
                    color
                );
            }

            // 绘制城市点
            if (_graph.BurgsList != null)
            {
                foreach (var burg in _graph.BurgsList)
                {
                    Vector2 pos = _graph.Points[burg.Cell];
                    DrawCircle(pos, 5f, Colors.White);
                }
            }
            
            GD.Print("Map drawing completed");
        }

        private void OnToolSelected(EditorToolType tool)
        {
            _currentTool = tool;
            string toolName = tool switch
            {
                EditorToolType.HillBrush => "山丘笔刷",
                EditorToolType.PitBrush => "坑洼笔刷",
                EditorToolType.RangeBrush => "山脉笔刷",
                EditorToolType.SmoothBrush => "平滑笔刷",
                EditorToolType.TroughBrush => "河谷笔刷",
                EditorToolType.BurgAdd => "添加城市",
                EditorToolType.BurgRemove => "移除城市",
                _ => "未知工具"
            };
            UpdateStatus($"当前工具: {toolName}");
        }

        private void OnRadiusChanged(float value)
        {
            _brushRadius = value;
            RadiusLabel.Text = $"笔刷半径: {value:F0}";
        }

        private void OnStrengthChanged(float value)
        {
            _brushStrength = value;
            StrengthLabel.Text = $"笔刷强度: {value:F2}";
        }

        private void UpdateStatus(string text)
        {
            StatusLabel.Text = text;
        }

        private void OnSavePressed()
        {
            GD.Print("Saving map...");
            UpdateStatus("地图已保存");
        }

        private void OnExportPressed()
        {
            GD.Print("Exporting map scene...");
            UpdateStatus("场景已导出");
        }

        private void OnBackPressed()
        {
            GetTree().ChangeSceneToFile("res://Scenes/Main/StartScene.tscn");
        }

        private void OnGeneratePressed()
        {
            GenerateMap();
            UpdateStatus("地图已重新生成");
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                switch (keyEvent.Keycode)
                {
                    case Key.H:
                        OnToolSelected(EditorToolType.HillBrush);
                        break;
                    case Key.P:
                        OnToolSelected(EditorToolType.PitBrush);
                        break;
                    case Key.M:
                        OnToolSelected(EditorToolType.RangeBrush);
                        break;
                    case Key.S:
                        OnToolSelected(EditorToolType.SmoothBrush);
                        break;
                    case Key.V:
                        OnToolSelected(EditorToolType.TroughBrush);
                        break;
                    case Key.A:
                        OnToolSelected(EditorToolType.BurgAdd);
                        break;
                    case Key.D:
                        OnToolSelected(EditorToolType.BurgRemove);
                        break;
                }
            }
        }
    }
}
