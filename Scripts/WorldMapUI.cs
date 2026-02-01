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
			if (_miniMap != null && MapManager != null)
			{
				// 更新小地图纹理（使用小地图的实际尺寸）
				var mapTexture = MapManager.GenerateMapOverviewTexture(_miniMap.MiniMapWidth, _miniMap.MiniMapHeight);
				_miniMap.SetMapTexture(mapTexture);
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

			// 生成并设置地图纹理（使用小地图的实际尺寸）
			var mapTexture = MapManager.GenerateMapOverviewTexture(_miniMap.MiniMapWidth, _miniMap.MiniMapHeight);
			_miniMap.SetMapTexture(mapTexture);
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
