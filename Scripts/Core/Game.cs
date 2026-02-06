using Godot;
using FantasyMapGenerator.Scripts.Rendering;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Core;

/// <summary>
/// 游戏主界面
/// </summary>
public partial class Game : Control
{
	private MapView _mapView;
	private Button _menuButton;
	private PanelContainer _menuPanel;
	private VBoxContainer _menuVBox;
	private Label _pausedLabel;
	private Button _resumeButton;
	private Button _regenerateButton;
	private Button _settingsButton;
	private Button _mainMenuButton;
	private Button _quitButton;

	private bool _isMenuVisible;
	private TranslationManager _translationManager;

	public override void _Ready()
	{
		_translationManager = TranslationManager.Instance;
		_translationManager.LanguageChanged += OnLanguageChanged;

		_mapView = GetNode<MapView>("MapView");
		
		// 获取菜单按钮
		_menuButton = GetNode<Button>("MenuButton");
		
		// 获取菜单面板
		_menuPanel = GetNode<PanelContainer>("MenuPanel");
		_menuVBox = GetNode<VBoxContainer>("MenuPanel/MenuVBox");
		_pausedLabel = GetNode<Label>("MenuPanel/MenuVBox/PausedLabel");
		_resumeButton = GetNode<Button>("MenuPanel/MenuVBox/ResumeButton");
		_regenerateButton = GetNode<Button>("MenuPanel/MenuVBox/RegenerateButton");
		_settingsButton = GetNode<Button>("MenuPanel/MenuVBox/SettingsButton");
		_mainMenuButton = GetNode<Button>("MenuPanel/MenuVBox/MainMenuButton");
		_quitButton = GetNode<Button>("MenuPanel/MenuVBox/QuitButton");

		SetupUI();
		UpdateUIText();
	}

	private void OnLanguageChanged(string language)
	{
		UpdateUIText();
	}

	private void UpdateUIText()
	{
		var tm = TranslationManager.Instance;
		if (_menuButton != null)
		{
			_menuButton.Text = tm.Tr("menu");
		}

		if (_pausedLabel != null)
		{
			_pausedLabel.Text = tm.Tr("game_paused");
		}

		if (_resumeButton != null)
		{
			_resumeButton.Text = tm.Tr("resume");
		}

		if (_regenerateButton != null)
		{
			_regenerateButton.Text = tm.Tr("regenerate_map");
		}

		if (_settingsButton != null)
		{
			_settingsButton.Text = tm.Tr("settings");
		}

		if (_mainMenuButton != null)
		{
			_mainMenuButton.Text = tm.Tr("back_to_main_menu");
		}

		if (_quitButton != null)
		{
			_quitButton.Text = tm.Tr("quit_game");
		}
	}

	private void SetupMapView()
	{
		if (_mapView == null)
		{
			_mapView = new MapView();
			_mapView.Name = "MapView";
			AddChild(_mapView);
			MoveChild(_mapView, 0);
		}
	}

	private void SetupUI()
	{
		// 菜单按钮点击事件
		if (_menuButton != null)
		{
			_menuButton.Pressed += OnMenuButtonPressed;
		}

		// 菜单内按钮点击事件
		if (_resumeButton != null)
		{
			_resumeButton.Pressed += OnResumePressed;
		}

		if (_regenerateButton != null)
		{
			_regenerateButton.Pressed += OnRegeneratePressed;
		}

		if (_settingsButton != null)
		{
			_settingsButton.Pressed += OnSettingsPressed;
		}

		if (_mainMenuButton != null)
		{
			_mainMenuButton.Pressed += OnMainMenuPressed;
		}

		if (_quitButton != null)
		{
			_quitButton.Pressed += OnQuitPressed;
		}

		_isMenuVisible = false;
		_menuPanel.Visible = false;
	}

	private void ToggleMenu()
	{
		_isMenuVisible = !_isMenuVisible;
		_menuPanel.Visible = _isMenuVisible;
	}

	private void OnMenuButtonPressed()
	{
		ToggleMenu();
	}

	private void OnResumePressed()
	{
		_isMenuVisible = false;
		_menuPanel.Visible = false;
	}

	private void OnRegeneratePressed()
	{
		// 隐藏菜单并重新生成地图
		_isMenuVisible = false;
		_menuPanel.Visible = false;
		
		// 触发地图重新生成
		_mapView?.GenerateMap();
	}

	private void OnSettingsPressed()
	{
		SwitchToScene("res://Scenes/UI/Settings.tscn");
	}

	private void OnMainMenuPressed()
	{
		SwitchToScene("res://Scenes/UI/MainMenu.tscn");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
		{
			ToggleMenu();
		}
	}

	private void SwitchToScene(string scenePath)
	{
		var error = GetTree().ChangeSceneToFile(scenePath);
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to load scene: {scenePath}");
		}
	}
}
