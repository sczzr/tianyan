using Godot;
using System;

using TianYanShop.Data;
using TianYanShop.World.Config;
using TianYanShop.World.Layer;

namespace TianYanShop.World.Map
{
	public partial class WorldMapUI : Control
	{
		[Export] public RealmMapManager MapManager;

		private Label _seedLabel;
		private Label _positionLabel;
		private Label _biomeLabel;
		private Label _zoomLabel;
		private Button _regenButton;

		private WorldMapCamera _camera;
		private MiniMap _miniMap;
		private MapLayerPanel _layerPanel;

		public override void _Ready()
		{
			_seedLabel = GetNode<Label>("InfoPanel/VBoxContainer/SeedLabel");
			_positionLabel = GetNode<Label>("InfoPanel/VBoxContainer/PositionLabel");
			_biomeLabel = GetNode<Label>("InfoPanel/VBoxContainer/BiomeLabel");
			_zoomLabel = GetNode<Label>("InfoPanel/VBoxContainer/ZoomLabel");
			_regenButton = GetNode<Button>("InfoPanel/VBoxContainer/RegenButton");

			_regenButton.Pressed += _on_regenerate_button_pressed;

			CallDeferred(nameof(InitializeReferences));
		}

		private void InitializeReferences()
		{
			if (MapManager == null)
			{
				MapManager = GetTree().Root.GetNodeOrNull<RealmMapManager>("WorldMapScene/MainMap");
			}

			if (MapManager != null)
			{
				_camera = MapManager.MapCamera;
				_seedLabel.Text = $"种子: {MapManager.Generator.Seed}";

				CreateMiniMap();
				CreateLayerPanel();

				MapManager.MapGenerated += OnMapGenerated;
			}
		}

		private void CreateMiniMap()
		{
			if (_miniMap == null)
			{
				_miniMap = new MiniMap();
				_miniMap.Name = "MiniMap";
				GetParent().AddChild(_miniMap);
				
				_miniMap.SetMainCamera(_camera);
				_miniMap.SetMapBounds(MapManager.MapWidth * MapManager.TileSize, MapManager.MapHeight * MapManager.TileSize);
				
				var overviewTexture = MapManager.GenerateOverviewTexture(_miniMap.MiniMapWidth, _miniMap.MiniMapHeight);
				_miniMap.SetMapTexture(overviewTexture);
			}
		}

		private void CreateLayerPanel()
		{
			if (_layerPanel == null)
			{
				_layerPanel = new MapLayerPanel();
				_layerPanel.Name = "MapLayerPanel";
				GetParent().AddChild(_layerPanel);
				GD.Print("[WorldMapUI] 已创建 MapLayerPanel");
			}
		}

		private void OnMapGenerated()
		{
			_seedLabel.Text = $"种子: {MapManager.Generator.Seed}";
			GD.Print("[WorldMapUI] 地图已重新生成");

			if (_miniMap != null)
			{
				var overviewTexture = MapManager.GenerateOverviewTexture(_miniMap.MiniMapWidth, _miniMap.MiniMapHeight);
				_miniMap.SetMapTexture(overviewTexture);
			}
		}

		public override void _Process(double delta)
		{
			if (_camera == null || MapManager == null) return;

			Vector2 mouseScreenPos = GetViewport().GetMousePosition();
			Vector2 worldPos = _camera.ScreenToWorld(mouseScreenPos);
			Vector2I tilePos = _camera.WorldToTile(worldPos);

			_positionLabel.Text = $"位置: ({tilePos.X}, {tilePos.Y})";

			RealmTile tile = MapManager.GetTileAt(tilePos.X, tilePos.Y);

			var terrainData = RealmTransitionConfig.GetTerrainData(tile.Terrain);
			var spiritData = RealmTransitionConfig.GetSpiritData(tile.Spirit);

			_biomeLabel.Text = $"地形: {terrainData.Name}";

			if (tile.Spirit != SpiritLevel.Normal)
			{
				_biomeLabel.Text += $" [{spiritData.Name}]";
			}

			if (tile.PrimaryElement != ElementTag.None)
			{
				_biomeLabel.Text += $" <{GetElementChineseName(tile.PrimaryElement)}>";
			}

			if (tile.Law != LawTag.None)
			{
				_biomeLabel.Text += $" [{GetLawChineseName(tile.Law)}]";
			}

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

		private string GetElementChineseName(ElementTag element)
		{
			return element switch
			{
				ElementTag.Metal => "金",
				ElementTag.Wood => "木",
				ElementTag.Water => "水",
				ElementTag.Fire => "火",
				ElementTag.Earth => "土",
				ElementTag.Wind => "风",
				ElementTag.Thunder => "雷",
				ElementTag.Light => "光",
				ElementTag.Dark => "暗",
				ElementTag.Ice => "冰",
				ElementTag.Sound => "音",
				ElementTag.Crystal => "晶",
				ElementTag.Swamp => "泽",
				_ => element.ToString()
			};
		}

		private string GetLawChineseName(LawTag law)
		{
			return law switch
			{
				LawTag.Space => "空间",
				LawTag.Time => "时间",
				LawTag.LifeDeath => "生死",
				LawTag.Fate => "命运",
				LawTag.ForceField => "力场",
				LawTag.Mental => "精神",
				LawTag.Chaos => "混沌",
				LawTag.Illusion => "幻象",
				LawTag.Boundary => "边界",
				_ => law.ToString()
			};
		}
	}
}
