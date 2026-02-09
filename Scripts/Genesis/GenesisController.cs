using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Genesis;

public partial class GenesisController : Control
{
	private Label _hubTitle;
	private Label _hubSubtitle;
	private Label _lawSectionTitle;
	private Label _planetSectionTitle;
	private Label _hierarchySectionTitle;
	private Label _orbitSectionTitle;
	private Label _lawValueLabel;
	private Label _civilizationValueLabel;
	private Label _timeFlowValueLabel;
	private Label _oceanValueLabel;
	private Label _temperatureValueLabel;
	private Label _atmosphereValueLabel;
	private Label _roleTitleLabel;
	private Label _summaryLabel;
	private Label _narrativeTitle;
	private RichTextLabel _narrativeConsole;
	private Label _progressLabel;
	private ProgressBar _progressBar;

	private Control _previewRoot;
	private PlanetTextureView _planetTextureView;
	private Label _cameraDistanceLabel;
	private Label _cameraDescLabel;

	private HSlider _lawAlignmentSlider;
	private HSlider _civilizationDensitySlider;
	private HSlider _timeFlowSlider;
	private OptionButton _planetElementSelector;
	private HSlider _oceanSlider;
	private HSlider _temperatureSlider;
	private HSlider _atmosphereSlider;
	private OptionButton _hierarchySelector;
	private Button _quickStartButton;
	private Button _generateButton;
	private CheckButton _showMoonOrbitToggle;
	private CheckButton _showPlanetOrbitToggle;

	private ThemeManager _themeManager;
	private PlanetPreviewController _planetPreviewController;
	private NarrativeGenerator _narrativeGenerator;
	private TranslationManager _translationManager;
	private UniverseData _tempUniverse;
	private readonly Random _random = new();
	private bool _isQuickRolling;
	private bool _isPreviewRootGuiInputBound;
	private bool _isPlanetTextureSignalsBound;
	private float _lastSolarBrightness = -1f;

	[Export]
	private CelestialSystemPhysicsConfig _defaultCelestialPhysics = CelestialSystemPhysicsConfig.CreateDefault();

	public event Action<UniverseData> GenerateRequested;

	public UniverseData CurrentUniverseData => CloneUniverse(_tempUniverse);

	public override void _Ready()
	{
		_translationManager = TranslationManager.Instance;
		_translationManager.LanguageChanged += OnLanguageChanged;

		CacheNodes();
		SetupServices();
		BindEvents();

		_tempUniverse = BuildDefaultUniverse();
		if (_defaultCelestialPhysics != null)
		{
			_tempUniverse.CelestialPhysics = _defaultCelestialPhysics.DuplicateConfig();
		}

		ApplyUniverseToControls(_tempUniverse);
		UpdateLocalizedText();
		RefreshAllViews();
		SetGeneratingState(false, 0f);
	}

	public override void _ExitTree()
	{
		if (_previewRoot != null && _isPreviewRootGuiInputBound)
		{
			_previewRoot.GuiInput -= OnPreviewRootGuiInput;
			_isPreviewRootGuiInputBound = false;
		}

		if (_planetTextureView != null && _isPlanetTextureSignalsBound)
		{
			_planetTextureView.PreviewActivated -= OnPlanetTextureActivated;
			_planetTextureView.LivePhotoModeChanged -= OnLivePhotoModeChanged;
			_planetTextureView.LightFollowModeChanged -= OnLightFollowModeChanged;
			_planetTextureView.LightResponseChanged -= OnLightResponseChanged;
			_planetTextureView.SolarBrightnessChanged -= OnSolarBrightnessChanged;
			_planetTextureView.SnapshotRefreshRequested -= OnSnapshotRefreshRequested;
			_planetTextureView.SkyTextureChanged -= OnSkyTextureChanged;
			_planetTextureView.PlanetSurfaceTextureChanged -= OnPlanetSurfaceTextureChanged;
			_planetTextureView.MoonTextureChanged -= OnMoonTextureChanged;
			_planetTextureView.SunTextureChanged -= OnSunTextureChanged;
			_planetTextureView.DownloadedOnlyFilterChanged -= OnDownloadedOnlyFilterChanged;
			_planetTextureView.CelestialPhysicsChanged -= OnCelestialPhysicsChanged;
			_isPlanetTextureSignalsBound = false;
		}

		if (_translationManager != null)
		{
			_translationManager.LanguageChanged -= OnLanguageChanged;
		}
	}

	public override void _Process(double delta)
	{
		_planetPreviewController?.Tick(delta);
		SyncSolarBrightnessFromUi();
		UpdatePreviewBuildState();
		UpdatePreviewCameraHud();
	}

	public void SetGeneratingState(bool generating, float progress01)
	{
		float clamped = Mathf.Clamp(progress01, 0f, 1f);

		if (_generateButton != null)
		{
			_generateButton.Disabled = generating;
		}

		if (_quickStartButton != null)
		{
			_quickStartButton.Disabled = generating || _isQuickRolling;
		}

		if (_progressBar != null)
		{
			_progressBar.Visible = generating;
			_progressBar.Value = clamped * 100f;
		}

		if (_progressLabel != null)
		{
			_progressLabel.Visible = generating;
		}
	}

	public void UpdateLocalizedText()
	{
		bool isZh = IsChineseMode();

		if (_hubTitle != null)
		{
			_hubTitle.Text = isZh ? "🌌 太虚幻境" : "🌌 Genesis Hub";
		}

		if (_hubSubtitle != null)
		{
			_hubSubtitle.Text = isZh ? "以天道塑界，以观测立世" : "Shape a world through laws and observation";
		}

		if (_lawSectionTitle != null)
		{
			_lawSectionTitle.Text = isZh ? "[壹] 天道法则" : "[I] Cosmic Laws";
		}

		if (_planetSectionTitle != null)
		{
			_planetSectionTitle.Text = isZh ? "[贰] 星辰法相" : "[II] Planet Shaping";
		}

		if (_hierarchySectionTitle != null)
		{
			_hierarchySectionTitle.Text = isZh ? "[叁] 观测垂范" : "[III] Hierarchy Archetype";
		}

		if (_narrativeTitle != null)
		{
			_narrativeTitle.Text = isZh ? "天道卷轴" : "Narrative Console";
		}

		if (_orbitSectionTitle != null)
		{
			_orbitSectionTitle.Text = isZh ? "[肆] 轨道显示" : "[IV] Orbit Display";
		}


		if (_quickStartButton != null)
		{
			_quickStartButton.Text = isZh ? "🎲 轮回" : "🎲 Cycle";
		}

		if (_generateButton != null)
		{
			_generateButton.Text = isZh ? "⚡ 开天辟地" : "⚡ Ignite Big Bang";
		}

		if (_showMoonOrbitToggle != null)
		{
			_showMoonOrbitToggle.Text = isZh ? "显示卫星轨道" : "Show Satellite Orbit";
		}

		if (_showPlanetOrbitToggle != null)
		{
			_showPlanetOrbitToggle.Text = isZh ? "显示行星轨道" : "Show Planet Orbit";
		}


		if (_progressLabel != null)
		{
			_progressLabel.Text = _translationManager.Tr("welcome_generating_map");
		}

		if (_cameraDistanceLabel != null && string.IsNullOrWhiteSpace(_cameraDistanceLabel.Text))
		{
			_cameraDistanceLabel.Text = isZh ? "1 AU" : "1 AU";
		}

		if (_cameraDescLabel != null && string.IsNullOrWhiteSpace(_cameraDescLabel.Text))
		{
			_cameraDescLabel.Text = _translationManager.Tr("preview_camera_idle_desc");
		}

		PopulatePlanetElementSelector(_tempUniverse?.CurrentPlanet?.Element ?? PlanetElement.Terra);
		PopulateHierarchySelector(_tempUniverse?.HierarchyConfig?.Archetype ?? HierarchyArchetype.Standard);
		RefreshAllViews();
	}

	private void CacheNodes()
	{
		_hubTitle = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/HubTitle");
		_hubSubtitle = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/HubSubtitle");
		_lawSectionTitle = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/LawSection/LawHeader/LawSectionTitle");
		_planetSectionTitle = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/PlanetHeader/PlanetSectionTitle");
		_hierarchySectionTitle = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/HierarchySection/HierarchyHeader/HierarchySectionTitle");
		_orbitSectionTitle = GetNodeOrNull<Label>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitSection/OrbitSectionVBox/OrbitSectionTitle");
		_lawValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/LawSection/LawValueLabel");
		_civilizationValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/LawSection/CivilizationValueLabel");
		_timeFlowValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/LawSection/TimeFlowValueLabel");
		_oceanValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/OceanValueLabel");
		_temperatureValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/TemperatureValueLabel");
		_atmosphereValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/AtmosphereValueLabel");
		_roleTitleLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/HierarchySection/RoleTitleLabel");
		_summaryLabel = GetNodeOrNull<Label>("RootMargin/MainRow/RightPanel/NarrativePanel/NarrativeVBox/SummaryLabel");
		_narrativeTitle = GetNodeOrNull<Label>("RootMargin/MainRow/RightPanel/NarrativePanel/NarrativeVBox/NarrativeTitle");
		_narrativeConsole = GetNodeOrNull<RichTextLabel>("RootMargin/MainRow/RightPanel/NarrativePanel/NarrativeVBox/NarrativeConsole");
		_progressLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/ActionSection/ProgressLabel");
		_progressBar = GetNodeOrNull<ProgressBar>("RootMargin/MainRow/LeftPanel/LeftVBox/ActionSection/ProgressBar");

		_previewRoot = GetNodeOrNull<Control>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot");
		_planetTextureView = GetNodeOrNull<PlanetTextureView>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture");
		_cameraDistanceLabel = GetNodeOrNull<Label>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitHudBottomRight/OrbitHudBottomVBox/CameraDistanceLabel");
		_cameraDescLabel = GetNodeOrNull<Label>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitHudBottomRight/OrbitHudBottomVBox/CameraDescLabel");

		_lawAlignmentSlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/LawSection/LawSlider");
		_civilizationDensitySlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/LawSection/CivilizationSlider");
		_timeFlowSlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/LawSection/TimeFlowSlider");
		_planetElementSelector = GetNodeOrNull<OptionButton>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/ElementSelector");
		_oceanSlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/OceanSlider");
		_temperatureSlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/TemperatureSlider");
		_atmosphereSlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/AtmosphereSlider");
		_hierarchySelector = GetNodeOrNull<OptionButton>("RootMargin/MainRow/LeftPanel/LeftVBox/HierarchySection/HierarchySelector");
		_quickStartButton = GetNodeOrNull<Button>("RootMargin/MainRow/LeftPanel/LeftVBox/ActionSection/ActionButtons/QuickStartButton");
		_generateButton = GetNodeOrNull<Button>("RootMargin/MainRow/LeftPanel/LeftVBox/ActionSection/ActionButtons/GenerateButton");
		_showMoonOrbitToggle = GetNodeOrNull<CheckButton>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitSection/OrbitSectionVBox/ShowMoonOrbitToggle");
		_showPlanetOrbitToggle = GetNodeOrNull<CheckButton>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitSection/OrbitSectionVBox/ShowPlanetOrbitToggle");
	}

	private void SetupServices()
	{
		var magicLayer = GetNodeOrNull<ColorRect>("MagicLayer");
		var techLayer = GetNodeOrNull<ColorRect>("TechLayer");
		ColorRect planetAura = null;

		var textItems = new List<CanvasItem>
		{
			_hubTitle,
			_hubSubtitle,
			_lawSectionTitle,
			_planetSectionTitle,
			_hierarchySectionTitle,
			_roleTitleLabel,
			_narrativeTitle,
			_narrativeConsole,
			_summaryLabel,
			_lawValueLabel,
			_civilizationValueLabel,
			_timeFlowValueLabel,
			_oceanValueLabel,
			_temperatureValueLabel,
			_atmosphereValueLabel
		};

		_themeManager = new ThemeManager(magicLayer, techLayer, textItems);
		_planetPreviewController = new PlanetPreviewController(_previewRoot, planetAura, _planetTextureView);
		_narrativeGenerator = new NarrativeGenerator();
	}

	private void BindEvents()
	{
		if (_lawAlignmentSlider != null)
		{
			_lawAlignmentSlider.ValueChanged += OnLawAlignmentChanged;
		}

		if (_civilizationDensitySlider != null)
		{
			_civilizationDensitySlider.ValueChanged += OnCivilizationDensityChanged;
		}

		if (_timeFlowSlider != null)
		{
			_timeFlowSlider.ValueChanged += OnTimeFlowChanged;
		}

		if (_planetElementSelector != null)
		{
			_planetElementSelector.ItemSelected += OnPlanetElementSelected;
		}

		if (_oceanSlider != null)
		{
			_oceanSlider.ValueChanged += OnOceanChanged;
		}

		if (_temperatureSlider != null)
		{
			_temperatureSlider.ValueChanged += OnTemperatureChanged;
		}

		if (_atmosphereSlider != null)
		{
			_atmosphereSlider.ValueChanged += OnAtmosphereChanged;
		}

		if (_hierarchySelector != null)
		{
			_hierarchySelector.ItemSelected += OnHierarchySelected;
		}

		if (_quickStartButton != null)
		{
			_quickStartButton.Pressed += OnQuickStartPressed;
		}

		if (_generateButton != null)
		{
			_generateButton.Pressed += OnGeneratePressed;
		}

		if (_showMoonOrbitToggle != null)
		{
			_showMoonOrbitToggle.Toggled += OnShowMoonOrbitToggled;
		}

		if (_showPlanetOrbitToggle != null)
		{
			_showPlanetOrbitToggle.Toggled += OnShowPlanetOrbitToggled;
		}

		if (_previewRoot != null && !_isPreviewRootGuiInputBound)
		{
			_previewRoot.GuiInput += OnPreviewRootGuiInput;
			_isPreviewRootGuiInputBound = true;
		}

		if (_planetTextureView != null && !_isPlanetTextureSignalsBound)
		{
			_planetTextureView.PreviewActivated += OnPlanetTextureActivated;
			_planetTextureView.LivePhotoModeChanged += OnLivePhotoModeChanged;
			_planetTextureView.LightFollowModeChanged += OnLightFollowModeChanged;
			_planetTextureView.LightResponseChanged += OnLightResponseChanged;
			_planetTextureView.SolarBrightnessChanged += OnSolarBrightnessChanged;
			_planetTextureView.SnapshotRefreshRequested += OnSnapshotRefreshRequested;
			_planetTextureView.SkyTextureChanged += OnSkyTextureChanged;
			_planetTextureView.PlanetSurfaceTextureChanged += OnPlanetSurfaceTextureChanged;
			_planetTextureView.MoonTextureChanged += OnMoonTextureChanged;
			_planetTextureView.SunTextureChanged += OnSunTextureChanged;
			_planetTextureView.DownloadedOnlyFilterChanged += OnDownloadedOnlyFilterChanged;
			_planetTextureView.CelestialPhysicsChanged += OnCelestialPhysicsChanged;
			_isPlanetTextureSignalsBound = true;
		}

		ApplyOrbitToggleState();
		ApplyPlanetTextureOptions();
	}

	private void OnLanguageChanged(string language)
	{
		UpdateLocalizedText();
		UpdatePreviewCameraHud();
	}

	private void OnPreviewRootGuiInput(InputEvent inputEvent)
	{
		if (_planetPreviewController?.IsThreeDActive != true)
		{
			return;
		}

		if (_planetTextureView != null && inputEvent is InputEventMouse mouseEvent)
		{
			Vector2 localPoint = mouseEvent.Position - _planetTextureView.Position;
			if (_planetTextureView.IsPointInInteractiveOverlay(localPoint))
			{
				return;
			}
		}

		_planetPreviewController.HandleGuiInput(inputEvent);
		AcceptEvent();
	}

	private void OnPlanetTextureActivated()
	{
		_planetPreviewController?.RequestActivateThreeD();
	}

	private void OnLivePhotoModeChanged(bool enabled)
	{
		_planetPreviewController?.SetPlanetPhotoRealtime(enabled);
	}

	private void OnLightFollowModeChanged(bool enabled)
	{
		_planetPreviewController?.SetLightFollowEnabled(enabled);
	}

	private void OnLightResponseChanged(float strength)
	{
		_planetPreviewController?.SetLightResponse(strength);
	}

	private void OnSolarBrightnessChanged(float brightness)
	{
		_lastSolarBrightness = brightness;
		_planetPreviewController?.SetSolarBrightness(brightness);
	}

	private void OnSnapshotRefreshRequested()
	{
		_planetPreviewController?.RefreshPlanetSnapshot();
	}

	private void OnSkyTextureChanged(string texturePath)
	{
		if (string.IsNullOrWhiteSpace(texturePath))
		{
			return;
		}

		_planetPreviewController?.SetSkyTexturePath(texturePath);
	}

	private void OnPlanetSurfaceTextureChanged(string texturePath)
	{
		_planetPreviewController?.SetPlanetSurfaceTexturePath(texturePath);
	}

	private void OnMoonTextureChanged(string texturePath)
	{
		_planetPreviewController?.SetMoonTexturePath(texturePath);
	}

	private void OnSunTextureChanged(string texturePath)
	{
		_planetPreviewController?.SetSunTexturePath(texturePath);
	}

	private void OnDownloadedOnlyFilterChanged(bool enabled)
	{
		ApplyPlanetTextureOptions();
	}

	private void OnCelestialPhysicsChanged(CelestialSystemPhysicsConfig config)
	{
		EnsureUniverseData();
		_tempUniverse.CelestialPhysics = config?.DuplicateConfig() ?? CelestialSystemPhysicsConfig.CreateDefault();
		_planetPreviewController?.SetCelestialSystemConfig(_tempUniverse.CelestialPhysics);
		UpdatePreviewCameraHud();
	}

	private void ApplyPlanetTextureOptions()
	{
		if (_planetPreviewController == null || _planetTextureView == null)
		{
			return;
		}

		_planetPreviewController.SetPlanetPhotoRealtime(_planetTextureView.IsLivePhotoUpdateEnabled);
		_planetPreviewController.SetLightFollowEnabled(_planetTextureView.IsLightFollowEnabled);
		_planetPreviewController.SetLightResponse(_planetTextureView.LightResponseStrength);
		_planetPreviewController.SetSolarBrightness(_planetTextureView.SolarBrightness);

		if (!string.IsNullOrWhiteSpace(_planetTextureView.SelectedSkyTexturePath))
		{
			_planetPreviewController.SetSkyTexturePath(_planetTextureView.SelectedSkyTexturePath);
		}

		_planetPreviewController.SetPlanetSurfaceTexturePath(_planetTextureView.SelectedPlanetSurfaceTexturePath);
		_planetPreviewController.SetMoonTexturePath(_planetTextureView.SelectedMoonTexturePath);
		_planetPreviewController.SetSunTexturePath(_planetTextureView.SelectedSunTexturePath);
	}

	private void NotifyLeftPanelEditing()
	{
		_planetPreviewController?.NotifyControlEditing();
	}

	private void SyncSolarBrightnessFromUi()
	{
		if (_planetPreviewController == null || _planetTextureView == null)
		{
			return;
		}

		float brightness = _planetTextureView.SolarBrightness;
		if (Mathf.IsEqualApprox(brightness, _lastSolarBrightness))
		{
			return;
		}

		_lastSolarBrightness = brightness;
		_planetPreviewController.SetSolarBrightness(brightness);
	}

	private void UpdatePreviewCameraHud()
	{
		if (_planetPreviewController == null)
		{
			return;
		}

		if (_planetPreviewController.IsBuildingThreeD)
		{
			if (_cameraDistanceLabel != null)
			{
				_cameraDistanceLabel.Text = _translationManager.Tr("preview_camera_status_building");
			}

			if (_cameraDescLabel != null)
			{
				_cameraDescLabel.Text = _translationManager.Tr("preview_camera_desc_building");
			}
			return;
		}

		if (!_planetPreviewController.IsThreeDActive)
		{
			if (_cameraDistanceLabel != null)
			{
				_cameraDistanceLabel.Text = _translationManager.Tr("preview_camera_status_click");
			}

			if (_cameraDescLabel != null)
			{
				_cameraDescLabel.Text = _translationManager.Tr("preview_camera_desc_click");
			}
			return;
		}

		if (_cameraDistanceLabel != null)
		{
			_cameraDistanceLabel.Text = _planetPreviewController.CameraDistanceText;
		}

		if (_cameraDescLabel != null)
		{
			_cameraDescLabel.Text = _planetPreviewController.CameraDescriptionText;
		}
	}

	private void UpdatePreviewBuildState()
	{
		if (_planetPreviewController == null || _planetTextureView == null)
		{
			return;
		}

		_planetTextureView.SetBuildState(
			_planetPreviewController.IsBuildingThreeD,
			_planetPreviewController.BuildProgress01,
			IsChineseMode());
	}

	private void OnShowMoonOrbitToggled(bool toggledOn)
	{
		if (_planetPreviewController == null)
		{
			return;
		}

		bool showPlanetOrbit = _showPlanetOrbitToggle?.ButtonPressed ?? true;
		_planetPreviewController.SetOrbitVisibility(toggledOn, showPlanetOrbit);
	}

	private void OnShowPlanetOrbitToggled(bool toggledOn)
	{
		if (_planetPreviewController == null)
		{
			return;
		}

		bool showMoonOrbit = _showMoonOrbitToggle?.ButtonPressed ?? true;
		_planetPreviewController.SetOrbitVisibility(showMoonOrbit, toggledOn);
	}

	private void ApplyOrbitToggleState()
	{
		if (_planetPreviewController == null)
		{
			return;
		}

		bool showMoonOrbit = _showMoonOrbitToggle?.ButtonPressed ?? true;
		bool showPlanetOrbit = _showPlanetOrbitToggle?.ButtonPressed ?? true;
		_planetPreviewController.SetOrbitVisibility(showMoonOrbit, showPlanetOrbit);
	}

	private void OnLawAlignmentChanged(double value)
	{
		NotifyLeftPanelEditing();
		EnsureUniverseData();
		_tempUniverse.LawAlignment = Mathf.Clamp((int)Math.Round(value), 0, 100);
		_tempUniverse.AestheticTheme = ResolveAestheticTheme(_tempUniverse.LawAlignment);
		RefreshAllViews();
	}

	private void OnCivilizationDensityChanged(double value)
	{
		NotifyLeftPanelEditing();
		EnsureUniverseData();
		_tempUniverse.CivilizationDensity = Mathf.Clamp((int)Math.Round(value), 0, 100);
		RefreshNarrative();
		UpdateValueLabels();
	}

	private void OnTimeFlowChanged(double value)
	{
		NotifyLeftPanelEditing();
		EnsureUniverseData();
		_tempUniverse.TimeFlowRate = Mathf.Clamp((float)value, 0.25f, 3f);
		RefreshNarrative();
		UpdateValueLabels();
	}

	private void OnPlanetElementSelected(long index)
	{
		NotifyLeftPanelEditing();
		EnsureUniverseData();
		if (_planetElementSelector == null)
		{
			return;
		}

		int elementId = _planetElementSelector.GetItemId((int)index);
		_tempUniverse.CurrentPlanet.Element = (PlanetElement)elementId;
		_tempUniverse.CurrentPlanet.Name = ResolvePlanetName(_tempUniverse.CurrentPlanet.Element);
		OnPlanetParamsChanged();
	}

	private void OnOceanChanged(double value)
	{
		NotifyLeftPanelEditing();
		EnsureUniverseData();
		_tempUniverse.CurrentPlanet.OceanCoverage = Mathf.Clamp((float)value, 0f, 1f);
		OnPlanetParamsChanged();
	}

	private void OnTemperatureChanged(double value)
	{
		NotifyLeftPanelEditing();
		EnsureUniverseData();
		_tempUniverse.CurrentPlanet.Temperature = Mathf.Clamp((float)value, 0f, 1f);
		OnPlanetParamsChanged();
	}

	private void OnAtmosphereChanged(double value)
	{
		NotifyLeftPanelEditing();
		EnsureUniverseData();
		_tempUniverse.CurrentPlanet.AtmosphereDensity = Mathf.Clamp((float)value, 0f, 1f);
		OnPlanetParamsChanged();
	}

	private void OnHierarchySelected(long index)
	{
		NotifyLeftPanelEditing();
		EnsureUniverseData();
		if (_hierarchySelector == null)
		{
			return;
		}

		int id = _hierarchySelector.GetItemId((int)index);
		var archetype = (HierarchyArchetype)id;
		_tempUniverse.HierarchyConfig = HierarchyConfigData.CreateFromArchetype(archetype);
		RefreshNarrative();
		UpdateValueLabels();
	}

	private async void OnQuickStartPressed()
	{
		if (_isQuickRolling)
		{
			return;
		}

		_isQuickRolling = true;
		if (_quickStartButton != null)
		{
			_quickStartButton.Disabled = true;
		}

		for (int i = 0; i < 14; i++)
		{
			ApplyRandomValuesToControls();
			PullControlsToUniverse();
			RefreshAllViews();
			await Task.Delay(45 + i * 6);
		}

		_isQuickRolling = false;
		if (_quickStartButton != null)
		{
			_quickStartButton.Disabled = false;
		}
	}

	private void OnGeneratePressed()
	{
		var payload = CloneUniverse(_tempUniverse);
		if (payload != null)
		{
			const int terrainWidth = 1024;
			const int terrainHeight = 512;
			payload.PlanetTerrainHeightmap = _planetPreviewController?.GeneratePlanetTerrainHeightmap(
				payload.CurrentPlanet,
				terrainWidth,
				terrainHeight);
			payload.PlanetTerrainWidth = terrainWidth;
			payload.PlanetTerrainHeight = terrainHeight;
		}

		GenerateRequested?.Invoke(payload);
	}

	private void OnPlanetParamsChanged()
	{
		_planetPreviewController?.SetCelestialSystemConfig(_tempUniverse.CelestialPhysics);
		_planetPreviewController?.UpdateUniverseMood(_tempUniverse.LawAlignment);
		_planetPreviewController?.UpdateShader(_tempUniverse.CurrentPlanet);
		RefreshNarrative();
		UpdateValueLabels();
	}

	private void RefreshAllViews()
	{
		if (_tempUniverse == null)
		{
			return;
		}

		_themeManager?.UpdateVisuals(_tempUniverse.LawAlignment);
		_planetPreviewController?.SetCelestialSystemConfig(_tempUniverse.CelestialPhysics);
		_planetPreviewController?.UpdateUniverseMood(_tempUniverse.LawAlignment);
		_planetPreviewController?.UpdateShader(_tempUniverse.CurrentPlanet);
		RefreshNarrative();
		UpdateValueLabels();
	}

	private void RefreshNarrative()
	{
		if (_tempUniverse == null || _narrativeGenerator == null)
		{
			return;
		}

		var snapshot = _narrativeGenerator.RefreshText(_tempUniverse);
		if (_roleTitleLabel != null)
		{
			_roleTitleLabel.Text = snapshot.RoleTitle;
		}

		if (_summaryLabel != null)
		{
			_summaryLabel.Text = snapshot.Summary;
		}

		if (_narrativeConsole != null)
		{
			_narrativeConsole.Clear();
			_narrativeConsole.AppendText(snapshot.ConsoleText);
		}
	}

	private void UpdateValueLabels()
	{
		if (_tempUniverse == null)
		{
			return;
		}

		bool isZh = IsChineseMode();
		int oceanPercent = Mathf.RoundToInt(_tempUniverse.CurrentPlanet.OceanCoverage * 100f);
		int temperaturePercent = Mathf.RoundToInt(_tempUniverse.CurrentPlanet.Temperature * 100f);
		int atmospherePercent = Mathf.RoundToInt(_tempUniverse.CurrentPlanet.AtmosphereDensity * 100f);

		if (_lawValueLabel != null)
		{
			_lawValueLabel.Text = isZh
				? $"灵气 / 机枢：{_tempUniverse.LawAlignment}%"
				: $"Magic / Tech: {_tempUniverse.LawAlignment}%";
		}

		if (_civilizationValueLabel != null)
		{
			_civilizationValueLabel.Text = isZh
				? $"星火密度：{_tempUniverse.CivilizationDensity}"
				: $"Civilization Density: {_tempUniverse.CivilizationDensity}";
		}

		if (_timeFlowValueLabel != null)
		{
			_timeFlowValueLabel.Text = isZh
				? $"光阴流速：{_tempUniverse.TimeFlowRate:0.00}x"
				: $"Time Flow: {_tempUniverse.TimeFlowRate:0.00}x";
		}

		if (_oceanValueLabel != null)
		{
			_oceanValueLabel.Text = isZh
				? $"沧海桑田：{oceanPercent}%"
				: $"Ocean Coverage: {oceanPercent}%";
		}

		if (_temperatureValueLabel != null)
		{
			_temperatureValueLabel.Text = isZh
				? $"阴阳调和：{temperaturePercent}%"
				: $"Temperature: {temperaturePercent}%";
		}

		if (_atmosphereValueLabel != null)
		{
			_atmosphereValueLabel.Text = isZh
				? $"混沌之气：{atmospherePercent}%"
				: $"Atmosphere: {atmospherePercent}%";
		}
	}

	private void PopulatePlanetElementSelector(PlanetElement selected)
	{
		if (_planetElementSelector == null)
		{
			return;
		}

		bool isZh = IsChineseMode();
		_planetElementSelector.Clear();
		_planetElementSelector.AddItem(isZh ? "后土 (Terra)" : "Terra", (int)PlanetElement.Terra);
		_planetElementSelector.AddItem(isZh ? "祝融 (Pyro)" : "Pyro", (int)PlanetElement.Pyro);
		_planetElementSelector.AddItem(isZh ? "玄冥 (Cryo)" : "Cryo", (int)PlanetElement.Cryo);
		_planetElementSelector.AddItem(isZh ? "罡风 (Aero)" : "Aero", (int)PlanetElement.Aero);

		int index = _planetElementSelector.GetItemIndex((int)selected);
		if (index >= 0)
		{
			_planetElementSelector.Select(index);
		}
	}

	private void PopulateHierarchySelector(HierarchyArchetype selected)
	{
		if (_hierarchySelector == null)
		{
			return;
		}

		bool isZh = IsChineseMode();
		_hierarchySelector.Clear();
		_hierarchySelector.AddItem(isZh ? "Simple · 红尘客" : "Simple · Wanderer", (int)HierarchyArchetype.Simple);
		_hierarchySelector.AddItem(isZh ? "Standard · 一方霸主" : "Standard · Sovereign", (int)HierarchyArchetype.Standard);
		_hierarchySelector.AddItem(isZh ? "Complex · 掌道者" : "Complex · Demiurge", (int)HierarchyArchetype.Complex);
		_hierarchySelector.AddItem(isZh ? "Custom · 观测者" : "Custom · Observer", (int)HierarchyArchetype.Custom);

		int index = _hierarchySelector.GetItemIndex((int)selected);
		if (index >= 0)
		{
			_hierarchySelector.Select(index);
		}
	}

	private void ApplyUniverseToControls(UniverseData universeData)
	{
		if (universeData == null)
		{
			return;
		}

		if (_lawAlignmentSlider != null)
		{
			_lawAlignmentSlider.Value = universeData.LawAlignment;
		}

		if (_civilizationDensitySlider != null)
		{
			_civilizationDensitySlider.Value = universeData.CivilizationDensity;
		}

		if (_timeFlowSlider != null)
		{
			_timeFlowSlider.Value = universeData.TimeFlowRate;
		}

		if (_oceanSlider != null)
		{
			_oceanSlider.Value = universeData.CurrentPlanet?.OceanCoverage ?? 0.35f;
		}

		if (_temperatureSlider != null)
		{
			_temperatureSlider.Value = universeData.CurrentPlanet?.Temperature ?? 0.5f;
		}

		if (_atmosphereSlider != null)
		{
			_atmosphereSlider.Value = universeData.CurrentPlanet?.AtmosphereDensity ?? 0.5f;
		}

		_planetTextureView?.ApplyCelestialConfig(universeData.CelestialPhysics);

		PopulatePlanetElementSelector(universeData.CurrentPlanet?.Element ?? PlanetElement.Terra);
		PopulateHierarchySelector(universeData.HierarchyConfig?.Archetype ?? HierarchyArchetype.Standard);
	}

	private void ApplyRandomValuesToControls()
	{
		if (_lawAlignmentSlider != null)
		{
			_lawAlignmentSlider.Value = _random.Next(0, 101);
		}

		if (_civilizationDensitySlider != null)
		{
			_civilizationDensitySlider.Value = _random.Next(10, 101);
		}

		if (_timeFlowSlider != null)
		{
			_timeFlowSlider.Value = _random.NextDouble() * 2.5 + 0.25;
		}

		if (_planetElementSelector != null && _planetElementSelector.ItemCount > 0)
		{
			_planetElementSelector.Select(_random.Next(0, _planetElementSelector.ItemCount));
		}

		if (_oceanSlider != null)
		{
			_oceanSlider.Value = _random.NextDouble() * 0.9;
		}

		if (_temperatureSlider != null)
		{
			_temperatureSlider.Value = _random.NextDouble();
		}

		if (_atmosphereSlider != null)
		{
			_atmosphereSlider.Value = _random.NextDouble();
		}

		if (_hierarchySelector != null && _hierarchySelector.ItemCount > 0)
		{
			_hierarchySelector.Select(_random.Next(0, _hierarchySelector.ItemCount));
		}
	}

	private void PullControlsToUniverse()
	{
		EnsureUniverseData();

		_tempUniverse.LawAlignment = _lawAlignmentSlider != null
			? Mathf.Clamp((int)Math.Round(_lawAlignmentSlider.Value), 0, 100)
			: _tempUniverse.LawAlignment;

		_tempUniverse.CivilizationDensity = _civilizationDensitySlider != null
			? Mathf.Clamp((int)Math.Round(_civilizationDensitySlider.Value), 0, 100)
			: _tempUniverse.CivilizationDensity;

		_tempUniverse.TimeFlowRate = _timeFlowSlider != null
			? Mathf.Clamp((float)_timeFlowSlider.Value, 0.25f, 3f)
			: _tempUniverse.TimeFlowRate;

		if (_planetElementSelector != null)
		{
			int selectedIndex = Mathf.Clamp(_planetElementSelector.Selected, 0, _planetElementSelector.ItemCount - 1);
			int itemId = _planetElementSelector.GetItemId(selectedIndex);
			_tempUniverse.CurrentPlanet.Element = (PlanetElement)itemId;
		}

		_tempUniverse.CurrentPlanet.OceanCoverage = _oceanSlider != null
			? Mathf.Clamp((float)_oceanSlider.Value, 0f, 1f)
			: _tempUniverse.CurrentPlanet.OceanCoverage;

		_tempUniverse.CurrentPlanet.Temperature = _temperatureSlider != null
			? Mathf.Clamp((float)_temperatureSlider.Value, 0f, 1f)
			: _tempUniverse.CurrentPlanet.Temperature;

		_tempUniverse.CurrentPlanet.AtmosphereDensity = _atmosphereSlider != null
			? Mathf.Clamp((float)_atmosphereSlider.Value, 0f, 1f)
			: _tempUniverse.CurrentPlanet.AtmosphereDensity;

		if (_hierarchySelector != null)
		{
			int selectedIndex = Mathf.Clamp(_hierarchySelector.Selected, 0, _hierarchySelector.ItemCount - 1);
			int itemId = _hierarchySelector.GetItemId(selectedIndex);
			_tempUniverse.HierarchyConfig = HierarchyConfigData.CreateFromArchetype((HierarchyArchetype)itemId);
		}

		_tempUniverse.CurrentPlanet.Name = ResolvePlanetName(_tempUniverse.CurrentPlanet.Element);
		_tempUniverse.AestheticTheme = ResolveAestheticTheme(_tempUniverse.LawAlignment);
	}

	private void EnsureUniverseData()
	{
		if (_tempUniverse == null)
		{
			_tempUniverse = BuildDefaultUniverse();
		}

		if (_tempUniverse.CurrentPlanet == null)
		{
			_tempUniverse.CurrentPlanet = new PlanetData();
		}

		if (_tempUniverse.CelestialPhysics == null)
		{
			_tempUniverse.CelestialPhysics = CelestialSystemPhysicsConfig.CreateDefault();
		}
	}

	private static UniverseData BuildDefaultUniverse()
	{
		return new UniverseData
		{
			Name = "太虚幻境",
			LawAlignment = 50,
			AestheticTheme = "Steampunk",
			CivilizationDensity = 55,
			TimeFlowRate = 1.0f,
			HierarchyConfig = HierarchyConfigData.CreateFromArchetype(HierarchyArchetype.Standard),
			CurrentPlanet = new PlanetData
			{
				Name = "后土",
				Element = PlanetElement.Terra,
				Size = PlanetSize.Medium,
				OceanCoverage = 0.35f,
				Temperature = 0.5f,
				AtmosphereDensity = 0.55f
			},
			CelestialPhysics = CelestialSystemPhysicsConfig.CreateDefault()
		};
	}
	private static UniverseData CloneUniverse(UniverseData source)
	{
		if (source == null)
		{
			return null;
		}

		float[] planetTerrain = null;
		if (source.PlanetTerrainHeightmap != null)
		{
			planetTerrain = new float[source.PlanetTerrainHeightmap.Length];
			Array.Copy(source.PlanetTerrainHeightmap, planetTerrain, planetTerrain.Length);
		}

		return new UniverseData
		{
			UniverseId = source.UniverseId,
			Name = source.Name,
			LawAlignment = source.LawAlignment,
			AestheticTheme = source.AestheticTheme,
			CivilizationDensity = source.CivilizationDensity,
			TimeFlowRate = source.TimeFlowRate,
			HierarchyConfig = source.HierarchyConfig == null
				? HierarchyConfigData.CreateFromArchetype(HierarchyArchetype.Standard)
				: new HierarchyConfigData
				{
					Archetype = source.HierarchyConfig.Archetype,
					LevelCount = source.HierarchyConfig.LevelCount,
					WorldCellCount = source.HierarchyConfig.WorldCellCount,
					CountryCellCount = source.HierarchyConfig.CountryCellCount,
					ProvinceCellCount = source.HierarchyConfig.ProvinceCellCount,
					CityCellCount = source.HierarchyConfig.CityCellCount
				},
			CurrentPlanet = source.CurrentPlanet == null
				? new PlanetData()
				: new PlanetData
				{
					PlanetId = source.CurrentPlanet.PlanetId,
					Name = source.CurrentPlanet.Name,
					Element = source.CurrentPlanet.Element,
					Size = source.CurrentPlanet.Size,
					OceanCoverage = source.CurrentPlanet.OceanCoverage,
					Temperature = source.CurrentPlanet.Temperature,
					AtmosphereDensity = source.CurrentPlanet.AtmosphereDensity
				},
			CelestialPhysics = source.CelestialPhysics?.DuplicateConfig()
				?? CelestialSystemPhysicsConfig.CreateDefault(),
			PlanetTerrainHeightmap = planetTerrain,
			PlanetTerrainWidth = source.PlanetTerrainWidth,
			PlanetTerrainHeight = source.PlanetTerrainHeight
		};
	}
	private bool IsChineseMode()
	{
		string language = _translationManager?.CurrentLanguage ?? "zh-CN";
		return language.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
	}

	private static string ResolveAestheticTheme(int lawAlignment)
	{
		if (lawAlignment <= 24)
		{
			return "Wuxia";
		}

		if (lawAlignment <= 49)
		{
			return "HighFantasy";
		}

		if (lawAlignment <= 74)
		{
			return "Steampunk";
		}

		return "Cyberpunk";
	}

	private static string ResolvePlanetName(PlanetElement element)
	{
		return element switch
		{
			PlanetElement.Terra => "后土",
			PlanetElement.Pyro => "祝融",
			PlanetElement.Cryo => "玄冥",
			PlanetElement.Aero => "罡风",
			_ => "后土"
		};
	}
}
