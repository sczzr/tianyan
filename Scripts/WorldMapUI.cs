using Godot;
using System;

namespace TianYanShop
{
	/// <summary>
	/// 世界地图 UI 控制器
	/// </summary>
	public partial class WorldMapUI : Control
	{
		[Export] public WorldMapManager MapManager;

		private Label _seedLabel;
		private Label _positionLabel;
		private Label _biomeLabel;
		private Label _zoomLabel;
		private Button _regenButton;

		private WorldMapCamera _camera;
		private MiniMap _miniMap;

		// 小地图渲染相关
		private SubViewportContainer? _miniMapViewPortContainer;
		private SubViewport? _miniMapViewPort;
		private Camera2D? _miniMapCamera;
		private TileMapLayer? _miniMapTileMapLayer;

		public override void _Ready()
		{
			// 获取UI组件引用
			_seedLabel = GetNode<Label>("InfoPanel/VBoxContainer/SeedLabel");
			_positionLabel = GetNode<Label>("InfoPanel/VBoxContainer/PositionLabel");
			_biomeLabel = GetNode<Label>("InfoPanel/VBoxContainer/BiomeLabel");
			_zoomLabel = GetNode<Label>("InfoPanel/VBoxContainer/ZoomLabel");
			_regenButton = GetNode<Button>("InfoPanel/VBoxContainer/RegenButton");

			// 连接信号
			_regenButton.Pressed += _on_regenerate_button_pressed;

			// 延迟获取MapManager和Camera
			CallDeferred(nameof(InitializeReferences));
		}

		private void InitializeReferences()
		{
			if (MapManager == null)
			{
				MapManager = GetTree().Root.GetNodeOrNull<WorldMapManager>("WorldMapScene/MainMap");
			}

			if (MapManager != null)
			{
				_camera = MapManager.MapCamera;
				_seedLabel.Text = $"种子: {MapManager.Generator.Seed}";

				// 创建并初始化小地图
				CreateMiniMap();

				// 连接地图生成信号，当地图重新生成时更新小地图
				MapManager.MapGenerated += OnMapGenerated;
			}
		}

		/// <summary>
		/// 地图重新生成时的回调
		/// </summary>
		private void OnMapGenerated()
		{
			if (_miniMap != null && MapManager != null && _miniMapViewPort != null)
			{
				// 重新创建小地图的 TileMapLayer
				RecreateMiniMapTileMap();
			}
		}

		/// <summary>
		/// 创建小地图
		/// </summary>
		private void CreateMiniMap()
		{
			if (_miniMap != null) return;

			// 从场景树中获取 MiniMap 节点
			_miniMap = GetNodeOrNull<MiniMap>("MiniMap");
			if (_miniMap == null)
			{
				GD.PrintErr("无法找到 MiniMap 节点");
				return;
			}

			// 设置主相机
			_miniMap.SetMainCamera(_camera);

			// 设置地图边界
			_miniMap.SetMapBounds(MapManager.MapWidth * MapManager.TileSize, MapManager.MapHeight * MapManager.TileSize);

			// 创建小地图视口容器
			CreateMiniMapViewPort();
		}

		/// <summary>
		/// 创建小地图视口用于实时渲染
		/// </summary>
		private void CreateMiniMapViewPort()
		{
			// 创建 SubViewportContainer
			_miniMapViewPortContainer = new SubViewportContainer();
			_miniMapViewPortContainer.Name = "MiniMapViewPortContainer";
			// 隐藏，不直接显示，只作为纹理源
			_miniMapViewPortContainer.Visible = false;
			AddChild(_miniMapViewPortContainer);

			// 创建 SubViewport
			_miniMapViewPort = new SubViewport();
			_miniMapViewPort.Name = "MiniMapViewPort";
			_miniMapViewPort.Size = new Vector2I(_miniMap.MiniMapWidth, _miniMap.MiniMapHeight);
			_miniMapViewPort.RenderTargetUpdateMode = SubViewport.UpdateMode.Always; // 始终更新
			_miniMapViewPort.TransparentBg = false; // 不透明背景
			_miniMapViewPortContainer.AddChild(_miniMapViewPort);

			// 创建小地图专用相机
			_miniMapCamera = new Camera2D();
			_miniMapCamera.Name = "MiniMapCamera";
			_miniMapViewPort.AddChild(_miniMapCamera);

			// 计算地图实际尺寸
			float mapPixelWidth = MapManager.MapWidth * MapManager.TileSize;
			float mapPixelHeight = MapManager.MapHeight * MapManager.TileSize;

			// 设置相机缩放以显示整个地图
			float zoomX = (float)_miniMap.MiniMapWidth / mapPixelWidth;
			float zoomY = (float)_miniMap.MiniMapHeight / mapPixelHeight;
			float zoom = Mathf.Min(zoomX, zoomY); // 使用较小的缩放比例以保持宽高比
			_miniMapCamera.Zoom = new Vector2(zoom, zoom);

			// 设置相机位置到地图中心
			_miniMapCamera.GlobalPosition = new Vector2(mapPixelWidth / 2, mapPixelHeight / 2);
			_miniMapCamera.PositionSmoothingEnabled = false;

			// 创建 TileMapLayer 副本
			RecreateMiniMapTileMap();

			// 创建 ViewportTexture 并设置到 MiniMap
			var viewportTexture = _miniMapViewPort.GetTexture();
			_miniMap.SetMapTexture(viewportTexture);
		}

		/// <summary>
		/// 重新创建小地图的 TileMapLayer
		/// </summary>
		private void RecreateMiniMapTileMap()
		{
			// 如果已有旧的小地图 TileMap，先删除
			if (_miniMapTileMapLayer != null && _miniMapTileMapLayer.IsInsideTree())
			{
				_miniMapTileMapLayer.QueueFree();
			}

			// 获取原始的 TileMapLayer
			var originalTileMap = GetTree().Root.GetNodeOrNull<TileMapLayer>("WorldMapScene/MainMap/WorldMapTileLayer");
			if (originalTileMap == null)
			{
				GD.PrintErr("无法找到原始的 TileMapLayer");
				return;
			}

			// 创建新的 TileMapLayer 作为副本
			_miniMapTileMapLayer = new TileMapLayer();
			_miniMapTileMapLayer.Name = "MiniMapTileLayer";
			_miniMapTileMapLayer.TileSet = originalTileMap.TileSet;
			_miniMapViewPort.AddChild(_miniMapTileMapLayer);

			// 复制所有瓦片数据
			CopyTileMapData(originalTileMap, _miniMapTileMapLayer);
		}

		/// <summary>
		/// 复制 TileMap 数据
		/// </summary>
		private void CopyTileMapData(TileMapLayer source, TileMapLayer destination)
		{
			// 获取源 TileMap 的所有已使用的单元格
			var usedCells = source.GetUsedCells();

			foreach (var cell in usedCells)
			{
				// 获取源的图集ID、图集坐标和替代图集坐标
				var sourceId = source.GetCellSourceId(cell);
				var atlasCoords = source.GetCellAtlasCoords(cell);
				var alternativeTile = source.GetCellAlternativeTile(cell);

				// 设置到目标 TileMap
				destination.SetCell(cell, sourceId, atlasCoords, alternativeTile);
			}
		}

		public override void _Process(double delta)
		{
			if (_camera == null || MapManager == null) return;

			// 获取鼠标位置并转换为世界坐标
			Vector2 mouseScreenPos = GetViewport().GetMousePosition();
			Vector2 worldPos = _camera.ScreenToWorld(mouseScreenPos);
			Vector2I tilePos = _camera.WorldToTile(worldPos);

			// 更新位置显示
			_positionLabel.Text = $"位置: ({tilePos.X}, {tilePos.Y})";

			// 更新生物群系显示
			MapTile tile = MapManager.GetTileAt(tilePos.X, tilePos.Y);
			if (WorldMapGenerator.Biomes.TryGetValue(tile.Biome, out var biomeData))
			{
				_biomeLabel.Text = $"生物群系: {biomeData.Name}";
			}

			// 更新缩放显示
			_zoomLabel.Text = $"缩放: {_camera.Zoom.X:F2}x";
		}

		private void _on_regenerate_button_pressed()
		{
			if (MapManager != null)
			{
				MapManager.RegenerateMap();
				_seedLabel.Text = $"种子: {MapManager.Generator.Seed}";
			}
		}
	}
}
