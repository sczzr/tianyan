using System;
using Godot;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Genesis;
using FantasyMapGenerator.Scripts.Map;
using FantasyMapGenerator.Scripts.Map.Heightmap;
using FantasyMapGenerator.Scripts.Rendering;
using FantasyMapGenerator.Scripts.UI.Controllers;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Core;

/// <summary>
/// 地图生成器主界面
/// </summary>
public partial class MapGeneratorUI : Control
{
	private const int DefaultCellCount = 2000;
	private const bool DefaultEnableMapDrilldown = true;
	private const int GlobalCityCellCount = 15000;
	private const int NationalCountyCellCount = 3000;

	private ColorRect _background;
	private Window _rootWindow;
	private Control _mapDisplayRoot;
	private Control _mapOverlayRoot;
	private MapView _mapView;
	private MapHierarchyController _mapHierarchyController;
	private MapLevel _rootMapLevel = MapLevel.World;
	private BottomMenuController _bottomMenuController;
	private MapDisplayPanelController _mapDisplayPanelController;
	private MenuController _menuController;
	private Control _welcomeOverlay;
	private GenesisController _genesisController;

	private bool _isMenuVisible;
	private bool _isWelcomeMode;
	private bool _isWelcomeGenerating;
	private TranslationManager _translationManager;
	private MapHierarchyConfig _mapHierarchyConfig;
	private bool _enableMapDrilldown = DefaultEnableMapDrilldown;

	private static MapContext _cachedMapContext;

	public static void PrepareNewGameEntry()
	{
		_cachedMapContext = null;
		MapView.ClearCachedMap();
		MapView.SuppressGenerateOnNextReady();
	}

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

		CacheNodeReferences();
		InitializeMapHierarchy();
		SetupWelcomeOverlay();

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

		if (_genesisController != null)
		{
			_genesisController.GenerateRequested -= OnGenesisGenerateRequested;
		}

		if (_mapView != null)
		{
			var cellSelectedCallable = new Callable(this, nameof(OnMapCellSelected));
			if (_mapView.IsConnected(MapView.SignalName.CellSelected, cellSelectedCallable))
			{
				_mapView.Disconnect(MapView.SignalName.CellSelected, cellSelectedCallable);
			}
		}

		if (_mapHierarchyController?.Current != null)
		{
			_cachedMapContext = _mapHierarchyController.Current;
		}
	}

	private void OnWindowSizeChanged()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		if (_mapDisplayRoot != null)
		{
			_mapDisplayRoot.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		}

		if (_mapOverlayRoot != null)
		{
			_mapOverlayRoot.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		}

		if (_background != null)
		{
			_background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		}

		if (_mapView != null)
		{
			_mapView.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		}

		if (_welcomeOverlay != null)
		{
			_welcomeOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
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
		UpdateWelcomeUIText();
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

		bool hasMapData = _mapView?.HasMapData == true;
		if (hasMapData)
		{
			RestoreOrInitializeMapContextWithoutRegeneration();
		}
		else
		{
			PrepareRootContextWithoutGeneration();
		}

		SetWelcomeMode(!hasMapData);

		if (!settingsLoaded)
		{
			_menuController.SaveMapSettings(_enableMapDrilldown);
		}
	}

	private void PrepareRootContextWithoutGeneration()
	{
		if (_mapHierarchyController == null)
		{
			return;
		}

		RefreshRootMapLevelFromSelector();
		int cellCount = _mapView?.CellCount ?? DefaultCellCount;
		_mapHierarchyController.SetRoot(_rootMapLevel, cellCount, regenerate: false);
		_menuController?.UpdateCellCountValue(cellCount);
		UpdateBackToParentButton();
	}

	private void RestoreOrInitializeMapContextWithoutRegeneration()
	{
		if (_mapHierarchyController == null)
		{
			return;
		}

		if (_cachedMapContext != null)
		{
			_mapHierarchyController.RestoreContext(_cachedMapContext);
			_menuController?.UpdateCellCountValue(_cachedMapContext.CellCount);
			UpdateBackToParentButton();
			return;
		}

		RefreshRootMapLevelFromSelector();
		int cellCount = _mapView?.CellCount ?? DefaultCellCount;
		_mapHierarchyController.SetRoot(_rootMapLevel, cellCount, regenerate: false);
		_menuController?.UpdateCellCountValue(cellCount);
		UpdateBackToParentButton();
	}

	private void SetupWelcomeOverlay()
	{
		if (_genesisController != null)
		{
			_genesisController.GenerateRequested += OnGenesisGenerateRequested;
		}

		SetWelcomeGeneratingState(false, 0f);
		UpdateWelcomeUIText();
	}

	private void UpdateWelcomeUIText()
	{
		_genesisController?.UpdateLocalizedText();
	}

	private void SetWelcomeMode(bool enabled)
	{
		_isWelcomeMode = enabled;
		if (_welcomeOverlay != null)
		{
			_welcomeOverlay.Visible = enabled;
		}

		if (_mapDisplayRoot != null)
		{
			_mapDisplayRoot.Visible = !enabled;
		}

		if (_mapOverlayRoot != null)
		{
			_mapOverlayRoot.Visible = !enabled;
		}

		if (enabled)
		{
			_isMenuVisible = false;
			_menuController?.SetMenuVisible(false);
			HideDropdowns();
		}
	}

	private async void OnGenesisGenerateRequested(UniverseData universeData)
	{
		if (_mapHierarchyController == null || _isWelcomeGenerating || _mapView == null)
		{
			return;
		}

		_isWelcomeGenerating = true;
		SetWelcomeGeneratingState(true, 0f);

		try
		{
			SyncMapGenerationSizeToWindow();
			ApplyUniverseSettings(universeData);

			int rootCellCount = _mapView.CellCount > 0 ? _mapView.CellCount : DefaultCellCount;
			string genesisSeed = BuildGenesisSeed(universeData);
			_mapHierarchyController.SetRoot(_rootMapLevel, rootCellCount, genesisSeed, regenerate: false);

			var targetContext = _mapHierarchyController.Current;
			if (targetContext == null)
			{
				return;
			}

			await _mapView.GenerateMapForContextAsync(targetContext, progress =>
			{
				SetWelcomeGeneratingState(true, progress);
			});

			OnMapGeneratedFromGenesis();

			SetWelcomeMode(false);
			UpdateBackToParentButton();
		}
		catch (Exception ex)
		{
			GD.PrintErr($"MapGeneratorUI: failed to generate welcome map: {ex.Message}");
		}
		finally
		{
			SetWelcomeGeneratingState(false, 0f);
			_isWelcomeGenerating = false;
		}
	}

	private void SetWelcomeGeneratingState(bool generating, float progress01)
	{
		float clamped = Mathf.Clamp(progress01, 0f, 1f);
		_genesisController?.SetGeneratingState(generating, clamped);
	}

	private void ApplyUniverseSettings(UniverseData universeData)
	{
		if (universeData == null || _mapView == null)
		{
			return;
		}

		if (universeData.HierarchyConfig != null && _mapHierarchyConfig != null)
		{
			_mapHierarchyConfig.WorldCellCount = Mathf.Max(500, universeData.HierarchyConfig.WorldCellCount);
			_mapHierarchyConfig.CountryCellCount = Mathf.Max(500, universeData.HierarchyConfig.CountryCellCount);
			_mapHierarchyConfig.ProvinceCellCount = Mathf.Max(500, universeData.HierarchyConfig.ProvinceCellCount);
			_mapHierarchyConfig.CityCellCount = Mathf.Max(500, universeData.HierarchyConfig.CityCellCount);
		}

		var archetype = universeData.HierarchyConfig?.Archetype ?? HierarchyArchetype.Standard;
		_rootMapLevel = ResolveRootLevelFromHierarchy(archetype);

		int fallbackCellCount = _mapView.CellCount > 0 ? _mapView.CellCount : DefaultCellCount;
		int baseCellCount = _mapHierarchyConfig?.GetCellCount(_rootMapLevel, fallbackCellCount) ?? fallbackCellCount;
		float densityScale = Mathf.Lerp(0.75f, 1.25f, Mathf.Clamp(universeData.CivilizationDensity / 100f, 0f, 1f));
		int targetCellCount = Mathf.Clamp(Mathf.RoundToInt(baseCellCount * densityScale), 300, 24000);

		_mapView.CellCount = targetCellCount;
		_menuController?.UpdateCellCountValue(targetCellCount);

		_mapView.ApplyVisualStyle(MapVisualStyleSelection.Relief);
		_mapView.TerrainStyleMode = MapView.TerrainStyle.Heightmap;
		_mapView.ApplyLayerPreset(MapView.LayerPreset.Heightmap);
		_mapView.UseTemplateGeneration = true;
		_mapView.UseRandomTemplateGeneration = false;
		_mapView.GenerationTemplateType = HeightmapTemplateType.Continents;
		_mapView.ApplyGenesisPlanetInfluence(universeData.CurrentPlanet, universeData.LawAlignment);
		_mapView.SetGenesisTerrainHeightmap(
			universeData.PlanetTerrainHeightmap,
			universeData.PlanetTerrainWidth,
			universeData.PlanetTerrainHeight,
			universeData.PlanetGenerationProfile);

		var terrainProfile = universeData.PlanetGenerationProfile;
		float civilizationSeaLevel = Mathf.Clamp(0.52f + (0.62f - terrainProfile.ContinentalFrequency) * 0.18f, 0.42f, 0.66f);
		float civilizationRiverDensity = Mathf.Clamp(0.55f + terrainProfile.MoistureTransport * 1.45f, 0.35f, 2.4f);

		_mapView.GenerationWaterLevel = civilizationSeaLevel;
		_mapView.RiverDensity = civilizationRiverDensity;
		_mapView.CountryCount = Mathf.Clamp(Mathf.RoundToInt(4f + universeData.CivilizationDensity / 6f), 1, 128);

		_enableMapDrilldown = (universeData.HierarchyConfig?.LevelCount ?? 2) > 1;
	}

	private static MapLevel ResolveRootLevelFromHierarchy(HierarchyArchetype archetype)
	{
		return archetype switch
		{
			HierarchyArchetype.Simple => MapLevel.Country,
			HierarchyArchetype.Standard => MapLevel.World,
			HierarchyArchetype.Complex => MapLevel.World,
			HierarchyArchetype.Custom => MapLevel.World,
			_ => MapLevel.World
		};
	}

	private static string BuildGenesisSeed(UniverseData universeData)
	{
		if (universeData == null)
		{
			return $"genesis_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
		}

		var planet = universeData.CurrentPlanet ?? new PlanetData();
		var profile = universeData.PlanetGenerationProfile;
		int mountains = Mathf.RoundToInt(Mathf.Clamp(planet.MountainIntensity, 0f, 1f) * 1000f);
		int polar = Mathf.RoundToInt(Mathf.Clamp(planet.PolarCoverage, 0f, 1f) * 1000f);
		int desert = Mathf.RoundToInt(Mathf.Clamp(planet.DesertRatio, 0f, 1f) * 1000f);
		int tectonic = profile.TectonicPlateCount;
		int windCells = profile.WindCellCount;
		int erosionIterations = profile.ErosionIterations;
		int erosionStrength = Mathf.RoundToInt(profile.ErosionStrength * 1000f);
		int heatFactor = Mathf.RoundToInt(profile.HeatFactor);
		int law = Mathf.Clamp(universeData.LawAlignment, 0, 100);
		int civ = Mathf.Clamp(universeData.CivilizationDensity, 0, 100);
		int role = (int)(universeData.HierarchyConfig?.Archetype ?? HierarchyArchetype.Standard);

		return $"genesis_{law}_{civ}_{(int)planet.Element}_{mountains}_{polar}_{desert}_{tectonic}_{windCells}_{erosionIterations}_{erosionStrength}_{heatFactor}_{role}";
	}

	private void OnMapGeneratedFromGenesis()
	{
		if (_mapHierarchyController == null || _mapView == null)
		{
			return;
		}

		int currentCellCount = _mapView.CellCount > 0 ? _mapView.CellCount : DefaultCellCount;
		if (_mapHierarchyController.Current == null)
		{
			_mapHierarchyController.SetRoot(_rootMapLevel, currentCellCount, regenerate: false);
		}

		var currentContext = _mapHierarchyController.Current;
		if (currentContext == null)
		{
			return;
		}

		var updatedContext = new MapContext(
			currentContext.Level,
			currentCellCount,
			currentContext.Seed,
			currentContext.Parent,
			currentContext.ParentCellId,
			currentContext.DisplayName,
			currentContext.ParentMapData);

		_mapHierarchyController.RestoreContext(updatedContext, regenerate: false);
		_mapView.ApplyVisualStyle(MapVisualStyleSelection.Relief);
		_mapView.TerrainStyleMode = MapView.TerrainStyle.Heightmap;
		_mapView.ApplyLayerPreset(MapView.LayerPreset.Heightmap);
		_menuController?.UpdateCellCountValue(currentCellCount);
		UpdateBackToParentButton();
	}

	private void SetupBottomMenu()
	{
		_bottomMenuController?.Initialize(
			OnMapDisplayPressed,
			OnBackToParentPressed,
			OnMapDropdownRegeneratePressed,
			OnMapDropdownSettingsPressed);
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
		SwitchToScene("res://Scenes/Screens/Settings.tscn");
	}

	private void OnMainMenuPressed()
	{
		HideDropdowns();
		SwitchToScene("res://Scenes/Screens/MainMenu.tscn");
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
		if (_isWelcomeMode)
		{
			return;
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.CtrlPressed && keyEvent.ShiftPressed && keyEvent.Keycode == Key.E)
			{
				string outputPath = _mapView?.ExportWorldVectorSvg() ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(outputPath))
				{
					GD.Print($"[MapGeneratorUI] SVG exported: {ProjectSettings.GlobalizePath(outputPath)}");
					_menuController?.ShowMapSettingsNotice("map_svg_exported");
				}
				else
				{
					_menuController?.ShowMapSettingsNotice("map_svg_export_failed");
				}
				return;
			}

			if (keyEvent.Keycode == Key.Escape)
			{
				if (_menuController != null && _menuController.IsMapSettingsVisible)
				{
					HideMapSettingsPopup();
					return;
				}

				HideDropdowns();
				ToggleMenu();
			}
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

	private void OnMapDisplayPressed()
	{
		HideDropdowns();
		_mapDisplayPanelController?.ShowPanel();
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
		if (_mapHierarchyController == null)
		{
			return;
		}

		bool canReturn = _mapHierarchyController.CanReturnToParent;
		_menuController?.UpdateBackToParentButton(canReturn);
		_bottomMenuController?.UpdateBackToParentButton(canReturn);
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

	private void CacheNodeReferences()
	{
		_mapDisplayRoot = GetNodeOrNull<Control>("MapDisplay")
			?? FindChild("MapDisplay", true, false) as Control;

		_mapOverlayRoot = GetNodeOrNull<Control>("MapGeneratorUI")
			?? GetNodeOrNull<Control>("GameUI")
			?? FindChild("MapGeneratorUI", true, false) as Control
			?? FindChild("GameUI", true, false) as Control;

		_background = _mapDisplayRoot?.GetNodeOrNull<ColorRect>("Background")
			?? GetNodeOrNull<ColorRect>("MapDisplay/Background");

		_mapView = _mapDisplayRoot?.GetNodeOrNull<MapView>("MapView")
			?? GetNodeOrNull<MapView>("MapDisplay/MapView");

		_mapDisplayPanelController = _mapOverlayRoot?.GetNodeOrNull<MapDisplayPanelController>("MapDisplayPanelUI")
			?? FindChild("MapDisplayPanelUI", true, false) as MapDisplayPanelController;

		_bottomMenuController = _mapOverlayRoot?.GetNodeOrNull<BottomMenuController>("BottomMenuUI")
			?? FindChild("BottomMenuUI", true, false) as BottomMenuController;

		_menuController = _mapOverlayRoot?.GetNodeOrNull<MenuController>("MenuUI")
			?? FindChild("MenuUI", true, false) as MenuController;

		_welcomeOverlay = GetNodeOrNull<Control>("GenesisHubUI")
			?? FindChild("GenesisHubUI", true, false) as Control
			?? GetNodeOrNull<Control>("WelcomeOverlay")
			?? FindChild("WelcomeOverlay", true, false) as Control;

		_genesisController = _welcomeOverlay as GenesisController
			?? GetNodeOrNull<GenesisController>("GenesisHubUI")
			?? FindChild("GenesisHubUI", true, false) as GenesisController;

		if (_mapView == null)
		{
			GD.PrintErr("MapGeneratorUI: MapView node not found.");
		}

		if (_bottomMenuController == null || _menuController == null)
		{
			GD.PrintErr("MapGeneratorUI: UI controllers were not fully resolved.");
		}

		if (_welcomeOverlay == null || _genesisController == null)
		{
			GD.PrintErr("MapGeneratorUI: Genesis hub nodes were not fully resolved.");
		}
	}

	private void SwitchToScene(string scenePath)
	{
		if (SceneNavigator.Instance.NavigateTo(scenePath))
		{
			return;
		}

		var error = GetTree().ChangeSceneToFile(scenePath);
		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to load scene: {scenePath}");
		}
	}
}
