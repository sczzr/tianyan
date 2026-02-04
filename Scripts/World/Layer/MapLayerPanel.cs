using Godot;
using System;

namespace TianYanShop.World.Layer
{
    public partial class MapLayerPanel : Control
    {
        private VBoxContainer _mainContainer;
        private CheckButton _spiritToggle;
        private CheckButton _elementToggle;
        private CheckButton _lawToggle;
        private HSlider _spiritOpacitySlider;
        private HSlider _elementOpacitySlider;
        private HSlider _lawOpacitySlider;
        private Label _spiritOpacityLabel;
        private Label _elementOpacityLabel;
        private Label _lawOpacityLabel;

        private MapLayerManager _layerManager;
        private bool _isInitialized = false;

        public override void _Ready()
        {
            SetupUI();
            FindLayerManager();
        }

        private void SetupUI()
        {
            _mainContainer = new VBoxContainer();
            _mainContainer.Name = "LayerPanel";
            _mainContainer.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            _mainContainer.CustomMinimumSize = new Vector2(200, 0);
            AddChild(_mainContainer);

            var titleLabel = new Label();
            titleLabel.Text = "地图图层";
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _mainContainer.AddChild(titleLabel);

            AddLayerToggle("灵气浓度", out _spiritToggle, out _spiritOpacitySlider, out _spiritOpacityLabel);
            AddLayerToggle("元素属性", out _elementToggle, out _elementOpacitySlider, out _elementOpacityLabel);
            AddLayerToggle("法则属性", out _lawToggle, out _lawOpacitySlider, out _lawOpacityLabel);

            _spiritToggle.Toggled += OnSpiritToggle;
            _elementToggle.Toggled += OnElementToggle;
            _lawToggle.Toggled += OnLawToggle;
            _spiritOpacitySlider.ValueChanged += (value) => OnSpiritOpacityChanged(value);
            _elementOpacitySlider.ValueChanged += (value) => OnElementOpacityChanged(value);
            _lawOpacitySlider.ValueChanged += (value) => OnLawOpacityChanged(value);
        }

        private void AddLayerToggle(string layerName, out CheckButton toggle, out HSlider opacitySlider, out Label opacityLabel)
        {
            var container = new VBoxContainer();
            _mainContainer.AddChild(container);

            var header = new HBoxContainer();
            container.AddChild(header);

            toggle = new CheckButton();
            toggle.Text = layerName;
            toggle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            header.AddChild(toggle);

            opacityLabel = new Label();
            opacityLabel.Text = "100%";
            opacityLabel.CustomMinimumSize = new Vector2(50, 0);
            header.AddChild(opacityLabel);

            opacitySlider = new HSlider();
            opacitySlider.MinValue = 0;
            opacitySlider.MaxValue = 100;
            opacitySlider.Value = 50;
            opacitySlider.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            container.AddChild(opacitySlider);
        }

        private void FindLayerManager()
        {
            CallDeferred(nameof(FindLayerManagerDeferred));
        }

        private void FindLayerManagerDeferred()
        {
            _layerManager = GetTree().Root.GetNodeOrNull<MapLayerManager>("WorldMapScene/MainMap/MapLayerManager");

            if (_layerManager == null)
            {
                _layerManager = GetTree().Root.GetNodeOrNull<MapLayerManager>("MainMap/MapLayerManager");
            }

            if (_layerManager == null)
            {
                _layerManager = GetNodeOrNull<MapLayerManager>("../../MapLayerManager");
            }

            if (_layerManager != null)
            {
                InitializeFromManager();
            }
            else
            {
                GD.PrintErr("[MapLayerPanel] 未找到 MapLayerManager");
            }
        }

        private void InitializeFromManager()
        {
            _isInitialized = true;

            _spiritToggle.ButtonPressed = _layerManager.IsLayerVisible(MapLayerType.Spirit);
            _elementToggle.ButtonPressed = _layerManager.IsLayerVisible(MapLayerType.Element);
            _lawToggle.ButtonPressed = _layerManager.IsLayerVisible(MapLayerType.Law);

            _spiritOpacitySlider.Value = _layerManager.GetLayerOpacity(MapLayerType.Spirit) * 100;
            _elementOpacitySlider.Value = _layerManager.GetLayerOpacity(MapLayerType.Element) * 100;
            _lawOpacitySlider.Value = _layerManager.GetLayerOpacity(MapLayerType.Law) * 100;

            UpdateOpacityLabels();
        }

        private void OnSpiritToggle(bool pressed)
        {
            if (_layerManager != null)
            {
                _layerManager.SetLayerVisibility(MapLayerType.Spirit, pressed);
            }
        }

        private void OnElementToggle(bool pressed)
        {
            if (_layerManager != null)
            {
                _layerManager.SetLayerVisibility(MapLayerType.Element, pressed);
            }
        }

        private void OnLawToggle(bool pressed)
        {
            if (_layerManager != null)
            {
                _layerManager.SetLayerVisibility(MapLayerType.Law, pressed);
            }
        }

        private void OnSpiritOpacityChanged(double value)
        {
            if (_layerManager != null)
            {
                _layerManager.SetLayerOpacity(MapLayerType.Spirit, (float)value / 100);
            }
            _spiritOpacityLabel.Text = $"{value:F0}%";
        }

        private void OnElementOpacityChanged(double value)
        {
            if (_layerManager != null)
            {
                _layerManager.SetLayerOpacity(MapLayerType.Element, (float)value / 100);
            }
            _elementOpacityLabel.Text = $"{value:F0}%";
        }

        private void OnLawOpacityChanged(double value)
        {
            if (_layerManager != null)
            {
                _layerManager.SetLayerOpacity(MapLayerType.Law, (float)value / 100);
            }
            _lawOpacityLabel.Text = $"{value:F0}%";
        }

        private void UpdateOpacityLabels()
        {
            _spiritOpacityLabel.Text = $"{_spiritOpacitySlider.Value:F0}%";
            _elementOpacityLabel.Text = $"{_elementOpacitySlider.Value:F0}%";
            _lawOpacityLabel.Text = $"{_lawOpacitySlider.Value:F0}%";
        }
    }
}
