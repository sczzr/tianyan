using Godot;
using System;

namespace TianYanShop
{
	/// <summary>
	/// 世界地图管理器 - 整合生成器和渲染器
	/// </summary>
	public partial class WorldMapManager : Node2D
	{
		[Export] public int MapWidth = 512;
		[Export] public int MapHeight = 512;
		[Export] public int Seed = -1;
		[Export] public bool RandomSeed = true;
		[Export] public bool GenerateOnReady = true;
		[Export] public int TileSize = 64;  // 单元格像素大小

		// 组件引用
		private WorldMapGenerator _generator;
		private TileMapLayer _tileMapLayer;
		private WorldMapCamera _camera;

		// 纹理尺寸信息 (从实际加载的纹理中获取)
		private Vector2I _textureSize;

		// 信号
		[Signal] public delegate void MapGeneratedEventHandler();

		public override void _Ready()
		{
			// 确保 WorldMapManager 的位置为 (0, 0)，地图从全局坐标左上角开始
			GlobalPosition = Vector2.Zero;

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
			_tileMapLayer.GlobalPosition = Vector2.Zero; // 确保 TileMap 从 (0,0) 开始
			AddChild(_tileMapLayer);

			// 创建 TileSet
			var tileSet = new TileSet();
			tileSet.TileSize = new Vector2I(TileSize, TileSize);

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

				// 存储纹理尺寸（只存储第一个有效纹理的尺寸）
				if (_textureSize == Vector2I.Zero)
				{
					_textureSize = new Vector2I(texture.GetWidth(), texture.GetHeight());
					GD.Print($"纹理尺寸: {_textureSize.X}x{_textureSize.Y}");
				}

				var atlasSource = new TileSetAtlasSource();
				atlasSource.Texture = texture;

				// 设置纹理区域大小为单元格大小
				atlasSource.TextureRegionSize = new Vector2I(TileSize, TileSize);

				// 计算纹理可以分成多少个瓦片
				int textureTileCountX = texture.GetWidth() / TileSize;
				int textureTileCountCountY = texture.GetHeight() / TileSize;

				// 为每个纹理位置创建一个瓦片
				for (int texX = 0; texX < textureTileCountX; texX++)
				{
					for (int texY = 0; texY < textureTileCountCountY; texY++)
					{
						Vector2I tileCoords = new Vector2I(texX, texY);
						atlasSource.CreateTile(tileCoords);
					}
				}

				int sourceId = tileSet.AddSource(atlasSource, (int)kvp.Key);

				GD.Print($"注册生物群系: {biomeData.Name} -> ID: {(int)kvp.Key}, SourceID: {sourceId}, 瓦片网格: {textureTileCountX}x{textureTileCountCountY}");
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
			_camera.SetMapBounds(MapWidth * TileSize, MapHeight * TileSize);
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

			// 将世界坐标的左上角 (0,0) 对齐到屏幕左上角
			_camera.AlignTopLeftToZero();

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

			// 计算纹理中包含多少个瓦片 (从实际纹理尺寸获取)
			int textureTileCountX = _textureSize.X / TileSize;
			int textureTileCountY = _textureSize.Y / TileSize;

			for (int x = 0; x < MapWidth; x++)
			{
				for (int y = 0; y < MapHeight; y++)
				{
					var tile = _generator.MapTiles[x, y];
					int atlasId = (int)tile.Biome;

					// 使用取模运算实现纹理平铺效果
					// 每个单元格根据其在地图中的位置映射到纹理的对应坐标
					int textureTileX = x % textureTileCountX;
					int textureTileY = y % textureTileCountY;
					Vector2I atlasCoords = new Vector2I(textureTileX, textureTileY);

					_tileMapLayer.SetCell(new Vector2I(x, y), atlasId, atlasCoords);
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

			// 发出信号通知地图已重新生成
			EmitSignal(SignalName.MapGenerated);

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
			int x = Mathf.FloorToInt(worldPos.X / TileSize);
			int y = Mathf.FloorToInt(worldPos.Y / TileSize);
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

		/// <summary>
		/// 生成整个地图的纹理用于小地图显示
		/// </summary>
		public ImageTexture GenerateMapOverviewTexture(int miniMapWidth = 400, int miniMapHeight = 400)
		{
			// 创建一个小图像来代表整个地图
			int overviewWidth = MapWidth;
			int overviewHeight = MapHeight;
			var image = Image.CreateEmpty(overviewWidth, overviewHeight, false, Image.Format.Rgba8);

			// 为每个瓦片生成一个颜色代表其生物群系
			for (int x = 0; x < MapWidth; x++)
			{
				for (int y = 0; y < MapHeight; y++)
				{
					var tile = _generator.MapTiles[x, y];
					Color biomeColor = GetBiomeColor(tile.Biome);
					image.SetPixel(x, y, biomeColor);
				}
			}

			// 缩放到小地图的尺寸
			image.Resize(miniMapWidth, miniMapHeight);
			var texture = ImageTexture.CreateFromImage(image);
			return texture;
		}

		/// <summary>
		/// 根据生物群系类型获取代表颜色
		/// </summary>
		private Color GetBiomeColor(BiomeType biome)
		{
			switch (biome)
			{
				case BiomeType.Ocean:
					return new Color(0.2f, 0.3f, 0.8f); // 蓝色
				case BiomeType.IceSheetOcean:
					return new Color(0.7f, 0.85f, 0.95f); // 冰架海洋
				case BiomeType.IceSheet:
					return new Color(0.9f, 0.95f, 1.0f); // 冰架
				case BiomeType.Tundra:
					return new Color(0.6f, 0.7f, 0.75f); // 苔原
				case BiomeType.ColdBog:
					return new Color(0.5f, 0.6f, 0.65f); // 寒冷沼泽
				case BiomeType.BorealForest:
					return new Color(0.2f, 0.4f, 0.3f); // 北方针叶林
				case BiomeType.TemperateForest:
					return new Color(0.2f, 0.6f, 0.2f); // 温带森林
				case BiomeType.TemperateSwamp:
					return new Color(0.3f, 0.5f, 0.3f); // 温带沼泽
				case BiomeType.AridShrubland:
					return new Color(0.7f, 0.6f, 0.3f); // 干旱灌木地
				case BiomeType.Desert:
					return new Color(0.9f, 0.8f, 0.5f); // 沙漠
				case BiomeType.ExtremeDesert:
					return new Color(1.0f, 0.9f, 0.6f); // 极端沙漠
				case BiomeType.TropicalRainforest:
					return new Color(0.1f, 0.5f, 0.1f); // 热带雨林
				case BiomeType.TropicalSwamp:
					return new Color(0.2f, 0.4f, 0.2f); // 热带沼泽
				default:
					return Colors.Gray;
			}
		}
	}
}
