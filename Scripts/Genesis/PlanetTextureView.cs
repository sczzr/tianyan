using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Genesis;

public partial class PlanetTextureView : TextureRect
{
	[Signal]
	public delegate void PreviewActivatedEventHandler();

	[Signal]
	public delegate void LivePhotoModeChangedEventHandler(bool enabled);

	[Signal]
	public delegate void LightFollowModeChangedEventHandler(bool enabled);

	[Signal]
	public delegate void LightResponseChangedEventHandler(float strength);

	[Signal]
	public delegate void SolarBrightnessChangedEventHandler(float brightness);

	[Signal]
	public delegate void SnapshotRefreshRequestedEventHandler();

	[Signal]
	public delegate void SkyTextureChangedEventHandler(string texturePath);

	[Signal]
	public delegate void PlanetSurfaceTextureChangedEventHandler(string texturePath);

	[Signal]
	public delegate void MoonTextureChangedEventHandler(string texturePath);

	[Signal]
	public delegate void SunTextureChangedEventHandler(string texturePath);

	[Signal]
	public delegate void DownloadedOnlyFilterChangedEventHandler(bool enabled);

	[Signal]
	public delegate void CelestialPhysicsChangedEventHandler(CelestialSystemPhysicsConfig config);

	private ShaderMaterial _previewMaterial;
	private PanelContainer _buildPanel;
	private Label _buildLabel;
	private ProgressBar _buildProgress;
	private PanelContainer _pauseBadge;
	private PanelContainer _orbitHudTopLeft;
	private PanelContainer _orbitHudBottomRight;
	private Label _orbitHudTitle;
	private Label _orbitHudSubtitle;
	private Label _orbitHudTarget;
	private Label _orbitHudHint;
	private Label _planetPhotoTitle;
	private Label _skyTextureLabel;
	private Control _orbitSection;
	private Control _planetPhotoPanel;
	private TextureRect _planetPhotoTexture;
	private Control _photoViewerOverlay;
	private TextureRect _photoViewerTexture;
	private Button _photoViewerCloseButton;
	private CheckBox _livePhotoToggle;
	private Label _livePhotoLabel;
	private CheckBox _lightFollowToggle;
	private Label _lightFollowLabel;
	private Label _lightResponseLabel;
	private HSlider _lightResponseSlider;
	private Label _solarBrightnessLabel;
	private HSlider _solarBrightnessSlider;
	private Button _refreshPhotoButton;
	private OptionButton _skyTextureSelector;
	private OptionButton _planetSurfaceTextureSelector;
	private OptionButton _moonTextureSelector;
	private OptionButton _sunTextureSelector;
	private CheckBox _downloadedOnlyToggle;
	private Label _downloadedOnlyLabel;
	private CheckBox _showMoonOrbitToggle;
	private Label _showMoonOrbitLabel;
	private CheckBox _showPlanetOrbitToggle;
	private Label _showPlanetOrbitLabel;
	private Button _orbitAdvancedToggleButton;
	private VBoxContainer _orbitAdvancedContainer;
	private Label _orbitSectionTitle;
	private Label _modeTierHintLabel;
	private Label _starMassLabel;
	private HSlider _starMassSlider;
	private Label _planetRadiusLabel;
	private HSlider _planetRadiusSlider;
	private Label _planetMassLabel;
	private HSlider _planetMassSlider;
	private Label _planetOrbitDistanceLabel;
	private HSlider _planetOrbitDistanceSlider;
	private CheckBox _autoPlanetRevolutionToggle;
	private Label _autoPlanetRevolutionLabel;
	private Label _planetRotationLabel;
	private HSlider _planetRotationSlider;
	private Label _planetRevolutionLabel;
	private HSlider _planetRevolutionSlider;
	private Label _moonOrbitDistanceLabel;
	private HSlider _moonOrbitDistanceSlider;
	private CheckBox _autoMoonRevolutionToggle;
	private Label _autoMoonRevolutionLabel;
	private Label _moonRotationLabel;
	private HSlider _moonRotationSlider;
	private Label _moonRevolutionLabel;
	private HSlider _moonRevolutionSlider;
	private Label _extraPlanetsCountLabel;
	private HSlider _extraPlanetsCountSlider;
	private Label _extraPlanetFirstOrbitLabel;
	private HSlider _extraPlanetFirstOrbitSlider;
	private Label _extraPlanetOrbitStepLabel;
	private HSlider _extraPlanetOrbitStepSlider;
	private Label _extraMoonsCountLabel;
	private HSlider _extraMoonsCountSlider;
	private Label _extraMoonFirstOrbitLabel;
	private HSlider _extraMoonFirstOrbitSlider;
	private Label _extraMoonOrbitStepLabel;
	private HSlider _extraMoonOrbitStepSlider;
	private bool _isApplyingCelestialUi;
	private TranslationManager _translationManager;
	private bool _isHardcoreMode;
	private bool _isOrbitAdvancedExpanded;
	private bool _isInspectingStar = true;
	private bool _isAnimationStyle;

	private int _lawAlignment = 50;
	private PlanetGenerationProfile _generationProfile = PlanetGenerationProfile.Default;
	private PlanetData _planetData = new PlanetData
	{
		Element = PlanetElement.Terra,
		OceanCoverage = 0.56f,
		Temperature = 0.52f,
		AtmosphereDensity = 0.62f,
		MountainIntensity = 0.55f,
		PolarCoverage = 0.55f,
		DesertRatio = 0.45f
	};
	private bool _useStaticPreview = true;
	private readonly Dictionary<int, string> _skyTexturePathById = new();
	private readonly Dictionary<int, string> _planetTexturePathById = new();
	private readonly Dictionary<int, string> _moonTexturePathById = new();
	private readonly Dictionary<int, string> _sunTexturePathById = new();

	private readonly struct TextureOption
	{
		public TextureOption(string source, string label, string path)
		{
			Source = source;
			Label = label;
			Path = path;
		}

		public string Source { get; }

		public string Label { get; }
		public string Path { get; }
	}

	private static readonly TextureOption[] SkyTextureOptions =
	{
		new TextureOption("内置", "默认背景 (动态)", string.Empty),
		new TextureOption("NASA", "银河 (EXR)", "res://Assets/Textures/Stars/NASA/starmap_2020_4k_gal.exr"),
		new TextureOption("NASA", "星空 (EXR)", "res://Assets/Textures/Stars/NASA/starmap_2020_4k.exr"),
		new TextureOption("NASA", "银河 (JPG)", "res://Assets/Textures/Stars/NASA/milkyway_2020_4k_gal_print.jpg"),
		new TextureOption("NASA", "银河星图 (JPG)", "res://Assets/Textures/Stars/NASA/starmap_2020_4k_gal_print.jpg"),
		new TextureOption("NASA", "星图 (JPG)", "res://Assets/Textures/Stars/NASA/starmap_2020_4k_print.jpg"),
		new TextureOption("SolarScope", "星空", "res://Assets/Textures/Stars/SolarSystemScope/2k_stars.jpg"),
		new TextureOption("SolarScope", "银河", "res://Assets/Textures/Stars/SolarSystemScope/2k_stars_milky_way.jpg")
	};

	private static readonly TextureOption[] PlanetSurfaceOptions =
	{
		new TextureOption("内置", "程序生成 (动态)", string.Empty),
		new TextureOption("SolarScope", "Earth 2K", "res://Assets/Textures/Planets/SolarSystemScope/2k_earth_daymap.jpg"),
		new TextureOption("SolarScope", "Mars 2K", "res://Assets/Textures/Planets/SolarSystemScope/2k_mars.jpg"),
		new TextureOption("SolarScope", "Jupiter 2K", "res://Assets/Textures/Planets/SolarSystemScope/2k_jupiter.jpg"),
		new TextureOption("SolarScope", "Saturn 2K", "res://Assets/Textures/Planets/SolarSystemScope/2k_saturn.jpg"),
		new TextureOption("OpenGameArt", "Red Surface", "res://Assets/Textures/Planets/OpenGameArt/RedPlanetAnimationSurface.jpg"),
		new TextureOption("OpenGameArt", "Planet Preview", "res://Assets/Textures/Planets/OpenGameArt/d6eQU.png"),
		new TextureOption("NASA JPL", "Earth (Legacy)", "res://Assets/Textures/Planets/NASA_JPL/earth.jpg")
	};

	private static readonly TextureOption[] MoonTextureOptions =
	{
		new TextureOption("内置", "默认卫星材质", string.Empty),
		new TextureOption("SolarScope", "Moon 2K", "res://Assets/Textures/Moons/SolarSystemScope/2k_moon.jpg")
	};

	private static readonly TextureOption[] SunTextureOptions =
	{
		new TextureOption("内置", "默认恒星材质", string.Empty),
		new TextureOption("SolarScope", "Sun 2K", "res://Assets/Textures/Stars/SolarSystemScope/2k_sun.jpg")
	};

	private const string SimulationSkyTexturePath = "res://Assets/Textures/Stars/NASA/starmap_2020_4k_gal.exr";
	private const string SimulationPlanetTexturePath = "res://Assets/Textures/Planets/SolarSystemScope/2k_earth_daymap.jpg";
	private const string SimulationMoonTexturePath = "res://Assets/Textures/Moons/SolarSystemScope/2k_moon.jpg";
	private const string SimulationSunTexturePath = "res://Assets/Textures/Stars/SolarSystemScope/2k_sun.jpg";
	private const string AnimationPlanetTexturePath = "res://Assets/Textures/Planets/OpenGameArt/d6eQU.png";

	public bool IsLivePhotoUpdateEnabled => _livePhotoToggle?.ButtonPressed ?? true;
	public bool IsLightFollowEnabled => _lightFollowToggle?.ButtonPressed ?? true;
	public float LightResponseStrength => _lightResponseSlider != null
		? Mathf.Clamp((float)_lightResponseSlider.Value, 0f, 1f)
		: 0.75f;
	public float SolarBrightness => _solarBrightnessSlider != null
		? Mathf.Clamp((float)_solarBrightnessSlider.Value, 0.5f, 2.5f)
		: 1.0f;
	public bool ShowDownloadedOnly => _downloadedOnlyToggle?.ButtonPressed ?? false;
	public string SelectedSkyTexturePath { get; private set; } = string.Empty;
	public string SelectedPlanetSurfaceTexturePath { get; private set; } = string.Empty;
	public string SelectedMoonTexturePath { get; private set; } = string.Empty;
	public string SelectedSunTexturePath { get; private set; } = string.Empty;

	public override void _Ready()
	{
		ExpandMode = ExpandModeEnum.IgnoreSize;
		StretchMode = StretchModeEnum.Scale;
		MouseFilter = MouseFilterEnum.Stop;

		_translationManager = TranslationManager.Instance;
		if (_translationManager != null)
		{
			_translationManager.LanguageChanged += OnLanguageChanged;
		}

		_buildPanel = GetNodeOrNull<PanelContainer>("BuildPanel");
		_buildLabel = GetNodeOrNull<Label>("BuildPanel/BuildVBox/BuildLabel");
		_buildProgress = GetNodeOrNull<ProgressBar>("BuildPanel/BuildVBox/BuildProgress");
		_pauseBadge = GetNodeOrNull<PanelContainer>("PauseBadge");
		_orbitHudTopLeft = GetNodeOrNull<PanelContainer>("OrbitHudTopLeft");
		_orbitHudBottomRight = GetNodeOrNull<PanelContainer>("OrbitHudBottomRight");
		_orbitHudTitle = GetNodeOrNull<Label>("OrbitHudTopLeft/OrbitHudTopVBox/OrbitHudTitle");
		_orbitHudSubtitle = GetNodeOrNull<Label>("OrbitHudTopLeft/OrbitHudTopVBox/OrbitHudSubtitle");
		_orbitHudTarget = GetNodeOrNull<Label>("OrbitHudTopLeft/OrbitHudTopVBox/OrbitHudTarget");
		_orbitHudHint = GetNodeOrNull<Label>("OrbitHudTopLeft/OrbitHudTopVBox/OrbitHudHint");
		_orbitSection = GetNodeOrNull<Control>("OrbitSection");
		_orbitSectionTitle = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitSectionTitle");
		_modeTierHintLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/ModeTierHintLabel");
		_showMoonOrbitToggle = GetNodeOrNull<CheckBox>("OrbitSection/OrbitSectionVBox/ShowMoonOrbitRow/ShowMoonOrbitToggle")
			?? GetNodeOrNull<CheckBox>("OrbitSection/OrbitSectionVBox/ShowMoonOrbitToggle");
		_showMoonOrbitLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/ShowMoonOrbitRow/ShowMoonOrbitLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/ShowMoonOrbitLabel");
		_showPlanetOrbitToggle = GetNodeOrNull<CheckBox>("OrbitSection/OrbitSectionVBox/ShowPlanetOrbitRow/ShowPlanetOrbitToggle")
			?? GetNodeOrNull<CheckBox>("OrbitSection/OrbitSectionVBox/ShowPlanetOrbitToggle");
		_showPlanetOrbitLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/ShowPlanetOrbitRow/ShowPlanetOrbitLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/ShowPlanetOrbitLabel");
		_orbitAdvancedToggleButton = GetNodeOrNull<Button>("OrbitSection/OrbitSectionVBox/OrbitAdvancedToggleButton");
		_orbitAdvancedContainer = GetNodeOrNull<VBoxContainer>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer");
		_planetPhotoPanel = GetNodeOrNull<Control>("PlanetPhotoPanel");
		_planetPhotoTitle = GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/PlanetPhotoTitle");
		_skyTextureLabel = GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/SkyTextureLabel");
		_planetPhotoTexture = GetNodeOrNull<TextureRect>("PlanetPhotoPanel/PlanetPhotoVBox/PlanetPhotoTexture");
		_photoViewerOverlay = GetNodeOrNull<Control>("PhotoViewerOverlay");
		_photoViewerTexture = GetNodeOrNull<TextureRect>("PhotoViewerOverlay/PhotoViewerTexture");
		_photoViewerCloseButton = GetNodeOrNull<Button>("PhotoViewerOverlay/PhotoViewerCloseButton");
		_livePhotoToggle = GetNodeOrNull<CheckBox>("PlanetPhotoPanel/PlanetPhotoVBox/LivePhotoRow/LivePhotoToggle")
			?? GetNodeOrNull<CheckBox>("PlanetPhotoPanel/PlanetPhotoVBox/LivePhotoToggle");
		_livePhotoLabel = GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/LivePhotoRow/LivePhotoLabel")
			?? GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/LivePhotoLabel");
		_lightFollowToggle = GetNodeOrNull<CheckBox>("PlanetPhotoPanel/PlanetPhotoVBox/LightFollowRow/LightFollowToggle")
			?? GetNodeOrNull<CheckBox>("PlanetPhotoPanel/PlanetPhotoVBox/LightFollowToggle");
		_lightFollowLabel = GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/LightFollowRow/LightFollowLabel")
			?? GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/LightFollowLabel");
		_lightResponseLabel = GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/LightResponseLabel");
		_lightResponseSlider = GetNodeOrNull<HSlider>("PlanetPhotoPanel/PlanetPhotoVBox/LightResponseSlider");
		_solarBrightnessLabel = GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/SolarBrightnessLabel");
		_solarBrightnessSlider = GetNodeOrNull<HSlider>("PlanetPhotoPanel/PlanetPhotoVBox/SolarBrightnessSlider");
		_refreshPhotoButton = GetNodeOrNull<Button>("PlanetPhotoPanel/PlanetPhotoVBox/RefreshPhotoButton");
		_skyTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/SkyTextureSelector");
		_planetSurfaceTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/PlanetSurfaceTextureSelector");
		_moonTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/MoonTextureSelector");
		_sunTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/SunTextureSelector");
		_downloadedOnlyToggle = GetNodeOrNull<CheckBox>("PlanetPhotoPanel/PlanetPhotoVBox/DownloadedOnlyRow/DownloadedOnlyToggle")
			?? GetNodeOrNull<CheckBox>("PlanetPhotoPanel/PlanetPhotoVBox/DownloadedOnlyToggle");
		_downloadedOnlyLabel = GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/DownloadedOnlyRow/DownloadedOnlyLabel")
			?? GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/DownloadedOnlyLabel");

		EnsureTextureSelectorNodes();
		EnsureCelestialPhysicsNodes();

		SetupTextureSelectors();
		BindCelestialPhysicsUi();
		ApplyCelestialConfig(CelestialSystemPhysicsConfig.CreateDefault());

		if (_planetPhotoTexture != null)
		{
			_planetPhotoTexture.MouseFilter = MouseFilterEnum.Stop;
			_planetPhotoTexture.MouseDefaultCursorShape = CursorShape.PointingHand;
			_planetPhotoTexture.GuiInput += OnPlanetPhotoTextureGuiInput;
		}

		if (_photoViewerOverlay != null)
		{
			_photoViewerOverlay.Visible = false;
			_photoViewerOverlay.MouseFilter = MouseFilterEnum.Stop;
			_photoViewerOverlay.GuiInput += OnPhotoViewerOverlayGuiInput;
		}

		if (_photoViewerCloseButton != null)
		{
			_photoViewerCloseButton.Pressed += OnPhotoViewerClosePressed;
		}

		if (_livePhotoToggle != null)
		{
			_livePhotoToggle.Toggled += OnLivePhotoToggleChanged;
		}

		if (_lightFollowToggle != null)
		{
			_lightFollowToggle.Toggled += OnLightFollowToggleChanged;
		}

		if (_lightResponseSlider != null)
		{
			_lightResponseSlider.ValueChanged += OnLightResponseSliderChanged;
			UpdateLightResponseLabel((float)_lightResponseSlider.Value);
		}

		if (_solarBrightnessSlider != null)
		{
			_solarBrightnessSlider.MinValue = 0.5;
			_solarBrightnessSlider.MaxValue = 2.5;
			_solarBrightnessSlider.Step = 0.01;
			_solarBrightnessSlider.Value = Mathf.Clamp(_solarBrightnessSlider.Value, 0.5, 2.5);
			_solarBrightnessSlider.ValueChanged += OnSolarBrightnessSliderChanged;
			UpdateSolarBrightnessLabel((float)_solarBrightnessSlider.Value);
		}

		if (_refreshPhotoButton != null)
		{
			_refreshPhotoButton.Pressed += OnRefreshPhotoPressed;
		}

		if (_downloadedOnlyToggle != null)
		{
			_downloadedOnlyToggle.Toggled += OnDownloadedOnlyToggleChanged;
		}

		if (_orbitAdvancedToggleButton != null)
		{
			_orbitAdvancedToggleButton.Pressed += OnOrbitAdvancedTogglePressed;
		}

		SetBuildState(false, 0f, true);
		SetEditingPaused(false);
		EnterStaticMode();
		ApplyVisualStylePreset(false);
		SetHardcoreMode(false);
		SetInspectorTarget(true);
		UpdatePreview(_planetData, _lawAlignment);
	}

	public void SetHardcoreMode(bool hardcore)
	{
		if (_isHardcoreMode == hardcore)
		{
			return;
		}

		_isHardcoreMode = hardcore;
		if (!_isHardcoreMode)
		{
			_isOrbitAdvancedExpanded = false;
		}
		ApplyInspectorVisibility();
		ApplyVisualLabels();
	}

	public void SetInspectorTarget(bool inspectStar)
	{
		if (_isInspectingStar == inspectStar)
		{
			return;
		}

		_isInspectingStar = inspectStar;
		ApplyInspectorVisibility();
		ApplyVisualLabels();
	}

	public void ApplyVisualStylePreset(bool animationStyle)
	{
		_isAnimationStyle = animationStyle;
		ApplyVisualLabels();

		if (_isAnimationStyle)
		{
			SelectSkyTextureByPath(string.Empty);
			SelectPlanetTextureByPath(AnimationPlanetTexturePath);
			SelectMoonTextureByPath(string.Empty);
			SelectSunTextureByPath(string.Empty);
		}
		else
		{
			SelectSkyTextureByPath(SimulationSkyTexturePath);
			SelectPlanetTextureByPath(SimulationPlanetTexturePath);
			SelectMoonTextureByPath(SimulationMoonTexturePath);
			SelectSunTextureByPath(SimulationSunTexturePath);
		}

		EmitSignal(SignalName.SkyTextureChanged, SelectedSkyTexturePath);
		EmitSignal(SignalName.PlanetSurfaceTextureChanged, SelectedPlanetSurfaceTexturePath);
		EmitSignal(SignalName.MoonTextureChanged, SelectedMoonTexturePath);
		EmitSignal(SignalName.SunTextureChanged, SelectedSunTexturePath);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationExitTree)
		{
			CleanupEventBindings();
		}
	}

	public override void _Process(double delta)
	{
		if (!_useStaticPreview || _previewMaterial == null)
		{
			return;
		}

		_previewMaterial.SetShaderParameter("time_sec", (float)Time.GetTicksMsec() / 1000f);
	}

	public override void _GuiInput(InputEvent inputEvent)
	{
		if (_photoViewerOverlay?.Visible == true)
		{
			if (inputEvent is InputEventKey keyEvent
				&& keyEvent.Pressed
				&& keyEvent.Keycode == Key.Escape)
			{
				ClosePhotoViewer();
				AcceptEvent();
			}

			return;
		}

		if (_buildPanel?.Visible == true)
		{
			return;
		}

		if (inputEvent is InputEventMouseButton mouseButton
			&& mouseButton.Pressed
			&& mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (IsPointInInteractiveOverlay(mouseButton.Position))
			{
				return;
			}

			EmitSignal(SignalName.PreviewActivated);
			AcceptEvent();
		}
	}

	public void UpdatePreview(PlanetData planetData, int lawAlignment)
	{
		if (planetData != null)
		{
			_planetData = ClonePlanetData(planetData);
		}

		_lawAlignment = Mathf.Clamp(lawAlignment, 0, 100);
		EnsurePreviewMaterial();
		if (_previewMaterial == null)
		{
			return;
		}

		float solarBrightness = SolarBrightness;
		float terrainVisualWeight = Mathf.Clamp(_planetData.MountainIntensity * 0.65f + _planetData.DesertRatio * 0.35f, 0f, 1f);
		float exposure = Mathf.Clamp(0.68f + solarBrightness * 0.28f, 0.78f, 1.18f);
		float contrast = Mathf.Lerp(0.98f, 1.04f, terrainVisualWeight);
		float saturation = Mathf.Lerp(0.88f, 0.98f, terrainVisualWeight);
		float rimStrength = Mathf.Lerp(0.24f, 0.52f, Mathf.Clamp(_planetData.AtmosphereDensity, 0f, 1f));

		_previewMaterial.SetShaderParameter("element_type", ResolveElementType(_planetData.Element));
		_previewMaterial.SetShaderParameter("ocean_level", Mathf.Lerp(0.2f, 0.78f, Mathf.Clamp(_planetData.OceanCoverage, 0f, 1f)));
		_previewMaterial.SetShaderParameter("temperature", Mathf.Clamp(_planetData.Temperature, 0f, 1f));
		_previewMaterial.SetShaderParameter("atmosphere_density", Mathf.Clamp(_planetData.AtmosphereDensity, 0f, 1f));
		_previewMaterial.SetShaderParameter("mountain_intensity", Mathf.Clamp(_planetData.MountainIntensity, 0f, 1f));
		_previewMaterial.SetShaderParameter("polar_coverage", Mathf.Clamp(_planetData.PolarCoverage, 0f, 1f));
		_previewMaterial.SetShaderParameter("desert_ratio", Mathf.Clamp(_planetData.DesertRatio, 0f, 1f));
		_previewMaterial.SetShaderParameter("preview_exposure", exposure);
		_previewMaterial.SetShaderParameter("preview_contrast", contrast);
		_previewMaterial.SetShaderParameter("preview_saturation", saturation);
		_previewMaterial.SetShaderParameter("rim_strength", rimStrength);
		_previewMaterial.SetShaderParameter("plate_count", _generationProfile.TectonicPlateCount);
		_previewMaterial.SetShaderParameter("wind_cells", _generationProfile.WindCellCount);
		_previewMaterial.SetShaderParameter("erosion_iterations", _generationProfile.ErosionIterations);
		_previewMaterial.SetShaderParameter("erosion_strength", _generationProfile.ErosionStrength);
		_previewMaterial.SetShaderParameter("heat_factor", _generationProfile.HeatFactor);
		_previewMaterial.SetShaderParameter("cloud_speed", Mathf.Lerp(0.08f, 0.34f, _lawAlignment / 100f));
		_previewMaterial.SetShaderParameter("time_sec", (float)Time.GetTicksMsec() / 1000f);
	}

	public void SetGenerationProfile(PlanetGenerationProfile profile)
	{
		_generationProfile = profile;
		UpdatePreview(_planetData, _lawAlignment);
	}

	public void SetLawAlignment(int lawAlignment)
	{
		_lawAlignment = Mathf.Clamp(lawAlignment, 0, 100);
		UpdatePreview(_planetData, _lawAlignment);
	}


	public void SetBuildState(bool isBuilding, float progress01, bool isZh)
	{
		if (_buildPanel != null)
		{
			_buildPanel.Visible = isBuilding;
		}

		if (_buildLabel != null)
		{
			_buildLabel.Text = _translationManager?.Tr("preview_building_cosmos")
				?? (isZh ? "正在构建3D寰宇..." : "Building 3D cosmos...");
		}

		if (_buildProgress != null)
		{
			_buildProgress.Value = Mathf.Clamp(progress01, 0f, 1f) * 100f;
		}
	}

	public void SetEditingPaused(bool isPaused)
	{
		if (_pauseBadge != null)
		{
			_pauseBadge.Visible = isPaused;
		}
	}

	public void SetPlanetSnapshot(Texture2D texture)
	{
		if (_planetPhotoTexture != null && texture != null)
		{
			_planetPhotoTexture.Texture = texture;
		}

		if (_photoViewerTexture != null && texture != null)
		{
			_photoViewerTexture.Texture = texture;
		}
	}

	public bool IsPointInInteractiveOverlay(Vector2 localPoint)
	{
		return IsControlHit(_orbitSection, localPoint)
			|| IsControlHit(_planetPhotoPanel, localPoint)
			|| IsControlHit(_photoViewerOverlay, localPoint);
	}

	public void EnterStaticMode()
	{
		_useStaticPreview = true;
		MouseFilter = MouseFilterEnum.Stop;
		EnsurePreviewMaterial();
	}

	public void EnterViewportMode()
	{
		_useStaticPreview = false;
		MouseFilter = MouseFilterEnum.Ignore;
		Material = null;
	}

	public void ApplyCelestialConfig(CelestialSystemPhysicsConfig config)
	{
		var source = config?.DuplicateConfig() ?? CelestialSystemPhysicsConfig.CreateDefault();
		var primary = source.PrimaryPlanet ?? new CelestialPlanetPhysicsConfig();
		var primaryMoon = source.Satellites != null && source.Satellites.Count > 0
			? source.Satellites[0]
			: new CelestialSatellitePhysicsConfig();

		_isApplyingCelestialUi = true;

		if (_starMassSlider != null)
		{
			_starMassSlider.Value = Mathf.Clamp(source.StarMassSolar, 0.08f, 80f);
		}

		if (_planetRadiusSlider != null)
		{
			_planetRadiusSlider.Value = Mathf.Clamp(primary.RadiusEarth, 0.1f, 20f);
		}

		if (_planetMassSlider != null)
		{
			_planetMassSlider.Value = Mathf.Clamp(primary.MassEarth, 0.01f, 500f);
		}

		if (_planetOrbitDistanceSlider != null)
		{
			_planetOrbitDistanceSlider.Value = Mathf.Clamp(primary.OrbitDistanceAu, 0.03f, 80f);
		}

		if (_planetRotationSlider != null)
		{
			_planetRotationSlider.Value = Mathf.Clamp(primary.RotationPeriodHours, 2f, 5000f);
		}

		if (_planetRevolutionSlider != null)
		{
			_planetRevolutionSlider.Value = Mathf.Clamp(primary.RevolutionPeriodDays, 0.2f, 500000f);
		}

		if (_autoPlanetRevolutionToggle != null)
		{
			_autoPlanetRevolutionToggle.ButtonPressed = primary.AutoResolveRevolutionPeriod;
		}

		if (_moonOrbitDistanceSlider != null)
		{
			_moonOrbitDistanceSlider.Value = Mathf.Clamp(primaryMoon.OrbitDistancePlanetRadii, 2f, 500f);
		}

		if (_moonRotationSlider != null)
		{
			_moonRotationSlider.Value = Mathf.Clamp(primaryMoon.RotationPeriodHours, 1f, 5000f);
		}

		if (_moonRevolutionSlider != null)
		{
			_moonRevolutionSlider.Value = Mathf.Clamp(primaryMoon.RevolutionPeriodDays, 0.1f, 100000f);
		}

		if (_autoMoonRevolutionToggle != null)
		{
			_autoMoonRevolutionToggle.ButtonPressed = primaryMoon.AutoResolveRevolutionPeriod;
		}

		int extraPlanetCount = Mathf.Max(0, (source.AdditionalPlanets?.Count ?? 0));
		if (_extraPlanetsCountSlider != null)
		{
			_extraPlanetsCountSlider.Value = extraPlanetCount;
		}

		float firstPlanetOrbit = 1.6f;
		float planetStep = 0.8f;
		if (source.AdditionalPlanets != null && source.AdditionalPlanets.Count > 0)
		{
			firstPlanetOrbit = Mathf.Max(0.1f, source.AdditionalPlanets[0].OrbitDistanceAu);
			if (source.AdditionalPlanets.Count > 1)
			{
				planetStep = Mathf.Max(0.1f, source.AdditionalPlanets[1].OrbitDistanceAu - source.AdditionalPlanets[0].OrbitDistanceAu);
			}
		}

		if (_extraPlanetFirstOrbitSlider != null)
		{
			_extraPlanetFirstOrbitSlider.Value = firstPlanetOrbit;
		}

		if (_extraPlanetOrbitStepSlider != null)
		{
			_extraPlanetOrbitStepSlider.Value = planetStep;
		}

		int extraMoonCount = Mathf.Max(0, (source.Satellites?.Count ?? 0) - 1);
		if (_extraMoonsCountSlider != null)
		{
			_extraMoonsCountSlider.Value = extraMoonCount;
		}

		float firstMoonOrbit = 86f;
		float moonStep = 24f;
		if (source.Satellites != null && source.Satellites.Count > 1)
		{
			firstMoonOrbit = Mathf.Max(2f, source.Satellites[1].OrbitDistancePlanetRadii);
			if (source.Satellites.Count > 2)
			{
				moonStep = Mathf.Max(1f, source.Satellites[2].OrbitDistancePlanetRadii - source.Satellites[1].OrbitDistancePlanetRadii);
			}
		}

		if (_extraMoonFirstOrbitSlider != null)
		{
			_extraMoonFirstOrbitSlider.Value = firstMoonOrbit;
		}

		if (_extraMoonOrbitStepSlider != null)
		{
			_extraMoonOrbitStepSlider.Value = moonStep;
		}

		_isApplyingCelestialUi = false;
		UpdateCelestialLabels();
	}

	private CelestialSystemPhysicsConfig BuildCelestialConfigFromControls()
	{
		var config = CelestialSystemPhysicsConfig.CreateDefault();

		if (_starMassSlider != null)
		{
			config.StarMassSolar = Mathf.Clamp((float)_starMassSlider.Value, 0.08f, 80f);
		}

		var primary = config.PrimaryPlanet;
		if (_planetRadiusSlider != null)
		{
			primary.RadiusEarth = Mathf.Clamp((float)_planetRadiusSlider.Value, 0.1f, 20f);
		}

		if (_planetMassSlider != null)
		{
			primary.MassEarth = Mathf.Clamp((float)_planetMassSlider.Value, 0.01f, 500f);
		}

		if (_planetOrbitDistanceSlider != null)
		{
			primary.OrbitDistanceAu = Mathf.Clamp((float)_planetOrbitDistanceSlider.Value, 0.03f, 80f);
		}

		if (_planetRotationSlider != null)
		{
			primary.RotationPeriodHours = Mathf.Clamp((float)_planetRotationSlider.Value, 2f, 5000f);
		}

		if (_planetRevolutionSlider != null)
		{
			primary.RevolutionPeriodDays = Mathf.Clamp((float)_planetRevolutionSlider.Value, 0.2f, 500000f);
		}

		if (_autoPlanetRevolutionToggle != null)
		{
			primary.AutoResolveRevolutionPeriod = _autoPlanetRevolutionToggle.ButtonPressed;
		}

		config.Satellites.Clear();
		var moon = new CelestialSatellitePhysicsConfig
		{
			BodyId = "moon_0",
			Name = "广寒",
			Visible = true,
			RadiusEarth = 0.2724f,
			MassEarth = 0.0123f,
			OrbitDistancePlanetRadii = _moonOrbitDistanceSlider != null ? Mathf.Clamp((float)_moonOrbitDistanceSlider.Value, 2f, 500f) : 60.3f,
			RotationPeriodHours = _moonRotationSlider != null ? Mathf.Clamp((float)_moonRotationSlider.Value, 1f, 5000f) : 655.7f,
			RevolutionPeriodDays = _moonRevolutionSlider != null ? Mathf.Clamp((float)_moonRevolutionSlider.Value, 0.1f, 100000f) : 27.3f,
			AutoResolveRevolutionPeriod = _autoMoonRevolutionToggle?.ButtonPressed ?? true
		};
		config.Satellites.Add(moon);

		config.AdditionalPlanets.Clear();
		int extraPlanetCount = _extraPlanetsCountSlider != null ? Mathf.RoundToInt((float)_extraPlanetsCountSlider.Value) : 0;
		float firstPlanetOrbit = _extraPlanetFirstOrbitSlider != null ? Mathf.Max(0.1f, (float)_extraPlanetFirstOrbitSlider.Value) : 1.6f;
		float planetStep = _extraPlanetOrbitStepSlider != null ? Mathf.Max(0.1f, (float)_extraPlanetOrbitStepSlider.Value) : 0.8f;
		for (int i = 0; i < extraPlanetCount; i++)
		{
			float t = (i + 1f) / Mathf.Max(1f, extraPlanetCount + 1f);
			float radiusEarth = Mathf.Lerp(0.45f, 1.35f, t);
			float massEarth = Mathf.Pow(radiusEarth, 3f) * Mathf.Lerp(0.72f, 1.28f, t);
			var extraPlanet = new CelestialPlanetPhysicsConfig
			{
				BodyId = $"planet_{i}",
				Name = $"辅星-{i + 1}",
				Visible = true,
				Element = (PlanetElement)(i % 4),
				RadiusEarth = radiusEarth,
				MassEarth = massEarth,
				OrbitDistanceAu = firstPlanetOrbit + i * planetStep,
				OrbitEccentricity = Mathf.Clamp(0.02f + i * 0.01f, 0f, 0.2f),
				OrbitInclinationDeg = -4f + i * 2.5f,
				RotationPeriodHours = 10f + i * 6f,
				RevolutionPeriodDays = 120f + i * 80f,
				AutoResolveRevolutionPeriod = true
			};
			config.AdditionalPlanets.Add(extraPlanet);
		}

		int extraMoonCount = _extraMoonsCountSlider != null ? Mathf.RoundToInt((float)_extraMoonsCountSlider.Value) : 0;
		float firstMoonOrbit = _extraMoonFirstOrbitSlider != null ? Mathf.Max(2f, (float)_extraMoonFirstOrbitSlider.Value) : 86f;
		float moonStep = _extraMoonOrbitStepSlider != null ? Mathf.Max(1f, (float)_extraMoonOrbitStepSlider.Value) : 24f;
		for (int i = 0; i < extraMoonCount; i++)
		{
			float t = (i + 1f) / Mathf.Max(1f, extraMoonCount + 1f);
			var extraMoon = new CelestialSatellitePhysicsConfig
			{
				BodyId = $"moon_{i + 1}",
				Name = $"伴月-{i + 1}",
				Visible = true,
				RadiusEarth = Mathf.Lerp(0.08f, 0.24f, t),
				MassEarth = Mathf.Lerp(0.001f, 0.018f, t),
				OrbitDistancePlanetRadii = firstMoonOrbit + i * moonStep,
				OrbitEccentricity = Mathf.Clamp(0.01f + i * 0.01f, 0f, 0.35f),
				OrbitInclinationDeg = -12f + i * 5f,
				RotationPeriodHours = 40f + i * 20f,
				RevolutionPeriodDays = 8f + i * 6f,
				AutoResolveRevolutionPeriod = true
			};
			config.Satellites.Add(extraMoon);
		}

		return config;
	}

	private void EmitCelestialPhysicsChanged()
	{
		if (_isApplyingCelestialUi)
		{
			return;
		}

		EmitSignal(SignalName.CelestialPhysicsChanged, BuildCelestialConfigFromControls());
	}

	private void EnsureCelestialPhysicsNodes()
	{
		var orbitVBox = GetNodeOrNull<VBoxContainer>("OrbitSection/OrbitSectionVBox");
		if (orbitVBox == null)
		{
			return;
		}

		if (_orbitSection != null)
		{
			_orbitSection.OffsetTop = -700.0f;
		}

		_orbitAdvancedContainer = GetNodeOrNull<VBoxContainer>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer");
		_starMassLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/StarMassLabel");
		_starMassSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/StarMassSlider");
		_planetRadiusLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/PlanetRadiusLabel");
		_planetRadiusSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/PlanetRadiusSlider");
		_planetOrbitDistanceLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/PlanetOrbitDistanceLabel");
		_planetOrbitDistanceSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/PlanetOrbitDistanceSlider");

		_planetMassLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/PlanetMassLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/PlanetMassLabel");
		_planetMassSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/PlanetMassSlider")
			?? GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/PlanetMassSlider");
		_autoPlanetRevolutionToggle = GetNodeOrNull<CheckBox>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/AutoPlanetRevolutionRow/AutoPlanetRevolutionToggle")
			?? GetNodeOrNull<CheckBox>("OrbitSection/OrbitSectionVBox/AutoPlanetRevolutionRow/AutoPlanetRevolutionToggle");
		_autoPlanetRevolutionLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/AutoPlanetRevolutionRow/AutoPlanetRevolutionLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/AutoPlanetRevolutionRow/AutoPlanetRevolutionLabel");
		_planetRotationLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/PlanetRotationLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/PlanetRotationLabel");
		_planetRotationSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/PlanetRotationSlider")
			?? GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/PlanetRotationSlider");
		_planetRevolutionLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/PlanetRevolutionLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/PlanetRevolutionLabel");
		_planetRevolutionSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/PlanetRevolutionSlider")
			?? GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/PlanetRevolutionSlider");
		_moonOrbitDistanceLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/MoonOrbitDistanceLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/MoonOrbitDistanceLabel");
		_moonOrbitDistanceSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/MoonOrbitDistanceSlider")
			?? GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/MoonOrbitDistanceSlider");
		_autoMoonRevolutionToggle = GetNodeOrNull<CheckBox>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/AutoMoonRevolutionRow/AutoMoonRevolutionToggle")
			?? GetNodeOrNull<CheckBox>("OrbitSection/OrbitSectionVBox/AutoMoonRevolutionRow/AutoMoonRevolutionToggle");
		_autoMoonRevolutionLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/AutoMoonRevolutionRow/AutoMoonRevolutionLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/AutoMoonRevolutionRow/AutoMoonRevolutionLabel");
		_moonRotationLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/MoonRotationLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/MoonRotationLabel");
		_moonRotationSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/MoonRotationSlider")
			?? GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/MoonRotationSlider");
		_moonRevolutionLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/MoonRevolutionLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/MoonRevolutionLabel");
		_moonRevolutionSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/MoonRevolutionSlider")
			?? GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/MoonRevolutionSlider");
		_extraPlanetsCountLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/ExtraPlanetsCountLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/ExtraPlanetsCountLabel");
		_extraPlanetsCountSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/ExtraPlanetsCountSlider")
			?? GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/ExtraPlanetsCountSlider");
		_extraPlanetFirstOrbitLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/ExtraPlanetFirstOrbitLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/ExtraPlanetFirstOrbitLabel");
		_extraPlanetFirstOrbitSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/ExtraPlanetFirstOrbitSlider")
			?? GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/ExtraPlanetFirstOrbitSlider");
		_extraPlanetOrbitStepLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/ExtraPlanetOrbitStepLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/ExtraPlanetOrbitStepLabel");
		_extraPlanetOrbitStepSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/ExtraPlanetOrbitStepSlider")
			?? GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/ExtraPlanetOrbitStepSlider");
		_extraMoonsCountLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/ExtraMoonsCountLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/ExtraMoonsCountLabel");
		_extraMoonsCountSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/ExtraMoonsCountSlider")
			?? GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/ExtraMoonsCountSlider");
		_extraMoonFirstOrbitLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/ExtraMoonFirstOrbitLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/ExtraMoonFirstOrbitLabel");
		_extraMoonFirstOrbitSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/ExtraMoonFirstOrbitSlider")
			?? GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/ExtraMoonFirstOrbitSlider");
		_extraMoonOrbitStepLabel = GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/ExtraMoonOrbitStepLabel")
			?? GetNodeOrNull<Label>("OrbitSection/OrbitSectionVBox/ExtraMoonOrbitStepLabel");
		_extraMoonOrbitStepSlider = GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/OrbitAdvancedContainer/ExtraMoonOrbitStepSlider")
			?? GetNodeOrNull<HSlider>("OrbitSection/OrbitSectionVBox/ExtraMoonOrbitStepSlider");
	}

	private void BindCelestialPhysicsUi()
	{
		BindCelestialSlider(_starMassSlider);
		BindCelestialSlider(_planetRadiusSlider);
		BindCelestialSlider(_planetMassSlider);
		BindCelestialSlider(_planetOrbitDistanceSlider);
		BindCelestialSlider(_planetRotationSlider);
		BindCelestialSlider(_planetRevolutionSlider);
		BindCelestialSlider(_moonOrbitDistanceSlider);
		BindCelestialSlider(_moonRotationSlider);
		BindCelestialSlider(_moonRevolutionSlider);
		BindCelestialSlider(_extraPlanetsCountSlider);
		BindCelestialSlider(_extraPlanetFirstOrbitSlider);
		BindCelestialSlider(_extraPlanetOrbitStepSlider);
		BindCelestialSlider(_extraMoonsCountSlider);
		BindCelestialSlider(_extraMoonFirstOrbitSlider);
		BindCelestialSlider(_extraMoonOrbitStepSlider);

		if (_autoPlanetRevolutionToggle != null)
		{
			_autoPlanetRevolutionToggle.Toggled += OnCelestialToggleChanged;
		}

		if (_autoMoonRevolutionToggle != null)
		{
			_autoMoonRevolutionToggle.Toggled += OnCelestialToggleChanged;
		}
	}

	private void BindCelestialSlider(HSlider slider)
	{
		if (slider != null)
		{
			slider.ValueChanged += OnCelestialSliderValueChanged;
		}
	}

	private void OnCelestialSliderValueChanged(double value)
	{
		UpdateCelestialLabels();
		EmitCelestialPhysicsChanged();
	}

	private void OnCelestialToggleChanged(bool toggled)
	{
		UpdateCelestialLabels();
		ApplyInspectorVisibility();
		EmitCelestialPhysicsChanged();
	}

	private void OnOrbitAdvancedTogglePressed()
	{
		if (!_isHardcoreMode)
		{
			return;
		}

		_isOrbitAdvancedExpanded = !_isOrbitAdvancedExpanded;
		ApplyInspectorVisibility();
		ApplyVisualLabels();
	}

	private void UpdateCelestialLabels()
	{
		bool isZh = _translationManager?.CurrentLanguage?.StartsWith("zh") ?? true;

		if (_starMassLabel != null && _starMassSlider != null)
		{
			_starMassLabel.Text = isZh
				? $"恒星质量: {(float)_starMassSlider.Value:0.00} M☉"
				: $"Star Mass: {(float)_starMassSlider.Value:0.00} M☉";
		}

		if (_planetRadiusLabel != null && _planetRadiusSlider != null)
		{
			_planetRadiusLabel.Text = isZh
				? $"行星半径: {(float)_planetRadiusSlider.Value:0.00} R⊕"
				: $"Planet Radius: {(float)_planetRadiusSlider.Value:0.00} R⊕";
		}

		if (_planetMassLabel != null && _planetMassSlider != null)
		{
			_planetMassLabel.Text = isZh
				? $"行星质量: {(float)_planetMassSlider.Value:0.00} M⊕"
				: $"Planet Mass: {(float)_planetMassSlider.Value:0.00} M⊕";
		}

		if (_planetOrbitDistanceLabel != null && _planetOrbitDistanceSlider != null)
		{
			_planetOrbitDistanceLabel.Text = isZh
				? $"行星轨道: {(float)_planetOrbitDistanceSlider.Value:0.000} AU"
				: $"Planet Orbit: {(float)_planetOrbitDistanceSlider.Value:0.000} AU";
		}

		if (_planetRotationLabel != null && _planetRotationSlider != null)
		{
			_planetRotationLabel.Text = isZh
				? $"行星自转: {(float)_planetRotationSlider.Value:0.0} 小时"
				: $"Planet Rotation: {(float)_planetRotationSlider.Value:0.0} h";
		}

		if (_planetRevolutionLabel != null && _planetRevolutionSlider != null)
		{
			_planetRevolutionLabel.Text = isZh
				? $"行星公转: {(float)_planetRevolutionSlider.Value:0.0} 天"
				: $"Planet Period: {(float)_planetRevolutionSlider.Value:0.0} d";
		}

		if (_moonOrbitDistanceLabel != null && _moonOrbitDistanceSlider != null)
		{
			_moonOrbitDistanceLabel.Text = isZh
				? $"卫星轨道: {(float)_moonOrbitDistanceSlider.Value:0.0} Rplanet"
				: $"Moon Orbit: {(float)_moonOrbitDistanceSlider.Value:0.0} Rplanet";
		}

		if (_moonRotationLabel != null && _moonRotationSlider != null)
		{
			_moonRotationLabel.Text = isZh
				? $"卫星自转: {(float)_moonRotationSlider.Value:0.0} 小时"
				: $"Moon Rotation: {(float)_moonRotationSlider.Value:0.0} h";
		}

		if (_moonRevolutionLabel != null && _moonRevolutionSlider != null)
		{
			_moonRevolutionLabel.Text = isZh
				? $"卫星公转: {(float)_moonRevolutionSlider.Value:0.0} 天"
				: $"Moon Period: {(float)_moonRevolutionSlider.Value:0.0} d";
		}

		if (_extraPlanetsCountLabel != null && _extraPlanetsCountSlider != null)
		{
			_extraPlanetsCountLabel.Text = isZh
				? $"额外行星: {Mathf.RoundToInt((float)_extraPlanetsCountSlider.Value)}"
				: $"Extra Planets: {Mathf.RoundToInt((float)_extraPlanetsCountSlider.Value)}";
		}

		if (_extraPlanetFirstOrbitLabel != null && _extraPlanetFirstOrbitSlider != null)
		{
			_extraPlanetFirstOrbitLabel.Text = isZh
				? $"额外行星起始轨道: {(float)_extraPlanetFirstOrbitSlider.Value:0.00} AU"
				: $"Extra Planet Start Orbit: {(float)_extraPlanetFirstOrbitSlider.Value:0.00} AU";
		}

		if (_extraPlanetOrbitStepLabel != null && _extraPlanetOrbitStepSlider != null)
		{
			_extraPlanetOrbitStepLabel.Text = isZh
				? $"额外行星轨道间隔: {(float)_extraPlanetOrbitStepSlider.Value:0.00} AU"
				: $"Extra Planet Spacing: {(float)_extraPlanetOrbitStepSlider.Value:0.00} AU";
		}

		if (_extraMoonsCountLabel != null && _extraMoonsCountSlider != null)
		{
			_extraMoonsCountLabel.Text = isZh
				? $"额外卫星: {Mathf.RoundToInt((float)_extraMoonsCountSlider.Value)}"
				: $"Extra Moons: {Mathf.RoundToInt((float)_extraMoonsCountSlider.Value)}";
		}

		if (_extraMoonFirstOrbitLabel != null && _extraMoonFirstOrbitSlider != null)
		{
			_extraMoonFirstOrbitLabel.Text = isZh
				? $"额外卫星起始轨道: {(float)_extraMoonFirstOrbitSlider.Value:0.0} Rplanet"
				: $"Extra Moon Start Orbit: {(float)_extraMoonFirstOrbitSlider.Value:0.0} Rplanet";
		}

		if (_extraMoonOrbitStepLabel != null && _extraMoonOrbitStepSlider != null)
		{
			_extraMoonOrbitStepLabel.Text = isZh
				? $"额外卫星轨道间隔: {(float)_extraMoonOrbitStepSlider.Value:0.0} Rplanet"
				: $"Extra Moon Spacing: {(float)_extraMoonOrbitStepSlider.Value:0.0} Rplanet";
		}

		if (_orbitSectionTitle != null)
		{
			_orbitSectionTitle.Text = isZh ? "[肆] 检查器" : "[IV] Inspector";
		}

		if (_showPlanetOrbitToggle != null)
		{
			_showPlanetOrbitToggle.Text = string.Empty;
		}
		if (_showPlanetOrbitLabel != null)
		{
			_showPlanetOrbitLabel.Text = isZh ? "显示行星轨道" : "Show Planet Orbit";
		}

		if (_showMoonOrbitToggle != null)
		{
			_showMoonOrbitToggle.Text = string.Empty;
		}
		if (_showMoonOrbitLabel != null)
		{
			_showMoonOrbitLabel.Text = isZh ? "显示卫星轨道" : "Show Moon Orbit";
		}

		if (_autoPlanetRevolutionToggle != null)
		{
			_autoPlanetRevolutionToggle.Text = string.Empty;
		}
		if (_autoPlanetRevolutionLabel != null)
		{
			_autoPlanetRevolutionLabel.Text = isZh ? "行星公转周期自动求解" : "Auto Solve Planet Period";
		}

		if (_autoMoonRevolutionToggle != null)
		{
			_autoMoonRevolutionToggle.Text = string.Empty;
		}
		if (_autoMoonRevolutionLabel != null)
		{
			_autoMoonRevolutionLabel.Text = isZh ? "卫星公转周期自动求解" : "Auto Solve Moon Period";
		}

		ApplyInspectorVisibility();
		ApplyVisualLabels();
	}

	private void EnsureTextureSelectorNodes()
	{
		_downloadedOnlyToggle = GetNodeOrNull<CheckBox>("PlanetPhotoPanel/PlanetPhotoVBox/DownloadedOnlyRow/DownloadedOnlyToggle")
			?? _downloadedOnlyToggle;
		_downloadedOnlyLabel = GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/DownloadedOnlyRow/DownloadedOnlyLabel")
			?? _downloadedOnlyLabel;
		_sunTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/SunTextureSelector")
			?? _sunTextureSelector;
		_planetSurfaceTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/PlanetSurfaceTextureSelector")
			?? _planetSurfaceTextureSelector;
		_moonTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/MoonTextureSelector")
			?? _moonTextureSelector;
		_solarBrightnessLabel = GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/SolarBrightnessLabel")
			?? _solarBrightnessLabel;
		_solarBrightnessSlider = GetNodeOrNull<HSlider>("PlanetPhotoPanel/PlanetPhotoVBox/SolarBrightnessSlider")
			?? _solarBrightnessSlider;
	}

	private void SetupTextureSelectors()
	{
		SetupSkyTextureSelector();
		SetupPlanetSurfaceTextureSelector();
		SetupMoonTextureSelector();
		SetupSunTextureSelector();
	}

	private void SetupSkyTextureSelector()
	{
		if (_skyTextureSelector == null)
		{
			return;
		}

		_skyTextureSelector.ItemSelected -= OnSkyTextureSelectorItemSelected;
		_skyTextureSelector.Clear();
		_skyTexturePathById.Clear();

		bool showDownloadedOnly = ShowDownloadedOnly;
		int itemId = 1;
		int selectedIndex = -1;

		for (int i = 0; i < SkyTextureOptions.Length; i++)
		{
			TextureOption option = SkyTextureOptions[i];
			if (showDownloadedOnly && !ResourceLoader.Exists(option.Path))
			{
				continue;
			}

			_skyTextureSelector.AddItem(option.Label, itemId);
			_skyTexturePathById[itemId] = option.Path;
			if (option.Path == SelectedSkyTexturePath)
			{
				selectedIndex = _skyTextureSelector.ItemCount - 1;
			}

			itemId++;
		}

		if (_skyTextureSelector.ItemCount > 0)
		{
			if (selectedIndex < 0)
			{
				selectedIndex = 0;
			}

			_skyTextureSelector.Select(selectedIndex);
			int selectedId = _skyTextureSelector.GetItemId(selectedIndex);
			if (_skyTexturePathById.TryGetValue(selectedId, out string texturePath))
			{
				SelectedSkyTexturePath = texturePath;
			}
		}

		_skyTextureSelector.ItemSelected += OnSkyTextureSelectorItemSelected;
		ApplyVisualLabels();
	}

	private void SetupPlanetSurfaceTextureSelector()
	{
		if (_planetSurfaceTextureSelector == null)
		{
			return;
		}

		_planetSurfaceTextureSelector.ItemSelected -= OnPlanetSurfaceTextureSelectorItemSelected;
		_planetSurfaceTextureSelector.Clear();
		_planetTexturePathById.Clear();

		PopulateTextureSelector(
			_planetSurfaceTextureSelector,
			_planetTexturePathById,
			PlanetSurfaceOptions,
			SelectedPlanetSurfaceTexturePath,
			out string selectedPath,
			out _);

		SelectedPlanetSurfaceTexturePath = selectedPath;
		_planetSurfaceTextureSelector.ItemSelected += OnPlanetSurfaceTextureSelectorItemSelected;
	}

	private void SetupMoonTextureSelector()
	{
		if (_moonTextureSelector == null)
		{
			return;
		}

		_moonTextureSelector.ItemSelected -= OnMoonTextureSelectorItemSelected;
		_moonTextureSelector.Clear();
		_moonTexturePathById.Clear();

		PopulateTextureSelector(
			_moonTextureSelector,
			_moonTexturePathById,
			MoonTextureOptions,
			SelectedMoonTexturePath,
			out string selectedPath,
			out _);

		SelectedMoonTexturePath = selectedPath;
		_moonTextureSelector.ItemSelected += OnMoonTextureSelectorItemSelected;
	}

	private void SetupSunTextureSelector()
	{
		if (_sunTextureSelector == null)
		{
			return;
		}

		_sunTextureSelector.ItemSelected -= OnSunTextureSelectorItemSelected;
		_sunTextureSelector.Clear();
		_sunTexturePathById.Clear();

		PopulateTextureSelector(
			_sunTextureSelector,
			_sunTexturePathById,
			SunTextureOptions,
			SelectedSunTexturePath,
			out string selectedPath,
			out _);

		SelectedSunTexturePath = selectedPath;
		_sunTextureSelector.ItemSelected += OnSunTextureSelectorItemSelected;
	}

	private void PopulateTextureSelector(
		OptionButton selector,
		Dictionary<int, string> pathMap,
		TextureOption[] options,
		string targetPath,
		out string selectedPath,
		out int selectedIndex)
	{
		bool showDownloadedOnly = ShowDownloadedOnly;
		int itemId = 1;
		selectedIndex = -1;
		selectedPath = string.Empty;

		for (int i = 0; i < options.Length; i++)
		{
			TextureOption option = options[i];
			bool isDefaultOption = string.IsNullOrEmpty(option.Path);
			if (showDownloadedOnly && !isDefaultOption && !ResourceLoader.Exists(option.Path))
			{
				continue;
			}

			selector.AddItem(FormatOptionLabel(option), itemId);
			pathMap[itemId] = option.Path;
			if (option.Path == targetPath)
			{
				selectedIndex = selector.ItemCount - 1;
			}

			itemId++;
		}

		if (selector.ItemCount <= 0)
		{
			selector.AddItem("(无可用资源)", 1);
			pathMap[1] = string.Empty;
			selectedIndex = 0;
		}

		if (selectedIndex < 0)
		{
			selectedIndex = 0;
		}

		selector.Select(selectedIndex);
		int selectedId = selector.GetItemId(selectedIndex);
		if (pathMap.TryGetValue(selectedId, out string finalPath))
		{
			selectedPath = finalPath;
		}
	}

	private void ApplyInspectorVisibility()
	{
		bool starMode = _isInspectingStar;
		bool hardcore = _isHardcoreMode;
		bool explorer = !hardcore;
		bool showAdvanced = hardcore && _isOrbitAdvancedExpanded;

		SetVisible(_orbitAdvancedToggleButton, hardcore);
		SetVisible(_orbitAdvancedContainer, showAdvanced);

		SetVisible(_showPlanetOrbitToggle, true);
		SetVisible(_showPlanetOrbitLabel, true);
		SetVisible(_showMoonOrbitToggle, showAdvanced);
		SetVisible(_showMoonOrbitLabel, showAdvanced);

		SetVisible(_starMassLabel, showAdvanced || starMode);
		SetVisible(_starMassSlider, showAdvanced || starMode);

		SetVisible(_planetRadiusLabel, showAdvanced || !starMode);
		SetVisible(_planetRadiusSlider, showAdvanced || !starMode);
		SetVisible(_planetOrbitDistanceLabel, showAdvanced || !starMode);
		SetVisible(_planetOrbitDistanceSlider, showAdvanced || !starMode);

		SetVisible(_planetMassLabel, showAdvanced);
		SetVisible(_planetMassSlider, showAdvanced);
		SetVisible(_autoPlanetRevolutionToggle, showAdvanced);
		SetVisible(_autoPlanetRevolutionLabel, showAdvanced);
		SetVisible(_planetRotationLabel, showAdvanced);
		SetVisible(_planetRotationSlider, showAdvanced);
		SetVisible(_planetRevolutionLabel, showAdvanced);
		SetVisible(_planetRevolutionSlider, showAdvanced);

		SetVisible(_moonOrbitDistanceLabel, showAdvanced);
		SetVisible(_moonOrbitDistanceSlider, showAdvanced);
		SetVisible(_autoMoonRevolutionToggle, showAdvanced);
		SetVisible(_autoMoonRevolutionLabel, showAdvanced);
		SetVisible(_moonRotationLabel, showAdvanced);
		SetVisible(_moonRotationSlider, showAdvanced);
		SetVisible(_moonRevolutionLabel, showAdvanced);
		SetVisible(_moonRevolutionSlider, showAdvanced);

		SetVisible(_extraPlanetsCountLabel, showAdvanced);
		SetVisible(_extraPlanetsCountSlider, showAdvanced);
		SetVisible(_extraPlanetFirstOrbitLabel, showAdvanced);
		SetVisible(_extraPlanetFirstOrbitSlider, showAdvanced);
		SetVisible(_extraPlanetOrbitStepLabel, showAdvanced);
		SetVisible(_extraPlanetOrbitStepSlider, showAdvanced);

		SetVisible(_extraMoonsCountLabel, showAdvanced);
		SetVisible(_extraMoonsCountSlider, showAdvanced);
		SetVisible(_extraMoonFirstOrbitLabel, showAdvanced);
		SetVisible(_extraMoonFirstOrbitSlider, showAdvanced);
		SetVisible(_extraMoonOrbitStepLabel, showAdvanced);
		SetVisible(_extraMoonOrbitStepSlider, showAdvanced);

		if (explorer)
		{
			SetVisible(_showMoonOrbitToggle, false);
			SetVisible(_showMoonOrbitLabel, false);
		}

		if (_planetRevolutionSlider != null && _autoPlanetRevolutionToggle != null)
		{
			bool editable = !_autoPlanetRevolutionToggle.ButtonPressed;
			SetSliderEditable(_planetRevolutionSlider, editable);
			_planetRevolutionSlider.Modulate = editable
				? Colors.White
				: new Color(1f, 1f, 1f, 0.55f);
		}

		if (_moonRevolutionSlider != null && _autoMoonRevolutionToggle != null)
		{
			bool editable = !_autoMoonRevolutionToggle.ButtonPressed;
			SetSliderEditable(_moonRevolutionSlider, editable);
			_moonRevolutionSlider.Modulate = editable
				? Colors.White
				: new Color(1f, 1f, 1f, 0.55f);
		}
	}

	private static void SetSliderEditable(HSlider slider, bool editable)
	{
		if (slider == null)
		{
			return;
		}

		slider.Set("editable", editable);
		slider.MouseFilter = editable
			? MouseFilterEnum.Stop
			: MouseFilterEnum.Ignore;
	}

	private void ApplyVisualLabels()
	{
		bool isZh = _translationManager?.CurrentLanguage?.StartsWith("zh") ?? true;

		if (_orbitHudTitle != null)
		{
			if (_isAnimationStyle)
			{
				_orbitHudTitle.Text = isZh ? "寰宇遨游 · 动画风格" : "Cosmos Preview · Animation";
			}
			else
			{
				_orbitHudTitle.Text = isZh ? "寰宇遨游 · 仿真风格" : "Cosmos Preview · Simulation";
			}
		}

		if (_orbitHudSubtitle != null)
		{
			_orbitHudSubtitle.Text = isZh
				? "左键旋转 | 右键平移 | 滚轮缩放"
				: "LMB Orbit | RMB Pan | Wheel Zoom";
		}

		if (_orbitHudTarget != null)
		{
			_orbitHudTarget.Text = _isInspectingStar
				? (isZh ? "检查器目标: 恒星" : "Inspector Target: Star")
				: (isZh ? "检查器目标: 行星" : "Inspector Target: Planet");
		}

		if (_orbitHudHint != null)
		{
			if (_isHardcoreMode)
			{
				_orbitHudHint.Text = isZh
					? "硬核模式：轨道动力学与自动求解已启用。"
					: "God Mode: full dynamics and auto-solver controls enabled.";
			}
			else
			{
				_orbitHudHint.Text = isZh
					? "探索模式：展示核心参数，降低操作复杂度。"
					: "Explorer Mode: only key controls are shown.";
			}
		}

		if (_planetPhotoTitle != null)
		{
			_planetPhotoTitle.Text = _isAnimationStyle
				? (isZh ? "行星图示（动画）" : "Planet Illustration (Animated)")
				: (isZh ? "行星图示（仿真）" : "Planet Illustration (Simulation)");
		}

		if (_skyTextureLabel != null)
		{
			_skyTextureLabel.Text = isZh ? "恒星系背景" : "System Background";
		}

		if (_livePhotoToggle != null)
		{
			_livePhotoToggle.Text = string.Empty;
		}
		if (_livePhotoLabel != null)
		{
			_livePhotoLabel.Text = isZh ? "实时更新行星照片" : "Live-update planet image";
		}
		if (_lightFollowToggle != null)
		{
			_lightFollowToggle.Text = string.Empty;
		}
		if (_lightFollowLabel != null)
		{
			_lightFollowLabel.Text = isZh ? "光照跟随参数变化" : "Light follows parameter changes";
		}
		if (_downloadedOnlyToggle != null)
		{
			_downloadedOnlyToggle.Text = string.Empty;
		}
		if (_downloadedOnlyLabel != null)
		{
			_downloadedOnlyLabel.Text = isZh ? "仅显示已下载资源" : "Only show downloaded assets";
		}

		if (_orbitSectionTitle != null)
		{
			if (_isHardcoreMode)
			{
				_orbitSectionTitle.Text = isZh ? "[肆] 上帝模式 · 全参数调谐" : "[IV] God Mode · Full Parameter Tuning";
			}
			else
			{
				_orbitSectionTitle.Text = _isInspectingStar
					? (isZh ? "[肆] 恒星参数（入门）" : "[IV] Star Parameters (Entry)")
					: (isZh ? "[肆] 行星参数（入门）" : "[IV] Planet Parameters (Entry)");
			}
		}

		if (_modeTierHintLabel != null)
		{
			_modeTierHintLabel.Text = isZh
				? "探索模式: 入门参数 | 上帝模式: 展开高级轨道参数"
				: "Explorer: entry parameters | God Mode: expand advanced orbital controls";
		}

		if (_orbitAdvancedToggleButton != null)
		{
			string prefix = _isOrbitAdvancedExpanded ? "▼" : "▶";
			_orbitAdvancedToggleButton.Text = isZh
				? $"{prefix} 高级轨道参数"
				: $"{prefix} Advanced Orbital Controls";
		}

		SetVisible(_orbitHudTopLeft, true);
		SetVisible(_orbitHudBottomRight, true);
	}

	private void SelectSkyTextureByPath(string path)
	{
		SelectTextureOptionByPath(_skyTextureSelector, _skyTexturePathById, path, out string selectedPath);
		SelectedSkyTexturePath = selectedPath;
	}

	private void SelectPlanetTextureByPath(string path)
	{
		SelectTextureOptionByPath(_planetSurfaceTextureSelector, _planetTexturePathById, path, out string selectedPath);
		SelectedPlanetSurfaceTexturePath = selectedPath;
	}

	private void SelectMoonTextureByPath(string path)
	{
		SelectTextureOptionByPath(_moonTextureSelector, _moonTexturePathById, path, out string selectedPath);
		SelectedMoonTexturePath = selectedPath;
	}

	private void SelectSunTextureByPath(string path)
	{
		SelectTextureOptionByPath(_sunTextureSelector, _sunTexturePathById, path, out string selectedPath);
		SelectedSunTexturePath = selectedPath;
	}

	private static void SelectTextureOptionByPath(
		OptionButton selector,
		Dictionary<int, string> pathMap,
		string targetPath,
		out string selectedPath)
	{
		selectedPath = string.Empty;
		if (selector == null || selector.ItemCount <= 0)
		{
			return;
		}

		int selectedIndex = 0;
		for (int i = 0; i < selector.ItemCount; i++)
		{
			int itemId = selector.GetItemId(i);
			if (!pathMap.TryGetValue(itemId, out string path))
			{
				continue;
			}

			if (path == (targetPath ?? string.Empty))
			{
				selectedIndex = i;
				break;
			}
		}

		selector.Select(selectedIndex);
		int selectedId = selector.GetItemId(selectedIndex);
		if (pathMap.TryGetValue(selectedId, out string resolvedPath))
		{
			selectedPath = resolvedPath ?? string.Empty;
		}
	}

	private static void SetVisible(CanvasItem node, bool visible)
	{
		if (node != null)
		{
			node.Visible = visible;
		}
	}

	private void OnSkyTextureSelectorItemSelected(long index)
	{
		if (_skyTextureSelector == null)
		{
			return;
		}

		int itemId = _skyTextureSelector.GetItemId((int)index);
		if (_skyTexturePathById.TryGetValue(itemId, out string texturePath))
		{
			SelectedSkyTexturePath = texturePath;
			EmitSignal(SignalName.SkyTextureChanged, texturePath);
		}
	}

	private void OnPlanetSurfaceTextureSelectorItemSelected(long index)
	{
		if (_planetSurfaceTextureSelector == null)
		{
			return;
		}

		int itemId = _planetSurfaceTextureSelector.GetItemId((int)index);
		if (_planetTexturePathById.TryGetValue(itemId, out string texturePath))
		{
			SelectedPlanetSurfaceTexturePath = texturePath;
			EmitSignal(SignalName.PlanetSurfaceTextureChanged, texturePath);
		}
	}

	private void OnMoonTextureSelectorItemSelected(long index)
	{
		if (_moonTextureSelector == null)
		{
			return;
		}

		int itemId = _moonTextureSelector.GetItemId((int)index);
		if (_moonTexturePathById.TryGetValue(itemId, out string texturePath))
		{
			SelectedMoonTexturePath = texturePath;
			EmitSignal(SignalName.MoonTextureChanged, texturePath);
		}
	}

	private void OnSunTextureSelectorItemSelected(long index)
	{
		if (_sunTextureSelector == null)
		{
			return;
		}

		int itemId = _sunTextureSelector.GetItemId((int)index);
		if (_sunTexturePathById.TryGetValue(itemId, out string texturePath))
		{
			SelectedSunTexturePath = texturePath;
			EmitSignal(SignalName.SunTextureChanged, texturePath);
		}
	}

	private void OnDownloadedOnlyToggleChanged(bool toggledOn)
	{
		SetupTextureSelectors();
		EmitSignal(SignalName.DownloadedOnlyFilterChanged, toggledOn);
	}

	private void OnLanguageChanged(string language)
	{
		if (_buildLabel != null)
		{
			_buildLabel.Text = _translationManager?.Tr("preview_building_cosmos") ?? _buildLabel.Text;
		}

		if (_lightResponseSlider != null)
		{
			UpdateLightResponseLabel((float)_lightResponseSlider.Value);
		}

		if (_solarBrightnessSlider != null)
		{
			UpdateSolarBrightnessLabel((float)_solarBrightnessSlider.Value);
		}

		UpdateCelestialLabels();
		ApplyVisualLabels();
	}

	private void CleanupEventBindings()
	{
		if (_translationManager != null)
		{
			_translationManager.LanguageChanged -= OnLanguageChanged;
		}

		if (_planetPhotoTexture != null)
		{
			_planetPhotoTexture.GuiInput -= OnPlanetPhotoTextureGuiInput;
		}

		if (_photoViewerOverlay != null)
		{
			_photoViewerOverlay.GuiInput -= OnPhotoViewerOverlayGuiInput;
		}

		if (_photoViewerCloseButton != null)
		{
			_photoViewerCloseButton.Pressed -= OnPhotoViewerClosePressed;
		}

		if (_livePhotoToggle != null)
		{
			_livePhotoToggle.Toggled -= OnLivePhotoToggleChanged;
		}

		if (_lightFollowToggle != null)
		{
			_lightFollowToggle.Toggled -= OnLightFollowToggleChanged;
		}

		if (_lightResponseSlider != null)
		{
			_lightResponseSlider.ValueChanged -= OnLightResponseSliderChanged;
		}

		if (_solarBrightnessSlider != null)
		{
			_solarBrightnessSlider.ValueChanged -= OnSolarBrightnessSliderChanged;
		}

		if (_refreshPhotoButton != null)
		{
			_refreshPhotoButton.Pressed -= OnRefreshPhotoPressed;
		}

		if (_downloadedOnlyToggle != null)
		{
			_downloadedOnlyToggle.Toggled -= OnDownloadedOnlyToggleChanged;
		}

		if (_orbitAdvancedToggleButton != null)
		{
			_orbitAdvancedToggleButton.Pressed -= OnOrbitAdvancedTogglePressed;
		}

		if (_skyTextureSelector != null)
		{
			_skyTextureSelector.ItemSelected -= OnSkyTextureSelectorItemSelected;
		}

		if (_planetSurfaceTextureSelector != null)
		{
			_planetSurfaceTextureSelector.ItemSelected -= OnPlanetSurfaceTextureSelectorItemSelected;
		}

		if (_moonTextureSelector != null)
		{
			_moonTextureSelector.ItemSelected -= OnMoonTextureSelectorItemSelected;
		}

		if (_sunTextureSelector != null)
		{
			_sunTextureSelector.ItemSelected -= OnSunTextureSelectorItemSelected;
		}
		UnbindCelestialSlider(_starMassSlider);
		UnbindCelestialSlider(_planetRadiusSlider);
		UnbindCelestialSlider(_planetMassSlider);
		UnbindCelestialSlider(_planetOrbitDistanceSlider);
		UnbindCelestialSlider(_planetRotationSlider);
		UnbindCelestialSlider(_planetRevolutionSlider);
		UnbindCelestialSlider(_moonOrbitDistanceSlider);
		UnbindCelestialSlider(_moonRotationSlider);
		UnbindCelestialSlider(_moonRevolutionSlider);
		UnbindCelestialSlider(_extraPlanetsCountSlider);
		UnbindCelestialSlider(_extraPlanetFirstOrbitSlider);
		UnbindCelestialSlider(_extraPlanetOrbitStepSlider);
		UnbindCelestialSlider(_extraMoonsCountSlider);
		UnbindCelestialSlider(_extraMoonFirstOrbitSlider);
		UnbindCelestialSlider(_extraMoonOrbitStepSlider);

		if (_autoPlanetRevolutionToggle != null)
		{
			_autoPlanetRevolutionToggle.Toggled -= OnCelestialToggleChanged;
		}

		if (_autoMoonRevolutionToggle != null)
		{
			_autoMoonRevolutionToggle.Toggled -= OnCelestialToggleChanged;
		}

	}

	private void UnbindCelestialSlider(HSlider slider)
	{
		if (slider != null)
		{
			slider.ValueChanged -= OnCelestialSliderValueChanged;
		}
	}

	private void OnLivePhotoToggleChanged(bool toggledOn)
	{
		EmitSignal(SignalName.LivePhotoModeChanged, toggledOn);
	}

	private void OnLightFollowToggleChanged(bool toggledOn)
	{
		EmitSignal(SignalName.LightFollowModeChanged, toggledOn);
	}

	private void OnLightResponseSliderChanged(double value)
	{
		float strength = Mathf.Clamp((float)value, 0f, 1f);
		UpdateLightResponseLabel(strength);
		EmitSignal(SignalName.LightResponseChanged, strength);
	}

	private void OnSolarBrightnessSliderChanged(double value)
	{
		float brightness = Mathf.Clamp((float)value, 0.5f, 2.5f);
		UpdateSolarBrightnessLabel(brightness);
		UpdatePreview(_planetData, _lawAlignment);
		EmitSignal(SignalName.SolarBrightnessChanged, brightness);
	}

	private void OnRefreshPhotoPressed()
	{
		EmitSignal(SignalName.SnapshotRefreshRequested);
	}

	private void OnPlanetPhotoTextureGuiInput(InputEvent inputEvent)
	{
		if (_planetPhotoTexture?.Texture == null)
		{
			return;
		}

		if (inputEvent is InputEventMouseButton mouseButton
			&& mouseButton.Pressed
			&& mouseButton.ButtonIndex == MouseButton.Left)
		{
			OpenPhotoViewer(_planetPhotoTexture.Texture);
			AcceptEvent();
		}
	}

	private void OnPhotoViewerOverlayGuiInput(InputEvent inputEvent)
	{
		if (_photoViewerOverlay?.Visible != true)
		{
			return;
		}

		if (inputEvent is InputEventMouseButton mouseButton
			&& mouseButton.Pressed
			&& mouseButton.ButtonIndex == MouseButton.Left)
		{
			ClosePhotoViewer();
			AcceptEvent();
		}
	}

	private void OnPhotoViewerClosePressed()
	{
		ClosePhotoViewer();
	}

	private void UpdateLightResponseLabel(float value)
	{
		if (_lightResponseLabel != null)
		{
			int percent = Mathf.RoundToInt(Mathf.Clamp(value, 0f, 1f) * 100f);
			_lightResponseLabel.Text = _translationManager?.TrWithFormat("preview_light_response", $"{percent}")
				?? $"Light Response: {percent}%";
		}
	}

	private void UpdateSolarBrightnessLabel(float value)
	{
		if (_solarBrightnessLabel == null)
		{
			return;
		}

		int percent = Mathf.RoundToInt(Mathf.Clamp(value, 0.5f, 2.5f) * 100f);
		bool isZh = _translationManager?.CurrentLanguage == "zh-CN";
		_solarBrightnessLabel.Text = isZh
			? $"太阳亮度: {percent}%"
			: $"Solar Brightness: {percent}%";
	}

	private void OpenPhotoViewer(Texture2D texture)
	{
		if (_photoViewerOverlay == null || _photoViewerTexture == null || texture == null)
		{
			return;
		}

		_photoViewerTexture.Texture = texture;
		_photoViewerOverlay.Visible = true;
		_photoViewerOverlay.GrabFocus();
	}

	private void ClosePhotoViewer()
	{
		if (_photoViewerOverlay == null)
		{
			return;
		}

		_photoViewerOverlay.Visible = false;
	}

	private void EnsurePreviewMaterial()
	{
		if (!_useStaticPreview)
		{
			return;
		}

		if (_previewMaterial == null)
		{
			var shader = GD.Load<Shader>("res://Shaders/PlanetPreview.gdshader");
			if (shader == null)
			{
				return;
			}

			_previewMaterial = new ShaderMaterial
			{
				Shader = shader
			};
		}

		Material = _previewMaterial;
		Texture = null;
		SelfModulate = Colors.White;
	}

	private static bool IsControlHit(Control control, Vector2 point)
	{
		if (control == null || !control.Visible)
		{
			return false;
		}

		var rect = new Rect2(control.Position, control.Size);
		return rect.HasPoint(point);
	}

	private static string FormatOptionLabel(TextureOption option)
	{
		if (string.IsNullOrWhiteSpace(option.Source))
		{
			return option.Label;
		}

		return $"[{option.Source}] {option.Label}";
	}

	private static float ResolveElementType(PlanetElement element)
	{
		return element switch
		{
			PlanetElement.Terra => 0f,
			PlanetElement.Pyro => 1f,
			PlanetElement.Cryo => 2f,
			PlanetElement.Aero => 3f,
			_ => 0f
		};
	}

	private static PlanetData ClonePlanetData(PlanetData planetData)
	{
		if (planetData == null)
		{
			return new PlanetData();
		}

		return new PlanetData
		{
			PlanetId = planetData.PlanetId,
			Name = planetData.Name,
			Element = planetData.Element,
			Size = planetData.Size,
			OceanCoverage = planetData.OceanCoverage,
			Temperature = planetData.Temperature,
			AtmosphereDensity = planetData.AtmosphereDensity,
			MountainIntensity = planetData.MountainIntensity,
			PolarCoverage = planetData.PolarCoverage,
			DesertRatio = planetData.DesertRatio
		};
	}
}
