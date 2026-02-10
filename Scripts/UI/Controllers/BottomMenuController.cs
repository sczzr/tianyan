using System;
using Godot;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.UI.Controllers;

/// <summary>
/// 底部菜单控制器：负责底部菜单按钮和设置下拉菜单事件
/// </summary>
public partial class BottomMenuController : Control
{
	private PanelContainer _bottomDockBackground;
	private HBoxContainer _bottomMenu;
	private Button _mapDisplayButton;
	private Button _backToParentButton;
	private Button _settingsMenuButton;
	private PanelContainer _settingsDropdown;
	private Button _mapDropdownRegenerateButton;
	private Button _mapDropdownSettingsButton;

	private bool _initialized;
	private Action _onMapDisplayRequested;
	private Action _onBackToParentRequested;
	private Action _onRegenerateRequested;
	private Action _onMapSettingsRequested;

	public override void _Ready()
	{
		_bottomDockBackground = GetNodeOrNull<PanelContainer>("BottomDockBackground");
		_bottomMenu = GetNodeOrNull<HBoxContainer>("BottomMenu");
		_mapDisplayButton = GetNodeOrNull<Button>("BottomMenu/DockLeft/MapDisplayButton");
		_backToParentButton = GetNodeOrNull<Button>("BottomMenu/DockLeft/BackToParentButton");
		_settingsMenuButton = GetNodeOrNull<Button>("BottomMenu/DockRight/SettingsMenuButton");
		_settingsDropdown = GetNodeOrNull<PanelContainer>("BottomMenu/DockRight/SettingsMenuButton/SettingsDropdown");
		_mapDropdownRegenerateButton = GetNodeOrNull<Button>("BottomMenu/DockRight/SettingsMenuButton/SettingsDropdown/DropdownVBox/RegenerateDropdownButton");
		_mapDropdownSettingsButton = GetNodeOrNull<Button>("BottomMenu/DockRight/SettingsMenuButton/SettingsDropdown/DropdownVBox/MapSettingsDropdownButton");
	}

	public void Initialize(
		Action onMapDisplayRequested,
		Action onBackToParentRequested,
		Action onRegenerateRequested,
		Action onMapSettingsRequested)
	{
		_onMapDisplayRequested = onMapDisplayRequested;
		_onBackToParentRequested = onBackToParentRequested;
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

		if (_backToParentButton != null)
		{
			_backToParentButton.Pressed += OnBackToParentButtonPressed;
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
		if (_settingsDropdown != null)
		{
			_settingsDropdown.Visible = false;
		}
	}

	public bool IsPointerInsideMenu(Vector2 globalPosition)
	{
		return IsPointInsideControl(_bottomDockBackground, globalPosition)
			|| IsPointInsideControl(_bottomMenu, globalPosition)
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

		if (_backToParentButton != null)
		{
			_backToParentButton.Text = tm.Tr("back_to_parent_map");
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
		HideDropdowns();
		_onMapDisplayRequested?.Invoke();
	}

	private void OnSettingsMenuButtonPressed()
	{
		ToggleDropdown(_settingsDropdown);
	}

	private void OnBackToParentButtonPressed()
	{
		HideDropdowns();
		_onBackToParentRequested?.Invoke();
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

	public void UpdateBackToParentButton(bool enabled)
	{
		if (_backToParentButton != null)
		{
			_backToParentButton.Disabled = !enabled;
		}
	}
}
