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
	private Control _orbitSection;
	private Control _planetPhotoPanel;
	private TextureRect _planetPhotoTexture;
	private Control _photoViewerOverlay;
	private TextureRect _photoViewerTexture;
	private Button _photoViewerCloseButton;
	private CheckButton _livePhotoToggle;
	private CheckButton _lightFollowToggle;
	private Label _lightResponseLabel;
	private HSlider _lightResponseSlider;
	private Label _solarBrightnessLabel;
	private HSlider _solarBrightnessSlider;
	private Button _refreshPhotoButton;
	private OptionButton _skyTextureSelector;
	private OptionButton _planetSurfaceTextureSelector;
	private OptionButton _moonTextureSelector;
	private OptionButton _sunTextureSelector;
	private CheckButton _downloadedOnlyToggle;
	private Label _starMassLabel;
	private HSlider _starMassSlider;
	private Label _planetRadiusLabel;
	private HSlider _planetRadiusSlider;
	private Label _planetMassLabel;
	private HSlider _planetMassSlider;
	private Label _planetOrbitDistanceLabel;
	private HSlider _planetOrbitDistanceSlider;
	private CheckButton _autoPlanetRevolutionToggle;
	private Label _planetRotationLabel;
	private HSlider _planetRotationSlider;
	private Label _planetRevolutionLabel;
	private HSlider _planetRevolutionSlider;
	private Label _moonOrbitDistanceLabel;
	private HSlider _moonOrbitDistanceSlider;
	private CheckButton _autoMoonRevolutionToggle;
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

	private int _lawAlignment = 50;
	private PlanetData _planetData = new PlanetData
	{
		Element = PlanetElement.Terra,
		OceanCoverage = 0.35f,
		Temperature = 0.5f,
		AtmosphereDensity = 0.55f
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
		_orbitSection = GetNodeOrNull<Control>("OrbitSection");
		_planetPhotoPanel = GetNodeOrNull<Control>("PlanetPhotoPanel");
		_planetPhotoTexture = GetNodeOrNull<TextureRect>("PlanetPhotoPanel/PlanetPhotoVBox/PlanetPhotoTexture");
		_photoViewerOverlay = GetNodeOrNull<Control>("PhotoViewerOverlay");
		_photoViewerTexture = GetNodeOrNull<TextureRect>("PhotoViewerOverlay/PhotoViewerTexture");
		_photoViewerCloseButton = GetNodeOrNull<Button>("PhotoViewerOverlay/PhotoViewerCloseButton");
		_livePhotoToggle = GetNodeOrNull<CheckButton>("PlanetPhotoPanel/PlanetPhotoVBox/LivePhotoToggle");
		_lightFollowToggle = GetNodeOrNull<CheckButton>("PlanetPhotoPanel/PlanetPhotoVBox/LightFollowToggle");
		_lightResponseLabel = GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/LightResponseLabel");
		_lightResponseSlider = GetNodeOrNull<HSlider>("PlanetPhotoPanel/PlanetPhotoVBox/LightResponseSlider");
		_solarBrightnessLabel = GetNodeOrNull<Label>("PlanetPhotoPanel/PlanetPhotoVBox/SolarBrightnessLabel");
		_solarBrightnessSlider = GetNodeOrNull<HSlider>("PlanetPhotoPanel/PlanetPhotoVBox/SolarBrightnessSlider");
		_refreshPhotoButton = GetNodeOrNull<Button>("PlanetPhotoPanel/PlanetPhotoVBox/RefreshPhotoButton");
		_skyTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/SkyTextureSelector");
		_planetSurfaceTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/PlanetSurfaceTextureSelector");
		_moonTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/MoonTextureSelector");
		_sunTextureSelector = GetNodeOrNull<OptionButton>("PlanetPhotoPanel/PlanetPhotoVBox/SunTextureSelector");
		_downloadedOnlyToggle = GetNodeOrNull<CheckButton>("PlanetPhotoPanel/PlanetPhotoVBox/DownloadedOnlyToggle");

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

		SetBuildState(false, 0f, true);
		SetEditingPaused(false);
		EnterStaticMode();
		UpdatePreview(_planetData, _lawAlignment);
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

		_previewMaterial.SetShaderParameter("element_type", ResolveElementType(_planetData.Element));
		_previewMaterial.SetShaderParameter("ocean_level", Mathf.Lerp(0.2f, 0.78f, Mathf.Clamp(_planetData.OceanCoverage, 0f, 1f)));
		_previewMaterial.SetShaderParameter("temperature", Mathf.Clamp(_planetData.Temperature, 0f, 1f));
		_previewMaterial.SetShaderParameter("atmosphere_density", Mathf.Clamp(_planetData.AtmosphereDensity, 0f, 1f));
		_previewMaterial.SetShaderParameter("cloud_speed", Mathf.Lerp(0.08f, 0.34f, _lawAlignment / 100f));
		_previewMaterial.SetShaderParameter("time_sec", (float)Time.GetTicksMsec() / 1000f);
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

		if (_starMassLabel == null)
		{
			_starMassLabel = CreateCompactSelectorLabel("StarMassLabel", "恒星质量: 1.00 M☉");
			orbitVBox.AddChild(_starMassLabel);
		}

		if (_starMassSlider == null)
		{
			_starMassSlider = new HSlider { Name = "StarMassSlider", MinValue = 0.08, MaxValue = 8.0, Step = 0.01, Value = 1.0 };
			orbitVBox.AddChild(_starMassSlider);
		}

		if (_planetRadiusLabel == null)
		{
			_planetRadiusLabel = CreateCompactSelectorLabel("PlanetRadiusLabel", "行星半径: 1.00 R⊕");
			orbitVBox.AddChild(_planetRadiusLabel);
		}

		if (_planetRadiusSlider == null)
		{
			_planetRadiusSlider = new HSlider { Name = "PlanetRadiusSlider", MinValue = 0.1, MaxValue = 20.0, Step = 0.01, Value = 1.0 };
			orbitVBox.AddChild(_planetRadiusSlider);
		}

		if (_planetMassLabel == null)
		{
			_planetMassLabel = CreateCompactSelectorLabel("PlanetMassLabel", "行星质量: 1.00 M⊕");
			orbitVBox.AddChild(_planetMassLabel);
		}

		if (_planetMassSlider == null)
		{
			_planetMassSlider = new HSlider { Name = "PlanetMassSlider", MinValue = 0.01, MaxValue = 500.0, Step = 0.01, Value = 1.0 };
			orbitVBox.AddChild(_planetMassSlider);
		}

		if (_planetOrbitDistanceLabel == null)
		{
			_planetOrbitDistanceLabel = CreateCompactSelectorLabel("PlanetOrbitDistanceLabel", "行星轨道: 1.000 AU");
			orbitVBox.AddChild(_planetOrbitDistanceLabel);
		}

		if (_planetOrbitDistanceSlider == null)
		{
			_planetOrbitDistanceSlider = new HSlider { Name = "PlanetOrbitDistanceSlider", MinValue = 0.03, MaxValue = 20.0, Step = 0.001, Value = 1.0 };
			orbitVBox.AddChild(_planetOrbitDistanceSlider);
		}

		if (_autoPlanetRevolutionToggle == null)
		{
			_autoPlanetRevolutionToggle = new CheckButton { Name = "AutoPlanetRevolutionToggle", Text = "行星公转周期自动求解", ButtonPressed = true };
			orbitVBox.AddChild(_autoPlanetRevolutionToggle);
		}

		if (_planetRotationLabel == null)
		{
			_planetRotationLabel = CreateCompactSelectorLabel("PlanetRotationLabel", "行星自转: 23.9 小时");
			orbitVBox.AddChild(_planetRotationLabel);
		}

		if (_planetRotationSlider == null)
		{
			_planetRotationSlider = new HSlider { Name = "PlanetRotationSlider", MinValue = 2.0, MaxValue = 5000.0, Step = 0.1, Value = 23.934 };
			orbitVBox.AddChild(_planetRotationSlider);
		}

		if (_planetRevolutionLabel == null)
		{
			_planetRevolutionLabel = CreateCompactSelectorLabel("PlanetRevolutionLabel", "行星公转: 365.3 天");
			orbitVBox.AddChild(_planetRevolutionLabel);
		}

		if (_planetRevolutionSlider == null)
		{
			_planetRevolutionSlider = new HSlider { Name = "PlanetRevolutionSlider", MinValue = 0.2, MaxValue = 500000.0, Step = 0.1, Value = 365.256 };
			orbitVBox.AddChild(_planetRevolutionSlider);
		}

		if (_moonOrbitDistanceLabel == null)
		{
			_moonOrbitDistanceLabel = CreateCompactSelectorLabel("MoonOrbitDistanceLabel", "卫星轨道: 60.3 Rplanet");
			orbitVBox.AddChild(_moonOrbitDistanceLabel);
		}

		if (_moonOrbitDistanceSlider == null)
		{
			_moonOrbitDistanceSlider = new HSlider { Name = "MoonOrbitDistanceSlider", MinValue = 2.0, MaxValue = 500.0, Step = 0.1, Value = 60.3 };
			orbitVBox.AddChild(_moonOrbitDistanceSlider);
		}

		if (_autoMoonRevolutionToggle == null)
		{
			_autoMoonRevolutionToggle = new CheckButton { Name = "AutoMoonRevolutionToggle", Text = "卫星公转周期自动求解", ButtonPressed = true };
			orbitVBox.AddChild(_autoMoonRevolutionToggle);
		}

		if (_moonRotationLabel == null)
		{
			_moonRotationLabel = CreateCompactSelectorLabel("MoonRotationLabel", "卫星自转: 655.7 小时");
			orbitVBox.AddChild(_moonRotationLabel);
		}

		if (_moonRotationSlider == null)
		{
			_moonRotationSlider = new HSlider { Name = "MoonRotationSlider", MinValue = 1.0, MaxValue = 5000.0, Step = 0.1, Value = 655.7 };
			orbitVBox.AddChild(_moonRotationSlider);
		}

		if (_moonRevolutionLabel == null)
		{
			_moonRevolutionLabel = CreateCompactSelectorLabel("MoonRevolutionLabel", "卫星公转: 27.3 天");
			orbitVBox.AddChild(_moonRevolutionLabel);
		}

		if (_moonRevolutionSlider == null)
		{
			_moonRevolutionSlider = new HSlider { Name = "MoonRevolutionSlider", MinValue = 0.1, MaxValue = 100000.0, Step = 0.1, Value = 27.3217 };
			orbitVBox.AddChild(_moonRevolutionSlider);
		}

		if (_extraPlanetsCountLabel == null)
		{
			_extraPlanetsCountLabel = CreateCompactSelectorLabel("ExtraPlanetsCountLabel", "额外行星: 0");
			orbitVBox.AddChild(_extraPlanetsCountLabel);
		}

		if (_extraPlanetsCountSlider == null)
		{
			_extraPlanetsCountSlider = new HSlider { Name = "ExtraPlanetsCountSlider", MinValue = 0.0, MaxValue = 6.0, Step = 1.0, Value = 0.0 };
			orbitVBox.AddChild(_extraPlanetsCountSlider);
		}

		if (_extraPlanetFirstOrbitLabel == null)
		{
			_extraPlanetFirstOrbitLabel = CreateCompactSelectorLabel("ExtraPlanetFirstOrbitLabel", "额外行星起始轨道: 1.60 AU");
			orbitVBox.AddChild(_extraPlanetFirstOrbitLabel);
		}

		if (_extraPlanetFirstOrbitSlider == null)
		{
			_extraPlanetFirstOrbitSlider = new HSlider { Name = "ExtraPlanetFirstOrbitSlider", MinValue = 0.1, MaxValue = 30.0, Step = 0.01, Value = 1.6 };
			orbitVBox.AddChild(_extraPlanetFirstOrbitSlider);
		}

		if (_extraPlanetOrbitStepLabel == null)
		{
			_extraPlanetOrbitStepLabel = CreateCompactSelectorLabel("ExtraPlanetOrbitStepLabel", "额外行星轨道间隔: 0.80 AU");
			orbitVBox.AddChild(_extraPlanetOrbitStepLabel);
		}

		if (_extraPlanetOrbitStepSlider == null)
		{
			_extraPlanetOrbitStepSlider = new HSlider { Name = "ExtraPlanetOrbitStepSlider", MinValue = 0.1, MaxValue = 10.0, Step = 0.01, Value = 0.8 };
			orbitVBox.AddChild(_extraPlanetOrbitStepSlider);
		}

		if (_extraMoonsCountLabel == null)
		{
			_extraMoonsCountLabel = CreateCompactSelectorLabel("ExtraMoonsCountLabel", "额外卫星: 0");
			orbitVBox.AddChild(_extraMoonsCountLabel);
		}

		if (_extraMoonsCountSlider == null)
		{
			_extraMoonsCountSlider = new HSlider { Name = "ExtraMoonsCountSlider", MinValue = 0.0, MaxValue = 6.0, Step = 1.0, Value = 0.0 };
			orbitVBox.AddChild(_extraMoonsCountSlider);
		}

		if (_extraMoonFirstOrbitLabel == null)
		{
			_extraMoonFirstOrbitLabel = CreateCompactSelectorLabel("ExtraMoonFirstOrbitLabel", "额外卫星起始轨道: 86.0 Rplanet");
			orbitVBox.AddChild(_extraMoonFirstOrbitLabel);
		}

		if (_extraMoonFirstOrbitSlider == null)
		{
			_extraMoonFirstOrbitSlider = new HSlider { Name = "ExtraMoonFirstOrbitSlider", MinValue = 2.0, MaxValue = 500.0, Step = 0.1, Value = 86.0 };
			orbitVBox.AddChild(_extraMoonFirstOrbitSlider);
		}

		if (_extraMoonOrbitStepLabel == null)
		{
			_extraMoonOrbitStepLabel = CreateCompactSelectorLabel("ExtraMoonOrbitStepLabel", "额外卫星轨道间隔: 24.0 Rplanet");
			orbitVBox.AddChild(_extraMoonOrbitStepLabel);
		}

		if (_extraMoonOrbitStepSlider == null)
		{
			_extraMoonOrbitStepSlider = new HSlider { Name = "ExtraMoonOrbitStepSlider", MinValue = 1.0, MaxValue = 150.0, Step = 0.1, Value = 24.0 };
			orbitVBox.AddChild(_extraMoonOrbitStepSlider);
		}
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
		EmitCelestialPhysicsChanged();
	}

	private void UpdateCelestialLabels()
	{
		if (_starMassLabel != null && _starMassSlider != null)
		{
			_starMassLabel.Text = $"恒星质量: {(float)_starMassSlider.Value:0.00} M☉";
		}

		if (_planetRadiusLabel != null && _planetRadiusSlider != null)
		{
			_planetRadiusLabel.Text = $"行星半径: {(float)_planetRadiusSlider.Value:0.00} R⊕";
		}

		if (_planetMassLabel != null && _planetMassSlider != null)
		{
			_planetMassLabel.Text = $"行星质量: {(float)_planetMassSlider.Value:0.00} M⊕";
		}

		if (_planetOrbitDistanceLabel != null && _planetOrbitDistanceSlider != null)
		{
			_planetOrbitDistanceLabel.Text = $"行星轨道: {(float)_planetOrbitDistanceSlider.Value:0.000} AU";
		}

		if (_planetRotationLabel != null && _planetRotationSlider != null)
		{
			_planetRotationLabel.Text = $"行星自转: {(float)_planetRotationSlider.Value:0.0} 小时";
		}

		if (_planetRevolutionLabel != null && _planetRevolutionSlider != null)
		{
			_planetRevolutionLabel.Text = $"行星公转: {(float)_planetRevolutionSlider.Value:0.0} 天";
		}

		if (_moonOrbitDistanceLabel != null && _moonOrbitDistanceSlider != null)
		{
			_moonOrbitDistanceLabel.Text = $"卫星轨道: {(float)_moonOrbitDistanceSlider.Value:0.0} Rplanet";
		}

		if (_moonRotationLabel != null && _moonRotationSlider != null)
		{
			_moonRotationLabel.Text = $"卫星自转: {(float)_moonRotationSlider.Value:0.0} 小时";
		}

		if (_moonRevolutionLabel != null && _moonRevolutionSlider != null)
		{
			_moonRevolutionLabel.Text = $"卫星公转: {(float)_moonRevolutionSlider.Value:0.0} 天";
		}

		if (_extraPlanetsCountLabel != null && _extraPlanetsCountSlider != null)
		{
			_extraPlanetsCountLabel.Text = $"额外行星: {Mathf.RoundToInt((float)_extraPlanetsCountSlider.Value)}";
		}

		if (_extraPlanetFirstOrbitLabel != null && _extraPlanetFirstOrbitSlider != null)
		{
			_extraPlanetFirstOrbitLabel.Text = $"额外行星起始轨道: {(float)_extraPlanetFirstOrbitSlider.Value:0.00} AU";
		}

		if (_extraPlanetOrbitStepLabel != null && _extraPlanetOrbitStepSlider != null)
		{
			_extraPlanetOrbitStepLabel.Text = $"额外行星轨道间隔: {(float)_extraPlanetOrbitStepSlider.Value:0.00} AU";
		}

		if (_extraMoonsCountLabel != null && _extraMoonsCountSlider != null)
		{
			_extraMoonsCountLabel.Text = $"额外卫星: {Mathf.RoundToInt((float)_extraMoonsCountSlider.Value)}";
		}

		if (_extraMoonFirstOrbitLabel != null && _extraMoonFirstOrbitSlider != null)
		{
			_extraMoonFirstOrbitLabel.Text = $"额外卫星起始轨道: {(float)_extraMoonFirstOrbitSlider.Value:0.0} Rplanet";
		}

		if (_extraMoonOrbitStepLabel != null && _extraMoonOrbitStepSlider != null)
		{
			_extraMoonOrbitStepLabel.Text = $"额外卫星轨道间隔: {(float)_extraMoonOrbitStepSlider.Value:0.0} Rplanet";
		}
	}

	private void EnsureTextureSelectorNodes()
	{
		var photoVBox = GetNodeOrNull<VBoxContainer>("PlanetPhotoPanel/PlanetPhotoVBox");
		if (photoVBox == null)
		{
			return;
		}

		if (_downloadedOnlyToggle == null)
		{
			_downloadedOnlyToggle = new CheckButton
			{
				Name = "DownloadedOnlyToggle",
				Text = "仅显示已下载资源",
				ButtonPressed = false
			};
			InsertBeforePlanetPhoto(photoVBox, _downloadedOnlyToggle);
		}

		if (_sunTextureSelector == null)
		{
			var label = CreateCompactSelectorLabel("SunTextureLabel", "恒星贴图");
			_sunTextureSelector = new OptionButton
			{
				Name = "SunTextureSelector"
			};
			InsertBeforePlanetPhoto(photoVBox, label);
			InsertBeforePlanetPhoto(photoVBox, _sunTextureSelector);
		}

		if (_planetSurfaceTextureSelector == null)
		{
			var label = CreateCompactSelectorLabel("PlanetSurfaceTextureLabel", "行星贴图");
			_planetSurfaceTextureSelector = new OptionButton
			{
				Name = "PlanetSurfaceTextureSelector"
			};
			InsertBeforePlanetPhoto(photoVBox, label);
			InsertBeforePlanetPhoto(photoVBox, _planetSurfaceTextureSelector);
		}

		if (_moonTextureSelector == null)
		{
			var label = CreateCompactSelectorLabel("MoonTextureLabel", "卫星贴图");
			_moonTextureSelector = new OptionButton
			{
				Name = "MoonTextureSelector"
			};
			InsertBeforePlanetPhoto(photoVBox, label);
			InsertBeforePlanetPhoto(photoVBox, _moonTextureSelector);
		}

		if (_solarBrightnessLabel == null)
		{
			_solarBrightnessLabel = CreateCompactSelectorLabel("SolarBrightnessLabel", "太阳亮度: 100%");
			InsertBeforePlanetPhoto(photoVBox, _solarBrightnessLabel);
		}

		if (_solarBrightnessSlider == null)
		{
			_solarBrightnessSlider = new HSlider
			{
				Name = "SolarBrightnessSlider",
				MinValue = 0.5,
				MaxValue = 2.5,
				Step = 0.01,
				Value = 1.0,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			InsertBeforePlanetPhoto(photoVBox, _solarBrightnessSlider);
		}
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

	private void InsertBeforePlanetPhoto(VBoxContainer photoVBox, Control control)
	{
		if (photoVBox == null || control == null)
		{
			return;
		}

		photoVBox.AddChild(control);

		if (_planetPhotoTexture == null)
		{
			return;
		}

		int photoIndex = FindChildIndex(photoVBox, _planetPhotoTexture);
		int controlIndex = FindChildIndex(photoVBox, control);
		if (photoIndex >= 0 && controlIndex >= 0 && controlIndex > photoIndex)
		{
			photoVBox.MoveChild(control, photoIndex);
		}
	}

	private static int FindChildIndex(Node parent, Node child)
	{
		if (parent == null || child == null)
		{
			return -1;
		}

		for (int i = 0; i < parent.GetChildCount(); i++)
		{
			if (parent.GetChild(i) == child)
			{
				return i;
			}
		}

		return -1;
	}

	private static Label CreateCompactSelectorLabel(string name, string text)
	{
		var label = new Label
		{
			Name = name,
			Text = text
		};

		label.AddThemeFontSizeOverride("font_size", 12);
		label.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.82f, 0.95f));
		return label;
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
			AtmosphereDensity = planetData.AtmosphereDensity
		};
	}
}
