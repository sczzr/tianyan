using Godot;

namespace TianYanShop.Scene
{
    public partial class StartScene : Control
    {
        private Button _newGameButton;
        private Button _loadGameButton;
        private Button _settingsButton;
        private Button _quitButton;
        private Button _mapEditorButton;
        private Label _titleLabel;
        private Label _versionLabel;

        public override void _Ready()
        {
            SetupUI();
            ConnectSignals();
        }

        private void SetupUI()
        {
            var background = new ColorRect();
            background.Color = new Color(0.1f, 0.1f, 0.15f);
            background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            AddChild(background);

            _titleLabel = new Label();
            _titleLabel.Text = "天衍峰：修仙商铺";
            _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _titleLabel.AddThemeFontSizeOverride("font_size", 48);
            _titleLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.8f, 0.3f));
            _titleLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
            _titleLabel.OffsetTop = 80;
            _titleLabel.OffsetBottom = 140;
            AddChild(_titleLabel);

            _versionLabel = new Label();
            _versionLabel.Text = "v1.0.0";
            _versionLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _versionLabel.AddThemeFontSizeOverride("font_size", 16);
            _versionLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
            _versionLabel.SetAnchorsPreset(Control.LayoutPreset.RightWide);
            _versionLabel.OffsetLeft = -80;
            _versionLabel.OffsetTop = -40;
            _versionLabel.OffsetRight = -20;
            _versionLabel.OffsetBottom = -20;
            AddChild(_versionLabel);

            var buttonContainer = new VBoxContainer();
            buttonContainer.SetAnchorsPreset(Control.LayoutPreset.Center);
            buttonContainer.OffsetLeft = -100;
            buttonContainer.OffsetTop = -180;
            buttonContainer.OffsetRight = 100;
            buttonContainer.OffsetBottom = 180;
            buttonContainer.AddThemeConstantOverride("separation", 20);
            AddChild(buttonContainer);

            _newGameButton = new Button();
            _newGameButton.Text = "新游戏";
            _newGameButton.CustomMinimumSize = new Godot.Vector2(200, 50);
            _newGameButton.AddThemeFontSizeOverride("font_size", 24);
            buttonContainer.AddChild(_newGameButton);

            _loadGameButton = new Button();
            _loadGameButton.Text = "加载存档";
            _loadGameButton.CustomMinimumSize = new Godot.Vector2(200, 50);
            _loadGameButton.AddThemeFontSizeOverride("font_size", 24);
            buttonContainer.AddChild(_loadGameButton);

            _settingsButton = new Button();
            _settingsButton.Text = "游戏设置";
            _settingsButton.CustomMinimumSize = new Godot.Vector2(200, 50);
            _settingsButton.AddThemeFontSizeOverride("font_size", 24);
            buttonContainer.AddChild(_settingsButton);

            _quitButton = new Button();
            _quitButton.Text = "退出游戏";
            _quitButton.CustomMinimumSize = new Godot.Vector2(200, 50);
            _quitButton.AddThemeFontSizeOverride("font_size", 24);
            buttonContainer.AddChild(_quitButton);

            _mapEditorButton = new Button();
            _mapEditorButton.Text = "地图编辑器";
            _mapEditorButton.CustomMinimumSize = new Godot.Vector2(200, 50);
            _mapEditorButton.AddThemeFontSizeOverride("font_size", 24);
            buttonContainer.AddChild(_mapEditorButton);
        }

        private void ConnectSignals()
        {
            _newGameButton.Pressed += OnNewGamePressed;
            _loadGameButton.Pressed += OnLoadGamePressed;
            _settingsButton.Pressed += OnSettingsPressed;
            _quitButton.Pressed += OnQuitPressed;
            _mapEditorButton.Pressed += OnMapEditorPressed;
        }

        private void OnNewGamePressed()
        {
            GD.Print("Starting new game...");
            GetTree().ChangeSceneToFile("res://Scenes/World/WorldMapDisplay.tscn");
        }

        private void OnLoadGamePressed()
        {
            GD.Print("Opening load game dialog...");
            ShowMessageBox("加载存档", "存档功能开发中...");
        }

        private void OnSettingsPressed()
        {
            GD.Print("Opening settings...");
            ShowMessageBox("游戏设置", "设置功能开发中...");
        }

        private void OnMapEditorPressed()
        {
            GD.Print("Opening map editor...");
            GetTree().ChangeSceneToFile("res://Scenes/World/MapEditorScene.tscn");
        }

        private void OnQuitPressed()
        {
            GetTree().Quit();
        }

        private void ShowMessageBox(string title, string message)
        {
            var dialog = new AcceptDialog();
            dialog.Title = title;
            dialog.DialogText = message;
            dialog.OkButtonText = "确定";
            AddChild(dialog);
            dialog.PopupCentered();
        }
    }
}
