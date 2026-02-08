using System;
using Godot;
using FantasyMapGenerator.Scripts.Map;
using FantasyMapGenerator.Scripts.Rendering;
using FantasyMapGenerator.Scripts.UI;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Core;

/// <summary>
/// 游戏主界面
/// </summary>
public partial class Game : Control
{
	private const int DefaultCellCount = 2000;
	private const bool DefaultEnableMapDrilldown = true;
	private const int GlobalCityCellCount = 15000;
	private const int NationalCountyCellCount = 3000;

	private ColorRect _background;
	private Window _rootWindow;
	private MapView _mapView;
	private MapHierarchyController _mapHierarchyController;
	private MapLevel _rootMapLevel = MapLevel.World;
	private BottomMenuController _bottomMenuController;
	private MapDisplayPanelController _mapDisplayPanelController;
	private MenuController _menuController;

	private bool _isMenuVisible;
	private TranslationManager _translationManager;
	private MapHierarchyConfig _mapHierarchyConfig;
	private bool _enableMapDrilldown = DefaultEnableMapDrilldown;

	private enum MapType
	{
		GlobalCity = 0,
		NationalCounty = 1,
		Custom = 2
	}

	public override void _Ready()
	{
		_translationManager = TranslationManager.Instance;
		_translationManager.LanguageChanged += OnLanguageChanged;

		_background = GetNode<ColorRect>("MapDisplay/Background");
		_mapView = GetNode<MapView>("MapDisplay/MapView");
		InitializeMapHierarchy();
		_mapDisplayPanelController = GetNodeOrNull<MapDisplayPanelController>("GameUI/MapDisplayPanelUI");
		_bottomMenuController = GetNodeOrNull<BottomMenuController>("GameUI/BottomMenuUI");
		
		// 获取菜单面板
		_menuController = GetNodeOrNull<MenuController>("GameUI/MenuUI");

		SetupUI();
		UpdateUIText();

		_rootWindow = GetTree().Root;
		if (_rootWindow != null)
		{
			_rootWindow.SizeChanged += OnWindowSizeChanged;
		}
		OnWindowSizeChanged();
	}

	public override void _ExitTree()
	{
		if (_rootWindow != null)
		{
			_rootWindow.SizeChanged -= OnWindowSizeChanged;
		}

		if (_mapView != null)
		{
			var cellSelectedCallable = new Callable(this, nameof(OnMapCellSelected));
			if (_mapView.IsConnected(MapView.SignalName.CellSelected, cellSelectedCallable))
			{
				_mapView.Disconnect(MapView.SignalName.CellSelected, cellSelectedCallable);
			}
		}
	}

	private void OnWindowSizeChanged()
	{
		var targetSize = _rootWindow?.Size ?? GetViewportRect().Size;
		Size = targetSize;
		if (_background != null)
		{
			_background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			_background.Size = targetSize;
		}
		if (_mapView != null)
		{
			_mapView.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			_mapView.Size = targetSize;
		}
	}

	private void SyncMapGenerationSizeToWindow()
	{
		if (_mapView == null)
		{
			return;
		}

		var targetSize = _rootWindow?.Size ?? GetViewportRect().Size;
		int width = Mathf.Clamp(Mathf.RoundToInt(targetSize.X), 128, 4096);
		int height = Mathf.Clamp(Mathf.RoundToInt(targetSize.Y), 128, 4096);

		if (_mapView.MapWidth != width)
		{
			_mapView.MapWidth = width;
		}

		if (_mapView.MapHeight != height)
		{
			_mapView.MapHeight = height;
		}
	}

	private void RegenerateMapForWindow()
	{
		SyncMapGenerationSizeToWindow();
		if (_mapHierarchyController != null)
		{
			if (_mapHierarchyController.HasContext)
			{
				_mapHierarchyController.RegenerateCurrentMap();
			}
			else
			{
				ApplyRootContextAndGenerate();
			}
			UpdateBackToParentButton();
			return;
		}

		_mapView?.GenerateMap();
	}

	private void OnLanguageChanged(string language)
	{
		UpdateUIText();
	}

	private void UpdateUIText()
	{
		_bottomMenuController?.UpdateUIText();
		_mapDisplayPanelController?.UpdateUIText();
		_menuController?.UpdateUIText();
	}

	private void SetupUI()
	{
		SetupBottomMenu();

		if (_menuController != null)
		{
			_menuController.Initialize(_mapView, _mapHierarchyConfig, _enableMapDrilldown);
			_menuController.OnResumeRequested += OnResumePressed;
			_menuController.OnRegenerateRequested += OnRegeneratePressed;
			_menuController.OnSettingsRequested += OnSettingsPressed;
			_menuController.OnMainMenuRequested += OnMainMenuPressed;
			_menuController.OnQuitRequested += OnQuitPressed;
			_menuController.OnBackToParentRequested += OnBackToParentPressed;
			_menuController.OnApplySettingsRequested += OnMapSettingsApplyPressed;
			_menuController.OnResetSettingsRequested += OnMapSettingsResetPressed;
			_menuController.OnCloseSettingsRequested += OnMapSettingsClosePressed;
			_menuController.OnMapViewScaleChanged += OnMapViewScaleChanged;
			_menuController.OnMapTypeSelectionChanged += OnMapTypeSelectionChanged;
		}

		InitializeMenuSettings();
		_mapDisplayPanelController?.Initialize(_mapView);

		_isMenuVisible = false;
		_menuController?.SetMenuVisible(false);
		HideDropdowns();
		UpdateBackToParentButton();
	}

	private void InitializeMenuSettings()
	{
		if (_menuController == null)
		{
			return;
		}

		bool settingsLoaded = _menuController.LoadMapSettings();
		_enableMapDrilldown = _menuController.IsEnableDrilldownChecked;
		RefreshRootMapLevelFromSelector();
		ApplyRootContextAndGenerate();
		if (!settingsLoaded)
		{
			_menuController.SaveMapSettings(_enableMapDrilldown);
		}
	}

	private void SetupBottomMenu()
	{
		_bottomMenuController?.Initialize(
			OnDisplayTabSelected,
			OnMapDropdownRegeneratePressed,
			OnMapDropdownSettingsPressed,
			OnSystemDropdownSettingsPressed,
			OnSystemDropdownMainMenuPressed,
			OnSystemDropdownQuitPressed);
		HideDropdowns();
	}

	private void ToggleMenu()
	{
		HideDropdowns();
		_isMenuVisible = !_isMenuVisible;
		_menuController?.SetMenuVisible(_isMenuVisible);
	}

	private void OnResumePressed()
	{
		HideDropdowns();
		_isMenuVisible = false;
		_menuController?.SetMenuVisible(false);
	}

	private void OnRegeneratePressed()
	{
		// 隐藏菜单并重新生成地图
		HideDropdowns();
		_isMenuVisible = false;
		_menuController?.SetMenuVisible(false);
		
		// 触发地图重新生成
		RegenerateMapForWindow();
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
		_menuController?.UpdateMapViewScaleLabel(value);
	}

	private void OnMapTypeSelectionChanged(MapTypeSelection selection)
	{
		var type = selection switch
		{
			MapTypeSelection.GlobalCity => MapType.GlobalCity,
			MapTypeSelection.NationalCounty => MapType.NationalCounty,
			_ => MapType.Custom
		};

		ApplyMapTypePreset(type);
	}

	private void ApplyMapTypePreset(MapType type)
	{
		if (_mapView == null)
		{
			return;
		}

		_rootMapLevel = ResolveRootLevelFromMapType(type);

		int preset;
		switch (type)
		{
			case MapType.GlobalCity:
				preset = _mapHierarchyConfig?.WorldCellCount ?? GlobalCityCellCount;
				break;
			case MapType.NationalCounty:
				preset = _mapHierarchyConfig?.CountryCellCount ?? NationalCountyCellCount;
				break;
			case MapType.Custom:
			default:
				return;
		}

		_menuController?.UpdateCellCountValue(preset);
		_mapView.CellCount = preset;
	}
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Escape)
		{
			if (_menuController != null && _menuController.IsMapSettingsVisible)
			{
				HideMapSettingsPopup();
				return;
			}

			HideDropdowns();
			ToggleMenu();
		}

		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (_menuController != null && _menuController.IsMapSettingsVisible && !_menuController.IsPointInsideSettings(mouseButton.Position))
			{
				HideMapSettingsPopup();
				return;
			}

			if (!IsPointerInsideMenu(mouseButton.Position))
			{
				HideDropdowns();
			}
		}
	}

	private void HideDropdowns()
	{
		_bottomMenuController?.HideDropdowns();
	}

	private void OnDisplayTabSelected(int tabIndex)
	{
		HideDropdowns();
		_mapDisplayPanelController?.OnDisplayTabSelected(tabIndex);
	}

	private void OnMapDropdownRegeneratePressed()
	{
		HideDropdowns();
		OnRegeneratePressed();
	}

	private void OnMapDropdownSettingsPressed()
	{
		HideDropdowns();
		ShowMapSettingsPopup();
	}

	private void OnMapSettingsApplyPressed()
	{
		HideMapSettingsPopup();
		_menuController?.ApplyCurrentValuesToMap();
		_enableMapDrilldown = _menuController?.IsEnableDrilldownChecked ?? _enableMapDrilldown;
		_menuController?.SaveMapSettings(_enableMapDrilldown);
		RefreshRootMapLevelFromSelector();
		RegenerateMapForWindow();
		_menuController?.ShowMapSettingsNotice("map_settings_saved");
	}

	private void OnMapSettingsResetPressed()
	{
		_menuController?.ApplyDefaultMapSettings();
		_menuController?.ApplyCurrentValuesToMap();
		_enableMapDrilldown = _menuController?.IsEnableDrilldownChecked ?? _enableMapDrilldown;
		_menuController?.SaveMapSettings(_enableMapDrilldown);
		RefreshRootMapLevelFromSelector();
		RegenerateMapForWindow();
		_menuController?.ShowMapSettingsNotice("map_settings_saved");
	}

	private void OnMapSettingsClosePressed()
	{
		HideMapSettingsPopup();
	}

	private void InitializeMapHierarchy()
	{
		if (_mapView == null)
		{
			return;
		}

		_mapHierarchyConfig = new MapHierarchyConfig
		{
			WorldCellCount = GlobalCityCellCount,
			CountryCellCount = NationalCountyCellCount,
			ProvinceCellCount = DefaultCellCount,
			CityCellCount = DefaultCellCount
		};

		_mapHierarchyController = new MapHierarchyController(_mapView, _mapHierarchyConfig);

		var cellSelectedCallable = new Callable(this, nameof(OnMapCellSelected));
		if (!_mapView.IsConnected(MapView.SignalName.CellSelected, cellSelectedCallable))
		{
			_mapView.Connect(MapView.SignalName.CellSelected, cellSelectedCallable);
		}
	}

	private void OnMapCellSelected(int cellId)
	{
		if (cellId < 0 || _mapHierarchyController == null || !_enableMapDrilldown)
		{
			return;
		}

		_mapHierarchyController.TryEnterChild(cellId);
		UpdateBackToParentButton();
	}

	private void ApplyRootContextAndGenerate()
	{
		if (_mapHierarchyController == null)
		{
			return;
		}

		SyncMapGenerationSizeToWindow();
		int cellCount = _mapView?.CellCount ?? DefaultCellCount;
		_mapHierarchyController.SetRoot(_rootMapLevel, cellCount);
		UpdateBackToParentButton();
	}

	private void RefreshRootMapLevelFromSelector()
	{
		_rootMapLevel = ResolveRootLevelFromMapType(GetSelectedMapType());
	}

	private MapType GetSelectedMapType()
	{
		if (_menuController == null)
		{
			return MapType.Custom;
		}

		return _menuController.GetMapTypeSelection() switch
		{
			MapTypeSelection.GlobalCity => MapType.GlobalCity,
			MapTypeSelection.NationalCounty => MapType.NationalCounty,
			_ => MapType.Custom
		};
	}

	private MapLevel ResolveRootLevelFromMapType(MapType type)
	{
		return type switch
		{
			MapType.GlobalCity => MapLevel.World,
			MapType.NationalCounty => MapLevel.Country,
			MapType.Custom => _rootMapLevel,
			_ => MapLevel.World
		};
	}

	private void OnBackToParentPressed()
	{
		HideDropdowns();
		_isMenuVisible = false;
		_menuController?.SetMenuVisible(false);

		if (_mapHierarchyController == null)
		{
			return;
		}

		if (_mapHierarchyController.TryReturnToParent())
		{
			UpdateBackToParentButton();
		}
	}

	private void UpdateBackToParentButton()
	{
		if (_menuController == null || _mapHierarchyController == null)
		{
			return;
		}

		_menuController.UpdateBackToParentButton(_mapHierarchyController.CanReturnToParent);
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

	private void ShowMapSettingsPopup()
	{
		_menuController?.ShowMapSettingsPopup();
	}

	private void HideMapSettingsPopup()
	{
		_menuController?.HideMapSettingsPopup();
	}

	private bool IsPointerInsideMenu(Vector2 globalPosition)
	{
		bool inBottom = _bottomMenuController != null && _bottomMenuController.IsPointerInsideMenu(globalPosition);
		bool inMenu = _menuController != null && _menuController.IsPointInsideMenu(globalPosition);
		bool inSettings = _menuController != null && _menuController.IsPointInsideSettings(globalPosition);
		return inBottom || inMenu || inSettings;
	}

	private void SwitchToScene(string scenePath)
	{
		SceneNavigator.Instance.NavigateTo(scenePath);
	}
}
