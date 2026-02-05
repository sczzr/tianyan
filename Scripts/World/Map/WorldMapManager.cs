using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop.World.Map
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
		[Export] public int TileSize = 64;

		[Export] public int BiomeTextureSize = 512;
		[Export] public float TextureTilingScale = 1.0f;

		// 组件引用
		private WorldMapGenerator _generator;
		private TileMapLayer _tileMapLayer;
		private WorldMapCamera _camera;
		private BiomeTextureGenerator _textureGenerator;

		// 纹理尺寸信息
		private Vector2I _textureSize;

		// 信号
		[Signal] public delegate void MapGeneratedEventHandler();

		public override void _Ready()
		{
			GlobalPosition = Vector2.Zero;

			int finalSeed = RandomSeed || Seed == -1 ? (int)Time.GetUnixTimeFromSystem() : Seed;
			_generator = new WorldMapGenerator(MapWidth, MapHeight, finalSeed);

			_textureGenerator = new BiomeTextureGenerator(BiomeTextureSize, TileSize, finalSeed);

			GD.Print($"世界地图管理器初始化 - 尺寸: {MapWidth}x{MapHeight}, 种子: {finalSeed}, 纹理尺寸: {BiomeTextureSize}x{BiomeTextureSize}");

			SetupTileMap();
			SetupCamera();

			if (GenerateOnReady)
			{
				GenerateAndRender();
			}
		}

		/// <summary>
		/// 设置纹理网格缩放（控制纹理的重复频率）
		/// </summary>
		public void SetTextureTilingScale(float scale)
		{
			TextureTilingScale = Mathf.Clamp(scale, 0.1f, 10.0f);
			GD.Print($"纹理网格缩放已更新: {TextureTilingScale:F2}");
			RenderMap();
		}

		/// <summary>
		/// 获取纹理生成器
		/// </summary>
		public BiomeTextureGenerator GetTextureGenerator()
		{
			return _textureGenerator;
		}

		/// <summary>
		/// 预生成所有生物群系纹理
		/// </summary>
		public void PreGenerateTextures()
		{
			_textureGenerator?.PreGenerateAll();
		}

		/// <summary>
		/// 设置 TileMap
		/// </summary>
		private void SetupTileMap()
		{
			_tileMapLayer = new TileMapLayer();
			_tileMapLayer.Name = "WorldMapTileLayer";
			_tileMapLayer.GlobalPosition = Vector2.Zero;
			AddChild(_tileMapLayer);

			var tileSet = new TileSet();
			tileSet.TileSize = new Vector2I(TileSize, TileSize);

			_textureSize = new Vector2I(BiomeTextureSize, BiomeTextureSize);
			GD.Print($"使用程序化生成纹理: {_textureSize.X}x{_textureSize.Y}");

			int atlasId = 0;
			foreach (var kvp in WorldMapGenerator.Biomes)
			{
				var biomeData = kvp.Key;
				var texture = _textureGenerator.GetBiomeTexture(biomeData);

				if (texture == null)
				{
					GD.PrintErr($"无法生成生物群系纹理: {biomeData}");
					continue;
				}

				var atlasSource = new TileSetAtlasSource();
				atlasSource.Texture = texture;
				atlasSource.TextureRegionSize = new Vector2I(TileSize, TileSize);

				int textureTileCountX = BiomeTextureSize / TileSize;
				int textureTileCountY = BiomeTextureSize / TileSize;

				for (int texX = 0; texX < textureTileCountX; texX++)
				{
					for (int texY = 0; texY < textureTileCountY; texY++)
					{
						Vector2I tileCoords = new Vector2I(texX, texY);
						atlasSource.CreateTile(tileCoords);
					}
				}

				int sourceId = tileSet.AddSource(atlasSource, atlasId);

				GD.Print($"注册生物群系: {biomeData} -> SourceID: {sourceId}, 瓦片网格: {textureTileCountX}x{textureTileCountY}");
				atlasId++;
			}

			_tileMapLayer.TileSet = tileSet;
		}

		/// <summary>
		/// 设置相机
		/// </summary>
		private void SetupCamera()
		{
			// 先尝试从父节点获取相机（相机是MainMap的兄弟节点）
			var parentNode = GetParent();
			if (parentNode != null)
			{
				_camera = parentNode.GetNodeOrNull<WorldMapCamera>("WorldMapCamera");
			}

			// 如果还没找到，再尝试从MainMap内部查找
			if (_camera == null)
			{
				_camera = GetNodeOrNull<WorldMapCamera>("WorldMapCamera");
			}

			if (_camera == null)
			{
				GD.PrintErr("警告：未找到 WorldMapCamera，将创建新相机");
				_camera = new WorldMapCamera();
				_camera.Name = "WorldMapCamera";
				AddChild(_camera);
			}

			// 设置地图边界
			_camera.SetMapBounds(MapWidth * TileSize, MapHeight * TileSize);

			GD.Print($"WorldMapManager: 找到相机 {_camera.Name}, 路径: {_camera.GetPath()}");
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

			_tileMapLayer.Clear();

			int textureTileCountX = _textureSize.X / TileSize;
			int textureTileCountY = _textureSize.Y / TileSize;

			var rand = new Random(Seed);

			var blendTiles = new Dictionary<Vector2I, (BiomeType secondary, float factor)>();

			float effectiveScale = TextureTilingScale;
			int textureGridWidth = textureTileCountX;
			int textureGridHeight = textureTileCountY;

			for (int x = 0; x < MapWidth; x++)
			{
				for (int y = 0; y < MapHeight; y++)
				{
					var tile = _generator.MapTiles[x, y];

					(BiomeType renderBiome, Vector2I atlasCoords, bool isBlend) = DetermineRenderTile(
						tile, x, y, textureGridWidth, textureGridHeight, effectiveScale, rand);

					int atlasId = (int)renderBiome;
					Vector2I cellPos = new Vector2I(x, y);

					_tileMapLayer.SetCell(cellPos, atlasId, atlasCoords);

					if (isBlend && tile.BlendFactor > 0.1f && tile.SecondaryBiome != tile.Biome)
					{
						blendTiles[cellPos] = (tile.SecondaryBiome, tile.BlendFactor);
					}
				}
			}

			ApplyBlendEffects(blendTiles, textureGridWidth, textureGridHeight, effectiveScale, rand);

			GD.Print($"地图渲染完成: {MapWidth}x{MapHeight} 瓦片, 混合区域: {blendTiles.Count}");
		}

		/// <summary>
		/// 应用混合效果 - 在边界区域添加次群系的视觉元素
		/// </summary>
		private void ApplyBlendEffects(Dictionary<Vector2I, (BiomeType secondary, float factor)> blendTiles,
			int textureTileCountX, int textureTileCountY, float scale, Random rand)
		{
			int textureGridWidth = textureTileCountX;
			int textureGridHeight = textureTileCountY;

			foreach (var kvp in blendTiles)
			{
				Vector2I pos = kvp.Key;
				var (secondaryBiome, factor) = kvp.Value;

				float noiseValue = GetSpatialNoise(pos.X, pos.Y, Seed);
				float threshold = 1f - (factor * 0.7f);

				if (noiseValue > threshold)
				{
					int secondaryAtlasId = (int)secondaryBiome;

					Vector2I atlasCoords = CalculateAtlasCoords(pos.X, pos.Y, textureGridWidth, textureGridHeight, scale);

					_tileMapLayer.SetCell(pos, secondaryAtlasId, atlasCoords);
				}
			}
		}

		/// <summary>
		/// 获取空间噪声值（确定性随机）
		/// </summary>
		private float GetSpatialNoise(int x, int y, int seed)
		{
			int hash = x * 374761 + y * 668265 + seed;
			hash = (hash ^ (hash >> 13)) * 1274126177;
			return ((hash & 0x7fffffff) / (float)int.MaxValue);
		}

		/// <summary>
		/// 根据世界坐标计算图集坐标（世界空间映射）
		/// atlasX = (x / TextureTilingScale) % TextureGridWidth
		/// </summary>
		private Vector2I CalculateAtlasCoords(int worldX, int worldY, int textureGridWidth, int textureGridHeight, float scale)
		{
			int scaledX = (int)(worldX / scale);
			int scaledY = (int)(worldY / scale);

			int atlasX = scaledX % textureGridWidth;
			int atlasY = scaledY % textureGridHeight;

			if (atlasX < 0) atlasX += textureGridWidth;
			if (atlasY < 0) atlasY += textureGridHeight;

			return new Vector2I(atlasX, atlasY);
		}

		/// <summary>
		/// 确定渲染瓦片的生物群系和纹理坐标（处理过渡效果）
		/// </summary>
		private (BiomeType biome, Vector2I coords, bool isBlend) DetermineRenderTile(MapTile tile, int x, int y,
			int textureTileCountX, int textureTileCountY, float scale, Random rand)
		{
			Vector2I atlasCoords = CalculateAtlasCoords(x, y, textureTileCountX, textureTileCountY, scale);

			if (tile.BlendFactor <= 0.01f || tile.Biome == tile.SecondaryBiome)
			{
				return (tile.Biome, atlasCoords, false);
			}

			float noiseValue = GetBlendNoise(x, y, tile.BlendFactor);
			BiomeType selectedBiome = noiseValue < tile.BlendFactor ? tile.SecondaryBiome : tile.Biome;

			atlasCoords = CalculateAtlasCoords(x, y, textureTileCountX, textureTileCountY, scale);
			atlasCoords = GetVariedTileCoords(atlasCoords.X, atlasCoords.Y, textureTileCountX, textureTileCountY,
				selectedBiome, x, y, rand);

			return (selectedBiome, atlasCoords, true);
		}

		/// <summary>
		/// 获取混合噪声值（用于创造自然的混合边界）
		/// </summary>
		private float GetBlendNoise(int x, int y, float blendFactor)
		{
			// 使用简单的哈希函数生成空间噪声
			int hash = x * 374761 + y * 668265 + Seed;
			hash = (hash ^ (hash >> 13)) * 1274126177;
			float noise = (hash & 0x7fffffff) / (float)int.MaxValue;

			// 添加基于位置的低频变化
			float lowFreqNoise = Mathf.Sin(x * 0.1f + y * 0.07f + Seed * 0.01f) * 0.5f + 0.5f;

			// 混合两种噪声
			return Mathf.Lerp(noise, lowFreqNoise, 0.3f);
		}

		/// <summary>
		/// 获取变化的纹理坐标（增加视觉多样性）
		/// </summary>
		private Vector2I GetVariedTileCoords(int baseX, int baseY, int countX, int countY,
			BiomeType biome, int worldX, int worldY, Random rand)
		{
			// 基于群系和位置计算偏移
			int offsetX = 0;
			int offsetY = 0;

			// 使用群系特定偏移（让每个群系有自己的纹理模式）
			switch (biome)
			{
				case BiomeType.BorealForest:
				case BiomeType.TemperateForest:
				case BiomeType.TropicalRainforest:
					// 森林使用更多垂直变化
					offsetY = (worldY % 3);
					break;

				case BiomeType.Desert:
				case BiomeType.ExtremeDesert:
				case BiomeType.AridShrubland:
					// 沙漠使用更多水平变化
					offsetX = (worldX % 3);
					break;

				case BiomeType.Tundra:
				case BiomeType.IceSheet:
					// 冰原使用对角线变化
					offsetX = ((worldX + worldY) % 2);
					offsetY = ((worldX + worldY) % 2);
					break;

				default:
					// 其他使用随机变化
					offsetX = ((worldX * 7 + worldY * 13) % 2);
					offsetY = ((worldX * 11 + worldY * 7) % 2);
					break;
			}

			// 计算最终坐标
			int finalX = ((baseX + offsetX) % countX + countX) % countX;
			int finalY = ((baseY + offsetY) % countY + countY) % countY;

			return new Vector2I(finalX, finalY);
		}

		/// <summary>
		/// 重新生成地图 (使用新种子)
		/// </summary>
		public void RegenerateMap()
		{
			Seed = (int)Time.GetUnixTimeFromSystem();
			_generator.Regenerate(Seed);
			_textureGenerator.RegenerateAll(Seed);
			RenderMap();

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
