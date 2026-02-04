using Godot;
using System;

namespace TianYanShop
{
    public partial class RealmMapManager : Node2D
    {
        [Export] public int MapWidth = 512;
        [Export] public int MapHeight = 512;
        [Export] public int Seed = -1;
        [Export] public bool RandomSeed = true;
        [Export] public bool GenerateOnReady = true;
        [Export] public int TileSize = 64;

        private RealmMapGenerator _generator;
        private TileMapLayer _tileMapLayer;
        private WorldMapCamera _camera;
        private Vector2I _textureSize;

        [Signal] public delegate void MapGeneratedEventHandler();

        public override void _Ready()
        {
            GlobalPosition = Vector2.Zero;

            int finalSeed = RandomSeed || Seed == -1 ? (int)Time.GetUnixTimeFromSystem() : Seed;
            _generator = new RealmMapGenerator(MapWidth, MapHeight, finalSeed);

            GD.Print($"灵域地图管理器初始化 - 尺寸: {MapWidth}x{MapHeight}, 种子: {finalSeed}");

            SetupTileMap();
            SetupCamera();

            if (GenerateOnReady)
            {
                GenerateAndRender();
            }
        }

        private void SetupTileMap()
        {
            _tileMapLayer = new TileMapLayer();
            _tileMapLayer.Name = "RealmTileLayer";
            _tileMapLayer.GlobalPosition = Vector2.Zero;
            AddChild(_tileMapLayer);

            var tileSet = new TileSet();
            tileSet.TileSize = new Vector2I(TileSize, TileSize);

            foreach (var kvp in RealmTransitionConfig.Realms)
            {
                var realmData = kvp.Value;
                var texture = GD.Load<Texture2D>(realmData.TexturePath);

                if (texture == null)
                {
                    GD.PrintErr($"无法加载纹理: {realmData.TexturePath}");
                    continue;
                }

                if (_textureSize == Vector2I.Zero)
                {
                    _textureSize = new Vector2I(texture.GetWidth(), texture.GetHeight());
                    GD.Print($"纹理尺寸: {_textureSize.X}x{_textureSize.Y}");
                }

                var atlasSource = new TileSetAtlasSource();
                atlasSource.Texture = texture;
                atlasSource.TextureRegionSize = new Vector2I(TileSize, TileSize);

                int textureTileCountX = texture.GetWidth() / TileSize;
                int textureTileCountY = texture.GetHeight() / TileSize;

                for (int texX = 0; texX < textureTileCountX; texX++)
                {
                    for (int texY = 0; texY < textureTileCountY; texY++)
                    {
                        Vector2I tileCoords = new Vector2I(texX, texY);
                        atlasSource.CreateTile(tileCoords);
                    }
                }

                int sourceId = tileSet.AddSource(atlasSource, (int)kvp.Key);
                GD.Print($"注册地形: {realmData.Name} -> ID: {(int)kvp.Key}");
            }

            _tileMapLayer.TileSet = tileSet;
        }

        private void SetupCamera()
        {
            var parentNode = GetParent();
            if (parentNode != null)
            {
                _camera = parentNode.GetNodeOrNull<WorldMapCamera>("WorldMapCamera");
            }

            if (_camera == null)
            {
                _camera = GetNodeOrNull<WorldMapCamera>("WorldMapCamera");
            }

            if (_camera == null)
            {
                GD.PrintErr("警告：未找到 WorldMapCamera");
                _camera = new WorldMapCamera();
                _camera.Name = "WorldMapCamera";
                AddChild(_camera);
            }

            _camera.SetMapBounds(MapWidth * TileSize, MapHeight * TileSize);
            GD.Print($"RealmMapManager: 找到相机 {_camera.Name}");
        }

        public void GenerateAndRender()
        {
            GD.Print("开始生成灵域地图...");

            _generator.GenerateMap();
            RenderMap();

            _camera.AlignTopLeftToZero();
            EmitSignal(SignalName.MapGenerated);

            GD.Print("灵域地图生成完成！");
        }

        private void RenderMap()
        {
            GD.Print("开始渲染地图...");

            _tileMapLayer.Clear();

            int textureTileCountX = _textureSize.X / TileSize;
            int textureTileCountY = _textureSize.Y / TileSize;

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    var tile = _generator.MapTiles[x, y];
                    int atlasId = (int)tile.Realm;

                    int baseTileX = x % textureTileCountX;
                    int baseTileY = y % textureTileCountY;

                    _tileMapLayer.SetCell(new Vector2I(x, y), atlasId, new Vector2I(baseTileX, baseTileY));
                }
            }

            GD.Print($"地图渲染完成: {MapWidth}x{MapHeight}");
        }

        public void RegenerateMap()
        {
            Seed = (int)Time.GetUnixTimeFromSystem();
            _generator.Regenerate(Seed);
            RenderMap();
            EmitSignal(SignalName.MapGenerated);
            GD.Print($"地图重新生成 - 新种子: {Seed}");
        }

        public RealmTile GetTileAt(int x, int y)
        {
            return _generator.GetTile(x, y);
        }

        public RealmTile GetTileAtWorldPosition(Vector2 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.X / TileSize);
            int y = Mathf.FloorToInt(worldPos.Y / TileSize);
            return _generator.GetTile(x, y);
        }

        public RealmMapGenerator Generator => _generator;
        public WorldMapCamera MapCamera => _camera;

        public ImageTexture GenerateOverviewTexture(int width = 400, int height = 400)
        {
            var image = Image.CreateEmpty(MapWidth, MapHeight, false, Image.Format.Rgba8);

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    var tile = _generator.MapTiles[x, y];

                    Color color = RealmTransitionConfig.GetCombinedColor(tile.Terrain, tile.Spirit);

                    if (tile.PrimaryElement != ElementTag.None)
                    {
                        var elementColors = RealmTransitionConfig.ElementColors;
                        if (elementColors.TryGetValue(tile.PrimaryElement, out var elementColor))
                        {
                            color = new Color(
                                (color.R + elementColor.R) / 2f,
                                (color.G + elementColor.G) / 2f,
                                (color.B + elementColor.B) / 2f);
                        }
                    }

                    if (tile.Law != LawTag.None)
                    {
                        var lawColors = RealmTransitionConfig.LawColors;
                        if (lawColors.TryGetValue(tile.Law, out var lawColor))
                        {
                            color = new Color(
                                (color.R + lawColor.R * 0.5f),
                                (color.G + lawColor.G * 0.5f),
                                (color.B + lawColor.B * 0.5f));
                        }
                    }

                    image.SetPixel(x, y, color);
                }
            }

            image.Resize(width, height);
            return ImageTexture.CreateFromImage(image);
        }
    }
}
