using System;
using Godot;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.UI;

/// <summary>
/// 底部菜单控制器：负责下拉菜单和标签页按钮事件
/// </summary>
public partial class BottomMenuController : Control
{
	private HBoxContainer _bottomMenu;
	private Button _mapDisplayButton;
	private Button _settingsMenuButton;
	private Button _systemMenuButton;
	private PanelContainer _mapDisplayDropdown;
	private PanelContainer _settingsDropdown;
	private PanelContainer _systemDropdown;
	private Button _layersTabButton;
	private Button _editTabButton;
	private Button _borderTabButton;
	private Button _themeTabButton;
	private Button _mapDropdownRegenerateButton;
	private Button _mapDropdownSettingsButton;
	private Button _systemDropdownSettingsButton;
	private Button _systemDropdownMainMenuButton;
	private Button _systemDropdownQuitButton;

	private bool _initialized;
	private Action<int> _onDisplayTabRequested;
	private Action _onRegenerateRequested;
	private Action _onMapSettingsRequested;
	private Action _onSystemSettingsRequested;
	private Action _onMainMenuRequested;
	private Action _onQuitRequested;

	public override void _Ready()
	{
		_bottomMenu = GetNodeOrNull<HBoxContainer>("BottomMenu");
		_mapDisplayButton = GetNodeOrNull<Button>("BottomMenu/MapDisplayButton");
		_settingsMenuButton = GetNodeOrNull<Button>("BottomMenu/SettingsMenuButton");
		_systemMenuButton = GetNodeOrNull<Button>("BottomMenu/SystemMenuButton");
		_mapDisplayDropdown = GetNodeOrNull<PanelContainer>("BottomMenu/MapDisplayButton/MapDisplayDropdown");
		_settingsDropdown = GetNodeOrNull<PanelContainer>("BottomMenu/SettingsMenuButton/SettingsDropdown");
		_systemDropdown = GetNodeOrNull<PanelContainer>("BottomMenu/SystemMenuButton/SystemDropdown");
		_layersTabButton = GetNodeOrNull<Button>("BottomMenu/MapDisplayButton/MapDisplayDropdown/DropdownVBox/LayersTabButton");
		_editTabButton = GetNodeOrNull<Button>("BottomMenu/MapDisplayButton/MapDisplayDropdown/DropdownVBox/EditTabButton");
		_borderTabButton = GetNodeOrNull<Button>("BottomMenu/MapDisplayButton/MapDisplayDropdown/DropdownVBox/BorderTabButton");
		_themeTabButton = GetNodeOrNull<Button>("BottomMenu/MapDisplayButton/MapDisplayDropdown/DropdownVBox/ThemeTabButton");
		_mapDropdownRegenerateButton = GetNodeOrNull<Button>("BottomMenu/SettingsMenuButton/SettingsDropdown/DropdownVBox/RegenerateDropdownButton");
		_mapDropdownSettingsButton = GetNodeOrNull<Button>("BottomMenu/SettingsMenuButton/SettingsDropdown/DropdownVBox/MapSettingsDropdownButton");
		_systemDropdownSettingsButton = GetNodeOrNull<Button>("BottomMenu/SystemMenuButton/SystemDropdown/DropdownVBox/SettingsDropdownButton");
		_systemDropdownMainMenuButton = GetNodeOrNull<Button>("BottomMenu/SystemMenuButton/SystemDropdown/DropdownVBox/MainMenuDropdownButton");
		_systemDropdownQuitButton = GetNodeOrNull<Button>("BottomMenu/SystemMenuButton/SystemDropdown/DropdownVBox/QuitDropdownButton");
	}

	public void Initialize(
		Action<int> onDisplayTabRequested,
		Action onRegenerateRequested,
		Action onMapSettingsRequested,
		Action onSystemSettingsRequested,
		Action onMainMenuRequested,
		Action onQuitRequested)
	{
		_onDisplayTabRequested = onDisplayTabRequested;
		_onRegenerateRequested = onRegenerateRequested;
		_onMapSettingsRequested = onMapSettingsRequested;
		_onSystemSettingsRequested = onSystemSettingsRequested;
		_onMainMenuRequested = onMainMenuRequested;
		_onQuitRequested = onQuitRequested;

		if (_initialized)
		{
			return;
		}

		_initialized = true;

		if (_mapDisplayButton != null)
		{
			_mapDisplayButton.Pressed += OnMapDisplayButtonPressed;
		}

		if (_settingsMenuButton != null)
		{
			_settingsMenuButton.Pressed += OnSettingsMenuButtonPressed;
		}

		if (_systemMenuButton != null)
		{
			_systemMenuButton.Pressed += OnSystemMenuButtonPressed;
		}

		if (_layersTabButton != null)
		{
			_layersTabButton.Pressed += () => OnDisplayTabSelected(0);
		}

		if (_editTabButton != null)
		{
			_editTabButton.Pressed += () => OnDisplayTabSelected(1);
		}

		if (_borderTabButton != null)
		{
			_borderTabButton.Pressed += () => OnDisplayTabSelected(2);
		}

		if (_themeTabButton != null)
		{
			_themeTabButton.Pressed += () => OnDisplayTabSelected(3);
		}

		if (_mapDropdownRegenerateButton != null)
		{
			_mapDropdownRegenerateButton.Pressed += OnMapDropdownRegeneratePressed;
		}

		if (_mapDropdownSettingsButton != null)
		{
			_mapDropdownSettingsButton.Pressed += OnMapDropdownSettingsPressed;
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

	public void HideDropdowns()
	{
		if (_mapDisplayDropdown != null)
		{
			_mapDisplayDropdown.Visible = false;
		}

		if (_settingsDropdown != null)
		{
			_settingsDropdown.Visible = false;
		}

		if (_systemDropdown != null)
		{
			_systemDropdown.Visible = false;
		}
	}

	public bool IsPointerInsideMenu(Vector2 globalPosition)
	{
		return IsPointInsideControl(_bottomMenu, globalPosition)
			|| IsPointInsideControl(_mapDisplayDropdown, globalPosition)
			|| IsPointInsideControl(_settingsDropdown, globalPosition)
			|| IsPointInsideControl(_systemDropdown, globalPosition);
	}

	public void UpdateUIText()
	{
		var tm = TranslationManager.Instance;
		if (_mapDisplayButton != null)
		{
			_mapDisplayButton.Text = tm.Tr("map_display");
		}

		if (_settingsMenuButton != null)
		{
			_settingsMenuButton.Text = tm.Tr("settings");
		}

		if (_systemMenuButton != null)
		{
			_systemMenuButton.Text = tm.Tr("system_menu");
		}

		if (_layersTabButton != null)
		{
			_layersTabButton.Text = tm.Tr("map_tab_layers");
		}

		if (_editTabButton != null)
		{
			_editTabButton.Text = tm.Tr("map_tab_edit");
		}

		if (_borderTabButton != null)
		{
			_borderTabButton.Text = tm.Tr("map_tab_border");
		}

		if (_themeTabButton != null)
		{
			_themeTabButton.Text = tm.Tr("map_tab_theme");
		}

		if (_mapDropdownRegenerateButton != null)
		{
			_mapDropdownRegenerateButton.Text = tm.Tr("regenerate_map");
		}

		if (_mapDropdownSettingsButton != null)
		{
			_mapDropdownSettingsButton.Text = tm.Tr("map_settings");
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
	}

	private void OnMapDisplayButtonPressed()
	{
		ToggleDropdown(_mapDisplayDropdown);
	}

	private void OnSettingsMenuButtonPressed()
	{
		ToggleDropdown(_settingsDropdown);
	}

	private void OnSystemMenuButtonPressed()
	{
		ToggleDropdown(_systemDropdown);
	}

	private void OnDisplayTabSelected(int tabIndex)
	{
		HideDropdowns();
		_onDisplayTabRequested?.Invoke(tabIndex);
	}

	private void OnMapDropdownRegeneratePressed()
	{
		HideDropdowns();
		_onRegenerateRequested?.Invoke();
	}

	private void OnMapDropdownSettingsPressed()
	{
		HideDropdowns();
		_onMapSettingsRequested?.Invoke();
	}

	private void OnSystemDropdownSettingsPressed()
	{
		HideDropdowns();
		_onSystemSettingsRequested?.Invoke();
	}

	private void OnSystemDropdownMainMenuPressed()
	{
		HideDropdowns();
		_onMainMenuRequested?.Invoke();
	}

	private void OnSystemDropdownQuitPressed()
	{
		HideDropdowns();
		_onQuitRequested?.Invoke();
	}

	private void ToggleDropdown(PanelContainer dropdown)
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

	private static bool IsPointInsideControl(Control control, Vector2 globalPosition)
	{
		return control != null && control.Visible && control.GetGlobalRect().HasPoint(globalPosition);
	}
}
