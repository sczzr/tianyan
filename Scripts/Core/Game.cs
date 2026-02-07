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
	private ColorRect _background;
	private Window _rootWindow;
	private MapView _mapView;
	private HBoxContainer _topMenu;
	private Button _mapMenuButton;
	private Button _systemMenuButton;
	private PanelContainer _mapDropdown;
	private PanelContainer _systemDropdown;
	private Button _mapDropdownRegenerateButton;
	private Button _mapDropdownSettingsButton;
	private Button _systemDropdownSettingsButton;
	private Button _systemDropdownMainMenuButton;
	private Button _systemDropdownQuitButton;
	private PanelContainer _menuPanel;
	private PanelContainer _mapSettingsPopup;
	private VBoxContainer _menuVBox;
	private Label _pausedLabel;
	private Label _generationSettingsLabel;
	private Label _mapWidthLabel;
	private Label _mapHeightLabel;
	private Label _cellCountLabel;
	private Label _riverDensityLabel;
	private Label _countrySectionLabel;
	private Label _countryCountLabel;
	private Label _countryBorderWidthLabel;
	private Label _countryFillAlphaLabel;
	private Label _countryBorderColorLabel;
	private Label _mapDisplayTitleLabel;
	private Label _layerSectionLabel;
	private Label _overlaySectionLabel;
	private Label _themeSectionLabel;
	private Label _themeLabel;
	private SpinBox _mapWidthSpinBox;
	private SpinBox _mapHeightSpinBox;
	private SpinBox _cellCountSpinBox;
	private SpinBox _countryCountSpinBox;
	private HSlider _riverDensitySlider;
	private HSlider _countryBorderWidthSlider;
	private HSlider _countryFillAlphaSlider;
	private CheckButton _terrainLayerCheck;
	private CheckButton _showCountriesCheck;
	private CheckButton _showCountryBordersCheck;
	private CheckButton _ecologyLayerCheck;
	private CheckButton _showRiversCheck;
	private CheckButton _showNamesCheck;
	private ColorPickerButton _countryBorderColorPicker;
	private OptionButton _mapThemeSelector;
	private Button _mapSettingsApplyButton;
	private Button _mapSettingsCloseButton;
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

		_background = GetNode<ColorRect>("Background");
		_mapView = GetNode<MapView>("MapView");
		_topMenu = GetNode<HBoxContainer>("TopMenu");
		_mapMenuButton = GetNode<Button>("TopMenu/MapMenuButton");
		_systemMenuButton = GetNode<Button>("TopMenu/SystemMenuButton");
		_mapDropdown = GetNode<PanelContainer>("TopMenu/MapMenuButton/MapDropdown");
		_systemDropdown = GetNode<PanelContainer>("TopMenu/SystemMenuButton/SystemDropdown");
		_mapDropdownRegenerateButton = GetNode<Button>("TopMenu/MapMenuButton/MapDropdown/DropdownVBox/RegenerateDropdownButton");
		_mapDropdownSettingsButton = GetNode<Button>("TopMenu/MapMenuButton/MapDropdown/DropdownVBox/MapSettingsDropdownButton");
		_systemDropdownSettingsButton = GetNode<Button>("TopMenu/SystemMenuButton/SystemDropdown/DropdownVBox/SettingsDropdownButton");
		_systemDropdownMainMenuButton = GetNode<Button>("TopMenu/SystemMenuButton/SystemDropdown/DropdownVBox/MainMenuDropdownButton");
		_systemDropdownQuitButton = GetNode<Button>("TopMenu/SystemMenuButton/SystemDropdown/DropdownVBox/QuitDropdownButton");
		
		// 获取菜单面板
		_menuPanel = GetNode<PanelContainer>("MenuPanel");
		_mapSettingsPopup = GetNode<PanelContainer>("MapSettingsPopup");
		_menuVBox = GetNode<VBoxContainer>("MenuPanel/MenuVBox");
		_pausedLabel = GetNode<Label>("MenuPanel/MenuVBox/PausedLabel");
		_generationSettingsLabel = GetNode<Label>("MapSettingsPopup/PopupVBox/GenerationSettingsLabel");
		_mapWidthLabel = GetNode<Label>("MapSettingsPopup/PopupVBox/MapSizeHBox/MapWidthLabel");
		_mapWidthSpinBox = GetNode<SpinBox>("MapSettingsPopup/PopupVBox/MapSizeHBox/MapWidthSpinBox");
		_mapHeightLabel = GetNode<Label>("MapSettingsPopup/PopupVBox/MapSizeHBox/MapHeightLabel");
		_mapHeightSpinBox = GetNode<SpinBox>("MapSettingsPopup/PopupVBox/MapSizeHBox/MapHeightSpinBox");
		_cellCountLabel = GetNode<Label>("MapSettingsPopup/PopupVBox/CellCountHBox/CellCountLabel");
		_cellCountSpinBox = GetNode<SpinBox>("MapSettingsPopup/PopupVBox/CellCountHBox/CellCountSpinBox");
		_riverDensityLabel = GetNode<Label>("MapSettingsPopup/PopupVBox/RiverDensityHBox/RiverDensityLabel");
		_riverDensitySlider = GetNode<HSlider>("MapSettingsPopup/PopupVBox/RiverDensityHBox/RiverDensitySlider");
		_countrySectionLabel = GetNode<Label>("MapSettingsPopup/PopupVBox/CountrySectionLabel");
		_countryCountLabel = GetNode<Label>("MapSettingsPopup/PopupVBox/CountryCountHBox/CountryCountLabel");
		_countryCountSpinBox = GetNode<SpinBox>("MapSettingsPopup/PopupVBox/CountryCountHBox/CountryCountSpinBox");
		_mapDisplayTitleLabel = GetNode<Label>("MapDisplayPanel/MapDisplayVBox/MapDisplayTitle");
		_layerSectionLabel = GetNode<Label>("MapDisplayPanel/MapDisplayVBox/LayerSectionLabel");
		_overlaySectionLabel = GetNode<Label>("MapDisplayPanel/MapDisplayVBox/OverlaySectionLabel");
		_themeSectionLabel = GetNode<Label>("MapDisplayPanel/MapDisplayVBox/ThemeSectionLabel");
		_themeLabel = GetNode<Label>("MapDisplayPanel/MapDisplayVBox/ThemeSelectorHBox/ThemeLabel");
		_terrainLayerCheck = GetNode<CheckButton>("MapDisplayPanel/MapDisplayVBox/TerrainLayerCheck");
		_showCountriesCheck = GetNode<CheckButton>("MapDisplayPanel/MapDisplayVBox/CountryLayerCheck");
		_ecologyLayerCheck = GetNode<CheckButton>("MapDisplayPanel/MapDisplayVBox/EcologyLayerCheck");
		_showCountryBordersCheck = GetNode<CheckButton>("MapDisplayPanel/MapDisplayVBox/ShowBordersCheck");
		_countryBorderWidthLabel = GetNode<Label>("MapDisplayPanel/MapDisplayVBox/CountryBorderWidthHBox/CountryBorderWidthLabel");
		_countryBorderWidthSlider = GetNode<HSlider>("MapDisplayPanel/MapDisplayVBox/CountryBorderWidthHBox/CountryBorderWidthSlider");
		_countryFillAlphaLabel = GetNode<Label>("MapDisplayPanel/MapDisplayVBox/CountryFillAlphaHBox/CountryFillAlphaLabel");
		_countryFillAlphaSlider = GetNode<HSlider>("MapDisplayPanel/MapDisplayVBox/CountryFillAlphaHBox/CountryFillAlphaSlider");
		_countryBorderColorLabel = GetNode<Label>("MapDisplayPanel/MapDisplayVBox/CountryBorderColorHBox/CountryBorderColorLabel");
		_countryBorderColorPicker = GetNode<ColorPickerButton>("MapDisplayPanel/MapDisplayVBox/CountryBorderColorHBox/CountryBorderColorPicker");
		_showRiversCheck = GetNode<CheckButton>("MapDisplayPanel/MapDisplayVBox/ShowRiversCheck");
		_showNamesCheck = GetNode<CheckButton>("MapDisplayPanel/MapDisplayVBox/ShowNamesCheck");
		_mapThemeSelector = GetNode<OptionButton>("MapDisplayPanel/MapDisplayVBox/ThemeSelectorHBox/MapThemeSelector");
		_mapSettingsApplyButton = GetNode<Button>("MapSettingsPopup/PopupVBox/PopupButtonsHBox/ApplyButton");
		_mapSettingsCloseButton = GetNode<Button>("MapSettingsPopup/PopupVBox/PopupButtonsHBox/CloseButton");
		_mapViewScaleLabel = GetNode<Label>("MenuPanel/MenuVBox/MapViewScaleLabel");
		_resumeButton = GetNode<Button>("MenuPanel/MenuVBox/ResumeButton");
		_regenerateButton = GetNode<Button>("MenuPanel/MenuVBox/RegenerateButton");
		_settingsButton = GetNode<Button>("MenuPanel/MenuVBox/SettingsButton");
		_mainMenuButton = GetNode<Button>("MenuPanel/MenuVBox/MainMenuButton");
		_quitButton = GetNode<Button>("MenuPanel/MenuVBox/QuitButton");
		_mapViewScaleSlider = GetNode<HSlider>("MenuPanel/MenuVBox/MapViewScaleSlider");

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

		if (_generationSettingsLabel != null)
		{
			_generationSettingsLabel.Text = tm.Tr("generation_settings");
		}

		if (_mapWidthLabel != null)
		{
			_mapWidthLabel.Text = tm.Tr("map_width");
		}

		if (_mapHeightLabel != null)
		{
			_mapHeightLabel.Text = tm.Tr("map_height");
		}

		if (_cellCountLabel != null)
		{
			_cellCountLabel.Text = tm.Tr("cell_count");
		}

		if (_countrySectionLabel != null)
		{
			_countrySectionLabel.Text = tm.Tr("country_settings");
		}

		if (_countryCountLabel != null)
		{
			_countryCountLabel.Text = tm.Tr("country_count");
		}

		if (_countryBorderColorLabel != null)
		{
			_countryBorderColorLabel.Text = tm.Tr("country_border_color");
		}

		if (_mapDisplayTitleLabel != null)
		{
			_mapDisplayTitleLabel.Text = tm.Tr("map_display");
		}

		if (_layerSectionLabel != null)
		{
			_layerSectionLabel.Text = tm.Tr("map_layers");
		}

		if (_overlaySectionLabel != null)
		{
			_overlaySectionLabel.Text = tm.Tr("map_overlay");
		}

		if (_themeSectionLabel != null)
		{
			_themeSectionLabel.Text = tm.Tr("map_theme");
		}

		if (_themeLabel != null)
		{
			_themeLabel.Text = tm.Tr("map_theme_style");
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

		if (_mapSettingsApplyButton != null)
		{
			_mapSettingsApplyButton.Text = tm.Tr("apply_generate");
		}

		if (_mapSettingsCloseButton != null)
		{
			_mapSettingsCloseButton.Text = tm.Tr("close");
		}

		RefreshMapThemeOptions();
		UpdateRiverDensityLabel();
		UpdateCountryBorderWidthLabel();
		UpdateCountryFillAlphaLabel();
		UpdateMapDisplayLabels();
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

		SetupGenerationSettings();
		SetupMapDisplaySettings();

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

	private void SetupGenerationSettings()
	{
		if (_mapView != null)
		{
			_mapView.AutoRegenerate = false;
		}

		if (_mapWidthSpinBox != null)
		{
			_mapWidthSpinBox.Value = _mapView?.MapWidth ?? 512;
			_mapWidthSpinBox.ValueChanged += OnMapWidthChanged;
		}

		if (_mapHeightSpinBox != null)
		{
			_mapHeightSpinBox.Value = _mapView?.MapHeight ?? 512;
			_mapHeightSpinBox.ValueChanged += OnMapHeightChanged;
		}

		if (_cellCountSpinBox != null)
		{
			_cellCountSpinBox.Value = _mapView?.CellCount ?? 2000;
			_cellCountSpinBox.ValueChanged += OnCellCountChanged;
		}

		if (_riverDensitySlider != null)
		{
			_riverDensitySlider.Value = _mapView?.RiverDensity ?? 1f;
			_riverDensitySlider.ValueChanged += OnRiverDensityChanged;
		}

		if (_countryCountSpinBox != null)
		{
			_countryCountSpinBox.Value = _mapView?.CountryCount ?? 12;
			_countryCountSpinBox.ValueChanged += OnCountryCountChanged;
		}

		if (_mapSettingsApplyButton != null)
		{
			_mapSettingsApplyButton.Pressed += OnMapSettingsApplyPressed;
		}

		if (_mapSettingsCloseButton != null)
		{
			_mapSettingsCloseButton.Pressed += OnMapSettingsClosePressed;
		}

		UpdateRiverDensityLabel();
	}

	private void SetupMapDisplaySettings()
	{
		if (_terrainLayerCheck != null)
		{
			_terrainLayerCheck.ButtonPressed = _mapView?.ShowTerrainLayer ?? true;
			_terrainLayerCheck.Toggled += OnTerrainLayerToggled;
		}

		if (_showCountriesCheck != null)
		{
			_showCountriesCheck.ButtonPressed = _mapView?.ShowCountries ?? true;
			_showCountriesCheck.Toggled += OnShowCountriesToggled;
		}

		if (_ecologyLayerCheck != null)
		{
			_ecologyLayerCheck.ButtonPressed = _mapView?.UseBiomeColors ?? true;
			_ecologyLayerCheck.Toggled += OnEcologyLayerToggled;
		}

		if (_showCountryBordersCheck != null)
		{
			_showCountryBordersCheck.ButtonPressed = _mapView?.ShowCountryBorders ?? true;
			_showCountryBordersCheck.Toggled += OnShowCountryBordersToggled;
		}

		if (_countryBorderWidthSlider != null)
		{
			_countryBorderWidthSlider.Value = _mapView?.CountryBorderWidth ?? 2f;
			_countryBorderWidthSlider.ValueChanged += OnCountryBorderWidthChanged;
		}

		if (_countryFillAlphaSlider != null)
		{
			_countryFillAlphaSlider.Value = _mapView?.CountryFillAlpha ?? 0.85f;
			_countryFillAlphaSlider.ValueChanged += OnCountryFillAlphaChanged;
		}

		if (_countryBorderColorPicker != null)
		{
			_countryBorderColorPicker.Color = _mapView?.CountryBorderColor ?? new Color(0.12f, 0.1f, 0.08f, 0.9f);
			_countryBorderColorPicker.ColorChanged += OnCountryBorderColorChanged;
		}

		if (_showRiversCheck != null)
		{
			_showRiversCheck.ButtonPressed = _mapView?.ShowRivers ?? true;
			_showRiversCheck.Toggled += OnShowRiversToggled;
		}

		if (_showNamesCheck != null)
		{
			_showNamesCheck.ButtonPressed = _mapView?.ShowNames ?? true;
			_showNamesCheck.Toggled += OnShowNamesToggled;
		}

		if (_mapThemeSelector != null)
		{
			RefreshMapThemeOptions();
			_mapThemeSelector.ItemSelected += OnMapThemeSelected;
		}

		UpdateCountryBorderWidthLabel();
		UpdateCountryFillAlphaLabel();
		UpdateMapDisplayLabels();
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

	private void OnMapWidthChanged(double value)
	{
		if (_mapView != null)
		{
			_mapView.MapWidth = (int)value;
		}
	}

	private void OnMapHeightChanged(double value)
	{
		if (_mapView != null)
		{
			_mapView.MapHeight = (int)value;
		}
	}

	private void OnCellCountChanged(double value)
	{
		if (_mapView != null)
		{
			_mapView.CellCount = (int)value;
		}
	}

	private void OnRiverDensityChanged(double value)
	{
		if (_mapView != null)
		{
			_mapView.RiverDensity = (float)value;
		}
		UpdateRiverDensityLabel();
	}

	private void OnCountryCountChanged(double value)
	{
		if (_mapView != null)
		{
			_mapView.CountryCount = (int)value;
		}
	}

	private void OnShowCountriesToggled(bool pressed)
	{
		if (_mapView != null)
		{
			_mapView.ShowCountries = pressed;
		}
	}

	private void OnShowCountryBordersToggled(bool pressed)
	{
		if (_mapView != null)
		{
			_mapView.ShowCountryBorders = pressed;
		}
	}

	private void OnTerrainLayerToggled(bool pressed)
	{
		if (_mapView != null)
		{
			_mapView.ShowTerrainLayer = pressed;
		}
	}

	private void OnEcologyLayerToggled(bool pressed)
	{
		if (_mapView != null)
		{
			_mapView.UseBiomeColors = pressed;
			_mapView.QueueRedraw();
		}
	}

	private void OnShowRiversToggled(bool pressed)
	{
		if (_mapView != null)
		{
			_mapView.ShowRivers = pressed;
		}
	}

	private void OnShowNamesToggled(bool pressed)
	{
		if (_mapView != null)
		{
			_mapView.ShowNames = pressed;
		}
	}

	private void OnMapThemeSelected(long index)
	{
		if (_mapView == null || _mapThemeSelector == null)
		{
			return;
		}

		_mapView.TerrainStyleMode = (MapView.TerrainStyle)_mapThemeSelector.GetItemId((int)index);
	}

	private void OnCountryBorderWidthChanged(double value)
	{
		if (_mapView != null)
		{
			_mapView.CountryBorderWidth = (float)value;
		}
		UpdateCountryBorderWidthLabel();
	}

	private void OnCountryFillAlphaChanged(double value)
	{
		if (_mapView != null)
		{
			_mapView.CountryFillAlpha = (float)value;
		}
		UpdateCountryFillAlphaLabel();
	}

	private void OnCountryBorderColorChanged(Color color)
	{
		if (_mapView != null)
		{
			_mapView.CountryBorderColor = color;
			_mapView.QueueRedraw();
		}
	}

	private void UpdateRiverDensityLabel()
	{
		if (_riverDensityLabel == null || _riverDensitySlider == null)
		{
			return;
		}

		var tm = TranslationManager.Instance;
		string densityText = _riverDensitySlider.Value.ToString("0.00");
		_riverDensityLabel.Text = tm.TrWithFormat("river_density", densityText);
	}

	private void UpdateCountryBorderWidthLabel()
	{
		if (_countryBorderWidthLabel == null || _countryBorderWidthSlider == null)
		{
			return;
		}

		var tm = TranslationManager.Instance;
		string widthText = _countryBorderWidthSlider.Value.ToString("0.0");
		_countryBorderWidthLabel.Text = tm.TrWithFormat("country_border_width", widthText);
	}

	private void UpdateCountryFillAlphaLabel()
	{
		if (_countryFillAlphaLabel == null || _countryFillAlphaSlider == null)
		{
			return;
		}

		var tm = TranslationManager.Instance;
		string alphaText = _countryFillAlphaSlider.Value.ToString("0.00");
		_countryFillAlphaLabel.Text = tm.TrWithFormat("country_fill_alpha", alphaText);
	}

	private void UpdateMapDisplayLabels()
	{
		var tm = TranslationManager.Instance;
		if (_terrainLayerCheck != null)
		{
			_terrainLayerCheck.Text = tm.Tr("map_layer_terrain");
		}

		if (_showCountriesCheck != null)
		{
			_showCountriesCheck.Text = tm.Tr("map_layer_countries");
		}

		if (_ecologyLayerCheck != null)
		{
			_ecologyLayerCheck.Text = tm.Tr("map_layer_ecology");
		}

		if (_showCountryBordersCheck != null)
		{
			_showCountryBordersCheck.Text = tm.Tr("show_country_borders");
		}

		if (_showRiversCheck != null)
		{
			_showRiversCheck.Text = tm.Tr("map_show_rivers");
		}

		if (_showNamesCheck != null)
		{
			_showNamesCheck.Text = tm.Tr("map_show_names");
		}
	}

	private void RefreshMapThemeOptions()
	{
		if (_mapThemeSelector == null)
		{
			return;
		}

		var tm = TranslationManager.Instance;
		var selected = _mapThemeSelector.Selected;
		_mapThemeSelector.Clear();
		_mapThemeSelector.AddItem(tm.Tr("map_theme_heightmap"), (int)MapView.TerrainStyle.Heightmap);
		_mapThemeSelector.AddItem(tm.Tr("map_theme_contour"), (int)MapView.TerrainStyle.Contour);
		_mapThemeSelector.AddItem(tm.Tr("map_theme_heatmap"), (int)MapView.TerrainStyle.Heatmap);

		if (_mapView != null)
		{
			_mapThemeSelector.Selected = (int)_mapView.TerrainStyleMode;
		}
		else if (selected >= 0 && selected < _mapThemeSelector.ItemCount)
		{
			_mapThemeSelector.Selected = selected;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.Escape)
		{
			if (_mapSettingsPopup != null && _mapSettingsPopup.Visible)
			{
				HideMapSettingsPopup();
				return;
			}

			HideDropdowns();
			ToggleMenu();
		}

		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (_mapSettingsPopup != null && _mapSettingsPopup.Visible && !IsPointInsideControl(_mapSettingsPopup, mouseButton.Position))
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

	private void OnMapDropdownSettingsPressed()
	{
		HideDropdowns();
		ShowMapSettingsPopup();
	}

	private void OnMapSettingsApplyPressed()
	{
		HideMapSettingsPopup();
		_mapView?.GenerateMap();
	}

	private void OnMapSettingsClosePressed()
	{
		HideMapSettingsPopup();
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
		if (_mapSettingsPopup != null)
		{
			_mapSettingsPopup.Visible = true;
		}
	}

	private void HideMapSettingsPopup()
	{
		if (_mapSettingsPopup != null)
		{
			_mapSettingsPopup.Visible = false;
		}
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
