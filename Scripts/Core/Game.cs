using System.Collections.Generic;
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
	private HBoxContainer _topMenu;
	private Button _mapMenuButton;
	private Button _systemMenuButton;
	private PanelContainer _mapDropdown;
	private PanelContainer _systemDropdown;
	private Button _mapDropdownRegenerateButton;
	private Button _systemDropdownSettingsButton;
	private Button _systemDropdownMainMenuButton;
	private Button _systemDropdownQuitButton;
	private PanelContainer _menuPanel;
	private VBoxContainer _menuVBox;
	private Label _pausedLabel;
	private Label _mapViewScaleLabel;
	private Button _resumeButton;
	private Button _regenerateButton;
	private Button _settingsButton;
	private Button _mainMenuButton;
	private Button _quitButton;
	private HSlider _mapViewScaleSlider;

	private bool _isMenuVisible;
	private TranslationManager _translationManager;

	public override void _Ready()
	{
		_translationManager = TranslationManager.Instance;
		_translationManager.LanguageChanged += OnLanguageChanged;

		_mapView = GetNode<MapView>("MapView");
		_topMenu = GetNode<HBoxContainer>("TopMenu");
		_mapMenuButton = GetNode<Button>("TopMenu/MapMenuButton");
		_systemMenuButton = GetNode<Button>("TopMenu/SystemMenuButton");
		_mapDropdown = GetNode<PanelContainer>("TopMenu/MapMenuButton/MapDropdown");
		_systemDropdown = GetNode<PanelContainer>("TopMenu/SystemMenuButton/SystemDropdown");
		_mapDropdownRegenerateButton = GetNode<Button>("TopMenu/MapMenuButton/MapDropdown/DropdownVBox/RegenerateDropdownButton");
		_systemDropdownSettingsButton = GetNode<Button>("TopMenu/SystemMenuButton/SystemDropdown/DropdownVBox/SettingsDropdownButton");
		_systemDropdownMainMenuButton = GetNode<Button>("TopMenu/SystemMenuButton/SystemDropdown/DropdownVBox/MainMenuDropdownButton");
		_systemDropdownQuitButton = GetNode<Button>("TopMenu/SystemMenuButton/SystemDropdown/DropdownVBox/QuitDropdownButton");
		
		// 获取菜单面板
		_menuPanel = GetNode<PanelContainer>("MenuPanel");
		_menuVBox = GetNode<VBoxContainer>("MenuPanel/MenuVBox");
		_pausedLabel = GetNode<Label>("MenuPanel/MenuVBox/PausedLabel");
		_mapViewScaleLabel = GetNode<Label>("MenuPanel/MenuVBox/MapViewScaleLabel");
		_resumeButton = GetNode<Button>("MenuPanel/MenuVBox/ResumeButton");
		_regenerateButton = GetNode<Button>("MenuPanel/MenuVBox/RegenerateButton");
		_settingsButton = GetNode<Button>("MenuPanel/MenuVBox/SettingsButton");
		_mainMenuButton = GetNode<Button>("MenuPanel/MenuVBox/MainMenuButton");
		_quitButton = GetNode<Button>("MenuPanel/MenuVBox/QuitButton");
		_mapViewScaleSlider = GetNode<HSlider>("MenuPanel/MenuVBox/MapViewScaleSlider");

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
		_mapMenuButton.Text = tm.Tr("map_menu");
		_systemMenuButton.Text = tm.Tr("system_menu");

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

		if (_mapDropdownRegenerateButton != null)
		{
			_mapDropdownRegenerateButton.Text = tm.Tr("regenerate_map");
		}

		if (_systemDropdownSettingsButton != null)
		{
			_systemDropdownSettingsButton.Text = tm.Tr("settings");
		}

		if (_systemDropdownMainMenuButton != null)
		{
			_systemDropdownMainMenuButton.Text = tm.Tr("back_to_main_menu");
		}

		if (_systemDropdownQuitButton != null)
		{
			_systemDropdownQuitButton.Text = tm.Tr("quit_game");
		}

		UpdateMapViewScaleLabel();
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
		SetupTopMenu();

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

		if (_mapViewScaleSlider != null)
		{
			_mapViewScaleSlider.ValueChanged += OnMapViewScaleChanged;
			OnMapViewScaleChanged(_mapViewScaleSlider.Value);
		}

		_isMenuVisible = false;
		_menuPanel.Visible = false;
		HideDropdowns();
	}

	private void SetupTopMenu()
	{
		if (_mapMenuButton != null)
		{
			_mapMenuButton.Pressed += OnMapMenuButtonPressed;
		}

		if (_systemMenuButton != null)
		{
			_systemMenuButton.Pressed += OnSystemMenuButtonPressed;
		}

		if (_mapDropdownRegenerateButton != null)
		{
			_mapDropdownRegenerateButton.Pressed += OnMapDropdownRegeneratePressed;
		}

		if (_systemDropdownSettingsButton != null)
		{
			_systemDropdownSettingsButton.Pressed += OnSystemDropdownSettingsPressed;
		}

		if (_systemDropdownMainMenuButton != null)
		{
			_systemDropdownMainMenuButton.Pressed += OnSystemDropdownMainMenuPressed;
		}

		if (_systemDropdownQuitButton != null)
		{
			_systemDropdownQuitButton.Pressed += OnSystemDropdownQuitPressed;
		}

		HideDropdowns();
	}

	private void ToggleMenu()
	{
		HideDropdowns();
		_isMenuVisible = !_isMenuVisible;
		_menuPanel.Visible = _isMenuVisible;
	}

	private void OnResumePressed()
	{
		HideDropdowns();
		_isMenuVisible = false;
		_menuPanel.Visible = false;
	}

	private void OnRegeneratePressed()
	{
		// 隐藏菜单并重新生成地图
		HideDropdowns();
		_isMenuVisible = false;
		_menuPanel.Visible = false;
		
		// 触发地图重新生成
		_mapView?.GenerateMap();
	}

	private void OnSettingsPressed()
	{
		HideDropdowns();
		SwitchToScene("res://Scenes/UI/Settings.tscn");
	}

	private void OnMainMenuPressed()
	{
		HideDropdowns();
		SwitchToScene("res://Scenes/UI/MainMenu.tscn");
	}

	private void OnQuitPressed()
	{
		HideDropdowns();
		GetTree().Quit();
	}

	private void OnMapViewScaleChanged(double value)
	{
		if (_mapView != null)
		{
			_mapView.ViewScale = (float)value;
		}
		UpdateMapViewScaleLabel();
	}

	private void UpdateMapViewScaleLabel()
	{
		if (_mapViewScaleLabel == null || _mapViewScaleSlider == null)
		{
			return;
		}

		var tm = TranslationManager.Instance;
		var percent = Mathf.RoundToInt((float)_mapViewScaleSlider.Value * 100f);
		_mapViewScaleLabel.Text = tm.TrWithFormat("map_view_scale", percent.ToString());
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Escape)
		{
			HideDropdowns();
			ToggleMenu();
		}

		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (!IsPointerInsideMenu(mouseButton.Position))
			{
				HideDropdowns();
			}
		}
	}

	private void HideDropdowns()
	{
		if (_mapDropdown != null)
		{
			_mapDropdown.Visible = false;
		}

		if (_systemDropdown != null)
		{
			_systemDropdown.Visible = false;
		}
	}

	private void OnMapMenuButtonPressed()
	{
		ToggleDropdown(_mapDropdown, _mapMenuButton);
	}

	private void OnSystemMenuButtonPressed()
	{
		ToggleDropdown(_systemDropdown, _systemMenuButton);
	}

	private void ToggleDropdown(PanelContainer dropdown, Control anchor)
	{
		if (dropdown == null)
		{
			return;
		}

		var shouldShow = !dropdown.Visible;
		HideDropdowns();
		if (shouldShow)
		{
			dropdown.Visible = true;
		}
	}

	private void OnMapDropdownRegeneratePressed()
	{
		HideDropdowns();
		OnRegeneratePressed();
	}

	private void OnSystemDropdownSettingsPressed()
	{
		HideDropdowns();
		OnSettingsPressed();
	}

	private void OnSystemDropdownMainMenuPressed()
	{
		HideDropdowns();
		OnMainMenuPressed();
	}

	private void OnSystemDropdownQuitPressed()
	{
		HideDropdowns();
		OnQuitPressed();
	}

	private bool IsPointerInsideMenu(Vector2 globalPosition)
	{
		return IsPointInsideControl(_topMenu, globalPosition)
			   || IsPointInsideControl(_mapDropdown, globalPosition)
			   || IsPointInsideControl(_systemDropdown, globalPosition);
	}

	private static bool IsPointInsideControl(Control control, Vector2 globalPosition)
	{
		return control != null && control.Visible && control.GetGlobalRect().HasPoint(globalPosition);
	}

	private void SwitchToScene(string scenePath)
	{
		SceneNavigator.Instance.NavigateTo(scenePath);
	}
}
