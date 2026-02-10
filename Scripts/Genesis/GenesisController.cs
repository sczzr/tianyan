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
	private Label _modeSectionTitle;
	private Label _visualStyleLabel;
	private Label _inspectorTargetLabel;
	private Label _lawValueLabel;
	private Label _civilizationValueLabel;
	private Label _timeFlowValueLabel;
	private Label _mountainIntensityValueLabel;
	private Label _polarCoverageValueLabel;
	private Label _desertRatioValueLabel;
	private Label _tectonicPlateValueLabel;
	private Label _windCellValueLabel;
	private Label _erosionIterationsValueLabel;
	private Label _erosionStrengthValueLabel;
	private Label _heatFactorValueLabel;
	private Label _terrainPresetLabel;
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
	private HSlider _mountainIntensitySlider;
	private HSlider _polarCoverageSlider;
	private HSlider _desertRatioSlider;
	private HSlider _tectonicPlateSlider;
	private HSlider _windCellSlider;
	private HSlider _erosionIterationsSlider;
	private HSlider _erosionStrengthSlider;
	private HSlider _heatFactorSlider;
	private OptionButton _terrainPresetSelector;
	private VBoxContainer _godAdvancedContainer;
	private OptionButton _experienceModeSelector;
	private OptionButton _visualStyleSelector;
	private OptionButton _inspectorTargetSelector;
	private OptionButton _hierarchySelector;
	private Button _starNodeButton;
	private Button _planetNodeButton;
	private Button _explorerModeButton;
	private Button _godModeButton;
	private Button _godAdvancedToggleButton;
	private Button _simulationStyleButton;
	private Button _animationStyleButton;
	private Label _modeTierHintLabel;
	private Button _quickStartButton;
	private Button _generateButton;
	private CheckBox _showMoonOrbitToggle;
	private Label _showMoonOrbitLabel;
	private CheckBox _showPlanetOrbitToggle;
	private Label _showPlanetOrbitLabel;

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
	private bool _isHardcoreMode;
	private bool _isGodAdvancedExpanded;
	private bool _isAnimationStyle;
	private bool _isInspectingStar = true;
	private bool _isSyncingGenerationProfileControls;
	private bool _isTooltipsApplied;
	private bool _isSyncingTerrainPresetSelector;

	private enum TerrainProfilePreset
	{
		Custom = -1,
		Balanced = 0,
		ContinentalPlates = 1,
		ArchipelagoSeas = 2,
		FrozenWastes = 3,
		VolcanicChaos = 4
	}

	private const float CivilizationOceanCoverage = 0.56f;
	private const float CivilizationTemperature = 0.52f;
	private const float CivilizationAtmosphereDensity = 0.62f;

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

		if (_modeSectionTitle != null)
		{
			_modeSectionTitle.Text = isZh ? "检查器配置" : "Inspector Config";
		}

		if (_visualStyleLabel != null)
		{
			_visualStyleLabel.Text = isZh ? "视觉风格" : "Visual Style";
		}

		if (_inspectorTargetLabel != null)
		{
			_inspectorTargetLabel.Text = isZh ? "检查器目标" : "Inspector Target";
		}

		if (_starNodeButton != null)
		{
			_starNodeButton.Text = isZh ? "☀️ 太一 (Sun)" : "☀️ Taiyi (Sun)";
		}

		if (_planetNodeButton != null)
		{
			_planetNodeButton.Text = isZh ? "🪐 后土 (Terra)" : "🪐 Houtu (Terra)";
		}

		if (_explorerModeButton != null)
		{
			_explorerModeButton.Text = isZh ? "探索 (Explorer)" : "Explorer";
		}

		if (_godModeButton != null)
		{
			_godModeButton.Text = isZh ? "上帝 (God Mode)" : "God Mode";
		}

		if (_simulationStyleButton != null)
		{
			_simulationStyleButton.Text = isZh ? "仿真风格" : "Simulation";
		}

		if (_animationStyleButton != null)
		{
			_animationStyleButton.Text = isZh ? "动画风格" : "Animation";
		}

		if (_modeTierHintLabel != null)
		{
			_modeTierHintLabel.Text = isZh
				? "探索模式：文明预设 · 上帝模式：展开高级参数"
				: "Explorer: civilization preset · God Mode: expand advanced controls";
		}

		UpdateGodAdvancedToggleLabel();

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
			_showMoonOrbitToggle.Text = string.Empty;
		}
		if (_showMoonOrbitLabel != null)
		{
			_showMoonOrbitLabel.Text = isZh ? "显示卫星轨道" : "Show Satellite Orbit";
		}

		if (_showPlanetOrbitToggle != null)
		{
			_showPlanetOrbitToggle.Text = string.Empty;
		}
		if (_showPlanetOrbitLabel != null)
		{
			_showPlanetOrbitLabel.Text = isZh ? "显示行星轨道" : "Show Planet Orbit";
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
		PopulateExperienceModeSelector();
		PopulateVisualStyleSelector();
		PopulateInspectorTargetSelector();
		PopulateTerrainPresetSelector();
		ApplyInspectorMode();
		ApplyInspectorTarget();
		ApplyVisualStylePreset();
		RefreshAllViews();
	}

	private void CacheNodes()
	{
		_hubTitle = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/HubTitle");
		_hubSubtitle = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/HubSubtitle");
		_modeSectionTitle = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/ModeSection/ModeSectionTitle");
		_visualStyleLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/ModeSection/VisualStyleLabel");
		_inspectorTargetLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/ModeSection/InspectorTargetLabel");
		_lawSectionTitle = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/LawSection/LawHeader/LawSectionTitle");
		_planetSectionTitle = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/PlanetHeader/PlanetSectionTitle");
		_hierarchySectionTitle = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/HierarchySection/HierarchyHeader/HierarchySectionTitle");
		_orbitSectionTitle = GetNodeOrNull<Label>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitSection/OrbitSectionVBox/OrbitSectionTitle");
		_lawValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/LawSection/LawValueLabel");
		_civilizationValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/LawSection/CivilizationValueLabel");
		_timeFlowValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/LawSection/TimeFlowValueLabel");
		_mountainIntensityValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/MountainIntensityValueLabel");
		_polarCoverageValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/PolarCoverageValueLabel");
		_desertRatioValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/DesertRatioValueLabel");
		_tectonicPlateValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/TectonicPlateValueLabel");
		_windCellValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/WindCellValueLabel");
		_erosionIterationsValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/ErosionIterationsValueLabel");
		_erosionStrengthValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/ErosionStrengthValueLabel");
		_heatFactorValueLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/HeatFactorValueLabel");
		_terrainPresetLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/TerrainPresetLabel");
		_godAdvancedToggleButton = GetNodeOrNull<Button>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedToggleButton");
		_godAdvancedContainer = GetNodeOrNull<VBoxContainer>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer");
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
		_mountainIntensityValueLabel ??= GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/MountainIntensityValueLabel");
		_polarCoverageValueLabel ??= GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/PolarCoverageValueLabel");
		_desertRatioValueLabel ??= GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/DesertRatioValueLabel");
		_tectonicPlateValueLabel ??= GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/TectonicPlateValueLabel");
		_windCellValueLabel ??= GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/WindCellValueLabel");
		_erosionIterationsValueLabel ??= GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/ErosionIterationsValueLabel");
		_erosionStrengthValueLabel ??= GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/ErosionStrengthValueLabel");
		_heatFactorValueLabel ??= GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/HeatFactorValueLabel");
		_terrainPresetLabel ??= GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/TerrainPresetLabel");
		_mountainIntensitySlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/MountainIntensitySlider")
			?? GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/MountainIntensitySlider");
		_polarCoverageSlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/PolarCoverageSlider")
			?? GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/PolarCoverageSlider");
		_desertRatioSlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/DesertRatioSlider")
			?? GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/DesertRatioSlider");
		_tectonicPlateSlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/TectonicPlateSlider")
			?? GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/TectonicPlateSlider");
		_windCellSlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/WindCellSlider")
			?? GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/WindCellSlider");
		_erosionIterationsSlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/ErosionIterationsSlider")
			?? GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/ErosionIterationsSlider");
		_erosionStrengthSlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/ErosionStrengthSlider")
			?? GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/ErosionStrengthSlider");
		_heatFactorSlider = GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/HeatFactorSlider")
			?? GetNodeOrNull<HSlider>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/HeatFactorSlider");
		_terrainPresetSelector = GetNodeOrNull<OptionButton>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/GodAdvancedContainer/TerrainPresetSelector")
			?? GetNodeOrNull<OptionButton>("RootMargin/MainRow/LeftPanel/LeftVBox/PlanetSection/TerrainPresetSelector");
		_experienceModeSelector = GetNodeOrNull<OptionButton>("RootMargin/MainRow/LeftPanel/LeftVBox/ModeSection/ExperienceModeSelector");
		_visualStyleSelector = GetNodeOrNull<OptionButton>("RootMargin/MainRow/LeftPanel/LeftVBox/ModeSection/VisualStyleSelector");
		_inspectorTargetSelector = GetNodeOrNull<OptionButton>("RootMargin/MainRow/LeftPanel/LeftVBox/ModeSection/InspectorTargetSelector");
		_hierarchySelector = GetNodeOrNull<OptionButton>("RootMargin/MainRow/LeftPanel/LeftVBox/HierarchySection/HierarchySelector");
		_starNodeButton = GetNodeOrNull<Button>("RootMargin/MainRow/LeftPanel/LeftVBox/HierarchySection/HierarchyTree/StarNodeButton");
		_planetNodeButton = GetNodeOrNull<Button>("RootMargin/MainRow/LeftPanel/LeftVBox/HierarchySection/HierarchyTree/PlanetNodeButton");
		_explorerModeButton = GetNodeOrNull<Button>("RootMargin/MainRow/LeftPanel/LeftVBox/TopLeftModePanel/TopLeftModeVBox/ExperienceSwitch/ExplorerModeButton");
		_godModeButton = GetNodeOrNull<Button>("RootMargin/MainRow/LeftPanel/LeftVBox/TopLeftModePanel/TopLeftModeVBox/ExperienceSwitch/GodModeButton");
		_simulationStyleButton = GetNodeOrNull<Button>("RootMargin/MainRow/LeftPanel/LeftVBox/TopLeftModePanel/TopLeftModeVBox/StyleSwitch/SimulationStyleButton");
		_animationStyleButton = GetNodeOrNull<Button>("RootMargin/MainRow/LeftPanel/LeftVBox/TopLeftModePanel/TopLeftModeVBox/StyleSwitch/AnimationStyleButton");
		_modeTierHintLabel = GetNodeOrNull<Label>("RootMargin/MainRow/LeftPanel/LeftVBox/TopLeftModePanel/TopLeftModeVBox/ModeHintLabel");
		_quickStartButton = GetNodeOrNull<Button>("RootMargin/MainRow/LeftPanel/LeftVBox/ActionSection/ActionButtons/QuickStartButton");
		_generateButton = GetNodeOrNull<Button>("RootMargin/MainRow/LeftPanel/LeftVBox/ActionSection/ActionButtons/GenerateButton");
		_showMoonOrbitToggle = GetNodeOrNull<CheckBox>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitSection/OrbitSectionVBox/ShowMoonOrbitRow/ShowMoonOrbitToggle")
			?? GetNodeOrNull<CheckBox>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitSection/OrbitSectionVBox/ShowMoonOrbitToggle");
		_showMoonOrbitLabel = GetNodeOrNull<Label>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitSection/OrbitSectionVBox/ShowMoonOrbitRow/ShowMoonOrbitLabel")
			?? GetNodeOrNull<Label>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitSection/OrbitSectionVBox/ShowMoonOrbitLabel");
		_showPlanetOrbitToggle = GetNodeOrNull<CheckBox>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitSection/OrbitSectionVBox/ShowPlanetOrbitRow/ShowPlanetOrbitToggle")
			?? GetNodeOrNull<CheckBox>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitSection/OrbitSectionVBox/ShowPlanetOrbitToggle");
		_showPlanetOrbitLabel = GetNodeOrNull<Label>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitSection/OrbitSectionVBox/ShowPlanetOrbitRow/ShowPlanetOrbitLabel")
			?? GetNodeOrNull<Label>("RootMargin/MainRow/RightPanel/PreviewPanel/PreviewRoot/PlanetTexture/OrbitSection/OrbitSectionVBox/ShowPlanetOrbitLabel");
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
		};

		_themeManager = new ThemeManager(magicLayer, techLayer, textItems);
		_planetPreviewController = new PlanetPreviewController(_previewRoot, planetAura, _planetTextureView);
		_narrativeGenerator = new NarrativeGenerator();
	}

	private void BindEvents()
	{
		if (_experienceModeSelector != null)
		{
			_experienceModeSelector.ItemSelected += OnExperienceModeSelected;
		}

		if (_visualStyleSelector != null)
		{
			_visualStyleSelector.ItemSelected += OnVisualStyleSelected;
		}

		if (_inspectorTargetSelector != null)
		{
			_inspectorTargetSelector.ItemSelected += OnInspectorTargetSelected;
		}

		if (_starNodeButton != null)
		{
			_starNodeButton.Pressed += OnStarNodePressed;
		}

		if (_planetNodeButton != null)
		{
			_planetNodeButton.Pressed += OnPlanetNodePressed;
		}

		if (_explorerModeButton != null)
		{
			_explorerModeButton.Pressed += OnExplorerModePressed;
		}

		if (_godModeButton != null)
		{
			_godModeButton.Pressed += OnGodModePressed;
		}

		if (_godAdvancedToggleButton != null)
		{
			_godAdvancedToggleButton.Pressed += OnGodAdvancedTogglePressed;
		}

		if (_simulationStyleButton != null)
		{
			_simulationStyleButton.Pressed += OnSimulationStylePressed;
		}

		if (_animationStyleButton != null)
		{
			_animationStyleButton.Pressed += OnAnimationStylePressed;
		}

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

		if (_mountainIntensitySlider != null)
		{
			_mountainIntensitySlider.ValueChanged += OnMountainIntensityChanged;
		}

		if (_polarCoverageSlider != null)
		{
			_polarCoverageSlider.ValueChanged += OnPolarCoverageChanged;
		}

		if (_desertRatioSlider != null)
		{
			_desertRatioSlider.ValueChanged += OnDesertRatioChanged;
		}

		if (_tectonicPlateSlider != null)
		{
			_tectonicPlateSlider.ValueChanged += OnTectonicPlateChanged;
		}

		if (_windCellSlider != null)
		{
			_windCellSlider.ValueChanged += OnWindCellChanged;
		}

		if (_erosionIterationsSlider != null)
		{
			_erosionIterationsSlider.ValueChanged += OnErosionIterationsChanged;
		}

		if (_erosionStrengthSlider != null)
		{
			_erosionStrengthSlider.ValueChanged += OnErosionStrengthChanged;
		}

		if (_heatFactorSlider != null)
		{
			_heatFactorSlider.ValueChanged += OnHeatFactorChanged;
		}

		if (_terrainPresetSelector != null)
		{
			_terrainPresetSelector.ItemSelected += OnTerrainPresetSelected;
		}

		ApplyGenerationProfileTooltips();

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
		ApplyInspectorMode();
		ApplyInspectorTarget();
		ApplyVisualStylePreset();
		ApplyTopSwitchState();
	}

	private void OnLanguageChanged(string language)
	{
		UpdateLocalizedText();
		ApplyGenerationProfileTooltips();
		SelectTerrainPreset(DetectPreset(_tempUniverse?.PlanetGenerationProfile ?? PlanetGenerationProfile.Default));
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
		_planetPreviewController.SetSkyTexturePath(_planetTextureView.SelectedSkyTexturePath);

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

	private void OnMountainIntensityChanged(double value)
	{
		NotifyLeftPanelEditing();
		EnsureUniverseData();
		_tempUniverse.CurrentPlanet.MountainIntensity = Mathf.Clamp((float)value, 0f, 1f);
		OnPlanetParamsChanged();
	}

	private void OnPolarCoverageChanged(double value)
	{
		NotifyLeftPanelEditing();
		EnsureUniverseData();
		_tempUniverse.CurrentPlanet.PolarCoverage = Mathf.Clamp((float)value, 0f, 1f);
		OnPlanetParamsChanged();
	}

	private void OnDesertRatioChanged(double value)
	{
		NotifyLeftPanelEditing();
		EnsureUniverseData();
		_tempUniverse.CurrentPlanet.DesertRatio = Mathf.Clamp((float)value, 0f, 1f);
		OnPlanetParamsChanged();
	}

	private void OnTectonicPlateChanged(double value)
	{
		if (_isSyncingGenerationProfileControls)
		{
			return;
		}

		NotifyLeftPanelEditing();
		EnsureUniverseData();
		var profile = _tempUniverse.PlanetGenerationProfile;
		_tempUniverse.PlanetGenerationProfile = new PlanetGenerationProfile(
			Mathf.Clamp(Mathf.RoundToInt((float)value), 1, 64),
			profile.WindCellCount,
			profile.ErosionIterations,
			profile.ErosionStrength,
			profile.HeatFactor,
			profile.ContinentalFrequency,
			profile.ReliefStrength,
			profile.MoistureTransport,
			profile.PlateBoundarySharpness);
		ApplyPlanetProfileToPreview();
		SelectTerrainPreset(DetectPreset(_tempUniverse.PlanetGenerationProfile));
		UpdateValueLabels();
	}

	private void OnWindCellChanged(double value)
	{
		if (_isSyncingGenerationProfileControls)
		{
			return;
		}

		NotifyLeftPanelEditing();
		EnsureUniverseData();
		var profile = _tempUniverse.PlanetGenerationProfile;
		_tempUniverse.PlanetGenerationProfile = new PlanetGenerationProfile(
			profile.TectonicPlateCount,
			Mathf.Clamp(Mathf.RoundToInt((float)value), 1, 24),
			profile.ErosionIterations,
			profile.ErosionStrength,
			profile.HeatFactor,
			profile.ContinentalFrequency,
			profile.ReliefStrength,
			profile.MoistureTransport,
			profile.PlateBoundarySharpness);
		ApplyPlanetProfileToPreview();
		SelectTerrainPreset(DetectPreset(_tempUniverse.PlanetGenerationProfile));
		UpdateValueLabels();
	}

	private void OnErosionIterationsChanged(double value)
	{
		if (_isSyncingGenerationProfileControls)
		{
			return;
		}

		NotifyLeftPanelEditing();
		EnsureUniverseData();
		var profile = _tempUniverse.PlanetGenerationProfile;
		_tempUniverse.PlanetGenerationProfile = new PlanetGenerationProfile(
			profile.TectonicPlateCount,
			profile.WindCellCount,
			Mathf.Clamp(Mathf.RoundToInt((float)value), 0, 16),
			profile.ErosionStrength,
			profile.HeatFactor,
			profile.ContinentalFrequency,
			profile.ReliefStrength,
			profile.MoistureTransport,
			profile.PlateBoundarySharpness);
		ApplyPlanetProfileToPreview();
		SelectTerrainPreset(DetectPreset(_tempUniverse.PlanetGenerationProfile));
		UpdateValueLabels();
	}

	private void OnErosionStrengthChanged(double value)
	{
		if (_isSyncingGenerationProfileControls)
		{
			return;
		}

		NotifyLeftPanelEditing();
		EnsureUniverseData();
		var profile = _tempUniverse.PlanetGenerationProfile;
		_tempUniverse.PlanetGenerationProfile = new PlanetGenerationProfile(
			profile.TectonicPlateCount,
			profile.WindCellCount,
			profile.ErosionIterations,
			Mathf.Clamp((float)value, 0f, 1f),
			profile.HeatFactor,
			profile.ContinentalFrequency,
			profile.ReliefStrength,
			profile.MoistureTransport,
			profile.PlateBoundarySharpness);
		ApplyPlanetProfileToPreview();
		SelectTerrainPreset(DetectPreset(_tempUniverse.PlanetGenerationProfile));
		UpdateValueLabels();
	}

	private void OnHeatFactorChanged(double value)
	{
		if (_isSyncingGenerationProfileControls)
		{
			return;
		}

		NotifyLeftPanelEditing();
		EnsureUniverseData();
		var profile = _tempUniverse.PlanetGenerationProfile;
		_tempUniverse.PlanetGenerationProfile = new PlanetGenerationProfile(
			profile.TectonicPlateCount,
			profile.WindCellCount,
			profile.ErosionIterations,
			profile.ErosionStrength,
			Mathf.Clamp((float)value, 1f, 1000f),
			profile.ContinentalFrequency,
			profile.ReliefStrength,
			profile.MoistureTransport,
			profile.PlateBoundarySharpness);
		ApplyPlanetProfileToPreview();
		SelectTerrainPreset(DetectPreset(_tempUniverse.PlanetGenerationProfile));
		UpdateValueLabels();
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
			if (!IsProfileValid(payload.PlanetGenerationProfile))
			{
				payload.PlanetGenerationProfile = PlanetGenerationProfile.FromPlanet(payload.CurrentPlanet, payload.LawAlignment);
			}

			_planetPreviewController?.SetGenerationProfile(payload.PlanetGenerationProfile);
			_planetTextureView?.SetGenerationProfile(payload.PlanetGenerationProfile);

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
		ApplyPlanetProfileToPreview();
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

		ApplyPlanetProfileToPreview();
		_themeManager?.UpdateVisuals(_tempUniverse.LawAlignment);
		_planetPreviewController?.SetCelestialSystemConfig(_tempUniverse.CelestialPhysics);
		_planetPreviewController?.UpdateUniverseMood(_tempUniverse.LawAlignment);
		_planetPreviewController?.UpdateShader(_tempUniverse.CurrentPlanet);
		RefreshNarrative();
		UpdateValueLabels();
	}

	private void ApplyPlanetProfileToPreview()
	{
		if (_tempUniverse == null)
		{
			return;
		}

		var currentProfile = _tempUniverse.PlanetGenerationProfile;
		var inferredProfile = PlanetGenerationProfile.FromPlanet(_tempUniverse.CurrentPlanet, _tempUniverse.LawAlignment);
		float baseHeat = inferredProfile.HeatFactor;

		float heatScale = baseHeat > 0.001f
			? Mathf.Clamp(currentProfile.HeatFactor / baseHeat, 0.5f, 1.5f)
			: 1f;
		float continentalScale = baseHeat > 0.001f
			? Mathf.Clamp(currentProfile.ContinentalFrequency / Mathf.Max(inferredProfile.ContinentalFrequency, 0.0001f), 0.7f, 1.4f)
			: 1f;
		float reliefScale = baseHeat > 0.001f
			? Mathf.Clamp(currentProfile.ReliefStrength / Mathf.Max(inferredProfile.ReliefStrength, 0.0001f), 0.7f, 1.35f)
			: 1f;
		float moistureScale = baseHeat > 0.001f
			? Mathf.Clamp(currentProfile.MoistureTransport / Mathf.Max(inferredProfile.MoistureTransport, 0.0001f), 0.7f, 1.35f)
			: 1f;
		float boundaryScale = baseHeat > 0.001f
			? Mathf.Clamp(currentProfile.PlateBoundarySharpness / Mathf.Max(inferredProfile.PlateBoundarySharpness, 0.0001f), 0.7f, 1.35f)
			: 1f;

		_tempUniverse.PlanetGenerationProfile = new PlanetGenerationProfile(
			currentProfile.TectonicPlateCount,
			currentProfile.WindCellCount,
			currentProfile.ErosionIterations,
			currentProfile.ErosionStrength,
			Mathf.Clamp(inferredProfile.HeatFactor * heatScale, 1f, 1000f),
			Mathf.Clamp(inferredProfile.ContinentalFrequency * continentalScale, 0.08f, 2.5f),
			Mathf.Clamp(inferredProfile.ReliefStrength * reliefScale, 0f, 1f),
			Mathf.Clamp(inferredProfile.MoistureTransport * moistureScale, 0f, 1f),
			Mathf.Clamp(inferredProfile.PlateBoundarySharpness * boundaryScale, 1f, 24f));

		_planetPreviewController?.SetGenerationProfile(_tempUniverse.PlanetGenerationProfile);
		_planetTextureView?.SetGenerationProfile(_tempUniverse.PlanetGenerationProfile);
		ApplyProfileToSliders(_tempUniverse.PlanetGenerationProfile);
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
		var generationProfile = _tempUniverse.PlanetGenerationProfile;
		int mountainPercent = Mathf.RoundToInt(_tempUniverse.CurrentPlanet.MountainIntensity * 100f);
		int polarPercent = Mathf.RoundToInt(_tempUniverse.CurrentPlanet.PolarCoverage * 100f);
		int desertPercent = Mathf.RoundToInt(_tempUniverse.CurrentPlanet.DesertRatio * 100f);

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

		if (_mountainIntensityValueLabel != null)
		{
			_mountainIntensityValueLabel.Text = isZh
				? $"山脉强度：{mountainPercent}%"
				: $"Mountain Intensity: {mountainPercent}%";
		}

		if (_polarCoverageValueLabel != null)
		{
			_polarCoverageValueLabel.Text = isZh
				? $"极地覆盖：{polarPercent}%"
				: $"Polar Coverage: {polarPercent}%";
		}

		if (_desertRatioValueLabel != null)
		{
			_desertRatioValueLabel.Text = isZh
				? $"荒漠比例：{desertPercent}%"
				: $"Desert Ratio: {desertPercent}%";
		}

		if (_tectonicPlateValueLabel != null)
		{
			_tectonicPlateValueLabel.Text = _translationManager?.TrWithFormat(
				"genesis_profile_tectonic_plates_value",
				generationProfile.TectonicPlateCount.ToString())
				?? (isZh
					? $"板块数量：{generationProfile.TectonicPlateCount}"
					: $"Tectonic Plates: {generationProfile.TectonicPlateCount}");
		}

		if (_windCellValueLabel != null)
		{
			_windCellValueLabel.Text = _translationManager?.TrWithFormat(
				"genesis_profile_wind_cells_value",
				generationProfile.WindCellCount.ToString())
				?? (isZh
					? $"风场单元：{generationProfile.WindCellCount}"
					: $"Wind Cells: {generationProfile.WindCellCount}");
		}

		if (_erosionIterationsValueLabel != null)
		{
			_erosionIterationsValueLabel.Text = _translationManager?.TrWithFormat(
				"genesis_profile_erosion_iterations_value",
				generationProfile.ErosionIterations.ToString())
				?? (isZh
					? $"侵蚀迭代：{generationProfile.ErosionIterations}"
					: $"Erosion Iterations: {generationProfile.ErosionIterations}");
		}

		if (_erosionStrengthValueLabel != null)
		{
			int erosionPercent = Mathf.RoundToInt(generationProfile.ErosionStrength * 100f);
			_erosionStrengthValueLabel.Text = _translationManager?.TrWithFormat(
				"genesis_profile_erosion_strength_value",
				erosionPercent.ToString())
				?? (isZh
					? $"侵蚀强度：{erosionPercent}%"
					: $"Erosion Strength: {erosionPercent}%");
		}

		if (_heatFactorValueLabel != null)
		{
			int heatFactor = Mathf.RoundToInt(generationProfile.HeatFactor);
			_heatFactorValueLabel.Text = _translationManager?.TrWithFormat(
				"genesis_profile_heat_factor_value",
				heatFactor.ToString())
				?? (isZh
					? $"热量因子：{heatFactor}"
					: $"Heat Factor: {heatFactor}");
		}

		if (!_isTooltipsApplied)
		{
			ApplyGenerationProfileTooltips();
		}
	}

	private void ApplyGenerationProfileTooltips()
	{
		SetControlTooltip(_tectonicPlateValueLabel, "genesis_profile_tectonic_plates_tip");
		SetControlTooltip(_tectonicPlateSlider, "genesis_profile_tectonic_plates_tip");
		SetControlTooltip(_windCellValueLabel, "genesis_profile_wind_cells_tip");
		SetControlTooltip(_windCellSlider, "genesis_profile_wind_cells_tip");
		SetControlTooltip(_erosionIterationsValueLabel, "genesis_profile_erosion_iterations_tip");
		SetControlTooltip(_erosionIterationsSlider, "genesis_profile_erosion_iterations_tip");
		SetControlTooltip(_erosionStrengthValueLabel, "genesis_profile_erosion_strength_tip");
		SetControlTooltip(_erosionStrengthSlider, "genesis_profile_erosion_strength_tip");
		SetControlTooltip(_heatFactorValueLabel, "genesis_profile_heat_factor_tip");
		SetControlTooltip(_heatFactorSlider, "genesis_profile_heat_factor_tip");
		SetControlTooltip(_terrainPresetLabel, "genesis_profile_preset_tip");
		SetControlTooltip(_terrainPresetSelector, "genesis_profile_preset_tip");
		_isTooltipsApplied = true;
	}

	private void SetControlTooltip(Control control, string translationKey)
	{
		if (control == null)
		{
			return;
		}

		control.TooltipText = _translationManager?.Tr(translationKey) ?? string.Empty;
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

	private void PopulateExperienceModeSelector()
	{
		if (_experienceModeSelector == null)
		{
			return;
		}

		bool isZh = IsChineseMode();
		_experienceModeSelector.Clear();
		_experienceModeSelector.AddItem(isZh ? "探索 (Explorer)" : "Explorer", 0);
		_experienceModeSelector.AddItem(isZh ? "上帝 (God Mode)" : "God Mode", 1);
		SelectOptionById(_experienceModeSelector, _isHardcoreMode ? 1 : 0);
	}

	private void PopulateVisualStyleSelector()
	{
		if (_visualStyleSelector == null)
		{
			return;
		}

		bool isZh = IsChineseMode();
		_visualStyleSelector.Clear();
		_visualStyleSelector.AddItem(isZh ? "仿真风格 (Simulation)" : "Simulation", 0);
		_visualStyleSelector.AddItem(isZh ? "动画风格 (Animation)" : "Animation", 1);
		SelectOptionById(_visualStyleSelector, _isAnimationStyle ? 1 : 0);
	}

	private void PopulateInspectorTargetSelector()
	{
		if (_inspectorTargetSelector == null)
		{
			return;
		}

		bool isZh = IsChineseMode();
		_inspectorTargetSelector.Clear();
		_inspectorTargetSelector.AddItem(isZh ? "恒星参数" : "Star Parameters", 0);
		_inspectorTargetSelector.AddItem(isZh ? "行星参数" : "Planet Parameters", 1);
		SelectOptionById(_inspectorTargetSelector, _isInspectingStar ? 0 : 1);
	}

	private void PopulateTerrainPresetSelector()
	{
		bool isZh = IsChineseMode();
		if (_terrainPresetLabel != null)
		{
			_terrainPresetLabel.Text = _translationManager?.Tr("genesis_profile_preset_label")
				?? (isZh ? "地形预设" : "Terrain Preset");
		}

		if (_terrainPresetSelector == null)
		{
			return;
		}

		_terrainPresetSelector.Clear();
		_terrainPresetSelector.AddItem(
			_translationManager?.Tr("genesis_profile_preset_balanced")
				?? (isZh ? "平衡风格" : "Balanced"),
			(int)TerrainProfilePreset.Balanced);
		_terrainPresetSelector.AddItem(
			_translationManager?.Tr("genesis_profile_preset_continental")
				?? (isZh ? "大陆板块" : "Continental Plates"),
			(int)TerrainProfilePreset.ContinentalPlates);
		_terrainPresetSelector.AddItem(
			_translationManager?.Tr("genesis_profile_preset_archipelago")
				?? (isZh ? "群岛海洋" : "Archipelago Seas"),
			(int)TerrainProfilePreset.ArchipelagoSeas);
		_terrainPresetSelector.AddItem(
			_translationManager?.Tr("genesis_profile_preset_frozen")
				?? (isZh ? "极寒荒原" : "Frozen Wastes"),
			(int)TerrainProfilePreset.FrozenWastes);
		_terrainPresetSelector.AddItem(
			_translationManager?.Tr("genesis_profile_preset_volcanic")
				?? (isZh ? "火山乱流" : "Volcanic Chaos"),
			(int)TerrainProfilePreset.VolcanicChaos);

		TerrainProfilePreset preset = DetectPreset(_tempUniverse?.PlanetGenerationProfile ?? PlanetGenerationProfile.Default);
		SelectTerrainPreset(preset);
	}

	private void SelectTerrainPreset(TerrainProfilePreset preset)
	{
		if (_terrainPresetSelector == null || _terrainPresetSelector.ItemCount <= 0)
		{
			return;
		}

		int targetId = preset == TerrainProfilePreset.Custom
			? (int)TerrainProfilePreset.Balanced
			: (int)preset;

		_isSyncingTerrainPresetSelector = true;
		try
		{
			SelectOptionById(_terrainPresetSelector, targetId);
		}
		finally
		{
			_isSyncingTerrainPresetSelector = false;
		}
	}

	private static void SelectOptionById(OptionButton selector, int itemId)
	{
		if (selector == null || selector.ItemCount <= 0)
		{
			return;
		}

		int index = selector.GetItemIndex(itemId);
		if (index >= 0)
		{
			selector.Select(index);
		}
	}

	private void OnTerrainPresetSelected(long index)
	{
		if (_terrainPresetSelector == null || _isSyncingTerrainPresetSelector)
		{
			return;
		}

		NotifyLeftPanelEditing();
		EnsureUniverseData();

		int presetId = _terrainPresetSelector.GetItemId((int)index);
		var preset = (TerrainProfilePreset)presetId;
		ApplyTerrainPreset(preset);
		ApplyPlanetProfileToPreview();
		UpdateValueLabels();
	}

	private void ApplyTerrainPreset(TerrainProfilePreset preset)
	{
		var current = _tempUniverse?.PlanetGenerationProfile ?? PlanetGenerationProfile.Default;
		PlanetGenerationProfile profile = preset switch
		{
			TerrainProfilePreset.Balanced => new PlanetGenerationProfile(24, 8, 4, 0.16f, 560f, 0.62f, 0.38f, 0.58f, 6.2f),
			TerrainProfilePreset.ContinentalPlates => new PlanetGenerationProfile(28, 7, 4, 0.15f, 575f, 0.7f, 0.4f, 0.56f, 6.8f),
			TerrainProfilePreset.ArchipelagoSeas => new PlanetGenerationProfile(14, 12, 6, 0.24f, 500f, 0.4f, 0.34f, 0.72f, 5.8f),
			TerrainProfilePreset.FrozenWastes => new PlanetGenerationProfile(18, 8, 3, 0.14f, 820f, 0.55f, 0.36f, 0.42f, 6.0f),
			TerrainProfilePreset.VolcanicChaos => new PlanetGenerationProfile(36, 10, 6, 0.3f, 360f, 0.58f, 0.7f, 0.48f, 10.0f),
			_ => current
		};

		if (_tempUniverse?.CurrentPlanet != null)
		{
			_tempUniverse.CurrentPlanet.OceanCoverage = preset switch
			{
				TerrainProfilePreset.ArchipelagoSeas => 0.68f,
				TerrainProfilePreset.FrozenWastes => 0.54f,
				_ => CivilizationOceanCoverage
			};

			_tempUniverse.CurrentPlanet.Temperature = preset switch
			{
				TerrainProfilePreset.FrozenWastes => 0.18f,
				TerrainProfilePreset.VolcanicChaos => 0.82f,
				_ => CivilizationTemperature
			};

			_tempUniverse.CurrentPlanet.AtmosphereDensity = preset switch
			{
				TerrainProfilePreset.VolcanicChaos => 0.72f,
				_ => CivilizationAtmosphereDensity
			};
		}

		_tempUniverse.PlanetGenerationProfile = profile;
		ApplyProfileToSliders(profile);
	}

	private TerrainProfilePreset DetectPreset(PlanetGenerationProfile profile)
	{
		if (IsPresetClose(profile, 24, 8, 4, 0.16f, 560f))
		{
			return TerrainProfilePreset.Balanced;
		}

		if (IsPresetClose(profile, 28, 7, 4, 0.15f, 575f))
		{
			return TerrainProfilePreset.ContinentalPlates;
		}

		if (IsPresetClose(profile, 14, 12, 6, 0.24f, 500f))
		{
			return TerrainProfilePreset.ArchipelagoSeas;
		}

		if (IsPresetClose(profile, 18, 8, 3, 0.14f, 820f))
		{
			return TerrainProfilePreset.FrozenWastes;
		}

		if (IsPresetClose(profile, 36, 10, 6, 0.3f, 360f))
		{
			return TerrainProfilePreset.VolcanicChaos;
		}

		return TerrainProfilePreset.Custom;
	}

	private static bool IsPresetClose(
		PlanetGenerationProfile profile,
		int tectonicPlates,
		int windCells,
		int erosionIterations,
		float erosionStrength,
		float heatFactor)
	{
		return Math.Abs(profile.TectonicPlateCount - tectonicPlates) <= 1
			&& Math.Abs(profile.WindCellCount - windCells) <= 1
			&& Math.Abs(profile.ErosionIterations - erosionIterations) <= 1
			&& MathF.Abs(profile.ErosionStrength - erosionStrength) <= 0.025f
			&& MathF.Abs(profile.HeatFactor - heatFactor) <= 30f;
	}

	private void OnExperienceModeSelected(long index)
	{
		if (_experienceModeSelector == null)
		{
			return;
		}

		int id = _experienceModeSelector.GetItemId((int)index);
		bool hardcore = id == 1;
		if (_isHardcoreMode == hardcore)
		{
			return;
		}

		_isHardcoreMode = hardcore;
		if (!_isHardcoreMode)
		{
			_isGodAdvancedExpanded = false;
		}
		ApplyInspectorMode();
		ApplyTopSwitchState();
	}

	private void OnVisualStyleSelected(long index)
	{
		if (_visualStyleSelector == null)
		{
			return;
		}

		int id = _visualStyleSelector.GetItemId((int)index);
		bool animationStyle = id == 1;
		if (_isAnimationStyle == animationStyle)
		{
			return;
		}

		_isAnimationStyle = animationStyle;
		ApplyVisualStylePreset();
		ApplyTopSwitchState();
	}

	private void OnInspectorTargetSelected(long index)
	{
		if (_inspectorTargetSelector == null)
		{
			return;
		}

		int id = _inspectorTargetSelector.GetItemId((int)index);
		bool inspectStar = id == 0;
		if (_isInspectingStar == inspectStar)
		{
			return;
		}

		_isInspectingStar = inspectStar;
		ApplyInspectorTarget();
	}

	private void OnStarNodePressed()
	{
		_isInspectingStar = true;
		SelectOptionById(_inspectorTargetSelector, 0);
		ApplyInspectorTarget();
	}

	private void OnPlanetNodePressed()
	{
		_isInspectingStar = false;
		SelectOptionById(_inspectorTargetSelector, 1);
		ApplyInspectorTarget();
	}

	private void OnExplorerModePressed()
	{
		if (!_explorerModeButton.ButtonPressed)
		{
			_explorerModeButton.ButtonPressed = true;
			return;
		}

		_isHardcoreMode = false;
		_isGodAdvancedExpanded = false;
		SelectOptionById(_experienceModeSelector, 0);
		ApplyInspectorMode();
		ApplyTopSwitchState();
	}

	private void OnGodModePressed()
	{
		if (!_godModeButton.ButtonPressed)
		{
			_godModeButton.ButtonPressed = true;
			return;
		}

		_isHardcoreMode = true;
		_isGodAdvancedExpanded = true;
		SelectOptionById(_experienceModeSelector, 1);
		ApplyInspectorMode();
		ApplyTopSwitchState();
	}

	private void OnGodAdvancedTogglePressed()
	{
		if (!_isHardcoreMode)
		{
			return;
		}

		_isGodAdvancedExpanded = !_isGodAdvancedExpanded;
		ApplyInspectorMode();
	}

	private void OnSimulationStylePressed()
	{
		if (!_simulationStyleButton.ButtonPressed)
		{
			_simulationStyleButton.ButtonPressed = true;
			return;
		}

		_isAnimationStyle = false;
		SelectOptionById(_visualStyleSelector, 0);
		ApplyVisualStylePreset();
		ApplyTopSwitchState();
	}

	private void OnAnimationStylePressed()
	{
		if (!_animationStyleButton.ButtonPressed)
		{
			_animationStyleButton.ButtonPressed = true;
			return;
		}

		_isAnimationStyle = true;
		SelectOptionById(_visualStyleSelector, 1);
		ApplyVisualStylePreset();
		ApplyTopSwitchState();
	}

	private void ApplyInspectorMode()
	{
		_planetTextureView?.SetHardcoreMode(_isHardcoreMode);
		SetVisible(_godAdvancedToggleButton, _isHardcoreMode);

		bool advancedVisible = _isHardcoreMode && _isGodAdvancedExpanded;
		if (_godAdvancedContainer != null)
		{
			SetVisible(_godAdvancedContainer, advancedVisible);
		}

		SetVisible(_mountainIntensityValueLabel, advancedVisible);
		SetVisible(_mountainIntensitySlider, advancedVisible);
		SetVisible(_polarCoverageValueLabel, advancedVisible);
		SetVisible(_polarCoverageSlider, advancedVisible);
		SetVisible(_desertRatioValueLabel, advancedVisible);
		SetVisible(_desertRatioSlider, advancedVisible);
		SetVisible(_tectonicPlateValueLabel, advancedVisible);
		SetVisible(_tectonicPlateSlider, advancedVisible);
		SetVisible(_windCellValueLabel, advancedVisible);
		SetVisible(_windCellSlider, advancedVisible);
		SetVisible(_erosionIterationsValueLabel, advancedVisible);
		SetVisible(_erosionIterationsSlider, advancedVisible);
		SetVisible(_erosionStrengthValueLabel, advancedVisible);
		SetVisible(_erosionStrengthSlider, advancedVisible);
		SetVisible(_heatFactorValueLabel, advancedVisible);
		SetVisible(_heatFactorSlider, advancedVisible);
		SetVisible(_terrainPresetLabel, advancedVisible);
		SetVisible(_terrainPresetSelector, advancedVisible);
		UpdateGodAdvancedToggleLabel();
	}

	private void UpdateGodAdvancedToggleLabel()
	{
		if (_godAdvancedToggleButton == null)
		{
			return;
		}

		bool isZh = IsChineseMode();
		string prefix = _isGodAdvancedExpanded ? "▼" : "▶";
		_godAdvancedToggleButton.Text = isZh
			? $"{prefix} 高级地形参数"
			: $"{prefix} Advanced Terrain Controls";
	}

	private static void SetVisible(CanvasItem item, bool visible)
	{
		if (item != null)
		{
			item.Visible = visible;
		}
	}

	private void ApplyInspectorTarget()
	{
		_planetTextureView?.SetInspectorTarget(_isInspectingStar);

		if (_starNodeButton != null)
		{
			_starNodeButton.ButtonPressed = _isInspectingStar;
		}

		if (_planetNodeButton != null)
		{
			_planetNodeButton.ButtonPressed = !_isInspectingStar;
		}
	}

	private void ApplyVisualStylePreset()
	{
		_planetTextureView?.ApplyVisualStylePreset(_isAnimationStyle);
	}

	private void ApplyTopSwitchState()
	{
		if (_explorerModeButton != null)
		{
			_explorerModeButton.ButtonPressed = !_isHardcoreMode;
		}

		if (_godModeButton != null)
		{
			_godModeButton.ButtonPressed = _isHardcoreMode;
		}

		if (_simulationStyleButton != null)
		{
			_simulationStyleButton.ButtonPressed = !_isAnimationStyle;
		}

		if (_animationStyleButton != null)
		{
			_animationStyleButton.ButtonPressed = _isAnimationStyle;
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

		if (universeData.CurrentPlanet != null)
		{
			universeData.CurrentPlanet.OceanCoverage = CivilizationOceanCoverage;
			universeData.CurrentPlanet.Temperature = CivilizationTemperature;
			universeData.CurrentPlanet.AtmosphereDensity = CivilizationAtmosphereDensity;
		}

		if (_mountainIntensitySlider != null)
		{
			_mountainIntensitySlider.Value = universeData.CurrentPlanet?.MountainIntensity ?? 0.55f;
		}

		if (_polarCoverageSlider != null)
		{
			_polarCoverageSlider.Value = universeData.CurrentPlanet?.PolarCoverage ?? 0.55f;
		}

		if (_desertRatioSlider != null)
		{
			_desertRatioSlider.Value = universeData.CurrentPlanet?.DesertRatio ?? 0.45f;
		}

		ApplyProfileToSliders(universeData.PlanetGenerationProfile);
		SelectTerrainPreset(DetectPreset(universeData.PlanetGenerationProfile));

		_planetTextureView?.ApplyCelestialConfig(universeData.CelestialPhysics);

		PopulatePlanetElementSelector(universeData.CurrentPlanet?.Element ?? PlanetElement.Terra);
		PopulateHierarchySelector(universeData.HierarchyConfig?.Archetype ?? HierarchyArchetype.Standard);
	}

	private void ApplyProfileToSliders(PlanetGenerationProfile profile)
	{
		_isSyncingGenerationProfileControls = true;
		try
		{
			if (_tectonicPlateSlider != null)
			{
				_tectonicPlateSlider.Value = profile.TectonicPlateCount;
			}

			if (_windCellSlider != null)
			{
				_windCellSlider.Value = profile.WindCellCount;
			}

			if (_erosionIterationsSlider != null)
			{
				_erosionIterationsSlider.Value = profile.ErosionIterations;
			}

			if (_erosionStrengthSlider != null)
			{
				_erosionStrengthSlider.Value = profile.ErosionStrength;
			}

			if (_heatFactorSlider != null)
			{
				_heatFactorSlider.Value = profile.HeatFactor;
			}
		}
		finally
		{
			_isSyncingGenerationProfileControls = false;
		}
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

		if (_mountainIntensitySlider != null)
		{
			_mountainIntensitySlider.Value = _random.NextDouble();
		}

		if (_polarCoverageSlider != null)
		{
			_polarCoverageSlider.Value = _random.NextDouble();
		}

		if (_desertRatioSlider != null)
		{
			_desertRatioSlider.Value = _random.NextDouble();
		}

		if (_tectonicPlateSlider != null)
		{
			_tectonicPlateSlider.Value = _random.Next(6, 41);
		}

		if (_windCellSlider != null)
		{
			_windCellSlider.Value = _random.Next(4, 19);
		}

		if (_erosionIterationsSlider != null)
		{
			_erosionIterationsSlider.Value = _random.Next(2, 11);
		}

		if (_erosionStrengthSlider != null)
		{
			_erosionStrengthSlider.Value = _random.NextDouble() * 0.55;
		}

		if (_heatFactorSlider != null)
		{
			_heatFactorSlider.Value = _random.Next(120, 981);
		}

		SelectTerrainPreset(DetectPreset(_tempUniverse?.PlanetGenerationProfile ?? PlanetGenerationProfile.Default));

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

		_tempUniverse.CurrentPlanet.OceanCoverage = CivilizationOceanCoverage;
		_tempUniverse.CurrentPlanet.Temperature = CivilizationTemperature;
		_tempUniverse.CurrentPlanet.AtmosphereDensity = CivilizationAtmosphereDensity;

		_tempUniverse.CurrentPlanet.MountainIntensity = _mountainIntensitySlider != null
			? Mathf.Clamp((float)_mountainIntensitySlider.Value, 0f, 1f)
			: _tempUniverse.CurrentPlanet.MountainIntensity;

		_tempUniverse.CurrentPlanet.PolarCoverage = _polarCoverageSlider != null
			? Mathf.Clamp((float)_polarCoverageSlider.Value, 0f, 1f)
			: _tempUniverse.CurrentPlanet.PolarCoverage;

		_tempUniverse.CurrentPlanet.DesertRatio = _desertRatioSlider != null
			? Mathf.Clamp((float)_desertRatioSlider.Value, 0f, 1f)
			: _tempUniverse.CurrentPlanet.DesertRatio;

		var currentProfile = _tempUniverse.PlanetGenerationProfile;
		int tectonicPlateCount = _tectonicPlateSlider != null
			? Mathf.Clamp(Mathf.RoundToInt((float)_tectonicPlateSlider.Value), 1, 64)
			: currentProfile.TectonicPlateCount;
		int windCellCount = _windCellSlider != null
			? Mathf.Clamp(Mathf.RoundToInt((float)_windCellSlider.Value), 1, 24)
			: currentProfile.WindCellCount;
		int erosionIterations = _erosionIterationsSlider != null
			? Mathf.Clamp(Mathf.RoundToInt((float)_erosionIterationsSlider.Value), 0, 16)
			: currentProfile.ErosionIterations;
		float erosionStrength = _erosionStrengthSlider != null
			? Mathf.Clamp((float)_erosionStrengthSlider.Value, 0f, 1f)
			: currentProfile.ErosionStrength;
		float heatFactor = _heatFactorSlider != null
			? Mathf.Clamp((float)_heatFactorSlider.Value, 1f, 1000f)
			: currentProfile.HeatFactor;

		_tempUniverse.PlanetGenerationProfile = new PlanetGenerationProfile(
			tectonicPlateCount,
			windCellCount,
			erosionIterations,
			erosionStrength,
			heatFactor,
			currentProfile.ContinentalFrequency,
			currentProfile.ReliefStrength,
			currentProfile.MoistureTransport,
			currentProfile.PlateBoundarySharpness);

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

		if (!IsProfileValid(_tempUniverse.PlanetGenerationProfile))
		{
			_tempUniverse.PlanetGenerationProfile = PlanetGenerationProfile.FromPlanet(_tempUniverse.CurrentPlanet, _tempUniverse.LawAlignment);
		}
	}

	private static bool IsProfileValid(PlanetGenerationProfile profile)
	{
		return profile.TectonicPlateCount > 0
			&& profile.WindCellCount > 0
			&& profile.HeatFactor > 0f;
	}

	private static UniverseData BuildDefaultUniverse()
	{
		var defaultPlanet = new PlanetData
		{
			Name = "后土",
			Element = PlanetElement.Terra,
			Size = PlanetSize.Medium,
			OceanCoverage = CivilizationOceanCoverage,
			Temperature = CivilizationTemperature,
			AtmosphereDensity = CivilizationAtmosphereDensity,
			MountainIntensity = 0.55f,
			PolarCoverage = 0.55f,
			DesertRatio = 0.45f
		};

		return new UniverseData
		{
			Name = "太虚幻境",
			LawAlignment = 50,
			AestheticTheme = "Steampunk",
			CivilizationDensity = 55,
			TimeFlowRate = 1.0f,
			HierarchyConfig = HierarchyConfigData.CreateFromArchetype(HierarchyArchetype.Standard),
			CurrentPlanet = defaultPlanet,
			PlanetGenerationProfile = new PlanetGenerationProfile(
				24,
				8,
				4,
				0.16f,
				560f,
				0.62f,
				0.38f,
				0.58f,
				6.2f),
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
					AtmosphereDensity = source.CurrentPlanet.AtmosphereDensity,
					MountainIntensity = source.CurrentPlanet.MountainIntensity,
					PolarCoverage = source.CurrentPlanet.PolarCoverage,
					DesertRatio = source.CurrentPlanet.DesertRatio
				},
			CelestialPhysics = source.CelestialPhysics?.DuplicateConfig()
				?? CelestialSystemPhysicsConfig.CreateDefault(),
			PlanetGenerationProfile = source.PlanetGenerationProfile,
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
