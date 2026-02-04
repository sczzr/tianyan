using Godot;
using System;
using System.Collections.Generic;
using TianYanShop.World.Config;

namespace TianYanShop.World.Map
{
    /// <summary>
    /// 世界地图渲染器 - 使用 TileMap 渲染地图，支持相机控制
    /// </summary>
    public partial class WorldMapRenderer : Node2D
    {
        [Export] public int MapWidth = 256;
        [Export] public int MapHeight = 256;
        [Export] public int Seed = -1;
        [Export] public bool RandomSeed = true;

        // 省份配置
        [Export] public string ProvinceName { get; set; } = "";

        // 自定义参数
        [Export] public float CustomWaterLevel { get; set; } = 0.35f;
        [Export] public float CustomForestDensity { get; set; } = 1.0f;
        [Export] public float CustomDesertDensity { get; set; } = 1.0f;
        [Export] public float CustomLakeDensity { get; set; } = 1.0f;
        [Export] public bool UseCustomParameters { get; set; } = false;

        // 组件引用
        private WorldMapGenerator _generator;
        private TileMapLayer _tileMapLayer;
        private WorldMapCamera _camera;

        // 生物群系图集映射
        private Dictionary<BiomeType, int> _biomeToAtlasId = new();

        public override void _Ready()
        {
            // 初始化生成器
            int finalSeed = RandomSeed || Seed == -1 ? (int)Time.GetUnixTimeFromSystem() : Seed;
            
            // 获取省份配置
            ChinaProvinceConfig provinceConfig = null;
            if (!string.IsNullOrEmpty(ProvinceName))
            {
                ProvinceConfigManager.Initialize();
                provinceConfig = ProvinceConfigManager.GetProvince(ProvinceName);
                GD.Print($"使用省份配置: {ProvinceName} ({provinceConfig.TerrainType})");
            }
            
            _generator = new WorldMapGenerator(MapWidth, MapHeight, finalSeed, provinceConfig);

            GD.Print($"世界地图初始化 - 尺寸: {MapWidth}x{MapHeight}, 种子: {finalSeed}");

            // 生成地图数据
            _generator.GenerateMap();

            // 设置 TileMap
            SetupTileMap();

            // 渲染地图
            RenderMap();

            // 设置相机
            SetupCamera();
        }

        /// <summary>
        /// 设置 TileMap
        /// </summary>
        private void SetupTileMap()
        {
            _tileMapLayer = new TileMapLayer();
            _tileMapLayer.Name = "WorldMapTileLayer";
            AddChild(_tileMapLayer);

            // 创建 TileSet
            var tileSet = new TileSet();
            tileSet.TileSize = new Vector2I(32, 32);

            // 为每个生物群系创建图集源
            int atlasId = 0;
            foreach (var kvp in WorldMapGenerator.Biomes)
            {
                var biomeData = kvp.Value;
                var texture = GD.Load<Texture2D>(biomeData.TexturePath);

                if (texture == null)
                {
                    GD.PrintErr($"无法加载纹理: {biomeData.TexturePath}");
                    continue;
                }

                var atlasSource = new TileSetAtlasSource();
                atlasSource.Texture = texture;
                atlasSource.TextureRegionSize = new Vector2I(32, 32);
                atlasSource.CreateTile(Vector2I.Zero);

                int sourceId = tileSet.AddSource(atlasSource);
                _biomeToAtlasId[kvp.Key] = sourceId;

                GD.Print($"注册生物群系: {biomeData.Name} -> AtlasID: {sourceId}");
            }

            _tileMapLayer.TileSet = tileSet;
        }

        /// <summary>
        /// 渲染地图到 TileMap
        /// </summary>
        private void RenderMap()
        {
            GD.Print("开始渲染地图...");

            int tilesX = Mathf.CeilToInt((float)MapWidth / 32);
            int tilesY = Mathf.CeilToInt((float)MapHeight / 32);

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    var tile = _generator.MapTiles[x, y];

                    if (_biomeToAtlasId.TryGetValue(tile.Biome, out int atlasId))
                    {
                        _tileMapLayer.SetCell(new Vector2I(x, y), atlasId, Vector2I.Zero);
                    }
                }
            }

            GD.Print($"地图渲染完成: {MapWidth}x{MapHeight} 瓦片");
        }

        /// <summary>
        /// 设置相机
        /// </summary>
        private void SetupCamera()
        {
            // 查找或创建相机
            _camera = GetNodeOrNull<WorldMapCamera>("WorldMapCamera");

            if (_camera == null)
            {
                _camera = new WorldMapCamera();
                _camera.Name = "WorldMapCamera";
                AddChild(_camera);
            }

            // 设置地图边界
            _camera.SetMapBounds(MapWidth * 32, MapHeight * 32);

            // 将相机置于地图中心
            _camera.CenterOnPosition(new Vector2(MapWidth * 16, MapHeight * 16));
        }

        /// <summary>
        /// 重新生成地图
        /// </summary>
        public void RegenerateMap()
        {
            Seed = (int)Time.GetUnixTimeFromSystem();

            ChinaProvinceConfig provinceConfig = null;

            if (UseCustomParameters)
            {
                GD.Print($"使用自定义参数 - 水位: {CustomWaterLevel}, 森林: {CustomForestDensity}, 沙漠: {CustomDesertDensity}, 湖泊: {CustomLakeDensity}");
                _generator = new WorldMapGenerator(MapWidth, MapHeight, Seed, null);
                _generator.WaterLevel = CustomWaterLevel;
                _generator.ForestDensity = CustomForestDensity;
                _generator.DesertDensity = CustomDesertDensity;
                _generator.LakeDensity = CustomLakeDensity;
            }
            else if (!string.IsNullOrEmpty(ProvinceName))
            {
                ProvinceConfigManager.Initialize();
                provinceConfig = ProvinceConfigManager.GetProvince(ProvinceName);
                GD.Print($"使用省份配置: {ProvinceName} ({provinceConfig.TerrainType})");
                _generator = new WorldMapGenerator(MapWidth, MapHeight, Seed, provinceConfig);
            }
            else
            {
                _generator = new WorldMapGenerator(MapWidth, MapHeight, Seed, null);
            }

            _generator.GenerateMap();

            _tileMapLayer.Clear();
            RenderMap();
            GD.Print($"地图重新生成 - 新种子: {Seed}, 省份: {ProvinceName}, 自定义: {UseCustomParameters}");
        }

        /// <summary>
        /// 获取世界位置的瓦片信息
        /// </summary>
        public MapTile GetTileAtWorldPosition(Vector2 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.X / 32);
            int y = Mathf.FloorToInt(worldPos.Y / 32);
            return _generator.GetTile(x, y);
        }
    }
}
