using Godot;
using System;

namespace TianYanShop
{
    /// <summary>
    /// 世界地图管理器 - 整合生成器和渲染器
    /// </summary>
    public partial class WorldMapManager : Node2D
    {
        [Export] public int MapWidth = 256;
        [Export] public int MapHeight = 256;
        [Export] public int Seed = -1;
        [Export] public bool RandomSeed = true;
        [Export] public bool GenerateOnReady = true;

        // 组件引用
        private WorldMapGenerator _generator;
        private TileMapLayer _tileMapLayer;
        private WorldMapCamera _camera;

        // 信号
        [Signal] public delegate void MapGeneratedEventHandler();

        public override void _Ready()
        {
            // 初始化生成器
            int finalSeed = RandomSeed || Seed == -1 ? (int)Time.GetUnixTimeFromSystem() : Seed;
            _generator = new WorldMapGenerator(MapWidth, MapHeight, finalSeed);

            GD.Print($"世界地图管理器初始化 - 尺寸: {MapWidth}x{MapHeight}, 种子: {finalSeed}");

            // 设置组件
            SetupTileMap();
            SetupCamera();

            // 生成地图
            if (GenerateOnReady)
            {
                GenerateAndRender();
            }
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

                int sourceId = tileSet.AddSource(atlasSource, (int)kvp.Key);

                GD.Print($"注册生物群系: {biomeData.Name} -> ID: {(int)kvp.Key}, SourceID: {sourceId}");
            }

            _tileMapLayer.TileSet = tileSet;
        }

        /// <summary>
        /// 设置相机
        /// </summary>
        private void SetupCamera()
        {
            _camera = GetNodeOrNull<WorldMapCamera>("WorldMapCamera");

            if (_camera == null)
            {
                _camera = new WorldMapCamera();
                _camera.Name = "WorldMapCamera";
                AddChild(_camera);
            }

            // 设置地图边界
            _camera.SetMapBounds(MapWidth * 32, MapHeight * 32);
        }

        /// <summary>
        /// 生成并渲染地图
        /// </summary>
        public void GenerateAndRender()
        {
            GD.Print("开始生成地图数据...");

            // 生成地图数据
            _generator.GenerateMap();

            // 渲染到 TileMap
            RenderMap();

            // 设置相机到地图中心
            _camera.CenterOnPosition(new Vector2(MapWidth * 16, MapHeight * 16));

            // 发出信号
            EmitSignal(SignalName.MapGenerated);

            GD.Print("地图生成完成！");
        }

        /// <summary>
        /// 渲染地图到 TileMap
        /// </summary>
        private void RenderMap()
        {
            GD.Print("开始渲染地图...");

            // 清除现有瓦片
            _tileMapLayer.Clear();

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    var tile = _generator.MapTiles[x, y];
                    int atlasId = (int)tile.Biome;

                    _tileMapLayer.SetCell(new Vector2I(x, y), atlasId, Vector2I.Zero);
                }
            }

            GD.Print($"地图渲染完成: {MapWidth}x{MapHeight} 瓦片");
        }

        /// <summary>
        /// 重新生成地图 (使用新种子)
        /// </summary>
        public void RegenerateMap()
        {
            Seed = (int)Time.GetUnixTimeFromSystem();
            _generator.Regenerate(Seed);
            RenderMap();
            GD.Print($"地图重新生成 - 新种子: {Seed}");
        }

        /// <summary>
        /// 获取指定位置的瓦片数据
        /// </summary>
        public MapTile GetTileAt(int x, int y)
        {
            return _generator.GetTile(x, y);
        }

        /// <summary>
        /// 获取世界位置的瓦片数据
        /// </summary>
        public MapTile GetTileAtWorldPosition(Vector2 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.X / 32);
            int y = Mathf.FloorToInt(worldPos.Y / 32);
            return _generator.GetTile(x, y);
        }

        /// <summary>
        /// 获取地图生成器
        /// </summary>
        public WorldMapGenerator Generator => _generator;

        /// <summary>
        /// 获取相机
        /// </summary>
        public WorldMapCamera MapCamera => _camera;
    }
}
