using System;
using System.Collections.Generic;
using Godot;

using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Types;

namespace TianYanShop.MapGeneration.Rendering
{
    public partial class MapRenderer : Godot.Node2D
    {
        [Export] public bool ShowBiomes { get; set; } = true;
        [Export] public bool ShowRivers { get; set; } = true;
        [Export] public bool ShowBorders { get; set; } = false;
        [Export] public bool ShowBurgs { get; set; } = true;
        [Export] public bool ShowLabels { get; set; } = false;
        [Export] public bool ShowCoastline { get; set; } = true;

        private VoronoiGraph _graph;
        private ImageTexture _biomeTexture;
        private Image _biomeImage;

        private readonly Color[] BiomeColors = new Color[13];

        public override void _Ready()
        {
            InitializeBiomeColors();
        }

        public void Initialize()
        {
            CreateBiomeTexture();
        }

        public void Render(VoronoiGraph graph)
        {
            _graph = graph;
            CreateBiomeTexture();
            QueueRedraw();
        }

        public void UpdateGraph(VoronoiGraph graph)
        {
            _graph = graph;
            CreateBiomeTexture();
            QueueRedraw();
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

        private void CreateBiomeTexture()
        {
            if (_graph == null) 
            {
                GD.PrintErr("_graph is null in CreateBiomeTexture");
                return;
            }

            int width = _graph.Width;
            int height = _graph.Height;

            GD.Print($"Creating biome texture: {width}x{height}");

            _biomeImage = Image.Create(width, height, false, Image.Format.Rgba8);
            
            // 填充海洋背景
            Color oceanColor = BiomeColors[(int)BiomeType.Ocean];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    _biomeImage.SetPixel(x, y, oceanColor);
                }
            }

            // 简单填充：找到每个点附近的像素
            int cellSize = System.Math.Max(width, height) / 10;
            for (int i = 0; i < _graph.CellsCount; i++)
            {
                Vector2 point = _graph.Points[i];
                int biomeId = _graph.Biomes[i];
                Color color = biomeId >= 0 && biomeId < BiomeColors.Length ? BiomeColors[biomeId] : oceanColor;
                
                int px = (int)point.X;
                int py = (int)point.Y;
                int size = System.Math.Max(5, cellSize);
                
                for (int dy = -size; dy <= size; dy++)
                {
                    for (int dx = -size; dx <= size; dx++)
                    {
                        int x = px + dx;
                        int y = py + dy;
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            _biomeImage.SetPixel(x, y, color);
                        }
                    }
                }
            }

            _biomeTexture = ImageTexture.CreateFromImage(_biomeImage);
            GD.Print($"_biomeTexture created: {_biomeTexture != null}, size: {width}x{height}");
        }

        private void DrawBiomes()
        {
            if (!ShowBiomes || _biomeTexture == null) return;
            DrawTexture(_biomeTexture, Vector2.Zero);
        }

        private void DrawCoastline()
        {
            if (!ShowCoastline) return;

            var coastlineColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            float lineWidth = 1.5f;

            for (int i = 0; i < _graph.CellsCount; i++)
            {
                if ((int)_graph.Terrains[i] == (int)TerrainType.Coastline)
                {
                    foreach (var neighbor in _graph.Neighbors[i])
                    {
                        if (neighbor >= 0 && neighbor < _graph.CellsCount &&
                            (int)_graph.Terrains[neighbor] == (int)TerrainType.Ocean)
                        {
                            Vector2 p1 = _graph.Points[i];
                            Vector2 p2 = _graph.Points[neighbor];
                            DrawLine(p1, p2, coastlineColor, lineWidth);
                        }
                    }
                }
            }
        }

        private void DrawRivers()
        {
            if (!ShowRivers) return;

            var riverColor = new Color(0.2f, 0.4f, 0.8f, 0.8f);

            foreach (var river in _graph.RiversList)
            {
                if (river.Cells.Count < 2) continue;

                for (int i = 0; i < river.Cells.Count - 1; i++)
                {
                    Vector2 p1 = _graph.Points[river.Cells[i]];
                    Vector2 p2 = _graph.Points[river.Cells[i + 1]];
                    DrawLine(p1, p2, riverColor, 1f);
                }
            }
        }

        public override void _Draw()
        {
            // 绘制测试矩形
            DrawRect(new Rect2(100, 100, 200, 200), Colors.Red);
            
            if (_graph == null) return;

            DrawBiomes();
            DrawCoastline();
            DrawRivers();
            DrawBorders();
            DrawBurgs();
            DrawLabels();
        }

        private void DrawBorders()
        {
            if (!ShowBorders) return;

            var borderColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            for (int i = 0; i < _graph.CellsCount; i++)
            {
                if (_graph.Neighbors[i] == null) continue;

                foreach (int neighbor in _graph.Neighbors[i])
                {
                    if (neighbor <= i) continue;
                    if (_graph.States[i] != _graph.States[neighbor])
                    {
                        Vector2 p1 = _graph.Points[i];
                        Vector2 p2 = _graph.Points[neighbor];
                        DrawLine(p1, p2, borderColor, 1f);
                    }
                }
            }
        }

        private void DrawBurgs()
        {
            if (!ShowBurgs) return;

            foreach (var burg in _graph.BurgsList)
            {
                Vector2 pos = _graph.Points[burg.Cell];
                DrawCircle(pos, 3f, Colors.White);

                if (burg.Walled)
                {
                    DrawCircle(pos, 5f, Colors.Gray, false, 1f);
                }
            }
        }

        private void DrawLabels()
        {
            if (!ShowLabels) return;

            foreach (var burg in _graph.BurgsList)
            {
                if (burg.Id == 0 || burg.Type < (int)BurgType.City) continue;

                Vector2 pos = _graph.Points[burg.Cell];
                DrawString(GetDefaultFont(), pos + new Vector2(8, -8), burg.Name,
                    HorizontalAlignment.Left, -1, 12, Colors.White);
            }
        }

        private Font GetDefaultFont()
        {
            return ThemeDB.GetFallbackFont();
        }

        public void SetLayerVisible(MapLayer layer, bool visible)
        {
            switch (layer)
            {
                case MapLayer.Biomes:
                    ShowBiomes = visible;
                    break;
                case MapLayer.Rivers:
                    ShowRivers = visible;
                    break;
                case MapLayer.Borders:
                    ShowBorders = visible;
                    break;
                case MapLayer.Burgs:
                    ShowBurgs = visible;
                    break;
                case MapLayer.Labels:
                    ShowLabels = visible;
                    break;
                case MapLayer.Coastline:
                    ShowCoastline = visible;
                    break;
            }
            QueueRedraw();
        }
    }
}
