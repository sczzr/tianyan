using Godot;
using System;

using TianYanShop.MapGeneration;
using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Rendering;
using TianYanShop.MapGeneration.Generation;

namespace TianYanShop.Scene
{
    public partial class WorldMapDisplay : Control
    {
        private MapGenerator _mapGenerator;
        private VoronoiGraph _graph;
        private MapRenderer _mapRenderer;

        private Panel _infoPanel;
        private Label _seedLabel;
        private Label _positionLabel;
        private Label _biomeLabel;
        private Label _zoomLabel;

        private Button _regenerateButton;
        private Button _enterWorldButton;
        private Button _editMapButton;

        private Camera2D _camera;
        private float _zoom = 1.0f;
        private float _minZoom = 0.25f;
        private float _maxZoom = 2.0f;
        private Vector2 _cameraPosition;
        private bool _isDragging = false;
        private Vector2 _dragStart;

        private string _currentSeed = "default";

        public override void _Ready()
        {
            SetupUI();
            SetupCamera();
            GenerateAndRenderMap();
        }

        private void SetupUI()
        {
            _infoPanel = new Panel();
            _infoPanel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            _infoPanel.CustomMinimumSize = new Godot.Vector2(0, 120);
            _infoPanel.OffsetTop = 0;
            _infoPanel.OffsetBottom = 120;
            AddChild(_infoPanel);

            var panelContainer = new HBoxContainer();
            panelContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            panelContainer.AddThemeConstantOverride("separation", 30);
            _infoPanel.AddChild(panelContainer);

            var infoSection = new VBoxContainer();
            infoSection.AddThemeConstantOverride("separation", 5);
            panelContainer.AddChild(infoSection);

            _seedLabel = new Label();
            _seedLabel.Text = $"地图种子: {_currentSeed}";
            infoSection.AddChild(_seedLabel);

            _positionLabel = new Label();
            _positionLabel.Text = "位置: (0, 0)";
            infoSection.AddChild(_positionLabel);

            _biomeLabel = new Label();
            _biomeLabel.Text = "地形: 未知";
            infoSection.AddChild(_biomeLabel);

            _zoomLabel = new Label();
            _zoomLabel.Text = $"缩放: {_zoom:F2}x";
            infoSection.AddChild(_zoomLabel);

            var controlsSection = new VBoxContainer();
            controlsSection.AddThemeConstantOverride("separation", 10);
            panelContainer.AddChild(controlsSection);

            var controlsLabel = new Label();
            controlsLabel.Text = "操作说明";
            controlsLabel.AddThemeFontSizeOverride("font_size", 16);
            controlsLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
            controlsSection.AddChild(controlsLabel);

            var controlsInfo = new Label();
            controlsInfo.Text = "WASD: 移动\n滚轮: 缩放\n左键: 选择位置";
            controlsSection.AddChild(controlsInfo);

            var spacer = new Control();
            spacer.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
            panelContainer.AddChild(spacer);

            var buttonSection = new VBoxContainer();
            buttonSection.AddThemeConstantOverride("separation", 10);
            panelContainer.AddChild(buttonSection);

            _regenerateButton = new Button();
            _regenerateButton.Text = "重新生成";
            _regenerateButton.CustomMinimumSize = new Godot.Vector2(120, 40);
            buttonSection.AddChild(_regenerateButton);

            _editMapButton = new Button();
            _editMapButton.Text = "编辑地图";
            _editMapButton.CustomMinimumSize = new Godot.Vector2(120, 40);
            buttonSection.AddChild(_editMapButton);

            _enterWorldButton = new Button();
            _enterWorldButton.Text = "进入世界";
            _enterWorldButton.CustomMinimumSize = new Godot.Vector2(120, 40);
            buttonSection.AddChild(_enterWorldButton);

            var backButton = new Button();
            backButton.Text = "返回主菜单";
            backButton.CustomMinimumSize = new Godot.Vector2(120, 40);
            buttonSection.AddChild(backButton);
            backButton.Pressed += OnBackPressed;

            ConnectSignals();
        }

        private void SetupCamera()
        {
            _camera = new Camera2D();
            _camera.MakeCurrent();
            _camera.Enabled = true;
            AddChild(_camera);
        }

        private void ConnectSignals()
        {
            _regenerateButton.Pressed += OnRegeneratePressed;
            _editMapButton.Pressed += OnEditMapPressed;
            _enterWorldButton.Pressed += OnEnterWorldPressed;
        }

        private void GenerateAndRenderMap()
        {
            GenerateAndRenderMapWithSeed(_currentSeed);
        }

        private void GenerateAndRenderMapWithSeed(string seed)
        {
            if (_mapRenderer != null)
            {
                _mapRenderer.QueueFree();
                _mapRenderer = null;
            }

            _mapGenerator = new MapGenerator(seed);
            _graph = _mapGenerator.Generate(new MapSettings
            {
                Width = 1024,
                Height = 1024,
                CellsCount = 8000,
                StatesNumber = Mathf.Max(5, 10),
                BurgsNumber = Mathf.Max(20, 50),
                AddRivers = true,
                AddLakes = true,
                AddRoads = true,
                AddSeaRoutes = true
            });

            _mapRenderer = new MapRenderer();
            _mapRenderer.Name = "MapRenderer";
            _mapRenderer.ShowBiomes = true;
            _mapRenderer.ShowRivers = true;
            _mapRenderer.ShowBorders = true;
            _mapRenderer.ShowBurgs = true;
            _mapRenderer.ShowLabels = true;
            _mapRenderer.ShowCoastline = true;
            AddChild(_mapRenderer);
            _mapRenderer.ZIndex = -1;
            _mapRenderer.Render(_graph);

            _seedLabel.Text = $"地图种子: {seed}";
        }

        private void OnRegeneratePressed()
        {
            _currentSeed = Guid.NewGuid().ToString();
            GenerateAndRenderMapWithSeed(_currentSeed);
            UpdateStatus("地图已重新生成");
        }

        private void OnEditMapPressed()
        {
            GD.Print("Opening map editor...");
            UpdateStatus("正在打开地图编辑器...");
        }

        private void OnEnterWorldPressed()
        {
            GD.Print("Entering game world...");
            UpdateStatus("正在进入游戏世界...");
        }

        private void OnBackPressed()
        {
            GetTree().ChangeSceneToFile("res://Scenes/Main/StartScene.tscn");
        }

        private void UpdateStatus(string text)
        {
            GD.Print(text);
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                HandleMouseInput(mouseEvent);
            }
            else if (@event is InputEventMouseMotion motionEvent)
            {
                HandleMouseMotion(motionEvent);
            }
            else if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                HandleKeyboardInput(keyEvent);
            }
        }

        private void HandleMouseInput(InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (mouseEvent.Pressed)
                {
                    _isDragging = true;
                    _dragStart = mouseEvent.Position;
                }
                else
                {
                    _isDragging = false;
                }
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed)
            {
                ZoomCamera(1.1f, mouseEvent.Position);
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed)
            {
                ZoomCamera(0.9f, mouseEvent.Position);
            }
        }

        private void HandleMouseMotion(InputEventMouseMotion motionEvent)
        {
            if (_isDragging)
            {
                Vector2 delta = motionEvent.Position - _dragStart;
                _cameraPosition -= delta / _zoom;
                _camera.Position = _cameraPosition;
                _dragStart = motionEvent.Position;
            }
        }

        private void ZoomCamera(float factor, Vector2 zoomCenter)
        {
            float newZoom = Mathf.Clamp(_zoom * factor, _minZoom, _maxZoom);
            if (newZoom != _zoom)
            {
                Vector2 worldPos = _camera.GetGlobalMousePosition();
                _camera.Zoom = new Vector2(newZoom, newZoom);
                _zoom = newZoom;
                _zoomLabel.Text = $"缩放: {_zoom:F2}x";
            }
        }

        private void HandleKeyboardInput(InputEventKey keyEvent)
        {
            float delta = (float)GetProcessDeltaTime();
            float moveSpeed = 500f * delta / _zoom;
            Vector2 move = Vector2.Zero;

            if (keyEvent.Keycode == Key.W) move.Y -= moveSpeed;
            if (keyEvent.Keycode == Key.S) move.Y += moveSpeed;
            if (keyEvent.Keycode == Key.A) move.X -= moveSpeed;
            if (keyEvent.Keycode == Key.D) move.X += moveSpeed;

            if (move != Vector2.Zero)
            {
                _cameraPosition += move;
                _camera.Position = _cameraPosition;
                UpdatePositionLabel();
            }
        }

        private void UpdatePositionLabel()
        {
            Vector2 worldPos = _camera.GetGlobalMousePosition();
            _positionLabel.Text = $"位置: ({worldPos.X:F0}, {worldPos.Y:F0})";
        }
    }
}
