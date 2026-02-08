using System;
using Godot;
using FantasyMapGenerator.Scripts.Map;
using FantasyMapGenerator.Scripts.Rendering;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.UI.Controllers;

/// <summary>
/// 菜单控制器：暂停菜单与地图设置弹窗
/// </summary>
public partial class MenuController : Control
{
	private const string MapSettingsConfigPath = "user://map_settings.cfg";
	private const string MapSettingsSection = "map_generation";

	private PanelContainer _menuPanel;
	private PanelContainer _mapSettingsPopup;
	private Label _pausedLabel;
	private Label _generationSettingsLabel;
	private Label _mapTypeLabel;
	private Label _cellCountLabel;
	private Label _riverDensityLabel;
	private Label _useMultithreadingLabel;
	private Label _countrySectionLabel;
	private Label _countryCountLabel;
	private Label _minCountryCellsLabel;
	private Label _hierarchySectionLabel;
	private Label _worldCellCountLabel;
	private Label _countryCellCountLabel;
	private Label _provinceCellCountLabel;
	private Label _cityCellCountLabel;
	private Label _enableDrilldownLabel;
	private Button _backToParentButton;
	private SpinBox _cellCountSpinBox;
	private OptionButton _mapTypeSelector;
	private SpinBox _countryCountSpinBox;
	private SpinBox _minCountryCellsSpinBox;
	private SpinBox _worldCellCountSpinBox;
	private SpinBox _countryCellCountSpinBox;
	private SpinBox _provinceCellCountSpinBox;
	private SpinBox _cityCellCountSpinBox;
	private HSlider _riverDensitySlider;
	private CheckButton _useMultithreadingCheck;
	private CheckButton _enableDrilldownCheck;
	private Button _mapSettingsApplyButton;
	private Button _mapSettingsResetButton;
	private Button _mapSettingsCloseButton;
	private PanelContainer _mapSettingsToast;
	private Label _mapSettingsToastLabel;
	private Label _mapViewScaleLabel;
	private Button _resumeButton;
	private Button _regenerateButton;
	private Button _settingsButton;
	private Button _mainMenuButton;
	private Button _quitButton;
	private HSlider _mapViewScaleSlider;
	private bool _mapTypeSelectorWired;

	private MapView _mapView;
	private MapHierarchyConfig _mapHierarchyConfig;

	public event Action OnResumeRequested;
	public event Action OnRegenerateRequested;
	public event Action OnSettingsRequested;
	public event Action OnMainMenuRequested;
	public event Action OnQuitRequested;
	public event Action OnBackToParentRequested;
	public event Action OnApplySettingsRequested;
	public event Action OnResetSettingsRequested;
	public event Action OnCloseSettingsRequested;
	public event Action<double> OnMapViewScaleChanged;
	public event Action<MapTypeSelection> OnMapTypeSelectionChanged;

	public bool IsMenuVisible => _menuPanel != null && _menuPanel.Visible;

	public override void _Ready()
	{
		_menuPanel = GetNodeOrNull<PanelContainer>("MenuPanel");
		_mapSettingsPopup = GetNodeOrNull<PanelContainer>("MapSettingsPopup");
		_pausedLabel = GetNodeOrNull<Label>("MenuPanel/MenuVBox/PausedLabel");
		_generationSettingsLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/GenerationSettingsLabel");
		_mapTypeLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/MapTypeHBox/MapTypeLabel");
		_mapTypeSelector = GetNodeOrNull<OptionButton>("MapSettingsPopup/PopupVBox/MapTypeHBox/MapTypeSelector");
		_cellCountLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/CellCountHBox/CellCountLabel");
		_cellCountSpinBox = GetNodeOrNull<SpinBox>("MapSettingsPopup/PopupVBox/CellCountHBox/CellCountSpinBox");
		_riverDensityLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/RiverDensityHBox/RiverDensityLabel");
		_riverDensitySlider = GetNodeOrNull<HSlider>("MapSettingsPopup/PopupVBox/RiverDensityHBox/RiverDensitySlider");
		_useMultithreadingLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/MultithreadingHBox/MultithreadingLabel");
		_useMultithreadingCheck = GetNodeOrNull<CheckButton>("MapSettingsPopup/PopupVBox/MultithreadingHBox/MultithreadingCheck");
		_countrySectionLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/CountrySectionLabel");
		_countryCountLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/CountryCountHBox/CountryCountLabel");
		_countryCountSpinBox = GetNodeOrNull<SpinBox>("MapSettingsPopup/PopupVBox/CountryCountHBox/CountryCountSpinBox");
		_minCountryCellsLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/MinCountryCellsHBox/MinCountryCellsLabel");
		_minCountryCellsSpinBox = GetNodeOrNull<SpinBox>("MapSettingsPopup/PopupVBox/MinCountryCellsHBox/MinCountryCellsSpinBox");
		_hierarchySectionLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/HierarchySectionLabel");
		_worldCellCountLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/WorldCellCountHBox/WorldCellCountLabel");
		_worldCellCountSpinBox = GetNodeOrNull<SpinBox>("MapSettingsPopup/PopupVBox/WorldCellCountHBox/WorldCellCountSpinBox");
		_countryCellCountLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/CountryCellCountHBox/CountryCellCountLabel");
		_countryCellCountSpinBox = GetNodeOrNull<SpinBox>("MapSettingsPopup/PopupVBox/CountryCellCountHBox/CountryCellCountSpinBox");
		_provinceCellCountLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/ProvinceCellCountHBox/ProvinceCellCountLabel");
		_provinceCellCountSpinBox = GetNodeOrNull<SpinBox>("MapSettingsPopup/PopupVBox/ProvinceCellCountHBox/ProvinceCellCountSpinBox");
		_cityCellCountLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/CityCellCountHBox/CityCellCountLabel");
		_cityCellCountSpinBox = GetNodeOrNull<SpinBox>("MapSettingsPopup/PopupVBox/CityCellCountHBox/CityCellCountSpinBox");
		_enableDrilldownLabel = GetNodeOrNull<Label>("MapSettingsPopup/PopupVBox/EnableDrilldownHBox/EnableDrilldownLabel");
		_enableDrilldownCheck = GetNodeOrNull<CheckButton>("MapSettingsPopup/PopupVBox/EnableDrilldownHBox/EnableDrilldownCheck");
		_mapSettingsApplyButton = GetNodeOrNull<Button>("MapSettingsPopup/PopupVBox/PopupButtonsHBox/ApplyButton");
		_mapSettingsResetButton = GetNodeOrNull<Button>("MapSettingsPopup/PopupVBox/PopupButtonsHBox/ResetButton");
		_mapSettingsCloseButton = GetNodeOrNull<Button>("MapSettingsPopup/PopupVBox/PopupButtonsHBox/CloseButton");
		_mapSettingsToast = GetNodeOrNull<PanelContainer>("MapSettingsToast");
		_mapSettingsToastLabel = GetNodeOrNull<Label>("MapSettingsToast/ToastLabel");
		_mapViewScaleLabel = GetNodeOrNull<Label>("MenuPanel/MenuVBox/MapViewScaleLabel");
		_resumeButton = GetNodeOrNull<Button>("MenuPanel/MenuVBox/ResumeButton");
		_regenerateButton = GetNodeOrNull<Button>("MenuPanel/MenuVBox/RegenerateButton");
		_backToParentButton = GetNodeOrNull<Button>("MenuPanel/MenuVBox/BackToParentButton");
		_settingsButton = GetNodeOrNull<Button>("MenuPanel/MenuVBox/SettingsButton");
		_mainMenuButton = GetNodeOrNull<Button>("MenuPanel/MenuVBox/MainMenuButton");
		_quitButton = GetNodeOrNull<Button>("MenuPanel/MenuVBox/QuitButton");
		_mapViewScaleSlider = GetNodeOrNull<HSlider>("MenuPanel/MenuVBox/MapViewScaleSlider");

		WireMenuButtons();
	}

	public void Initialize(MapView mapView, MapHierarchyConfig mapHierarchyConfig, bool enableDrilldown)
	{
		_mapView = mapView;
		_mapHierarchyConfig = mapHierarchyConfig;

		if (_enableDrilldownCheck != null)
		{
			_enableDrilldownCheck.ButtonPressed = enableDrilldown;
			_enableDrilldownCheck.Toggled += OnEnableDrilldownToggled;
		}

		SetupGenerationSettings();
		UpdateUIText();
		BindValueChangeHandlers();
	}

	private void BindValueChangeHandlers()
	{
		if (_cellCountSpinBox != null)
		{
			_cellCountSpinBox.ValueChanged += value =>
			{
				if (_mapView != null)
				{
					_mapView.CellCount = (int)value;
				}
			};
		}

		if (_riverDensitySlider != null)
		{
			_riverDensitySlider.ValueChanged += value =>
			{
				if (_mapView != null)
				{
					_mapView.RiverDensity = (float)value;
				}
				UpdateRiverDensityLabel();
			};
		}

		if (_useMultithreadingCheck != null)
		{
			_useMultithreadingCheck.Toggled += pressed =>
			{
				if (_mapView != null)
				{
					_mapView.UseMultithreading = pressed;
				}
			};
		}

		if (_countryCountSpinBox != null)
		{
			_countryCountSpinBox.ValueChanged += value =>
			{
				if (_mapView != null)
				{
					_mapView.CountryCount = (int)value;
				}
			};
		}

		if (_minCountryCellsSpinBox != null)
		{
			_minCountryCellsSpinBox.ValueChanged += value =>
			{
				if (_mapView != null)
				{
					_mapView.MinCountryCells = (int)value;
				}
			};
		}

		if (_worldCellCountSpinBox != null)
		{
			_worldCellCountSpinBox.ValueChanged += value =>
			{
				if (_mapHierarchyConfig != null)
				{
					_mapHierarchyConfig.WorldCellCount = (int)value;
				}
			};
		}

		if (_countryCellCountSpinBox != null)
		{
			_countryCellCountSpinBox.ValueChanged += value =>
			{
				if (_mapHierarchyConfig != null)
				{
					_mapHierarchyConfig.CountryCellCount = (int)value;
				}
			};
		}

		if (_provinceCellCountSpinBox != null)
		{
			_provinceCellCountSpinBox.ValueChanged += value =>
			{
				if (_mapHierarchyConfig != null)
				{
					_mapHierarchyConfig.ProvinceCellCount = (int)value;
				}
			};
		}

		if (_cityCellCountSpinBox != null)
		{
			_cityCellCountSpinBox.ValueChanged += value =>
			{
				if (_mapHierarchyConfig != null)
				{
					_mapHierarchyConfig.CityCellCount = (int)value;
				}
			};
		}
	}

	public void ApplyCurrentValuesToMap()
	{
		if (_mapView == null)
		{
			return;
		}

		if (_cellCountSpinBox != null)
		{
			_mapView.CellCount = (int)_cellCountSpinBox.Value;
		}

		if (_riverDensitySlider != null)
		{
			_mapView.RiverDensity = (float)_riverDensitySlider.Value;
		}

		if (_useMultithreadingCheck != null)
		{
			_mapView.UseMultithreading = _useMultithreadingCheck.ButtonPressed;
		}

		if (_countryCountSpinBox != null)
		{
			_mapView.CountryCount = (int)_countryCountSpinBox.Value;
		}

		if (_minCountryCellsSpinBox != null)
		{
			_mapView.MinCountryCells = (int)_minCountryCellsSpinBox.Value;
		}

		if (_mapHierarchyConfig != null)
		{
			if (_worldCellCountSpinBox != null)
			{
				_mapHierarchyConfig.WorldCellCount = (int)_worldCellCountSpinBox.Value;
			}

			if (_countryCellCountSpinBox != null)
			{
				_mapHierarchyConfig.CountryCellCount = (int)_countryCellCountSpinBox.Value;
			}

			if (_provinceCellCountSpinBox != null)
			{
				_mapHierarchyConfig.ProvinceCellCount = (int)_provinceCellCountSpinBox.Value;
			}

			if (_cityCellCountSpinBox != null)
			{
				_mapHierarchyConfig.CityCellCount = (int)_cityCellCountSpinBox.Value;
			}
		}
	}

	public void SetMenuVisible(bool visible)
	{
		if (_menuPanel != null)
		{
			_menuPanel.Visible = visible;
		}
	}

	public void ShowMapSettingsPopup()
	{
		if (_mapSettingsPopup != null)
		{
			_mapSettingsPopup.Visible = true;
		}
	}

	public void HideMapSettingsPopup()
	{
		if (_mapSettingsPopup != null)
		{
			_mapSettingsPopup.Visible = false;
		}
	}

	public bool IsMapSettingsVisible => _mapSettingsPopup != null && _mapSettingsPopup.Visible;

	public bool IsPointInsideMenu(Vector2 globalPosition)
	{
		return IsPointInsideControl(_menuPanel, globalPosition);
	}

	public bool IsPointInsideSettings(Vector2 globalPosition)
	{
		return IsPointInsideControl(_mapSettingsPopup, globalPosition);
	}

	public void UpdateMapViewScaleLabel(double sliderValue)
	{
		if (_mapViewScaleLabel == null)
		{
			return;
		}

		var tm = TranslationManager.Instance;
		var percent = Mathf.RoundToInt((float)sliderValue * 100f);
		_mapViewScaleLabel.Text = tm.TrWithFormat("map_view_scale", percent.ToString());
	}

	public void ShowMapSettingsNotice(string textKey)
	{
		if (_mapSettingsToast == null || _mapSettingsToastLabel == null)
		{
			return;
		}

		_mapSettingsToastLabel.Text = TranslationManager.Instance.Tr(textKey);
		_mapSettingsToast.Visible = true;
		_mapSettingsToast.Modulate = new Color(1f, 1f, 1f, 0f);

		var tween = CreateTween();
		tween.TweenProperty(_mapSettingsToast, "modulate", new Color(1f, 1f, 1f, 1f), 0.15f);
		tween.TweenInterval(1.0f);
		tween.TweenProperty(_mapSettingsToast, "modulate", new Color(1f, 1f, 1f, 0f), 0.25f);
		tween.TweenCallback(Callable.From(() =>
		{
			if (_mapSettingsToast != null)
			{
				_mapSettingsToast.Visible = false;
			}
		}));
	}

	public void UpdateUIText()
	{
		var tm = TranslationManager.Instance;
		if (_pausedLabel != null)
		{
			_pausedLabel.Text = tm.Tr("game_paused");
		}

		if (_generationSettingsLabel != null)
		{
			_generationSettingsLabel.Text = tm.Tr("generation_settings");
		}

		if (_mapTypeLabel != null)
		{
			_mapTypeLabel.Text = tm.Tr("map_type");
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

		if (_minCountryCellsLabel != null)
		{
			_minCountryCellsLabel.Text = tm.Tr("min_country_cells");
		}

		if (_hierarchySectionLabel != null)
		{
			_hierarchySectionLabel.Text = tm.Tr("map_hierarchy_settings");
		}

		if (_worldCellCountLabel != null)
		{
			_worldCellCountLabel.Text = tm.Tr("world_cell_count");
		}

		if (_countryCellCountLabel != null)
		{
			_countryCellCountLabel.Text = tm.Tr("country_cell_count");
		}

		if (_provinceCellCountLabel != null)
		{
			_provinceCellCountLabel.Text = tm.Tr("province_cell_count");
		}

		if (_cityCellCountLabel != null)
		{
			_cityCellCountLabel.Text = tm.Tr("city_cell_count");
		}

		if (_enableDrilldownLabel != null)
		{
			_enableDrilldownLabel.Text = tm.Tr("enable_map_drilldown");
		}

		if (_useMultithreadingLabel != null)
		{
			_useMultithreadingLabel.Text = tm.Tr("use_multithreading");
		}

		if (_resumeButton != null)
		{
			_resumeButton.Text = tm.Tr("resume");
		}

		if (_regenerateButton != null)
		{
			_regenerateButton.Text = tm.Tr("regenerate_map");
		}

		if (_backToParentButton != null)
		{
			_backToParentButton.Text = tm.Tr("back_to_parent_map");
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

		if (_mapSettingsApplyButton != null)
		{
			_mapSettingsApplyButton.Text = tm.Tr("apply_generate");
		}

		if (_mapSettingsResetButton != null)
		{
			_mapSettingsResetButton.Text = tm.Tr("reset_map_settings");
		}

		if (_mapSettingsCloseButton != null)
		{
			_mapSettingsCloseButton.Text = tm.Tr("close");
		}

		UpdateMapTypeLabelOptions();
		UpdateRiverDensityLabel();
		UpdateMapViewScaleLabel(_mapViewScaleSlider?.Value ?? 1.0);
	}

	public bool LoadMapSettings()
	{
		if (_mapView == null)
		{
			return false;
		}

		var config = new ConfigFile();
		if (config.Load(MapSettingsConfigPath) != Error.Ok)
		{
			ApplyDefaultMapSettings();
			return false;
		}

		var mapType = (int)ReadInt(config, "map_type", 2);
		if (_mapTypeSelector != null)
		{
			var index = _mapTypeSelector.GetItemIndex(mapType);
			if (index >= 0)
			{
				_mapTypeSelector.Selected = index;
			}
		}

		if (_enableDrilldownCheck != null)
		{
			_enableDrilldownCheck.ButtonPressed = ReadBool(config, "enable_map_drilldown", _enableDrilldownCheck.ButtonPressed);
		}

		_mapView.CellCount = Mathf.Clamp(ReadInt(config, "cell_count", _mapView.CellCount), 200, 20000);
		_mapView.RiverDensity = Mathf.Clamp(ReadFloat(config, "river_density", _mapView.RiverDensity), 0.25f, 3f);
		_mapView.UseMultithreading = ReadBool(config, "use_multithreading", _mapView.UseMultithreading);
		_mapView.CountryCount = ReadInt(config, "country_count", _mapView.CountryCount);
		_mapView.MinCountryCells = ReadInt(config, "min_country_cells", _mapView.MinCountryCells);

		if (_cellCountSpinBox != null)
		{
			_cellCountSpinBox.Value = _mapView.CellCount;
		}

		if (_riverDensitySlider != null)
		{
			_riverDensitySlider.Value = _mapView.RiverDensity;
		}

		if (_useMultithreadingCheck != null)
		{
			_useMultithreadingCheck.ButtonPressed = _mapView.UseMultithreading;
		}

		if (_countryCountSpinBox != null)
		{
			_countryCountSpinBox.Value = _mapView.CountryCount;
		}

		if (_minCountryCellsSpinBox != null)
		{
			_minCountryCellsSpinBox.Value = _mapView.MinCountryCells;
		}

		if (_mapHierarchyConfig != null)
		{
			_mapHierarchyConfig.WorldCellCount = ReadInt(config, "world_cell_count", _mapHierarchyConfig.WorldCellCount);
			_mapHierarchyConfig.CountryCellCount = ReadInt(config, "country_cell_count", _mapHierarchyConfig.CountryCellCount);
			_mapHierarchyConfig.ProvinceCellCount = ReadInt(config, "province_cell_count", _mapHierarchyConfig.ProvinceCellCount);
			_mapHierarchyConfig.CityCellCount = ReadInt(config, "city_cell_count", _mapHierarchyConfig.CityCellCount);
		}

		if (_worldCellCountSpinBox != null)
		{
			_worldCellCountSpinBox.Value = _mapHierarchyConfig?.WorldCellCount ?? _worldCellCountSpinBox.Value;
		}

		if (_countryCellCountSpinBox != null)
		{
			_countryCellCountSpinBox.Value = _mapHierarchyConfig?.CountryCellCount ?? _countryCellCountSpinBox.Value;
		}

		if (_provinceCellCountSpinBox != null)
		{
			_provinceCellCountSpinBox.Value = _mapHierarchyConfig?.ProvinceCellCount ?? _provinceCellCountSpinBox.Value;
		}

		if (_cityCellCountSpinBox != null)
		{
			_cityCellCountSpinBox.Value = _mapHierarchyConfig?.CityCellCount ?? _cityCellCountSpinBox.Value;
		}

		UpdateRiverDensityLabel();
		return true;
	}

	public void SaveMapSettings(bool enableDrilldown)
	{
		if (_mapView == null)
		{
			return;
		}

		var config = new ConfigFile();
		config.Load(MapSettingsConfigPath);
		if (_mapTypeSelector != null)
		{
			var mapType = _mapTypeSelector.GetItemId(_mapTypeSelector.Selected);
			config.SetValue(MapSettingsSection, "map_type", mapType);
		}
		config.SetValue(MapSettingsSection, "cell_count", _mapView.CellCount);
		config.SetValue(MapSettingsSection, "river_density", _mapView.RiverDensity);
		config.SetValue(MapSettingsSection, "use_multithreading", _mapView.UseMultithreading);
		config.SetValue(MapSettingsSection, "country_count", _mapView.CountryCount);
		config.SetValue(MapSettingsSection, "min_country_cells", _mapView.MinCountryCells);
		config.SetValue(MapSettingsSection, "enable_map_drilldown", enableDrilldown);
		if (_mapHierarchyConfig != null)
		{
			config.SetValue(MapSettingsSection, "world_cell_count", _mapHierarchyConfig.WorldCellCount);
			config.SetValue(MapSettingsSection, "country_cell_count", _mapHierarchyConfig.CountryCellCount);
			config.SetValue(MapSettingsSection, "province_cell_count", _mapHierarchyConfig.ProvinceCellCount);
			config.SetValue(MapSettingsSection, "city_cell_count", _mapHierarchyConfig.CityCellCount);
		}
		config.Save(MapSettingsConfigPath);
	}

	public void ApplyDefaultMapSettings()
	{
		if (_mapView == null)
		{
			return;
		}

		_mapView.CellCount = 2000;
		_mapView.RiverDensity = 1f;
		_mapView.UseMultithreading = true;
		_mapView.CountryCount = 12;
		_mapView.MinCountryCells = 3;

		if (_cellCountSpinBox != null)
		{
			_cellCountSpinBox.Value = _mapView.CellCount;
		}

		if (_riverDensitySlider != null)
		{
			_riverDensitySlider.Value = _mapView.RiverDensity;
		}

		if (_useMultithreadingCheck != null)
		{
			_useMultithreadingCheck.ButtonPressed = _mapView.UseMultithreading;
		}

		if (_countryCountSpinBox != null)
		{
			_countryCountSpinBox.Value = _mapView.CountryCount;
		}

		if (_minCountryCellsSpinBox != null)
		{
			_minCountryCellsSpinBox.Value = _mapView.MinCountryCells;
		}

		if (_mapHierarchyConfig != null)
		{
			_mapHierarchyConfig.WorldCellCount = 15000;
			_mapHierarchyConfig.CountryCellCount = 3000;
			_mapHierarchyConfig.ProvinceCellCount = 2000;
			_mapHierarchyConfig.CityCellCount = 2000;
		}

		if (_worldCellCountSpinBox != null)
		{
			_worldCellCountSpinBox.Value = _mapHierarchyConfig?.WorldCellCount ?? 15000;
		}

		if (_countryCellCountSpinBox != null)
		{
			_countryCellCountSpinBox.Value = _mapHierarchyConfig?.CountryCellCount ?? 3000;
		}

		if (_provinceCellCountSpinBox != null)
		{
			_provinceCellCountSpinBox.Value = _mapHierarchyConfig?.ProvinceCellCount ?? 2000;
		}

		if (_cityCellCountSpinBox != null)
		{
			_cityCellCountSpinBox.Value = _mapHierarchyConfig?.CityCellCount ?? 2000;
		}
	}

	public MapTypeSelection GetMapTypeSelection()
	{
		if (_mapTypeSelector == null)
		{
			return MapTypeSelection.Custom;
		}

		return (MapTypeSelection)_mapTypeSelector.GetItemId(_mapTypeSelector.Selected);
	}

	public void SetMapTypeSelection(MapTypeSelection selection)
	{
		if (_mapTypeSelector == null)
		{
			return;
		}

		var index = _mapTypeSelector.GetItemIndex((int)selection);
		if (index >= 0)
		{
			_mapTypeSelector.Selected = index;
		}
	}

	public bool IsEnableDrilldownChecked => _enableDrilldownCheck?.ButtonPressed ?? false;

	public void ApplyMapTypeSelection(MapTypeSelection selection)
	{
		SetMapTypeSelection(selection);
	}

	public void UpdateCellCountValue(int value)
	{
		if (_cellCountSpinBox != null)
		{
			_cellCountSpinBox.Value = value;
		}
	}

	public void UpdateRiverDensityLabel()
	{
		if (_riverDensityLabel == null || _riverDensitySlider == null)
		{
			return;
		}

		var tm = TranslationManager.Instance;
		string densityText = _riverDensitySlider.Value.ToString("0.00");
		_riverDensityLabel.Text = tm.TrWithFormat("river_density", densityText);
	}

	public void SetMapViewScaleValue(float value)
	{
		if (_mapViewScaleSlider != null)
		{
			_mapViewScaleSlider.Value = value;
		}
	}

	public void UpdateBackToParentButton(bool enabled)
	{
		if (_backToParentButton != null)
		{
			_backToParentButton.Disabled = !enabled;
		}
	}

	public int CurrentCellCount => _cellCountSpinBox != null ? (int)_cellCountSpinBox.Value : 0;
	public int WorldCellCount => _worldCellCountSpinBox != null ? (int)_worldCellCountSpinBox.Value : 0;
	public int CountryCellCount => _countryCellCountSpinBox != null ? (int)_countryCellCountSpinBox.Value : 0;
	public int ProvinceCellCount => _provinceCellCountSpinBox != null ? (int)_provinceCellCountSpinBox.Value : 0;
	public int CityCellCount => _cityCellCountSpinBox != null ? (int)_cityCellCountSpinBox.Value : 0;

	private void WireMenuButtons()
	{
		if (_resumeButton != null)
		{
			_resumeButton.Pressed += () => OnResumeRequested?.Invoke();
		}

		if (_regenerateButton != null)
		{
			_regenerateButton.Pressed += () => OnRegenerateRequested?.Invoke();
		}

		if (_backToParentButton != null)
		{
			_backToParentButton.Pressed += () => OnBackToParentRequested?.Invoke();
		}

		if (_settingsButton != null)
		{
			_settingsButton.Pressed += () => OnSettingsRequested?.Invoke();
		}

		if (_mainMenuButton != null)
		{
			_mainMenuButton.Pressed += () => OnMainMenuRequested?.Invoke();
		}

		if (_quitButton != null)
		{
			_quitButton.Pressed += () => OnQuitRequested?.Invoke();
		}

		if (_mapSettingsApplyButton != null)
		{
			_mapSettingsApplyButton.Pressed += () => OnApplySettingsRequested?.Invoke();
		}

		if (_mapSettingsResetButton != null)
		{
			_mapSettingsResetButton.Pressed += () => OnResetSettingsRequested?.Invoke();
		}

		if (_mapSettingsCloseButton != null)
		{
			_mapSettingsCloseButton.Pressed += () => OnCloseSettingsRequested?.Invoke();
		}

		if (_mapViewScaleSlider != null)
		{
			_mapViewScaleSlider.ValueChanged += value => OnMapViewScaleChanged?.Invoke(value);
		}

		if (_riverDensitySlider != null)
		{
			_riverDensitySlider.ValueChanged += _ => UpdateRiverDensityLabel();
		}
	}

	private void OnEnableDrilldownToggled(bool pressed)
	{
		_ = pressed;
	}

	private void SetupGenerationSettings()
	{
		if (_mapView != null)
		{
			_mapView.AutoRegenerate = false;
		}

		WireMapTypeSelector();

		if (_cellCountSpinBox != null)
		{
			_cellCountSpinBox.Value = _mapView?.CellCount ?? 2000;
		}

		if (_riverDensitySlider != null)
		{
			_riverDensitySlider.Value = _mapView?.RiverDensity ?? 1f;
		}

		if (_useMultithreadingCheck != null)
		{
			_useMultithreadingCheck.ButtonPressed = _mapView?.UseMultithreading ?? true;
		}

		if (_countryCountSpinBox != null)
		{
			_countryCountSpinBox.Value = _mapView?.CountryCount ?? 12;
		}

		if (_minCountryCellsSpinBox != null)
		{
			_minCountryCellsSpinBox.Value = _mapView?.MinCountryCells ?? 3;
		}

		if (_worldCellCountSpinBox != null)
		{
			_worldCellCountSpinBox.Value = _mapHierarchyConfig?.WorldCellCount ?? 15000;
		}

		if (_countryCellCountSpinBox != null)
		{
			_countryCellCountSpinBox.Value = _mapHierarchyConfig?.CountryCellCount ?? 3000;
		}

		if (_provinceCellCountSpinBox != null)
		{
			_provinceCellCountSpinBox.Value = _mapHierarchyConfig?.ProvinceCellCount ?? 2000;
		}

		if (_cityCellCountSpinBox != null)
		{
			_cityCellCountSpinBox.Value = _mapHierarchyConfig?.CityCellCount ?? 2000;
		}
	}

	private void WireMapTypeSelector()
	{
		if (_mapTypeSelector == null || _mapTypeSelectorWired)
		{
			return;
		}

		_mapTypeSelectorWired = true;
		_mapTypeSelector.ItemSelected += OnMapTypeSelected;
		UpdateMapTypeLabelOptions();
	}

	private void UpdateMapTypeLabelOptions()
	{
		if (_mapTypeSelector == null)
		{
			return;
		}

		var tm = TranslationManager.Instance;
		var selectedId = _mapTypeSelector.ItemCount > 0
			? _mapTypeSelector.GetItemId(_mapTypeSelector.Selected)
			: (int)MapTypeSelection.Custom;
		_mapTypeSelector.Clear();
		_mapTypeSelector.AddItem(tm.Tr("map_type_global_city"), (int)MapTypeSelection.GlobalCity);
		_mapTypeSelector.AddItem(tm.Tr("map_type_national_county"), (int)MapTypeSelection.NationalCounty);
		_mapTypeSelector.AddItem(tm.Tr("map_type_custom"), (int)MapTypeSelection.Custom);
		var index = _mapTypeSelector.GetItemIndex((int)selectedId);
		_mapTypeSelector.Selected = index >= 0 ? index : _mapTypeSelector.GetItemIndex((int)MapTypeSelection.Custom);
	}

	private void OnMapTypeSelected(long index)
	{
		if (_mapTypeSelector == null)
		{
			return;
		}

		var selection = (MapTypeSelection)_mapTypeSelector.GetItemId((int)index);
		OnMapTypeSelectionChanged?.Invoke(selection);
	}

	private int ReadInt(ConfigFile config, string key, int defaultValue)
	{
		if (!config.HasSectionKey(MapSettingsSection, key))
		{
			return defaultValue;
		}

		var text = config.GetValue(MapSettingsSection, key, defaultValue).AsString();
		if (int.TryParse(text, out var parsed))
		{
			return parsed;
		}

		if (float.TryParse(text, out var parsedFloat))
		{
			return Mathf.RoundToInt(parsedFloat);
		}

		return defaultValue;
	}

	private float ReadFloat(ConfigFile config, string key, float defaultValue)
	{
		if (!config.HasSectionKey(MapSettingsSection, key))
		{
			return defaultValue;
		}

		var text = config.GetValue(MapSettingsSection, key, defaultValue).AsString();
		if (float.TryParse(text, out var parsed))
		{
			return parsed;
		}

		if (int.TryParse(text, out var parsedInt))
		{
			return parsedInt;
		}

		if (bool.TryParse(text, out var parsedBool))
		{
			return parsedBool ? 1f : 0f;
		}

		return defaultValue;
	}

	private bool ReadBool(ConfigFile config, string key, bool defaultValue)
	{
		if (!config.HasSectionKey(MapSettingsSection, key))
		{
			return defaultValue;
		}

		var text = config.GetValue(MapSettingsSection, key, defaultValue).AsString();
		if (bool.TryParse(text, out var parsed))
		{
			return parsed;
		}

		if (int.TryParse(text, out var parsedInt))
		{
			return parsedInt != 0;
		}

		if (float.TryParse(text, out var parsedFloat))
		{
			return Mathf.Abs(parsedFloat) > 0.0001f;
		}

		return defaultValue;
	}

	private static bool IsPointInsideControl(Control control, Vector2 globalPosition)
	{
		return control != null && control.Visible && control.GetGlobalRect().HasPoint(globalPosition);
	}
}

public enum MapTypeSelection
{
	GlobalCity = 0,
	NationalCounty = 1,
	Custom = 2
}
