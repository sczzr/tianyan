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
        [Export] public float CustomTemperature { get; set; } = 0.5f;
        [Export] public float CustomPrecipitation { get; set; } = 0.5f;
        [Export] public float CustomContinentality { get; set; } = 0.5f;
        [Export] public float CustomElevationVariation { get; set; } = 1.0f;
        [Export] public float CustomSpiritDensity { get; set; } = 0.5f;
        [Export] public bool UseCustomParameters { get; set; } = false;

        // 组件引用
        private WorldMapGenerator _generator;
        private TileMapLayer _tileMapLayer;
        private WorldMapCamera _camera;
        private Layer.MapLayerManager _layerManager;

        // 宗门系统
        private Sect.SectGenerator _sectGenerator;

        // 生物群系图集映射
        private Dictionary<BiomeType, int> _biomeToAtlasId = new();

        // 单元格选择相关
        private Vector2I _selectedCell = new Vector2I(-1, -1);
        private TileMapLayer _selectionLayer;
        private Color _selectionColor = new Color(1f, 1f, 0f, 0.5f);
        private int _selectionSourceId = 0;
        public event Action<Vector2I, MapTile>? OnCellSelected;
        public event Action<Vector2I, MapTile>? OnCellClicked;

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

            // 初始化图层管理器
            InitializeLayerManager();

            // 初始化宗门系统
            InitializeSectSystem();

            // 初始化选择层
            InitializeSelectionLayer();

            // 设置输入处理
            SetProcessInput(true);
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
            int seed = RandomSeed || Seed == -1 ? (int)Time.GetUnixTimeFromSystem() : Seed;

            ChinaProvinceConfig provinceConfig = null;
 
            if (UseCustomParameters)
            {
                GD.Print($"使用自定义参数 - 温度: {CustomTemperature}, 降水: {CustomPrecipitation}, 大陆度: {CustomContinentality}, 海拔变异: {CustomElevationVariation}, 灵气: {CustomSpiritDensity}");
                _generator = new WorldMapGenerator(MapWidth, MapHeight, seed, null);
                // 使用温度系数作为海洋阈值
                _generator.WaterLevel = 0.35f; // 固定水位
                _generator.BaseTemperature = CustomTemperature;
                _generator.BasePrecipitation = CustomPrecipitation;
                _generator.Continentality = CustomContinentality;
                _generator.ElevationVariation = CustomElevationVariation;
                _generator.BaseSpiritDensity = CustomSpiritDensity;
            }
            else if (!string.IsNullOrEmpty(ProvinceName))
            {
                ProvinceConfigManager.Initialize();
                provinceConfig = ProvinceConfigManager.GetProvince(ProvinceName);
                GD.Print($"使用省份配置: {ProvinceName} ({provinceConfig.TerrainType})");
                _generator = new WorldMapGenerator(MapWidth, MapHeight, seed, provinceConfig);
            }
            else
            {
                _generator = new WorldMapGenerator(MapWidth, MapHeight, seed, null);
            }

            _generator.GenerateMap();

            _tileMapLayer.Clear();
            RenderMap();

            _selectionLayer.Clear();

            // 重新初始化图层管理器
            InitializeLayerManager();

            // 重新初始化宗门系统
            InitializeSectSystem();

            // 更新相机边界
            if (_camera != null)
            {
                _camera.SetMapBounds(MapWidth * 32, MapHeight * 32);
                // 在 Fixed Top Left 模式下，GlobalPosition 是屏幕左上角的世界坐标
                // 要让地图中心显示在屏幕中心，需要减去视口的一半（考虑缩放）
                Vector2 viewportHalf = GetViewportRect().Size / 2.0f / _camera.Zoom.X;
                _camera.CenterOnPosition(new Vector2(MapWidth * 16, MapHeight * 16) - viewportHalf);
            }

            GD.Print($"地图重新生成 - 新种子: {seed}, 省份: {ProvinceName}, 自定义: {UseCustomParameters}");
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

        /// <summary>
        /// 初始化图层管理器
        /// </summary>
        private void InitializeLayerManager()
        {
            _layerManager = GetNodeOrNull<Layer.MapLayerManager>("MapLayerManager");
            
            if (_layerManager == null)
            {
                _layerManager = new Layer.MapLayerManager();
                _layerManager.Name = "MapLayerManager";
                AddChild(_layerManager);
                GD.Print("[WorldMapRenderer] 已创建 MapLayerManager");
            }
        }

        /// <summary>
        /// 初始化宗门系统
        /// </summary>
        private void InitializeSectSystem()
        {
            try
            {
                // 创建宗门生成器
                _sectGenerator = new Sect.SectGenerator(_generator.Seed);

                // 获取灵气数据
                float[,] spiritMap = _generator.GetSpiritPowerMap();

                // 初始化宗门生成器
                _sectGenerator.Initialize(MapWidth, MapHeight, spiritMap);

                // 生成所有宗门
                _sectGenerator.GenerateAllSects();

                // 打印统计信息
                _sectGenerator.PrintStatistics();

                // 更新图层管理器的宗门数据
                if (_layerManager != null)
                {
                    var sectList = new System.Collections.Generic.List<Sect.SectData>();
                    sectList.AddRange(_sectGenerator.TopSects);
                    sectList.AddRange(_sectGenerator.LargeSects);
                    sectList.AddRange(_sectGenerator.SmallSects);
                    _layerManager.UpdateSectData(sectList, 32);
                }

                GD.Print($"[WorldMapRenderer] 宗门系统初始化完成，共生成 {_sectGenerator.Sects.Count} 个宗门");
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"[WorldMapRenderer] 宗门系统初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取宗门生成器
        /// </summary>
        public Sect.SectGenerator GetSectGenerator()
        {
            return _sectGenerator;
        }

        /// <summary>
        /// 获取指定位置的宗门信息
        /// </summary>
        public Sect.SectData? GetSectAtPosition(int x, int y)
        {
            return _sectGenerator?.GetSectAtPosition(x, y);
        }

        /// <summary>
        /// 获取指定位置最近的宗门
        /// </summary>
        public Sect.SectData? GetNearestSect(Vector2I position, float maxDistance = -1)
        {
            return _sectGenerator?.GetNearestSect(position, maxDistance);
        }

        /// <summary>
        /// 初始化选择层
        /// </summary>
        private void InitializeSelectionLayer()
        {
            _selectionLayer = new TileMapLayer();
            _selectionLayer.Name = "SelectionLayer";
            _selectionLayer.ZIndex = 100;
            _selectionLayer.ZAsRelative = true;
            AddChild(_selectionLayer);
            MoveChild(_selectionLayer, GetChildCount() - 1);

            var tileSet = new TileSet();
            tileSet.TileSize = new Vector2I(32, 32);

            var selectionTexture = CreateSelectionTexture(_selectionColor);
            var selectionSource = new TileSetAtlasSource();
            selectionSource.Texture = selectionTexture;
            selectionSource.TextureRegionSize = new Vector2I(32, 32);
            selectionSource.CreateTile(Vector2I.Zero);
            _selectionSourceId = tileSet.AddSource(selectionSource);

            _selectionLayer.TileSet = tileSet;
            _selectionLayer.YSortEnabled = false;

            GD.Print("[WorldMapRenderer] 选择层初始化完成");
        }

        /// <summary>
        /// 创建选择纹理
        /// </summary>
        private ImageTexture CreateSelectionTexture(Color color)
        {
            var image = Image.Create(32, 32, false, Image.Format.Rgba8);
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    image.SetPixel(x, y, color);
                }
            }
            return ImageTexture.CreateFromImage(image);
        }

        /// <summary>
        /// 处理输入事件
        /// </summary>
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseButton)
            {
                if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
                {
                    Vector2 worldPos = GetGlobalMousePosition();
                    GD.Print($"鼠标世界坐标: {worldPos}");
                    Vector2I cell = GetCellFromWorldPosition(worldPos);
                    GD.Print($"单元格坐标: ({cell.X}, {cell.Y})");
                    HandleCellClick(cell);
                }
            }
        }

        /// <summary>
        /// 从世界坐标获取单元格坐标
        /// </summary>
        public Vector2I GetCellFromWorldPosition(Vector2 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.X / 32);
            int y = Mathf.FloorToInt(worldPos.Y / 32);
            return new Vector2I(x, y);
        }

        /// <summary>
        /// 处理单元格点击
        /// </summary>
        private void HandleCellClick(Vector2I cell)
        {
            GD.Print($"HandleCellClick: ({cell.X}, {cell.Y}), MapWidth: {MapWidth}, MapHeight: {MapHeight}");

            if (cell.X < 0 || cell.X >= MapWidth || cell.Y < 0 || cell.Y >= MapHeight)
            {
                GD.Print("单元格超出边界");
                return;
            }

            GD.Print("选中单元格");
            _selectedCell = cell;
            UpdateSelectionVisual();

            var tile = _generator.GetTile(cell.X, cell.Y);
            OnCellClicked?.Invoke(cell, tile);
            OnCellSelected?.Invoke(cell, tile);

            GD.Print($"单元格点击: ({cell.X}, {cell.Y}) - 生物群系: {tile.Biome}, 海拔: {tile.Elevation:F2}, 灵气: {tile.Spirit:F2}");
        }

        /// <summary>
        /// 更新选中视觉效果
        /// </summary>
        private void UpdateSelectionVisual()
        {
            GD.Print($"UpdateSelectionVisual: ({_selectedCell.X}, {_selectedCell.Y}), sourceId: {_selectionSourceId}");
            _selectionLayer.Clear();
            if (_selectedCell.X >= 0 && _selectedCell.X < MapWidth &&
                _selectedCell.Y >=0 && _selectedCell.Y < MapHeight)
            {
                GD.Print("设置单元格高亮");
                _selectionLayer.SetCell(_selectedCell, _selectionSourceId, Vector2I.Zero);
            }
        }

        /// <summary>
        /// 获取当前选中的单元格
        /// </summary>
        public Vector2I GetSelectedCell()
        {
            return _selectedCell;
        }

        /// <summary>
        /// 设置选中单元格
        /// </summary>
        public void SetSelectedCell(Vector2I cell)
        {
            if (cell.X >= 0 && cell.X < MapWidth && cell.Y >= 0 && cell.Y < MapHeight)
            {
                _selectedCell = cell;
                UpdateSelectionVisual();
                var tile = _generator.GetTile(cell.X, cell.Y);
                OnCellSelected?.Invoke(cell, tile);
            }
        }

        /// <summary>
        /// 清除选中状态
        /// </summary>
        public void ClearSelection()
        {
            _selectedCell = new Vector2I(-1, -1);
            _selectionLayer.Clear();
        }

        /// <summary>
        /// 设置选择颜色
        /// </summary>
        public void SetSelectionColor(Color color)
        {
            _selectionColor = color;
        }
    }
}
