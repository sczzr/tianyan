using System;
using Godot;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.UI.Controllers;

/// <summary>
/// 底部菜单控制器：负责下拉菜单和标签页按钮事件
/// </summary>
public partial class BottomMenuController : Control
{
	private HBoxContainer _bottomMenu;
	private Button _mapDisplayButton;
	private Button _settingsMenuButton;
	private PanelContainer _mapDisplayDropdown;
	private PanelContainer _settingsDropdown;
	private Button _layersTabButton;
	private Button _editTabButton;
	private Button _borderTabButton;
	private Button _themeTabButton;
	private Button _mapDropdownRegenerateButton;
	private Button _mapDropdownSettingsButton;

	private bool _initialized;
	private Action<int> _onDisplayTabRequested;
	private Action _onRegenerateRequested;
	private Action _onMapSettingsRequested;

	public override void _Ready()
	{
		_bottomMenu = GetNodeOrNull<HBoxContainer>("BottomMenu");
		_mapDisplayButton = GetNodeOrNull<Button>("BottomMenu/MapDisplayButton");
		_settingsMenuButton = GetNodeOrNull<Button>("BottomMenu/SettingsMenuButton");
		_mapDisplayDropdown = GetNodeOrNull<PanelContainer>("BottomMenu/MapDisplayButton/MapDisplayDropdown");
		_settingsDropdown = GetNodeOrNull<PanelContainer>("BottomMenu/SettingsMenuButton/SettingsDropdown");
		_layersTabButton = GetNodeOrNull<Button>("BottomMenu/MapDisplayButton/MapDisplayDropdown/DropdownVBox/LayersTabButton");
		_editTabButton = GetNodeOrNull<Button>("BottomMenu/MapDisplayButton/MapDisplayDropdown/DropdownVBox/EditTabButton");
		_borderTabButton = GetNodeOrNull<Button>("BottomMenu/MapDisplayButton/MapDisplayDropdown/DropdownVBox/BorderTabButton");
		_themeTabButton = GetNodeOrNull<Button>("BottomMenu/MapDisplayButton/MapDisplayDropdown/DropdownVBox/ThemeTabButton");
		_mapDropdownRegenerateButton = GetNodeOrNull<Button>("BottomMenu/SettingsMenuButton/SettingsDropdown/DropdownVBox/RegenerateDropdownButton");
		_mapDropdownSettingsButton = GetNodeOrNull<Button>("BottomMenu/SettingsMenuButton/SettingsDropdown/DropdownVBox/MapSettingsDropdownButton");
	}

	public void Initialize(
		Action<int> onDisplayTabRequested,
		Action onRegenerateRequested,
		Action onMapSettingsRequested)
	{
		_onDisplayTabRequested = onDisplayTabRequested;
		_onRegenerateRequested = onRegenerateRequested;
		_onMapSettingsRequested = onMapSettingsRequested;

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
	}

	public bool IsPointerInsideMenu(Vector2 globalPosition)
	{
		return IsPointInsideControl(_bottomMenu, globalPosition)
			|| IsPointInsideControl(_mapDisplayDropdown, globalPosition)
			|| IsPointInsideControl(_settingsDropdown, globalPosition);
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
	}

	private void OnMapDisplayButtonPressed()
	{
		ToggleDropdown(_mapDisplayDropdown);
	}

	private void OnSettingsMenuButtonPressed()
	{
		ToggleDropdown(_settingsDropdown);
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
