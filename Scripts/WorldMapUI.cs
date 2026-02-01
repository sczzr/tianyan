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
                MapManager = GetTree().Root.GetNodeOrNull<WorldMapManager>("WorldMapScene/WorldMapManager");
            }

            if (MapManager != null)
            {
                _camera = MapManager.MapCamera;
                _seedLabel.Text = $"种子: {MapManager.Generator.Seed}";
            }
        }

        public override void _Process(double delta)
        {
            if (_camera == null || MapManager == null) return;

            // 更新位置显示
            Vector2I tilePos = _camera.WorldToTile(_camera.GlobalPosition);
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
