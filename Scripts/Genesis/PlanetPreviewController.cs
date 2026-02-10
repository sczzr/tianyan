using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Genesis;

public class PlanetPreviewController
{
	private readonly Control _previewRoot;
	private readonly ColorRect _auraLayer;
	private readonly PlanetTextureView _planetTextureView;

	private SubViewport _subViewport;
	private TextureRect _viewportTextureRect;

	private Node3D _worldRoot;
	private Node3D _earthGroup;
	private Node3D _sunGroup;
	private Node3D _starFieldPivot;
	private Camera3D _camera;
	private DirectionalLight3D _sunDirectionalLight;
	private OmniLight3D _sunPointLight;
	private OmniLight3D _nightFillLight;
	private MeshInstance3D _planetMesh;
	private MeshInstance3D _cloudLayer;
	private MeshInstance3D _moonMesh;
	private MeshInstance3D _sunCore;
	private MeshInstance3D _sunGlow;
	private Node3D _moonOrbit;
	private Node3D _planetOrbit;
	private MeshInstance3D _starShell;
	private Node3D _starPointCloud;
	private readonly List<Node3D> _extraPlanetPivots = new();
	private readonly List<MeshInstance3D> _extraPlanetMeshes = new();
	private readonly List<float> _extraPlanetOrbitSpeeds = new();
	private readonly List<float> _extraPlanetSpinSpeeds = new();
	private readonly List<Node3D> _extraPlanetOrbitLines = new();
	private readonly List<Node3D> _extraMoonPivots = new();
	private readonly List<MeshInstance3D> _extraMoonMeshes = new();
	private readonly List<float> _extraMoonOrbitSpeeds = new();
	private readonly List<float> _extraMoonSpinSpeeds = new();
	private readonly List<float> _extraMoonOrbitDistances = new();
	private readonly List<Node3D> _extraMoonOrbitLines = new();
	private Control _armillaryRing1;
	private Control _armillaryRing2;
	private WorldEnvironment _worldEnvironment;

	private string _selectedSkyTexturePath;
	private static readonly string[] NasaSkyTextureCandidates =
	{
		"res://Assets/Textures/Stars/NASA/starmap_2020_4k_gal.exr",
		"res://Assets/Textures/Stars/NASA/starmap_2020_4k.exr",
		"res://Assets/Textures/Stars/NASA/milkyway_2020_4k_gal_print.jpg",
		"res://Assets/Textures/Stars/NASA/starmap_2020_4k_gal_print.jpg",
		"res://Assets/Textures/Stars/NASA/starmap_2020_4k_print.jpg"
	};

	private StandardMaterial3D _planetMaterial;
	private StandardMaterial3D _cloudMaterial;
	private StandardMaterial3D _shellMaterial;
	private StandardMaterial3D _moonMaterial;
	private StandardMaterial3D _sunCoreMaterial;
	private ShaderMaterial _sunCoreShaderMaterial;
	private ShaderMaterial _sunCoronaMaterial;
	private Texture2D _activeSkyTexture;
	private Texture2D _sunDetailTexture;
	private string _selectedPlanetSurfaceTexturePath = string.Empty;
	private string _selectedMoonTexturePath = string.Empty;
	private string _selectedSunTexturePath = string.Empty;

	private readonly Vector3 _earthOrigin = Vector3.Zero;
	private Vector3 _sunPosition = new(-420f, 0f, -860f);

	private bool _isOrbitDragging;
	private bool _isPanDragging;
	private Vector2 _lastPointer;

	private float _targetYaw = 0.95f;
	private float _targetPitch = 0.28f;
	private float _targetDistance = 120f;
	private float _currentYaw = 0.95f;
	private float _currentPitch = 0.28f;
	private float _currentDistance = 120f;
	private Vector3 _targetFocus = Vector3.Zero;
	private Vector3 _currentFocus = Vector3.Zero;

	private const float MinDistance = 14f;
	private const float MaxDistance = 2600f;
	private const float OrbitSensitivity = 0.006f;
	private const float PanSensitivity = 0.0035f;
	private const float ZoomInFactor = 0.9f;
	private const float ZoomOutFactor = 1.12f;
	private const float PreviewSupersampleScale = 1.5f;
	private const float MoonOrbitRadius = 40f;
	private const float MoonOrbitAngularSpeed = 0.22f;
	private const float ControlEditPauseSeconds = 0.9f;
	private const float MoonOrbitVisibleMaxDistance = 520f;
	private const float PlanetOrbitVisibleMinDistance = 150f;
	private const float MaxFocusRadius = 2400f;
	private const float OrbitCloseInspectionAltitudeFactor = 0.45f;

	private float _moonOrbitAngle = 0f;
	private int _currentLawAlignment = 50;
	private PlanetData _cachedPlanetData = new PlanetData();
	private PlanetGenerationProfile _generationProfile = PlanetGenerationProfile.Default;
	private Texture2D _latestPlanetSnapshot;
	private bool _showMoonOrbit = true;
	private bool _showPlanetOrbit = true;
	private bool _isThreeDActive;
	private bool _isBuildingThreeD;
	private bool _isControlEditPaused;
	private float _controlEditPauseLeft;
	private float _buildProgress01;
	private int _buildStage;
	private bool _resizeBound;
	private bool _planetPhotoRealtimeUpdate = true;
	private bool _lightFollowWithPlanet = true;
	private float _lightResponseStrength = 0.75f;
	private float _solarBrightness = 1.0f;
	private float _currentPlanetRadiusUnits = 10f;
	private readonly TranslationManager _translationManager;
	private CelestialSystemPhysicsConfig _celestialSystemConfig = CelestialSystemPhysicsConfig.CreateDefault();
	private CelestialPhysicsSolver.ResolvedSystem _resolvedCelestialSystem;
	private float _primaryPlanetSpinSpeed = 0.16f;
	private float _primaryPlanetRadiusFactor = 1.0f;
	private float _primaryMoonOrbitDistancePlanetRadii = 4.0f;
	private float _primaryMoonOrbitSpeed = MoonOrbitAngularSpeed;
	private float _primaryMoonSpinSpeed = 0.35f;

	private const float PlanetRadiusUnits = 10f;
	private const float KilometersPerSceneUnit = 125000f;
	private const float AstronomicalUnitKm = 149597870.7f;
	private const float OrbitTimeAcceleration = 86400f;
	private const float SpinTimeAcceleration = 2200f;
	private const float CivilizationOceanCoverage = 0.56f;
	private const float CivilizationTemperature = 0.52f;
	private const float CivilizationAtmosphereDensity = 0.62f;
	private static readonly bool ForceEcologicalProceduralSurface = true;

	private static readonly Color DrawJsOceanDeep = new Color(0.12157f, 0.24706f, 0.55294f, 1f);           // #1f3f8d
	private static readonly Color DrawJsOceanMid = new Color(0.16863f, 0.33725f, 0.66275f, 1f);            // #2b56a9
	private static readonly Color DrawJsOceanShallow = new Color(0.18431f, 0.41961f, 0.72157f, 1f);        // #2f6bb8
	private static readonly Color DrawJsCoastland = new Color(0.78039f, 0.72549f, 0.56078f, 1f);           // #c7b98f
	private static readonly Color DrawJsTropicalRainForest = new Color(0.08627f, 0.43137f, 0.12157f, 1f); // #166e1f
	private static readonly Color DrawJsTropicalSeasonalForest = new Color(0.2f, 0.47059f, 0.18824f, 1f); // #337830
	private static readonly Color DrawJsShrubland = new Color(0.43529f, 0.45490f, 0.25882f, 1f);           // #6f7442
	private static readonly Color DrawJsSavannah = new Color(0.60392f, 0.58039f, 0.34510f, 1f);            // #9a9458
	private static readonly Color DrawJsTropicalDesert = new Color(0.76471f, 0.69020f, 0.52157f, 1f);      // #c3b085
	private static readonly Color DrawJsTemperateRainForest = new Color(0.15686f, 0.45490f, 0.18431f, 1f); // #28742f
	private static readonly Color DrawJsTemperateSeasonalForest = new Color(0.27451f, 0.47059f, 0.25098f, 1f); // #467840
	private static readonly Color DrawJsChaparral = new Color(0.54118f, 0.49804f, 0.36863f, 1f);           // #8a7f5e
	private static readonly Color DrawJsGrassland = new Color(0.50980f, 0.60784f, 0.30980f, 1f);           // #829b4f
	private static readonly Color DrawJsSteppe = new Color(0.61176f, 0.57255f, 0.44706f, 1f);              // #9c9272
	private static readonly Color DrawJsTemperateDesert = new Color(0.73725f, 0.64706f, 0.48235f, 1f);     // #bca57b
	private static readonly Color DrawJsBorealForest = new Color(0.20392f, 0.38824f, 0.25882f, 1f);        // #346343
	private static readonly Color DrawJsTaiga = new Color(0.37255f, 0.43529f, 0.38431f, 1f);               // #5f6f62
	private static readonly Color DrawJsTundra = new Color(0.55294f, 0.58039f, 0.53333f, 1f);              // #8d9488
	private static readonly Color DrawJsIce = new Color(0.93333f, 0.94510f, 0.96078f, 1f);                 // #eef1f5
	private static readonly Color DrawJsRockyMountain = new Color(0.56078f, 0.48627f, 0.38824f, 1f);       // #8f7c63
	private static readonly Color DrawJsSnowyMountain = new Color(0.95686f, 0.96471f, 0.98039f, 1f);       // #f4f6fa
	private static readonly Color DrawJsRiver = new Color(0.16863f, 0.4f, 0.74510f, 1f);                   // #2b66be

	public string CameraDistanceText { get; private set; } = "20,000 km";
	public string CameraDescriptionText { get; private set; } = "高轨观测点";
	public bool IsThreeDActive => _isThreeDActive;
	public bool IsBuildingThreeD => _isBuildingThreeD;
	public float BuildProgress01 => _buildProgress01;

	public PlanetPreviewController(Control previewRoot, ColorRect auraLayer, PlanetTextureView planetTextureView)
	{
		_previewRoot = previewRoot;
		_auraLayer = auraLayer;
		_planetTextureView = planetTextureView;
		_translationManager = TranslationManager.Instance;
		_planetTextureView?.EnterStaticMode();
		_resolvedCelestialSystem = CelestialPhysicsSolver.Resolve(_celestialSystemConfig);
		ApplyResolvedCelestialConfig();
		UpdateCameraHud();
	}

	public void SetCelestialSystemConfig(CelestialSystemPhysicsConfig config)
	{
		_celestialSystemConfig = config?.DuplicateConfig() ?? CelestialSystemPhysicsConfig.CreateDefault();
		_resolvedCelestialSystem = CelestialPhysicsSolver.Resolve(_celestialSystemConfig);
		ApplyResolvedCelestialConfig();
	}

	public void ConfigurePrimaryPlanet(
		float radiusEarth,
		float massEarth,
		float orbitDistanceAu,
		float rotationPeriodHours,
		float revolutionPeriodDays,
		bool autoResolveRevolution = true)
	{
		if (_celestialSystemConfig == null)
		{
			_celestialSystemConfig = CelestialSystemPhysicsConfig.CreateDefault();
		}

		if (_celestialSystemConfig.PrimaryPlanet == null)
		{
			_celestialSystemConfig.PrimaryPlanet = new CelestialPlanetPhysicsConfig();
		}

		_celestialSystemConfig.PrimaryPlanet.RadiusEarth = radiusEarth;
		_celestialSystemConfig.PrimaryPlanet.MassEarth = massEarth;
		_celestialSystemConfig.PrimaryPlanet.OrbitDistanceAu = orbitDistanceAu;
		_celestialSystemConfig.PrimaryPlanet.RotationPeriodHours = rotationPeriodHours;
		_celestialSystemConfig.PrimaryPlanet.RevolutionPeriodDays = revolutionPeriodDays;
		_celestialSystemConfig.PrimaryPlanet.AutoResolveRevolutionPeriod = autoResolveRevolution;

		SetCelestialSystemConfig(_celestialSystemConfig);
	}

	public void ConfigureAdditionalPlanets(Godot.Collections.Array<CelestialPlanetPhysicsConfig> planets)
	{
		if (_celestialSystemConfig == null)
		{
			_celestialSystemConfig = CelestialSystemPhysicsConfig.CreateDefault();
		}

		_celestialSystemConfig.AdditionalPlanets = planets ?? new Godot.Collections.Array<CelestialPlanetPhysicsConfig>();
		SetCelestialSystemConfig(_celestialSystemConfig);
	}

	public void ConfigureSatellites(Godot.Collections.Array<CelestialSatellitePhysicsConfig> satellites)
	{
		if (_celestialSystemConfig == null)
		{
			_celestialSystemConfig = CelestialSystemPhysicsConfig.CreateDefault();
		}

		_celestialSystemConfig.Satellites = satellites ?? new Godot.Collections.Array<CelestialSatellitePhysicsConfig>();
		SetCelestialSystemConfig(_celestialSystemConfig);
	}

	public void RequestActivateThreeD()
	{
		if (_isThreeDActive || _isBuildingThreeD || _previewRoot == null)
		{
			return;
		}

		_isControlEditPaused = false;
		_controlEditPauseLeft = 0f;
		_planetTextureView?.SetEditingPaused(false);
		_isBuildingThreeD = true;
		_buildProgress01 = 0f;
		_buildStage = 0;
	}

	public void NotifyControlEditing()
	{
		if (!_isThreeDActive || _isBuildingThreeD || _camera == null)
		{
			return;
		}

		_isOrbitDragging = false;
		_isPanDragging = false;
		_isControlEditPaused = true;
		_controlEditPauseLeft = ControlEditPauseSeconds;
		_targetFocus = _earthOrigin;
		_targetDistance = Mathf.Clamp(Mathf.Min(_targetDistance, 90f), MinDistance, MaxDistance);
		_targetPitch = Mathf.Clamp(_targetPitch, -0.22f, 0.45f);
		_planetTextureView?.SetEditingPaused(true);
	}

	public bool SetSkyTexturePath(string texturePath)
	{
		if (string.IsNullOrWhiteSpace(texturePath))
		{
			_selectedSkyTexturePath = string.Empty;
			_activeSkyTexture = null;

			if (_worldEnvironment?.Environment != null)
			{
				_worldEnvironment.Environment.BackgroundMode = Environment.BGMode.Color;
				_worldEnvironment.Environment.Sky = null;
				_worldEnvironment.Environment.BackgroundColor = new Color(0.008f, 0.012f, 0.026f);
			}

			if (_starShell != null)
			{
				_starShell.Visible = true;
			}

			UpdateStarfieldByElement(_cachedPlanetData?.Element ?? PlanetElement.Terra);
			return true;
		}

		Texture2D skyTexture = GD.Load<Texture2D>(texturePath);
		if (skyTexture == null)
		{
			return false;
		}

		_selectedSkyTexturePath = texturePath;
		_activeSkyTexture = skyTexture;
		ApplySkyTexture(skyTexture);
		return true;
	}

	public bool SetPlanetSurfaceTexturePath(string texturePath)
	{
		_selectedPlanetSurfaceTexturePath = texturePath ?? string.Empty;

		if (ForceEcologicalProceduralSurface)
		{
			_selectedPlanetSurfaceTexturePath = string.Empty;
			if (_isThreeDActive && _planetMaterial != null)
			{
				Texture2D surfaceTexture = UpdatePlanetTextures(_cachedPlanetData, 1024, 512);
				if (surfaceTexture != null)
				{
					_latestPlanetSnapshot = surfaceTexture;
					if (_planetPhotoRealtimeUpdate)
					{
						_planetTextureView?.SetPlanetSnapshot(surfaceTexture);
					}
				}
			}

			return true;
		}

		if (string.IsNullOrWhiteSpace(_selectedPlanetSurfaceTexturePath))
		{
			if (_isThreeDActive && _planetMaterial != null)
			{
				UpdatePlanetTextures(_cachedPlanetData, 1024, 512);
			}
			return true;
		}

		Texture2D texture = GD.Load<Texture2D>(_selectedPlanetSurfaceTexturePath);
		if (texture == null)
		{
			return false;
		}

		if (_planetMaterial != null)
		{
			_planetMaterial.AlbedoTexture = texture;
		}

		_latestPlanetSnapshot = texture;
		if (_planetPhotoRealtimeUpdate)
		{
			_planetTextureView?.SetPlanetSnapshot(texture);
		}

		return true;
	}

	public bool SetMoonTexturePath(string texturePath)
	{
		_selectedMoonTexturePath = texturePath ?? string.Empty;

		if (_moonMaterial == null)
		{
			return true;
		}

		if (string.IsNullOrWhiteSpace(_selectedMoonTexturePath))
		{
			_moonMaterial.AlbedoTexture = null;
			_moonMaterial.AlbedoColor = new Color(0.72f, 0.72f, 0.74f);
			return true;
		}

		Texture2D texture = GD.Load<Texture2D>(_selectedMoonTexturePath);
		if (texture == null)
		{
			return false;
		}

		_moonMaterial.AlbedoTexture = texture;
		_moonMaterial.AlbedoColor = Colors.White;
		return true;
	}

	public bool SetSunTexturePath(string texturePath)
	{
		_selectedSunTexturePath = texturePath ?? string.Empty;

		if (_sunCoreMaterial == null)
		{
			return true;
		}

		if (string.IsNullOrWhiteSpace(_selectedSunTexturePath))
		{
			_sunDetailTexture = null;
			ApplySunCoreVisualStyle(false);
			return true;
		}

		Texture2D texture = GD.Load<Texture2D>(_selectedSunTexturePath);
		if (texture == null)
		{
			return false;
		}

		_sunDetailTexture = texture;
		ApplySunCoreVisualStyle(true);
		return true;
	}

	private void ApplySunCoreVisualStyle(bool usingTexture)
	{
		if (_sunCoreMaterial == null)
		{
			return;
		}

		EnsureSunSurfaceShader();

		if (_sunCoreShaderMaterial != null)
		{
			_sunCoreShaderMaterial.SetShaderParameter("use_detail_tex", usingTexture && _sunDetailTexture != null);
			if (usingTexture && _sunDetailTexture != null)
			{
				_sunCoreShaderMaterial.SetShaderParameter("detail_tex", _sunDetailTexture);
			}

			if (usingTexture)
			{
				_sunCoreShaderMaterial.SetShaderParameter("emission_strength", 26.0f * _solarBrightness);
				_sunCoreShaderMaterial.SetShaderParameter("detail_mix", 0.14f);
			}
			else
			{
				_sunCoreShaderMaterial.SetShaderParameter("emission_strength", 22.0f * _solarBrightness);
				_sunCoreShaderMaterial.SetShaderParameter("detail_mix", 0.0f);
			}
		}

		ApplySolarBrightness();
	}

	private void EnsureSunSurfaceShader()
	{
		if (_sunCore == null)
		{
			return;
		}

		if (_sunCoreShaderMaterial != null)
		{
			if (_sunCore.MaterialOverride != _sunCoreShaderMaterial)
			{
				_sunCore.MaterialOverride = _sunCoreShaderMaterial;
			}
			return;
		}

		Shader shader = GD.Load<Shader>("res://Shaders/SunSurface.gdshader");
		if (shader == null)
		{
			return;
		}

		_sunCoreShaderMaterial = new ShaderMaterial
		{
			Shader = shader
		};

		_sunCoreShaderMaterial.SetShaderParameter("base_color", new Color(1.0f, 0.78f, 0.34f, 1.0f));
		_sunCoreShaderMaterial.SetShaderParameter("hot_color", new Color(1.0f, 0.98f, 0.74f, 1.0f));
		_sunCoreShaderMaterial.SetShaderParameter("emission_strength", 22.0f * _solarBrightness);
		_sunCoreShaderMaterial.SetShaderParameter("flow_speed", 0.52f);
		_sunCoreShaderMaterial.SetShaderParameter("granulation_scale", 14.5f);
		_sunCoreShaderMaterial.SetShaderParameter("pulse_strength", 0.10f);
		_sunCoreShaderMaterial.SetShaderParameter("detail_mix", 0.0f);
		_sunCoreShaderMaterial.SetShaderParameter("brightness_multiplier", _solarBrightness);
		_sunCore.MaterialOverride = _sunCoreShaderMaterial;
	}

	public void HandleGuiInput(InputEvent inputEvent)
	{
		if (!_isThreeDActive || _camera == null)
		{
			return;
		}

		if (inputEvent is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				_isOrbitDragging = mouseButton.Pressed;
				_lastPointer = mouseButton.Position;
				if (!mouseButton.Pressed)
				{
					_isOrbitDragging = false;
				}
			}
			else if (mouseButton.ButtonIndex == MouseButton.Right)
			{
				_isPanDragging = mouseButton.Pressed;
				_lastPointer = mouseButton.Position;
				if (!mouseButton.Pressed)
				{
					_isPanDragging = false;
				}
			}
			else if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				_targetDistance = Mathf.Clamp(_targetDistance * ZoomInFactor, MinDistance, MaxDistance);
			}
			else if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				_targetDistance = Mathf.Clamp(_targetDistance * ZoomOutFactor, MinDistance, MaxDistance);
			}

			return;
		}

		if (inputEvent is not InputEventMouseMotion mouseMotion)
		{
			return;
		}

		if (_isOrbitDragging)
		{
			_targetYaw -= mouseMotion.Relative.X * OrbitSensitivity;
			_targetPitch = Mathf.Clamp(
				_targetPitch - mouseMotion.Relative.Y * OrbitSensitivity,
				-Mathf.Pi * 0.47f,
				Mathf.Pi * 0.47f);
		}
		else if (_isPanDragging)
		{
			Vector3 forward = (_currentFocus - _camera.Position).Normalized();
			Vector3 right = forward.Cross(Vector3.Up).Normalized();
			Vector3 up = right.Cross(forward).Normalized();
			float scale = Mathf.Max(2.8f, _currentDistance * PanSensitivity);
			_targetFocus += (-right * mouseMotion.Relative.X + up * mouseMotion.Relative.Y) * scale;
			if (_targetFocus.Length() > MaxFocusRadius)
			{
				_targetFocus = _targetFocus.Normalized() * MaxFocusRadius;
			}
		}
	}

	public void UpdateUniverseMood(int lawAlignment)
	{
		_currentLawAlignment = Mathf.Clamp(lawAlignment, 0, 100);
		_planetTextureView?.SetLawAlignment(_currentLawAlignment);

		if (!_isThreeDActive)
		{
			return;
		}

		float techFactor = _currentLawAlignment / 100f;
		if (_shellMaterial != null)
		{
			_shellMaterial.Emission = new Color(0.08f, 0.15f, 0.28f).Lerp(new Color(0.05f, 0.28f, 0.34f), techFactor * 0.55f);
		}
	}

	public void UpdateShader(PlanetData planetData)
	{
		if (planetData == null)
		{
			return;
		}

		_cachedPlanetData = ClonePlanetData(planetData);
		ResolveEffectiveClimate(_cachedPlanetData, out float normalizedOcean, out float normalizedTemperature, out float normalizedAtmosphere);
		_cachedPlanetData.OceanCoverage = normalizedOcean;
		_cachedPlanetData.Temperature = normalizedTemperature;
		_cachedPlanetData.AtmosphereDensity = normalizedAtmosphere;
		_planetTextureView?.UpdatePreview(_cachedPlanetData, _currentLawAlignment);

		if (_planetPhotoRealtimeUpdate)
		{
			UpdatePlanetSnapshot(_cachedPlanetData, 512, 256);
		}

		if (_auraLayer != null)
		{
			ResolveEffectiveClimate(_cachedPlanetData, out float _, out float climateTemperature, out float climateAtmosphere);
			Color auraColor = ResolveAuraColor(_cachedPlanetData.Element, climateTemperature);
			float alpha = Mathf.Lerp(0.1f, 0.42f, climateAtmosphere);
			_auraLayer.Color = new Color(auraColor.R, auraColor.G, auraColor.B, alpha);
		}

		if (_lightFollowWithPlanet)
		{
			ApplyLightingFromPlanet(_cachedPlanetData);
		}

		if (!_isThreeDActive || _planetMaterial == null)
		{
			return;
		}

		ApplyPlanetToThreeD(_cachedPlanetData);
	}

	public void SetPlanetPhotoRealtime(bool enabled)
	{
		_planetPhotoRealtimeUpdate = enabled;
		if (_planetPhotoRealtimeUpdate)
		{
			if (_latestPlanetSnapshot != null)
			{
				_planetTextureView?.SetPlanetSnapshot(_latestPlanetSnapshot);
			}
			else
			{
				UpdatePlanetSnapshot(_cachedPlanetData, 512, 256);
			}
		}
	}

	public void SetLightFollowEnabled(bool enabled)
	{
		_lightFollowWithPlanet = enabled;
		if (_lightFollowWithPlanet)
		{
			ApplyLightingFromPlanet(_cachedPlanetData);
		}
		else
		{
			ApplySolarBrightness();
		}
	}

	public void SetLightResponse(float strength)
	{
		_lightResponseStrength = Mathf.Clamp(strength, 0f, 1f);
		if (_lightFollowWithPlanet)
		{
			ApplyLightingFromPlanet(_cachedPlanetData);
		}
	}

	public void SetSolarBrightness(float brightness)
	{
		_solarBrightness = Mathf.Clamp(brightness, 0.5f, 2.5f);
		ApplySolarBrightness();
	}

	private void ApplySolarBrightness()
	{
		bool usingTexture = !string.IsNullOrWhiteSpace(_selectedSunTexturePath) && _sunDetailTexture != null;

		if (_sunCoreShaderMaterial != null)
		{
			float baseEmission = usingTexture ? 26.0f : 22.0f;
			_sunCoreShaderMaterial.SetShaderParameter("emission_strength", baseEmission * _solarBrightness);
			_sunCoreShaderMaterial.SetShaderParameter("brightness_multiplier", _solarBrightness);
		}

		if (_sunCoronaMaterial != null)
		{
			_sunCoronaMaterial.SetShaderParameter("intensity", 6.6f * _solarBrightness);
		}

		if (_sunCoreMaterial != null)
		{
			float colorBoost = Mathf.Clamp(_solarBrightness, 0.5f, 2.5f);
			_sunCoreMaterial.AlbedoColor = new Color(1.12f, 0.9f, 0.68f) * colorBoost;
			_sunCoreMaterial.Emission = new Color(1.2f, 0.68f, 0.24f) * colorBoost;
			_sunCoreMaterial.EmissionEnergyMultiplier = 5.8f * _solarBrightness;
		}

		if (_lightFollowWithPlanet)
		{
			ApplyLightingFromPlanet(_cachedPlanetData);
			return;
		}

		if (_sunPointLight != null)
		{
			_sunPointLight.LightEnergy = 30.0f * _solarBrightness;
		}

		if (_sunDirectionalLight != null)
		{
			_sunDirectionalLight.LightEnergy = 0.36f * _solarBrightness;
		}
	}

	public void RefreshPlanetSnapshot()
	{
		UpdatePlanetSnapshot(_cachedPlanetData, 1024, 512);
	}

	public void SetGenerationProfile(PlanetGenerationProfile profile)
	{
		_generationProfile = profile;
	}

	public float[] GeneratePlanetTerrainHeightmap(PlanetData planetData, int width, int height)
	{
		var sourcePlanet = planetData ?? _cachedPlanetData;
		if (sourcePlanet == null)
		{
			return null;
		}

		int targetWidth = Mathf.Max(64, width);
		int targetHeight = Mathf.Max(32, height);
		int seed = BuildTextureSeed(sourcePlanet, 137);
		return PlanetTerrainGenerator.GenerateHeightmap(sourcePlanet, _generationProfile, targetWidth, targetHeight, seed);
	}

	public void SetOrbitVisibility(bool showMoonOrbit, bool showPlanetOrbit)
	{
		_showMoonOrbit = showMoonOrbit;
		_showPlanetOrbit = showPlanetOrbit;
		UpdateOrbitVisibilityByCameraDistance();
	}

	public void Tick(double delta)
	{
		if (_isBuildingThreeD)
		{
			TickBuildPipeline(delta);
			return;
		}

		if (!_isThreeDActive)
		{
			return;
		}

		TryBindViewportTexture();

		if (_camera == null || _earthGroup == null)
		{
			return;
		}

		float deltaF = (float)delta;
		float elapsed = (float)Time.GetTicksMsec() / 1000f;

		if (_isControlEditPaused)
		{
			_controlEditPauseLeft -= deltaF;
			if (_controlEditPauseLeft <= 0f)
			{
				_isControlEditPaused = false;
				_planetTextureView?.SetEditingPaused(false);
			}
		}

		float smoothOrbit = 1f - Mathf.Exp(-deltaF * 8f);
		float smoothFocus = 1f - Mathf.Exp(-deltaF * 10f);

		_currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, smoothOrbit);
		_currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, smoothOrbit);
		_currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, smoothOrbit);
		_currentFocus = _currentFocus.Lerp(_targetFocus, smoothFocus);

		Vector3 orbitOffset = new(
			_currentDistance * Mathf.Cos(_currentPitch) * Mathf.Sin(_currentYaw),
			_currentDistance * Mathf.Sin(_currentPitch),
			_currentDistance * Mathf.Cos(_currentPitch) * Mathf.Cos(_currentYaw));

		_camera.Position = _currentFocus + orbitOffset;
		_camera.LookAt(_currentFocus, Vector3.Up);
		UpdateSkyBackdropTransform();

		if (!_isControlEditPaused)
		{
			if (_planetMesh != null)
			{
				var planetRot = _planetMesh.Rotation;
				planetRot.Y += deltaF * _primaryPlanetSpinSpeed;
				_planetMesh.Rotation = planetRot;
			}

			if (_cloudLayer != null)
			{
				var cloudRot = _cloudLayer.Rotation;
				cloudRot.Y += deltaF * (_primaryPlanetSpinSpeed + 0.06f);
				cloudRot.X = Mathf.Sin(elapsed * 0.25f) * 0.03f;
				_cloudLayer.Rotation = cloudRot;
			}

			float moonOrbitRadiusUnits = Mathf.Max(1.2f, _primaryMoonOrbitDistancePlanetRadii * _currentPlanetRadiusUnits);
			if (_moonOrbit != null)
			{
				float baseOrbitPlanetRadii = MoonOrbitRadius / Mathf.Max(0.001f, PlanetRadiusUnits);
				float moonOrbitScale = (_currentPlanetRadiusUnits / Mathf.Max(0.001f, PlanetRadiusUnits))
					* (_primaryMoonOrbitDistancePlanetRadii / Mathf.Max(0.001f, baseOrbitPlanetRadii));
				_moonOrbit.Scale = new Vector3(moonOrbitScale, moonOrbitScale, moonOrbitScale);
			}

			if (_moonMesh != null)
			{
				_moonOrbitAngle = Mathf.PosMod(_moonOrbitAngle + deltaF * _primaryMoonOrbitSpeed, Mathf.Tau);
				_moonMesh.Position = new Vector3(
					Mathf.Cos(_moonOrbitAngle) * moonOrbitRadiusUnits,
					0f,
					Mathf.Sin(_moonOrbitAngle) * moonOrbitRadiusUnits);

				var selfRot = _moonMesh.Rotation;
				selfRot.Y += deltaF * _primaryMoonSpinSpeed;
				_moonMesh.Rotation = selfRot;
			}

			for (int i = 0; i < _extraPlanetPivots.Count; i++)
			{
				Node3D pivot = _extraPlanetPivots[i];
				MeshInstance3D mesh = i < _extraPlanetMeshes.Count ? _extraPlanetMeshes[i] : null;
				float orbitSpeed = i < _extraPlanetOrbitSpeeds.Count ? _extraPlanetOrbitSpeeds[i] : 0f;
				float spinSpeed = i < _extraPlanetSpinSpeeds.Count ? _extraPlanetSpinSpeeds[i] : 0f;

				if (pivot != null)
				{
					var rotation = pivot.Rotation;
					rotation.Y += deltaF * orbitSpeed;
					pivot.Rotation = rotation;
				}

				if (mesh != null)
				{
					var rotation = mesh.Rotation;
					rotation.Y += deltaF * spinSpeed;
					mesh.Rotation = rotation;
				}
			}

			for (int i = 0; i < _extraMoonPivots.Count; i++)
			{
				Node3D pivot = _extraMoonPivots[i];
				MeshInstance3D mesh = i < _extraMoonMeshes.Count ? _extraMoonMeshes[i] : null;
				float orbitSpeed = i < _extraMoonOrbitSpeeds.Count ? _extraMoonOrbitSpeeds[i] : 0f;
				float spinSpeed = i < _extraMoonSpinSpeeds.Count ? _extraMoonSpinSpeeds[i] : 0f;
				float orbitDistanceRadii = i < _extraMoonOrbitDistances.Count ? _extraMoonOrbitDistances[i] : 6f;

				if (pivot != null)
				{
					var rotation = pivot.Rotation;
					rotation.Y += deltaF * orbitSpeed;
					pivot.Rotation = rotation;
				}

				if (mesh != null)
				{
					mesh.Position = new Vector3(Mathf.Max(1.0f, orbitDistanceRadii * _currentPlanetRadiusUnits), 0f, 0f);
					var rotation = mesh.Rotation;
					rotation.Y += deltaF * spinSpeed;
					mesh.Rotation = rotation;
				}

				if (i < _extraMoonOrbitLines.Count && _extraMoonOrbitLines[i] != null)
				{
					float moonOrbitScale = _currentPlanetRadiusUnits / Mathf.Max(0.001f, PlanetRadiusUnits);
					_extraMoonOrbitLines[i].Scale = new Vector3(moonOrbitScale, moonOrbitScale, moonOrbitScale);
				}
			}

			if (_starFieldPivot != null)
			{
				var starRot = _starFieldPivot.Rotation;
				starRot.Y += deltaF * 0.006f;
				_starFieldPivot.Rotation = starRot;
			}

			if (_armillaryRing1 != null)
			{
				_armillaryRing1.Rotation += deltaF * 0.06f;
			}

			if (_armillaryRing2 != null)
			{
				_armillaryRing2.Rotation -= deltaF * 0.09f;
			}
		}

		UpdateSunCoronaByCameraDistance();
		UpdateOrbitVisibilityByCameraDistance();

		UpdateCameraHud();
	}

	private void UpdateSkyBackdropTransform()
	{
		if (_camera == null)
		{
			return;
		}

		if (_starShell != null)
		{
			_starShell.GlobalPosition = _camera.GlobalPosition;
		}

		if (_starFieldPivot != null)
		{
			_starFieldPivot.GlobalPosition = _camera.GlobalPosition;
		}
	}

	private void UpdateOrbitVisibilityByCameraDistance()
	{
		if (_camera == null)
		{
			if (_moonOrbit != null)
			{
				_moonOrbit.Visible = _showMoonOrbit;
			}

			for (int i = 0; i < _extraMoonOrbitLines.Count; i++)
			{
				if (_extraMoonOrbitLines[i] != null)
				{
					_extraMoonOrbitLines[i].Visible = _showMoonOrbit;
				}
			}

			if (_planetOrbit != null)
			{
				_planetOrbit.Visible = _showPlanetOrbit;
			}

			for (int i = 0; i < _extraPlanetOrbitLines.Count; i++)
			{
				if (_extraPlanetOrbitLines[i] != null)
				{
					_extraPlanetOrbitLines[i].Visible = _showPlanetOrbit;
				}
			}
			return;
		}

		float focusDistance = _camera.GlobalPosition.DistanceTo(_earthOrigin);
		float altitudeFromSurface = focusDistance - _currentPlanetRadiusUnits;
		bool closeInspection = altitudeFromSurface <= Mathf.Max(14f, _currentPlanetRadiusUnits * OrbitCloseInspectionAltitudeFactor);
		float orbitDistanceScale = Mathf.Max(1f, _currentPlanetRadiusUnits / PlanetRadiusUnits);
		float moonOrbitVisibleMaxDistance = MoonOrbitVisibleMaxDistance * orbitDistanceScale;
		float planetOrbitVisibleMinDistance = PlanetOrbitVisibleMinDistance * orbitDistanceScale;

		if (_moonOrbit != null)
		{
			_moonOrbit.Visible = _showMoonOrbit && !closeInspection && focusDistance <= moonOrbitVisibleMaxDistance;
		}

		for (int i = 0; i < _extraMoonOrbitLines.Count; i++)
		{
			if (_extraMoonOrbitLines[i] != null)
			{
				_extraMoonOrbitLines[i].Visible = _showMoonOrbit && !closeInspection && focusDistance <= moonOrbitVisibleMaxDistance;
			}
		}

		if (_planetOrbit != null)
		{
			_planetOrbit.Visible = _showPlanetOrbit && !closeInspection && focusDistance >= planetOrbitVisibleMinDistance;
		}

		for (int i = 0; i < _extraPlanetOrbitLines.Count; i++)
		{
			if (_extraPlanetOrbitLines[i] != null)
			{
				_extraPlanetOrbitLines[i].Visible = _showPlanetOrbit && !closeInspection && focusDistance >= planetOrbitVisibleMinDistance;
			}
		}
	}

	private void EnsureViewportInfrastructure()
	{
		if (_previewRoot == null || _subViewport != null)
		{
			return;
		}

		_subViewport = new SubViewport
		{
			Name = "Planet3DViewport",
			TransparentBg = false,
			Disable3D = false,
			HandleInputLocally = false,
			Msaa3D = Viewport.Msaa.Msaa8X,
			UseTaa = true,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always
		};

		_subViewport.World3D = new World3D();
		_previewRoot.AddChild(_subViewport);

		_viewportTextureRect = _previewRoot.GetNodeOrNull<TextureRect>("PlanetTexture");
		if (_viewportTextureRect == null)
		{
			_viewportTextureRect = new TextureRect
			{
				Name = "PlanetTexture",
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.Scale,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};

			_previewRoot.AddChild(_viewportTextureRect);
		}

		_viewportTextureRect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_viewportTextureRect.ZIndex = 60;
		_viewportTextureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_viewportTextureRect.StretchMode = TextureRect.StretchModeEnum.Scale;
		_viewportTextureRect.TextureFilter = CanvasItem.TextureFilterEnum.Linear;
		_viewportTextureRect.SelfModulate = Colors.White;

		_armillaryRing1 = _previewRoot.GetNodeOrNull<Control>("ArmillaryRing1");
		_armillaryRing2 = _previewRoot.GetNodeOrNull<Control>("ArmillaryRing2");

		if (!_resizeBound)
		{
			_previewRoot.Resized += OnPreviewRootResized;
			_resizeBound = true;
		}

		OnPreviewRootResized();
	}

	private void ConfigureWorldEnvironment()
	{
		if (_worldRoot == null || _worldEnvironment != null)
		{
			return;
		}

		var environment = new Environment
		{
			BackgroundMode = Environment.BGMode.Color,
			BackgroundColor = new Color(0.008f, 0.012f, 0.026f),
			BackgroundEnergyMultiplier = 1.0f,
			AmbientLightSource = Environment.AmbientSource.Color,
			AmbientLightColor = new Color(0.12f, 0.14f, 0.2f),
			AmbientLightEnergy = 0.05f
		};

		_worldEnvironment = new WorldEnvironment
		{
			Name = "PreviewWorldEnvironment",
			Environment = environment
		};
		_worldRoot.AddChild(_worldEnvironment);

		Texture2D skyTexture = LoadPreferredSkyTexture();
		if (skyTexture != null)
		{
			ApplySkyTexture(skyTexture);
		}
	}

	private Texture2D LoadPreferredSkyTexture()
	{
		if (!string.IsNullOrWhiteSpace(_selectedSkyTexturePath))
		{
			Texture2D selected = GD.Load<Texture2D>(_selectedSkyTexturePath);
			if (selected != null)
			{
				_activeSkyTexture = selected;
				return selected;
			}
		}

		return null;
	}

	private void ApplySkyTexture(Texture2D skyTexture)
	{
		if (skyTexture == null)
		{
			return;
		}

		_activeSkyTexture = skyTexture;

		if (_worldEnvironment?.Environment != null)
		{
			var panorama = new PanoramaSkyMaterial
			{
				Panorama = skyTexture,
				Filter = true
			};

			var sky = new Sky
			{
				SkyMaterial = panorama
			};

			_worldEnvironment.Environment.BackgroundMode = Environment.BGMode.Sky;
			_worldEnvironment.Environment.Sky = sky;
		}

		ApplySkyTextureToStarShell(skyTexture);
	}

	private void ApplySkyTextureToStarShell(Texture2D skyTexture)
	{
		if (skyTexture == null || _shellMaterial == null)
		{
			return;
		}

		if (_worldEnvironment?.Environment != null
			&& _worldEnvironment.Environment.BackgroundMode == Environment.BGMode.Sky)
		{
			if (_starShell != null)
			{
				_starShell.Visible = false;
			}
			return;
		}

		_shellMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
		_shellMaterial.AlbedoTexture = skyTexture;
		_shellMaterial.AlbedoColor = Colors.White;
		_shellMaterial.EmissionEnabled = true;
		_shellMaterial.Emission = Colors.White;
		_shellMaterial.EmissionEnergyMultiplier = 0.65f;
		_shellMaterial.CullMode = BaseMaterial3D.CullModeEnum.Front;

		if (_starShell != null)
		{
			_starShell.Visible = true;
		}
	}

	private void Build3DWorld()
	{
		if (_subViewport == null || _worldRoot != null)
		{
			return;
		}

		_worldRoot = new Node3D { Name = "WorldRoot" };
		_subViewport.AddChild(_worldRoot);

		_camera = new Camera3D
		{
			Name = "PreviewCamera",
			Position = new Vector3(56f, 30f, 100f),
			Current = true,
			Fov = 45f,
			Near = 0.1f,
			Far = 24000f
		};
		_worldRoot.AddChild(_camera);
		ConfigureWorldEnvironment();

		var ambient = new DirectionalLight3D
		{
			Name = "AmbientFill",
			LightEnergy = 0.015f,
			LightColor = new Color(0.22f, 0.24f, 0.3f),
			Rotation = new Vector3(0.32f, 1.26f, 0f)
		};
		_worldRoot.AddChild(ambient);

		_sunGroup = new Node3D { Name = "SunGroup", Position = _sunPosition };
		_worldRoot.AddChild(_sunGroup);

		_sunCore = new MeshInstance3D
		{
			Name = "SunCore",
			Mesh = new SphereMesh
			{
				Radius = 60f,
				Height = 120f,
				RadialSegments = 40,
				Rings = 24
			}
		};
		_sunCoreMaterial = BuildSunCoreMaterial();
		_sunCore.MaterialOverride = _sunCoreMaterial;
		_sunCore.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		EnsureSunSurfaceShader();
		ApplySunCoreVisualStyle(!string.IsNullOrWhiteSpace(_selectedSunTexturePath));
		_sunGroup.AddChild(_sunCore);

		_sunGlow = new MeshInstance3D
		{
			Name = "SunGlow",
			Mesh = new SphereMesh
			{
				Radius = 74f,
				Height = 148f,
				RadialSegments = 64,
				Rings = 36
			}
		};
		_sunGlow.MaterialOverride = BuildSunGlowMaterial();
		_sunGlow.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		_sunGroup.AddChild(_sunGlow);
		_sunCoronaMaterial = _sunGlow.MaterialOverride as ShaderMaterial;

		_sunDirectionalLight = new DirectionalLight3D
		{
			Name = "SunDirectionalLight",
			LightEnergy = 0.32f,
			LightColor = new Color(1f, 0.95f, 0.88f),
			ShadowEnabled = true
		};
		_worldRoot.AddChild(_sunDirectionalLight);
		_sunDirectionalLight.Position = _sunPosition;
		_sunDirectionalLight.LookAt(_earthOrigin, Vector3.Up);

		_sunPointLight = new OmniLight3D
		{
			Name = "SunPointLight",
			Position = _sunPosition,
			LightEnergy = 30.0f * _solarBrightness,
			LightColor = new Color(1f, 0.92f, 0.82f),
			OmniRange = 7200f,
			ShadowEnabled = false
		};
		_worldRoot.AddChild(_sunPointLight);

		_nightFillLight = new OmniLight3D
		{
			Name = "NightFillLight",
			Position = _earthOrigin,
			LightEnergy = 0.14f,
			LightColor = new Color(0.14f, 0.2f, 0.3f),
			OmniRange = 340f,
			ShadowEnabled = false
		};
		_worldRoot.AddChild(_nightFillLight);

		_earthGroup = new Node3D { Name = "EarthGroup", Position = _earthOrigin };
		_worldRoot.AddChild(_earthGroup);

		_planetMesh = new MeshInstance3D
		{
			Name = "PlanetMesh",
			Mesh = new SphereMesh
			{
				Radius = 10f,
				Height = 20f,
				RadialSegments = 96,
				Rings = 64
			}
		};
		_planetMaterial = BuildPlanetMaterial();
		_planetMesh.MaterialOverride = _planetMaterial;
		_planetMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
		_earthGroup.AddChild(_planetMesh);

		_cloudLayer = new MeshInstance3D
		{
			Name = "CloudLayer",
			Mesh = new SphereMesh
			{
				Radius = 10.25f,
				Height = 20.5f,
				RadialSegments = 64,
				Rings = 36
			}
		};
		_cloudMaterial = BuildCloudMaterial();
		_cloudLayer.MaterialOverride = _cloudMaterial;
		_cloudLayer.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		_earthGroup.AddChild(_cloudLayer);

		_moonMesh = new MeshInstance3D
		{
			Name = "Moon",
			Position = new Vector3(MoonOrbitRadius, 0f, 0f),
			Mesh = new SphereMesh
			{
				Radius = 2.7f,
				Height = 5.4f,
				RadialSegments = 32,
				Rings = 20
			}
		};
		_moonMaterial = BuildMoonMaterial();
		_moonMesh.MaterialOverride = _moonMaterial;
		_moonMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
		_earthGroup.AddChild(_moonMesh);

		_moonOrbit = BuildOrbitNode(
			"MoonOrbit",
			MoonOrbitRadius,
			1280,
			new Color(0.82f, 0.92f, 1f, 0.68f),
			new Color(0.58f, 0.76f, 1f, 0.28f),
			0.06f);
		_earthGroup.AddChild(_moonOrbit);

		float earthOrbitRadius = _sunPosition.DistanceTo(_earthOrigin);
		_planetOrbit = BuildOrbitNode(
			"PlanetOrbit",
			earthOrbitRadius,
			2200,
			new Color(1f, 0.82f, 0.58f, 0.5f),
			new Color(1f, 0.68f, 0.44f, 0.22f),
			0.18f);
		_planetOrbit.Position = _sunPosition;
		_worldRoot.AddChild(_planetOrbit);

		_starShell = new MeshInstance3D
		{
			Name = "StarShell",
			Mesh = new SphereMesh
			{
				Radius = 12000f,
				Height = 24000f,
				RadialSegments = 96,
				Rings = 64
			}
		};
		_shellMaterial = BuildStarShellMaterial();
		_starShell.MaterialOverride = _shellMaterial;
		_worldRoot.AddChild(_starShell);
		ApplySkyTextureToStarShell(_activeSkyTexture);

		_starFieldPivot = new Node3D { Name = "StarFieldPivot" };
		_worldRoot.AddChild(_starFieldPivot);
		_starPointCloud = BuildStarPointCloud(900);
		if (_starPointCloud != null)
		{
			_starFieldPivot.AddChild(_starPointCloud);
		}

		SetPlanetSurfaceTexturePath(_selectedPlanetSurfaceTexturePath);
		SetMoonTexturePath(_selectedMoonTexturePath);
		SetSunTexturePath(_selectedSunTexturePath);

		ApplyResolvedCelestialConfig();
		SetOrbitVisibility(_showMoonOrbit, _showPlanetOrbit);
		if (_lightFollowWithPlanet)
		{
			ApplyLightingFromPlanet(_cachedPlanetData);
		}

		UpdateSunCoronaByCameraDistance();
		UpdateSkyBackdropTransform();
		UpdateOrbitVisibilityByCameraDistance();
	}

	private void ApplyResolvedCelestialConfig()
	{
		_resolvedCelestialSystem ??= CelestialPhysicsSolver.Resolve(_celestialSystemConfig);
		var primary = _resolvedCelestialSystem?.PrimaryPlanet;
		if (primary == null)
		{
			return;
		}

		_primaryPlanetRadiusFactor = Mathf.Clamp(primary.RadiusEarth, 0.1f, 20.0f);
		_primaryPlanetSpinSpeed = Mathf.Max(0.0f, primary.SpinAngularSpeedRadPerSec * SpinTimeAcceleration);
		_sunPosition = ResolveSunPosition(primary.OrbitDistanceAu);

		if (_sunGroup != null)
		{
			_sunGroup.Position = _sunPosition;
		}

		if (_sunPointLight != null)
		{
			_sunPointLight.Position = _sunPosition;
		}

		if (_sunDirectionalLight != null)
		{
			_sunDirectionalLight.Position = _sunPosition;
			_sunDirectionalLight.LookAt(_earthOrigin, Vector3.Up);
		}

		UpdatePlanetScale(_cachedPlanetData?.Size ?? PlanetSize.Medium);
		ApplyPrimaryMoonPhysics();
		RebuildPrimaryPlanetOrbitLine();
		RebuildExtraPlanets();
		RebuildExtraSatellites();
		UpdateCameraHud();
	}

	private static Vector3 ResolveSunPosition(float orbitDistanceAu)
	{
		float normalized = Mathf.Clamp(
			Mathf.Log(1.0f + Mathf.Max(0.03f, orbitDistanceAu)) / Mathf.Log(21.0f),
			0f,
			1f);
		float distanceUnits = Mathf.Lerp(220f, 1200f, normalized);
		Vector3 direction = new Vector3(-0.44f, 0f, -0.90f).Normalized();
		return direction * distanceUnits;
	}

	private void ApplyPrimaryMoonPhysics()
	{
		CelestialPhysicsSolver.ResolvedSatellite primaryMoon = null;
		if (_resolvedCelestialSystem?.Satellites != null && _resolvedCelestialSystem.Satellites.Count > 0)
		{
			primaryMoon = _resolvedCelestialSystem.Satellites[0];
		}

		if (primaryMoon == null)
		{
			_primaryMoonOrbitDistancePlanetRadii = MoonOrbitRadius / Mathf.Max(0.001f, PlanetRadiusUnits);
			_primaryMoonOrbitSpeed = MoonOrbitAngularSpeed;
			_primaryMoonSpinSpeed = 0.35f;
			return;
		}

		_primaryMoonOrbitDistancePlanetRadii = Mathf.Max(2.0f, primaryMoon.OrbitDistancePlanetRadii);
		_primaryMoonOrbitSpeed = Mathf.Max(0.0f, primaryMoon.OrbitAngularSpeedRadPerSec * OrbitTimeAcceleration);
		_primaryMoonSpinSpeed = Mathf.Max(0.0f, primaryMoon.SpinAngularSpeedRadPerSec * SpinTimeAcceleration);

		if (_moonMesh != null)
		{
			float moonScale = Mathf.Clamp(primaryMoon.RadiusEarth / 0.2724f, 0.2f, 7.5f);
			_moonMesh.Scale = new Vector3(moonScale, moonScale, moonScale);
		}

		if (_moonOrbit != null)
		{
			float baseOrbitPlanetRadii = MoonOrbitRadius / Mathf.Max(0.001f, PlanetRadiusUnits);
			float orbitScale = (_currentPlanetRadiusUnits / Mathf.Max(0.001f, PlanetRadiusUnits))
				* (_primaryMoonOrbitDistancePlanetRadii / Mathf.Max(0.001f, baseOrbitPlanetRadii));
			_moonOrbit.Scale = new Vector3(orbitScale, orbitScale, orbitScale);
		}
	}

	private void RebuildPrimaryPlanetOrbitLine()
	{
		if (_worldRoot == null)
		{
			return;
		}

		if (_planetOrbit != null)
		{
			_planetOrbit.QueueFree();
			_planetOrbit = null;
		}

		float radius = _sunPosition.DistanceTo(_earthOrigin);
		_planetOrbit = BuildOrbitNode(
			"PlanetOrbit",
			radius,
			2200,
			new Color(1f, 0.82f, 0.58f, 0.5f),
			new Color(1f, 0.68f, 0.44f, 0.22f),
			0.18f);
		_planetOrbit.Position = _sunPosition;
		_worldRoot.AddChild(_planetOrbit);
	}

	private void RebuildExtraPlanets()
	{
		ClearNodeList(_extraPlanetOrbitLines);
		ClearNodeList(_extraPlanetPivots);
		_extraPlanetMeshes.Clear();
		_extraPlanetOrbitSpeeds.Clear();
		_extraPlanetSpinSpeeds.Clear();

		if (_worldRoot == null || _resolvedCelestialSystem?.AdditionalPlanets == null)
		{
			return;
		}

		for (int i = 0; i < _resolvedCelestialSystem.AdditionalPlanets.Count; i++)
		{
			var planet = _resolvedCelestialSystem.AdditionalPlanets[i];
			if (planet == null || !planet.Visible)
			{
				continue;
			}

			float orbitRadius = Mathf.Clamp((planet.OrbitDistanceAu * AstronomicalUnitKm) / KilometersPerSceneUnit, 40f, 3800f);
			float radiusUnits = Mathf.Clamp(planet.RadiusEarth * PlanetRadiusUnits * 0.62f, 1.2f, 30f);

			var pivot = new Node3D
			{
				Name = $"ExtraPlanetPivot_{i}",
				Position = _sunPosition,
				Rotation = new Vector3(Mathf.DegToRad(planet.OrbitInclinationDeg), 0f, 0f)
			};
			_worldRoot.AddChild(pivot);
			_extraPlanetPivots.Add(pivot);

			var mesh = new MeshInstance3D
			{
				Name = $"ExtraPlanet_{i}",
				Position = new Vector3(orbitRadius, 0f, 0f),
				Mesh = new SphereMesh
				{
					Radius = radiusUnits,
					Height = radiusUnits * 2f,
					RadialSegments = 40,
					Rings = 24
				},
				MaterialOverride = BuildAuxPlanetMaterial(planet.Element)
			};
			mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
			pivot.AddChild(mesh);
			_extraPlanetMeshes.Add(mesh);
			_extraPlanetOrbitSpeeds.Add(Mathf.Max(0f, planet.OrbitAngularSpeedRadPerSec * OrbitTimeAcceleration));
			_extraPlanetSpinSpeeds.Add(Mathf.Max(0f, planet.SpinAngularSpeedRadPerSec * SpinTimeAcceleration));

			Node3D orbitLine = BuildOrbitNode(
				$"ExtraPlanetOrbit_{i}",
				orbitRadius,
				1600,
				new Color(1f, 0.84f, 0.62f, 0.38f),
				new Color(1f, 0.66f, 0.44f, 0.16f),
				0.12f);
			orbitLine.Position = _sunPosition;
			_worldRoot.AddChild(orbitLine);
			_extraPlanetOrbitLines.Add(orbitLine);
		}
	}

	private void RebuildExtraSatellites()
	{
		ClearNodeList(_extraMoonOrbitLines);
		ClearNodeList(_extraMoonPivots);
		_extraMoonMeshes.Clear();
		_extraMoonOrbitSpeeds.Clear();
		_extraMoonSpinSpeeds.Clear();
		_extraMoonOrbitDistances.Clear();

		if (_earthGroup == null || _resolvedCelestialSystem?.Satellites == null || _resolvedCelestialSystem.Satellites.Count <= 1)
		{
			return;
		}

		for (int i = 1; i < _resolvedCelestialSystem.Satellites.Count; i++)
		{
			var moon = _resolvedCelestialSystem.Satellites[i];
			if (moon == null || !moon.Visible)
			{
				continue;
			}

			float baseOrbit = Mathf.Max(2.2f, moon.OrbitDistancePlanetRadii);
			float moonRadiusUnits = Mathf.Clamp(moon.RadiusEarth * PlanetRadiusUnits, 0.5f, 8f);

			var pivot = new Node3D
			{
				Name = $"ExtraMoonPivot_{i}",
				Rotation = new Vector3(Mathf.DegToRad(moon.OrbitInclinationDeg), 0f, 0f)
			};
			_earthGroup.AddChild(pivot);
			_extraMoonPivots.Add(pivot);

			var mesh = new MeshInstance3D
			{
				Name = $"ExtraMoon_{i}",
				Position = new Vector3(baseOrbit * _currentPlanetRadiusUnits, 0f, 0f),
				Mesh = new SphereMesh
				{
					Radius = moonRadiusUnits,
					Height = moonRadiusUnits * 2f,
					RadialSegments = 24,
					Rings = 14
				},
				MaterialOverride = BuildMoonMaterial()
			};
			mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
			pivot.AddChild(mesh);
			_extraMoonMeshes.Add(mesh);
			_extraMoonOrbitSpeeds.Add(Mathf.Max(0f, moon.OrbitAngularSpeedRadPerSec * OrbitTimeAcceleration));
			_extraMoonSpinSpeeds.Add(Mathf.Max(0f, moon.SpinAngularSpeedRadPerSec * SpinTimeAcceleration));
			_extraMoonOrbitDistances.Add(baseOrbit);

			Node3D orbitLine = BuildOrbitNode(
				$"ExtraMoonOrbit_{i}",
				baseOrbit * PlanetRadiusUnits,
				1280,
				new Color(0.82f, 0.92f, 1f, 0.58f),
				new Color(0.58f, 0.76f, 1f, 0.2f),
				0.04f);
			_earthGroup.AddChild(orbitLine);
			_extraMoonOrbitLines.Add(orbitLine);
		}
	}

	private static void ClearNodeList(List<Node3D> nodes)
	{
		if (nodes == null)
		{
			return;
		}

		for (int i = 0; i < nodes.Count; i++)
		{
			Node3D node = nodes[i];
			if (node != null)
			{
				node.QueueFree();
			}
		}

		nodes.Clear();
	}

	private Node3D BuildStarPointCloud(int starCount)
	{
		var pointCloud = new Node3D { Name = "StarPointCloud" };
		var rng = new RandomNumberGenerator();
		rng.Seed = 778190UL;

		for (int i = 0; i < starCount; i++)
		{
			float radius = rng.RandfRange(1800f, 3900f);
			float theta = rng.RandfRange(0f, Mathf.Pi * 2f);
			float phi = Mathf.Acos(rng.RandfRange(-1f, 1f));

			float sinPhi = Mathf.Sin(phi);
			Vector3 position = new(
				radius * sinPhi * Mathf.Cos(theta),
				radius * sinPhi * Mathf.Sin(theta),
				radius * Mathf.Cos(phi));

			var starMesh = new MeshInstance3D
			{
				Position = position,
				Mesh = new SphereMesh
				{
					Radius = rng.RandfRange(0.4f, 1.4f),
					Height = rng.RandfRange(0.8f, 2.8f),
					RadialSegments = 6,
					Rings = 4
				}
			};

			Color starColor;
			float colorType = rng.Randf();
			if (colorType > 0.9f)
			{
				starColor = new Color(1f, 0.67f, 0.38f, 0.86f);
			}
			else if (colorType > 0.7f)
			{
				starColor = new Color(1f, 0.92f, 0.7f, 0.82f);
			}
			else
			{
				starColor = new Color(0.7f, 0.82f, 1f, 0.8f);
			}

			starMesh.MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				AlbedoColor = starColor,
				EmissionEnabled = true,
				Emission = starColor,
				EmissionEnergyMultiplier = 0.9f
			};

			pointCloud.AddChild(starMesh);
		}

		return pointCloud;
	}

	private void TickBuildPipeline(double delta)
	{
		_buildProgress01 = Mathf.Clamp(_buildProgress01 + (float)delta * 0.85f, 0f, 1f);

		if (_buildStage == 0 && _buildProgress01 >= 0.18f)
		{
			EnsureViewportInfrastructure();
			_buildStage = 1;
		}

		if (_buildStage == 1 && _buildProgress01 >= 0.62f)
		{
			Build3DWorld();
			_buildStage = 2;
		}

		if (_buildStage == 2 && _buildProgress01 >= 0.9f)
		{
			_planetTextureView?.EnterViewportMode();
			TryBindViewportTexture();
			_buildStage = 3;
		}

		if (_buildProgress01 >= 1f)
		{
			_isBuildingThreeD = false;
			_isThreeDActive = true;
			_isControlEditPaused = false;
			_controlEditPauseLeft = 0f;
			_planetTextureView?.SetEditingPaused(false);
			ApplyPlanetToThreeD(_cachedPlanetData);
			UpdateUniverseMood(_currentLawAlignment);
			UpdateCameraHud();
		}
	}

	private void ApplyPlanetToThreeD(PlanetData planetData)
	{
		if (planetData == null || _planetMaterial == null)
		{
			return;
		}

		UpdatePlanetMaterial(planetData);
		UpdatePlanetScale(planetData.Size);
		UpdateStarfieldByElement(planetData.Element);

		Texture2D surfaceTexture = UpdatePlanetTextures(planetData, 1024, 512);
		if (surfaceTexture != null)
		{
			_latestPlanetSnapshot = surfaceTexture;
			if (_planetPhotoRealtimeUpdate)
			{
				_planetTextureView?.SetPlanetSnapshot(surfaceTexture);
			}
		}

		if (_lightFollowWithPlanet)
		{
			ApplyLightingFromPlanet(planetData);
		}
	}

	private void UpdatePlanetSnapshot(PlanetData planetData, int width, int height)
	{
		if (planetData == null)
		{
			return;
		}

		Texture2D snapshot = BuildPlanetSurfaceTexture(planetData, Mathf.Max(64, width), Mathf.Max(32, height));
		if (snapshot == null)
		{
			return;
		}

		_latestPlanetSnapshot = snapshot;
		_planetTextureView?.SetPlanetSnapshot(snapshot);
	}

	private void ApplyLightingFromPlanet(PlanetData planetData)
	{
		if (planetData == null)
		{
			return;
		}

		float response = Mathf.Clamp(_lightResponseStrength, 0f, 1f);
		ResolveEffectiveClimate(planetData, out float ocean, out float temperature, out float atmosphere);

		if (_sunPointLight != null)
		{
			float baseEnergy = 32.0f * _solarBrightness;
			float temperatureShift = Mathf.Lerp(-2.0f, 3.8f, temperature) * response;
			float atmosphereShift = Mathf.Lerp(-0.4f, 1.8f, atmosphere) * response;
			_sunPointLight.LightEnergy = Mathf.Max(3.0f, baseEnergy + temperatureShift + atmosphereShift);

			Color warm = new Color(1f, 0.92f, 0.82f);
			Color cold = new Color(0.78f, 0.88f, 1f);
			Color hot = new Color(1f, 0.78f, 0.58f);
			Color tempColor = temperature < 0.5f
				? cold.Lerp(warm, temperature * 2f)
				: warm.Lerp(hot, (temperature - 0.5f) * 2f);
			_sunPointLight.LightColor = tempColor.Lerp(new Color(0.74f, 0.84f, 1f), ocean * 0.08f * response);
		}

		if (_sunDirectionalLight != null)
		{
			float baseDirectional = 0.42f * _solarBrightness;
			float directionalShift = Mathf.Lerp(-0.3f, 1.1f, temperature) * response;
			_sunDirectionalLight.LightEnergy = Mathf.Max(0.08f, baseDirectional + directionalShift * 0.42f);
		}

		if (_nightFillLight != null)
		{
			_nightFillLight.LightEnergy = Mathf.Lerp(0.11f, 0.3f, atmosphere) * Mathf.Lerp(0.92f, 1.22f, response);
		}
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

	private void OnPreviewRootResized()
	{
		if (_previewRoot == null || _subViewport == null)
		{
			return;
		}

		Vector2 size = _previewRoot.Size;
		if (size.X < 8f || size.Y < 8f)
		{
			return;
		}

		_subViewport.Size = new Vector2I(
			Mathf.Max(96, Mathf.RoundToInt(size.X * PreviewSupersampleScale)),
			Mathf.Max(96, Mathf.RoundToInt(size.Y * PreviewSupersampleScale)));
		TryBindViewportTexture();
	}

	private void TryBindViewportTexture()
	{
		if (_subViewport == null || _viewportTextureRect == null)
		{
			return;
		}

		var viewportTexture = _subViewport.GetTexture();
		if (viewportTexture != null && _viewportTextureRect.Texture != viewportTexture)
		{
			_viewportTextureRect.Texture = viewportTexture;
		}

		_viewportTextureRect.SelfModulate = _viewportTextureRect.Texture == null
			? new Color(1f, 0.22f, 0.2f, 1f)
			: Colors.White;
	}

	private Texture2D UpdatePlanetTextures(PlanetData planetData, int width, int height)
	{
		if (_planetMaterial == null || _cloudMaterial == null)
		{
			return null;
		}

		Texture2D surfaceTexture = BuildPlanetSurfaceTexture(planetData, width, height);
		if (!ForceEcologicalProceduralSurface && !string.IsNullOrWhiteSpace(_selectedPlanetSurfaceTexturePath))
		{
			surfaceTexture = GD.Load<Texture2D>(_selectedPlanetSurfaceTexturePath) ?? surfaceTexture;
		}

		Texture2D cloudTexture = BuildPlanetCloudTexture(planetData, width, height);
		_planetMaterial.AlbedoTexture = surfaceTexture;
		_cloudMaterial.AlbedoTexture = cloudTexture;
		return surfaceTexture;
	}

	private Texture2D BuildPlanetSurfaceTexture(PlanetData planetData, int width, int height)
	{
		var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);

		const int MaxGenerationPixels = 2400000;
		float generationScale = 1f;
		int requestedPixels = width * height;
		if (requestedPixels > MaxGenerationPixels)
		{
			generationScale = Mathf.Sqrt(MaxGenerationPixels / (float)requestedPixels);
		}

		int surfaceWidth = Mathf.Clamp(Mathf.RoundToInt(width * generationScale), 512, width);
		int surfaceHeight = Mathf.Clamp(Mathf.RoundToInt(height * generationScale), 256, height);

		int seed = BuildTextureSeed(planetData, 137);
		PlanetTerrainGenerator.PlanetSurfaceData surfaceData = PlanetTerrainGenerator.GenerateSurfaceData(
			planetData,
			_generationProfile,
			surfaceWidth,
			surfaceHeight,
			seed);

		if (surfaceData.Elevation == null
			|| surfaceData.Biomes == null
			|| surfaceData.RiverLayer == null
			|| surfaceData.Elevation.Length != surfaceWidth * surfaceHeight
			|| surfaceData.Biomes.Length != surfaceWidth * surfaceHeight
			|| surfaceData.RiverLayer.Length != surfaceWidth * surfaceHeight)
		{
			return ImageTexture.CreateFromImage(image);
		}

		ResolveSurfacePalette(planetData.Element, out Color elementLandColor, out Color elementOceanColor);
		bool isTerra = planetData.Element == PlanetElement.Terra;
		bool hasTemperature = surfaceData.Temperature != null && surfaceData.Temperature.Length == surfaceWidth * surfaceHeight;
		bool hasMoisture = surfaceData.Moisture != null && surfaceData.Moisture.Length == surfaceWidth * surfaceHeight;

		var surfaceImage = Image.CreateEmpty(surfaceWidth, surfaceHeight, false, Image.Format.Rgba8);

		for (int y = 0; y < surfaceHeight; y++)
		{
			for (int x = 0; x < surfaceWidth; x++)
			{
				int index = y * surfaceWidth + x;
				float elevation = surfaceData.Elevation[index];
				bool isLand = elevation >= surfaceData.SeaLevel;
				float temperature = hasTemperature ? surfaceData.Temperature[index] : 0.5f;
				float moisture = hasMoisture ? surfaceData.Moisture[index] : 0.5f;
				float latitude01 = surfaceHeight > 1 ? y / (float)(surfaceHeight - 1) : 0.5f;
				PlanetTerrainGenerator.PlanetBiomeType biome = surfaceData.Biomes[index];
				int westX = x > 0 ? x - 1 : surfaceWidth - 1;
				int eastX = x + 1 < surfaceWidth ? x + 1 : 0;
				int northY = y > 0 ? y - 1 : 0;
				int southY = y + 1 < surfaceHeight ? y + 1 : surfaceHeight - 1;
				float elevationDx = surfaceData.Elevation[y * surfaceWidth + eastX] - surfaceData.Elevation[y * surfaceWidth + westX];
				float elevationDy = surfaceData.Elevation[southY * surfaceWidth + x] - surfaceData.Elevation[northY * surfaceWidth + x];
				float relief = Mathf.Clamp(Mathf.Sqrt(elevationDx * elevationDx + elevationDy * elevationDy) * 5.8f, 0f, 1f);

				Color color = ResolveDrawJsBiomeColor(biome);
				color = ApplyEarthPaletteShading(
					color,
					biome,
					elevation,
					surfaceData.SeaLevel,
					temperature,
					moisture,
					latitude01);
				float riverStrength = surfaceData.RiverLayer[index];
				if (isLand && riverStrength > 0.0001f && !IsFrozenBiomeForRiver(biome))
				{
					color = color.Lerp(DrawJsRiver, Mathf.Clamp(0.16f + riverStrength * 0.42f, 0f, 0.58f));
				}

				if (!isTerra)
				{
					Color elementBase = isLand ? elementLandColor : elementOceanColor;
					color = color.Lerp(elementBase, isLand ? 0.04f : 0.06f);
				}

				if (isLand)
				{
					color = color.Lerp(color.Darkened(0.16f), relief * 0.18f);
				}
				else
				{
					color = color.Lightened(relief * 0.04f);
				}

				surfaceImage.SetPixel(x, y, color);
			}
		}

		surfaceImage = BlendBiomeColorEdges(
			surfaceImage,
			surfaceData.Biomes,
			surfaceData.Elevation,
			surfaceWidth,
			surfaceHeight,
			surfaceData.SeaLevel,
			2,
			0.28f);

		if (surfaceWidth != width || surfaceHeight != height)
		{
			surfaceImage.Resize(width, height, Image.Interpolation.Bilinear);
		}

		BlendVerticalSeam(surfaceImage, 0, 2);
		BlendVerticalSeam(surfaceImage, width / 2, 2);
		image = surfaceImage;

		return ImageTexture.CreateFromImage(image);
	}

	private static Image BlendBiomeColorEdges(
		Image source,
		PlanetTerrainGenerator.PlanetBiomeType[] biomes,
		float[] elevation,
		int width,
		int height,
		float seaLevel,
		int passes,
		float strength)
	{
		if (source == null
			|| biomes == null
			|| elevation == null
			|| biomes.Length != width * height
			|| elevation.Length != width * height)
		{
			return source;
		}

		Image current = source;
		for (int pass = 0; pass < passes; pass++)
		{
			var blended = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int index = y * width + x;
					Color center = current.GetPixel(x, y);

					if (elevation[index] < seaLevel)
					{
						blended.SetPixel(x, y, center);
						continue;
					}

					PlanetTerrainGenerator.PlanetBiomeType biome = biomes[index];
					int differentNeighbors = 0;
					float sumR = center.R;
					float sumG = center.G;
					float sumB = center.B;
					float sumA = center.A;
					int samples = 1;

					for (int q = -1; q <= 1; q++)
					{
						for (int p = -1; p <= 1; p++)
						{
							if (p == 0 && q == 0)
							{
								continue;
							}

							int sampleY = y + q;
							if (sampleY < 0 || sampleY >= height)
							{
								continue;
							}

							int sampleX = ((x + p) % width + width) % width;
							int sampleIndex = sampleY * width + sampleX;
							if (elevation[sampleIndex] < seaLevel)
							{
								continue;
							}

							PlanetTerrainGenerator.PlanetBiomeType neighborBiome = biomes[sampleIndex];
							if (neighborBiome == biome)
							{
								continue;
							}

							differentNeighbors++;
							Color neighborColor = current.GetPixel(sampleX, sampleY);
							sumR += neighborColor.R;
							sumG += neighborColor.G;
							sumB += neighborColor.B;
							sumA += neighborColor.A;
							samples++;
						}
					}

					if (differentNeighbors < 2)
					{
						blended.SetPixel(x, y, center);
						continue;
					}

					Color average = new Color(
						sumR / samples,
						sumG / samples,
						sumB / samples,
						sumA / samples);
					float blendFactor = Mathf.Clamp(strength * (differentNeighbors / 8f), 0f, 0.45f);
					blended.SetPixel(x, y, center.Lerp(average, blendFactor));
				}
			}

			current = blended;
		}

		return current;
	}

	private static Color ApplyEarthPaletteShading(
		Color baseColor,
		PlanetTerrainGenerator.PlanetBiomeType biome,
		float elevation,
		float seaLevel,
		float temperature,
		float moisture,
		float latitude01)
	{
		bool isOcean = biome == PlanetTerrainGenerator.PlanetBiomeType.Ocean
			|| biome == PlanetTerrainGenerator.PlanetBiomeType.ShallowOcean;

		float latAbs = Mathf.Abs(latitude01 * 2f - 1f);
		float coldness = Mathf.Clamp(1f - temperature, 0f, 1f);
		float humidity = Mathf.Clamp(moisture, 0f, 1f);
		float dryness = 1f - humidity;

		if (isOcean)
		{
			float depth = Mathf.Clamp((seaLevel - elevation) / Mathf.Max(0.0001f, seaLevel), 0f, 1f);

			Color shallow = new Color(0.23922f, 0.48235f, 0.73333f, 1f); // #3d7bbb
			Color tropical = new Color(0.10980f, 0.30980f, 0.57255f, 1f); // #1c4f92
			Color temperate = new Color(0.09020f, 0.26275f, 0.52157f, 1f); // #174385
			Color deep = new Color(0.04314f, 0.14510f, 0.34118f, 1f); // #0b2557

			Color warmSea = shallow.Lerp(tropical, depth * 0.62f);
			Color coolSea = shallow.Lerp(temperate, depth * 0.68f);
			float coldLatitude = Mathf.Clamp(latAbs * 0.82f + coldness * 0.34f, 0f, 1f);
			Color ocean = warmSea.Lerp(coolSea, coldLatitude);
			ocean = ocean.Lerp(deep, depth * 0.66f);
			return ocean;
		}

		float normalizedHeight = Mathf.Clamp((elevation - seaLevel) / Mathf.Max(0.0001f, 1f - seaLevel), 0f, 1f);

		Color humidWarm = new Color(0.17647f, 0.40392f, 0.15686f, 1f); // #2d6728
		Color humidCool = new Color(0.25098f, 0.41569f, 0.22745f, 1f); // #406a3a
		Color dryWarm = new Color(0.73333f, 0.66667f, 0.52941f, 1f); // #bba987
		Color dryCool = new Color(0.55686f, 0.52549f, 0.44706f, 1f); // #8e8672
		Color alpine = new Color(0.56078f, 0.52941f, 0.48627f, 1f); // #8f877c

		float warmness = Mathf.Clamp((temperature - 0.18f) / 0.62f, 0f, 1f);
		Color warmBlend = dryWarm.Lerp(humidWarm, humidity);
		Color coolBlend = dryCool.Lerp(humidCool, humidity);
		Color climateColor = coolBlend.Lerp(warmBlend, warmness);
		Color land = climateColor.Lerp(baseColor, 0.22f);

		switch (biome)
		{
			case PlanetTerrainGenerator.PlanetBiomeType.TropicalDesert:
				land = land.Lerp(new Color(0.79216f, 0.72157f, 0.58039f, 1f), 0.46f);
				break;
			case PlanetTerrainGenerator.PlanetBiomeType.TemperateDesert:
				land = land.Lerp(new Color(0.74118f, 0.67843f, 0.55294f, 1f), 0.42f);
				break;
			case PlanetTerrainGenerator.PlanetBiomeType.Savannah:
				land = land.Lerp(new Color(0.52941f, 0.58039f, 0.27843f, 1f), 0.24f);
				break;
			case PlanetTerrainGenerator.PlanetBiomeType.Shrubland:
				land = land.Lerp(new Color(0.45098f, 0.52941f, 0.25098f, 1f), 0.28f);
				break;
			case PlanetTerrainGenerator.PlanetBiomeType.Grassland:
				land = land.Lerp(new Color(0.47059f, 0.60784f, 0.26275f, 1f), 0.32f);
				break;
			case PlanetTerrainGenerator.PlanetBiomeType.Chaparral:
				land = land.Lerp(new Color(0.52549f, 0.53333f, 0.31765f, 1f), 0.2f);
				break;
			case PlanetTerrainGenerator.PlanetBiomeType.TropicalRainForest:
				land = land.Lerp(new Color(0.08235f, 0.47451f, 0.10980f, 1f), 0.62f);
				break;
			case PlanetTerrainGenerator.PlanetBiomeType.TemperateRainForest:
			case PlanetTerrainGenerator.PlanetBiomeType.TemperateSeasonalForest:
				land = land.Lerp(new Color(0.15686f, 0.45882f, 0.16471f, 1f), 0.52f);
				break;
			case PlanetTerrainGenerator.PlanetBiomeType.BorealForest:
			case PlanetTerrainGenerator.PlanetBiomeType.Taiga:
				land = land.Lerp(new Color(0.21961f, 0.40784f, 0.23137f, 1f), 0.44f);
				break;
			case PlanetTerrainGenerator.PlanetBiomeType.RockyMountain:
				land = land.Lerp(new Color(0.58039f, 0.53333f, 0.47451f, 1f), 0.38f);
				break;
			case PlanetTerrainGenerator.PlanetBiomeType.SnowyMountain:
				land = land.Lerp(new Color(0.94118f, 0.94510f, 0.95294f, 1f), 0.76f);
				break;
			case PlanetTerrainGenerator.PlanetBiomeType.Ice:
				float iceCover = Mathf.Clamp(0.5f + coldness * 0.35f + latAbs * 0.35f - normalizedHeight * 0.08f, 0.45f, 0.92f);
				land = land.Lerp(new Color(0.94902f, 0.95294f, 0.96078f, 1f), iceCover);
				break;
		}

		float lowland = Mathf.Clamp(1f - normalizedHeight * 1.65f, 0f, 1f);
		land = land.Lerp(new Color(0.27059f, 0.56078f, 0.21569f, 1f), humidity * lowland * 0.34f);
		land = land.Lerp(new Color(0.74118f, 0.68627f, 0.57255f, 1f), dryness * 0.1f);
		land = land.Lerp(alpine, Mathf.Pow(normalizedHeight, 1.22f) * 0.32f);

		float polarSnow = Mathf.Clamp(coldness * 0.82f + latAbs * 0.34f + normalizedHeight * 0.28f - 0.72f, 0f, 1f);
		land = land.Lerp(new Color(0.92941f, 0.93333f, 0.94118f, 1f), polarSnow * 0.75f);

		return land;
	}

	private static void BlendVerticalSeam(Image image, int seamX, int feather)
	{
		if (image == null || seamX < 0 || feather <= 0)
		{
			return;
		}

		int width = image.GetWidth();
		int height = image.GetHeight();
		if (width <= 1 || height <= 0)
		{
			return;
		}

		int seam = ((seamX % width) + width) % width;
		int blendRadius = Mathf.Min(feather, Mathf.Max(1, width / 8));
		for (int y = 0; y < height; y++)
		{
			for (int r = 0; r <= blendRadius; r++)
			{
				int leftX = (seam - 1 - r + width) % width;
				int rightX = (seam + r) % width;
				float t = (r + 1f) / (blendRadius + 1f);

				Color left = image.GetPixel(leftX, y);
				Color right = image.GetPixel(rightX, y);
				Color mixed = left.Lerp(right, 0.5f);

				image.SetPixel(leftX, y, left.Lerp(mixed, 1f - t));
				image.SetPixel(rightX, y, right.Lerp(mixed, 1f - t));
			}
		}
	}

	private void ResolveEffectiveClimate(PlanetData planetData, out float oceanCoverage, out float temperature, out float atmosphere)
	{
		if (planetData == null)
		{
			oceanCoverage = CivilizationOceanCoverage;
			temperature = CivilizationTemperature;
			atmosphere = CivilizationAtmosphereDensity;
			return;
		}

		ResolveElementClimateBaseline(planetData.Element, out float baseOcean, out float baseTemperature, out float baseAtmosphere);

		float storedOcean = Mathf.Clamp(planetData.OceanCoverage, 0f, 1f);
		float storedTemperature = Mathf.Clamp(planetData.Temperature, 0f, 1f);
		float storedAtmosphere = Mathf.Clamp(planetData.AtmosphereDensity, 0f, 1f);

		float profileOcean = Mathf.Clamp(
			0.56f + (0.62f - _generationProfile.ContinentalFrequency) * 0.18f,
			0.14f,
			0.84f);
		float profileTemperature = Mathf.Clamp((1000f - _generationProfile.HeatFactor) / 999f, 0.05f, 0.95f);
		float profileAtmosphere = Mathf.Clamp(0.34f + _generationProfile.MoistureTransport * 0.58f, 0.12f, 0.96f);

		oceanCoverage = Mathf.Clamp(baseOcean * 0.5f + profileOcean * 0.35f + storedOcean * 0.15f, 0f, 1f);
		temperature = Mathf.Clamp(baseTemperature * 0.45f + profileTemperature * 0.42f + storedTemperature * 0.13f, 0f, 1f);
		atmosphere = Mathf.Clamp(baseAtmosphere * 0.52f + profileAtmosphere * 0.33f + storedAtmosphere * 0.15f, 0f, 1f);
	}

	private static void ResolveElementClimateBaseline(PlanetElement element, out float oceanCoverage, out float temperature, out float atmosphere)
	{
		switch (element)
		{
			case PlanetElement.Pyro:
				oceanCoverage = 0.24f;
				temperature = 0.84f;
				atmosphere = 0.68f;
				break;
			case PlanetElement.Cryo:
				oceanCoverage = 0.6f;
				temperature = 0.18f;
				atmosphere = 0.54f;
				break;
			case PlanetElement.Aero:
				oceanCoverage = 0.66f;
				temperature = 0.44f;
				atmosphere = 0.76f;
				break;
			case PlanetElement.Terra:
			default:
				oceanCoverage = CivilizationOceanCoverage;
				temperature = CivilizationTemperature;
				atmosphere = CivilizationAtmosphereDensity;
				break;
		}
	}

	private static bool IsFrozenBiomeForRiver(PlanetTerrainGenerator.PlanetBiomeType biome)
	{
		return biome == PlanetTerrainGenerator.PlanetBiomeType.Ice
			|| biome == PlanetTerrainGenerator.PlanetBiomeType.SnowyMountain
			|| biome == PlanetTerrainGenerator.PlanetBiomeType.Tundra;
	}

	private static Color ResolveDrawJsBiomeColor(PlanetTerrainGenerator.PlanetBiomeType biome)
	{
		switch (biome)
		{
			case PlanetTerrainGenerator.PlanetBiomeType.Ocean:
				return DrawJsOceanDeep;
			case PlanetTerrainGenerator.PlanetBiomeType.ShallowOcean:
				return DrawJsOceanMid;
			case PlanetTerrainGenerator.PlanetBiomeType.Coastland:
				return DrawJsCoastland;
			case PlanetTerrainGenerator.PlanetBiomeType.TropicalRainForest:
				return DrawJsTropicalRainForest;
			case PlanetTerrainGenerator.PlanetBiomeType.TropicalSeasonalForest:
				return DrawJsTropicalSeasonalForest;
			case PlanetTerrainGenerator.PlanetBiomeType.Shrubland:
				return DrawJsShrubland;
			case PlanetTerrainGenerator.PlanetBiomeType.Savannah:
				return DrawJsSavannah;
			case PlanetTerrainGenerator.PlanetBiomeType.TropicalDesert:
				return DrawJsTropicalDesert;
			case PlanetTerrainGenerator.PlanetBiomeType.TemperateRainForest:
				return DrawJsTemperateRainForest;
			case PlanetTerrainGenerator.PlanetBiomeType.TemperateSeasonalForest:
				return DrawJsTemperateSeasonalForest;
			case PlanetTerrainGenerator.PlanetBiomeType.Chaparral:
				return DrawJsChaparral;
			case PlanetTerrainGenerator.PlanetBiomeType.Grassland:
				return DrawJsGrassland;
			case PlanetTerrainGenerator.PlanetBiomeType.Steppe:
				return DrawJsSteppe;
			case PlanetTerrainGenerator.PlanetBiomeType.TemperateDesert:
				return DrawJsTemperateDesert;
			case PlanetTerrainGenerator.PlanetBiomeType.BorealForest:
				return DrawJsBorealForest;
			case PlanetTerrainGenerator.PlanetBiomeType.Taiga:
				return DrawJsTaiga;
			case PlanetTerrainGenerator.PlanetBiomeType.Tundra:
				return DrawJsTundra;
			case PlanetTerrainGenerator.PlanetBiomeType.Ice:
				return DrawJsIce;
			case PlanetTerrainGenerator.PlanetBiomeType.RockyMountain:
				return DrawJsRockyMountain;
			case PlanetTerrainGenerator.PlanetBiomeType.SnowyMountain:
				return DrawJsSnowyMountain;
			default:
				return DrawJsRockyMountain;
		}
	}



	private Texture2D BuildPlanetCloudTexture(PlanetData planetData, int width, int height)
	{
		var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
		ResolveEffectiveClimate(planetData, out float _, out float _, out float atmosphere);
		if (atmosphere <= 0.01f)
		{
			return ImageTexture.CreateFromImage(image);
		}

		int seed = BuildTextureSeed(planetData, 503);
		var cloudNoise = new FastNoiseLite
		{
			Seed = seed,
			NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
			Frequency = 5.4f,
			FractalOctaves = 4,
			FractalGain = 0.55f
		};

		for (int y = 0; y < height; y++)
		{
			float v = (float)y / (height - 1);
			for (int x = 0; x < width; x++)
			{
				float u = (float)x / (width - 1);
				float cloud = (cloudNoise.GetNoise2D(u * 14f, v * 8f) + 1f) * 0.5f;
				float alpha = Mathf.Clamp((cloud - 0.56f) * 2.6f, 0f, 1f) * atmosphere * 0.92f;
				Color cloudColor = new(0.92f, 0.95f, 1f, alpha);
				image.SetPixel(x, y, cloudColor);
			}
		}

		return ImageTexture.CreateFromImage(image);
	}

	private void UpdatePlanetMaterial(PlanetData planetData)
	{
		ResolveEffectiveClimate(planetData, out float ocean, out float _, out float atmosphere);

		_planetMaterial.AlbedoColor = Colors.White;
		_planetMaterial.Emission = new Color(0.03f, 0.04f, 0.05f, 1f);
		_planetMaterial.EmissionEnergyMultiplier = Mathf.Lerp(0.012f, 0.03f, atmosphere);
		_planetMaterial.Roughness = Mathf.Lerp(0.8f, 0.44f, 1f - ocean * 0.4f);

		if (_cloudMaterial != null)
		{
			float alpha = Mathf.Lerp(0.04f, 0.5f, atmosphere);
			_cloudMaterial.AlbedoColor = new Color(0.9f, 0.94f, 0.98f, alpha);
			_cloudMaterial.Emission = new Color(0.08f, 0.11f, 0.16f, 1f);
			_cloudMaterial.EmissionEnergyMultiplier = Mathf.Lerp(0.02f, 0.06f, atmosphere);
		}
	}

	private void UpdatePlanetScale(PlanetSize size)
	{
		if (_earthGroup == null)
		{
			return;
		}

		float sizeScale = size switch
		{
			PlanetSize.Small => 0.8f,
			PlanetSize.Medium => 1.0f,
			PlanetSize.Large => 1.2f,
			PlanetSize.Colossal => 1.46f,
			_ => 1.0f
		};

		float physicalScale = Mathf.Clamp(_primaryPlanetRadiusFactor, 0.1f, 20.0f);
		float finalScale = sizeScale * physicalScale;

		_earthGroup.Scale = new Vector3(finalScale, finalScale, finalScale);
		_currentPlanetRadiusUnits = PlanetRadiusUnits * finalScale;

		float minDistanceFromScale = Mathf.Max(MinDistance, _currentPlanetRadiusUnits * 1.12f);
		_targetDistance = Mathf.Max(_targetDistance, minDistanceFromScale);
		_currentDistance = Mathf.Max(_currentDistance, minDistanceFromScale);
	}

	private void UpdateStarfieldByElement(PlanetElement element)
	{
		if (_shellMaterial == null)
		{
			return;
		}

		if (_activeSkyTexture != null)
		{
			ApplySkyTextureToStarShell(_activeSkyTexture);
			return;
		}

		_shellMaterial.AlbedoTexture = null;
		_shellMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		_shellMaterial.AlbedoColor = element switch
		{
			PlanetElement.Terra => new Color(0.05f, 0.08f, 0.15f, 0.22f),
			PlanetElement.Pyro => new Color(0.12f, 0.05f, 0.06f, 0.2f),
			PlanetElement.Cryo => new Color(0.06f, 0.1f, 0.18f, 0.22f),
			PlanetElement.Aero => new Color(0.08f, 0.1f, 0.2f, 0.22f),
			_ => new Color(0.05f, 0.08f, 0.15f, 0.22f)
		};
	}

	private void UpdateCameraHud()
	{
		float distanceUnits = Mathf.Max(0.001f, _targetDistance);
		float radiusUnits = Mathf.Max(0.001f, _currentPlanetRadiusUnits);
		float centerDistanceKm = distanceUnits * KilometersPerSceneUnit;
		float altitudeKm = Mathf.Max(0f, (distanceUnits - radiusUnits) * KilometersPerSceneUnit);
		float zoomPercent = Mathf.Clamp(radiusUnits / distanceUnits * 100f, 0f, 100f);
		string altitudeText = FormatDistance(altitudeKm);

		float gravityEarth = Mathf.Max(0.01f, _resolvedCelestialSystem?.PrimaryPlanet?.SurfaceGravityEarth ?? 1.0f);
		float gravityMs2 = Mathf.Max(0.1f, _resolvedCelestialSystem?.PrimaryPlanet?.SurfaceGravityMs2 ?? 9.80665f);

		CameraDistanceText = FormatDistance(centerDistanceKm);
		string baseDesc = _translationManager?.TrWithFormat(
			"preview_camera_desc",
			$"{zoomPercent:0.0}",
			altitudeText) ?? $"Zoom {zoomPercent:0.0}% · Altitude {altitudeText}";

		bool isZh = _translationManager?.CurrentLanguage?.StartsWith("zh") ?? true;
		string gravityDesc = isZh
			? $"地表重力 {gravityEarth:0.00}g ({gravityMs2:0.00}m/s²)"
			: $"Surface Gravity {gravityEarth:0.00}g ({gravityMs2:0.00}m/s²)";

		CameraDescriptionText = $"{baseDesc} · {gravityDesc}";
	}

	private void UpdateSunCoronaByCameraDistance()
	{
		if (_camera == null || _sunGlow == null)
		{
			return;
		}

		float sunDistance = _camera.GlobalPosition.DistanceTo(_sunGlow.GlobalPosition);
		float distanceT = Mathf.Clamp((sunDistance - 280f) / 2400f, 0f, 1f);
		float distanceBoost = Mathf.Lerp(0.9f, 1.85f, distanceT);

		if (_sunCoronaMaterial != null)
		{
			_sunCoronaMaterial.SetShaderParameter("distance_boost", distanceBoost);
			_sunCoronaMaterial.SetShaderParameter("jet_strength", Mathf.Lerp(0.75f, 1.22f, distanceT));
			_sunCoronaMaterial.SetShaderParameter("flicker_speed", Mathf.Lerp(1.25f, 2.05f, distanceT));
		}

		if (_sunCoreShaderMaterial != null)
		{
			float detailMix = string.IsNullOrWhiteSpace(_selectedSunTexturePath) ? 0.0f : Mathf.Lerp(0.08f, 0.16f, distanceT);
			_sunCoreShaderMaterial.SetShaderParameter("flow_speed", Mathf.Lerp(0.46f, 0.62f, distanceT));
			_sunCoreShaderMaterial.SetShaderParameter("pulse_strength", Mathf.Lerp(0.06f, 0.10f, distanceT));
			_sunCoreShaderMaterial.SetShaderParameter("detail_mix", detailMix);
		}

		float elapsed = (float)Time.GetTicksMsec() / 1000f;
		float pulse = 1.0f + Mathf.Sin(elapsed * 0.85f) * 0.02f;
		float scale = Mathf.Lerp(1.0f, 1.18f, distanceT) * pulse;
		_sunGlow.Scale = _sunGlow.Scale.Lerp(new Vector3(scale, scale, scale), 0.08f);
	}

	private string FormatDistance(float kilometers)
	{
		float value = Mathf.Max(0f, kilometers);
		if (value >= AstronomicalUnitKm * 0.2f)
		{
			return _translationManager?.TrWithFormat("preview_distance_au", $"{value / AstronomicalUnitKm:0.000}")
				?? $"{value / AstronomicalUnitKm:0.000} AU";
		}

		if (value >= 1000000f)
		{
			return _translationManager?.TrWithFormat("preview_distance_million_km", $"{value / 1000000f:0.00}")
				?? $"{value / 1000000f:0.00}M km";
		}

		return _translationManager?.TrWithFormat("preview_distance_km", $"{value:0,0}")
			?? $"{value:0,0} km";
	}

	private static Vector3 GetSphericalDirection(float u, float v)
	{
		float longitude = (u - 0.5f) * Mathf.Tau;
		float latitude = (0.5f - v) * Mathf.Pi;
		float cosLat = Mathf.Cos(latitude);
		return new Vector3(
			cosLat * Mathf.Cos(longitude),
			Mathf.Sin(latitude),
			cosLat * Mathf.Sin(longitude));
	}

	private static float NoiseToUnitRange(float noiseValue)
	{
		return (noiseValue + 1f) * 0.5f;
	}

	private static float[] BlurScalarMap(float[] source, int width, int height, int passes)
	{
		if (source == null || source.Length != width * height || passes <= 0)
		{
			return source;
		}

		float[] read = source;
		float[] write = new float[source.Length];

		for (int pass = 0; pass < passes; pass++)
		{
			for (int y = 0; y < height; y++)
			{
				int y0 = Mathf.Max(0, y - 1);
				int y1 = y;
				int y2 = Mathf.Min(height - 1, y + 1);

				for (int x = 0; x < width; x++)
				{
					int x0 = Mathf.Max(0, x - 1);
					int x1 = x;
					int x2 = Mathf.Min(width - 1, x + 1);

					float weightedSum =
						read[y0 * width + x0] * 1f + read[y0 * width + x1] * 2f + read[y0 * width + x2] * 1f +
						read[y1 * width + x0] * 2f + read[y1 * width + x1] * 4f + read[y1 * width + x2] * 2f +
						read[y2 * width + x0] * 1f + read[y2 * width + x1] * 2f + read[y2 * width + x2] * 1f;

					write[y * width + x] = weightedSum / 16f;
				}
			}

		(read, write) = (write, read);
		}

		return read;
	}

	private int BuildTextureSeed(PlanetData planetData, int salt)
	{
		int mountains = Mathf.RoundToInt(Mathf.Clamp(planetData.MountainIntensity, 0f, 1f) * 1000f);
		int polar = Mathf.RoundToInt(Mathf.Clamp(planetData.PolarCoverage, 0f, 1f) * 1000f);
		int desert = Mathf.RoundToInt(Mathf.Clamp(planetData.DesertRatio, 0f, 1f) * 1000f);
		int tectonic = _generationProfile.TectonicPlateCount;
		int windCells = _generationProfile.WindCellCount;
		int erosionIterations = _generationProfile.ErosionIterations;
		int erosionStrength = Mathf.RoundToInt(_generationProfile.ErosionStrength * 1000f);
		int heatFactor = Mathf.RoundToInt(_generationProfile.HeatFactor);
		int continental = Mathf.RoundToInt(_generationProfile.ContinentalFrequency * 1000f);

		return ((int)planetData.Element + 1) * 92821
			+ mountains * 911
			+ polar * 1013
			+ desert * 1231
			+ tectonic * 53
			+ windCells * 79
			+ erosionIterations * 97
			+ erosionStrength * 13
			+ heatFactor * 7
			+ continental * 19
			+ salt;
	}

	private static void ResolveSurfacePalette(PlanetElement element, out Color landColor, out Color oceanColor)
	{
		switch (element)
		{
			case PlanetElement.Pyro:
				landColor = new Color(0.62f, 0.22f, 0.12f, 1f);
				oceanColor = new Color(0.25f, 0.06f, 0.07f, 1f);
				break;
			case PlanetElement.Cryo:
				landColor = new Color(0.75f, 0.86f, 0.95f, 1f);
				oceanColor = new Color(0.41f, 0.57f, 0.76f, 1f);
				break;
			case PlanetElement.Aero:
				landColor = new Color(0.68f, 0.75f, 0.86f, 1f);
				oceanColor = new Color(0.35f, 0.48f, 0.68f, 1f);
				break;
			case PlanetElement.Terra:
			default:
				landColor = new Color(0.3f, 0.52f, 0.31f, 1f);
				oceanColor = new Color(0.08f, 0.26f, 0.56f, 1f);
				break;
		}
	}

	private static Color ResolveAuraColor(PlanetElement element, float temperature)
	{
		float temp = Mathf.Clamp(temperature, 0f, 1f);
		return element switch
		{
			PlanetElement.Terra => new Color(0.32f + temp * 0.2f, 0.64f, 0.52f),
			PlanetElement.Pyro => new Color(0.8f, 0.24f + temp * 0.2f, 0.12f),
			PlanetElement.Cryo => new Color(0.48f, 0.74f, 0.92f),
			PlanetElement.Aero => new Color(0.58f, 0.66f + temp * 0.1f, 0.9f),
			_ => new Color(0.52f, 0.68f, 0.82f)
		};
	}

	private static StandardMaterial3D BuildPlanetMaterial()
	{
		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
			EmissionEnabled = true,
			Emission = new Color(0.04f, 0.06f, 0.1f),
			EmissionEnergyMultiplier = 0.07f,
			Roughness = 0.64f,
			Metallic = 0.02f
		};
	}

	private static StandardMaterial3D BuildCloudMaterial()
	{
		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			EmissionEnabled = true,
			Emission = new Color(0.09f, 0.12f, 0.17f),
			EmissionEnergyMultiplier = 0.04f,
			Roughness = 0.14f,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled
		};
	}

	private static StandardMaterial3D BuildAuxPlanetMaterial(PlanetElement element)
	{
		ResolveSurfacePalette(element, out Color landColor, out Color oceanColor);
		Color albedo = landColor.Lerp(oceanColor, 0.35f);
		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
			AlbedoColor = albedo,
			EmissionEnabled = true,
			Emission = albedo.Darkened(0.25f),
			EmissionEnergyMultiplier = 0.08f,
			Roughness = 0.74f,
			Metallic = 0.02f
		};
	}

	private static StandardMaterial3D BuildMoonMaterial()
	{
		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
			AlbedoColor = new Color(0.72f, 0.72f, 0.74f),
			EmissionEnabled = true,
			Emission = new Color(0.18f, 0.18f, 0.2f),
			EmissionEnergyMultiplier = 0.06f,
			Roughness = 0.84f,
			Metallic = 0.0f
		};
	}

	private static ArrayMesh BuildOrbitLineMesh(float radius, int segments)
	{
		var surfaceTool = new SurfaceTool();
		surfaceTool.Begin(Mesh.PrimitiveType.LineStrip);

		int segmentCount = Mathf.Max(64, segments);
		for (int i = 0; i <= segmentCount; i++)
		{
			float t = (float)i / segmentCount;
			float angle = t * Mathf.Tau;
			surfaceTool.AddVertex(new Vector3(
				Mathf.Cos(angle) * radius,
				0f,
				Mathf.Sin(angle) * radius));
		}

		return surfaceTool.Commit();
	}

	private static Node3D BuildOrbitNode(
		string name,
		float radius,
		int segments,
		Color coreColor,
		Color glowColor,
		float thicknessStep)
	{
		var orbitNode = new Node3D { Name = name };

		orbitNode.AddChild(BuildOrbitLayer(radius, segments, coreColor, 1.0f, false, 0.95f));
		orbitNode.AddChild(BuildOrbitLayer(radius - thicknessStep, segments, glowColor, 0.85f, false, 0.7f));
		orbitNode.AddChild(BuildOrbitLayer(radius + thicknessStep, segments, glowColor, 0.85f, false, 0.7f));

		return orbitNode;
	}

	private static MeshInstance3D BuildOrbitLayer(
		float radius,
		int segments,
		Color color,
		float alphaScale,
		bool noDepthTest,
		float emissionEnergy)
	{
		float safeRadius = Mathf.Max(0.1f, radius);
		Color layerColor = new Color(color.R, color.G, color.B, Mathf.Clamp(color.A * alphaScale, 0f, 1f));

		return new MeshInstance3D
		{
			Mesh = BuildOrbitLineMesh(safeRadius, segments),
			MaterialOverride = BuildOrbitMaterial(layerColor, noDepthTest, emissionEnergy),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};
	}

	private static StandardMaterial3D BuildOrbitMaterial(Color color, bool noDepthTest, float emissionEnergyMultiplier)
	{
		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			AlbedoColor = color,
			EmissionEnabled = true,
			Emission = color,
			EmissionEnergyMultiplier = emissionEnergyMultiplier,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			NoDepthTest = noDepthTest
		};
	}

	private static StandardMaterial3D BuildSunCoreMaterial()
	{
		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = new Color(1.12f, 0.9f, 0.68f),
			EmissionEnabled = true,
			Emission = new Color(1.2f, 0.68f, 0.24f),
			EmissionEnergyMultiplier = 5.8f,
			Roughness = 0.0f,
			Metallic = 0.0f
		};
	}

	private static Material BuildSunGlowMaterial()
	{
		Shader shader = GD.Load<Shader>("res://Shaders/SunCorona.gdshader");
		if (shader != null)
		{
			var shaderMaterial = new ShaderMaterial
			{
				Shader = shader
			};

			shaderMaterial.SetShaderParameter("intensity", 6.6f);
			shaderMaterial.SetShaderParameter("rim_power", 2.7f);
			shaderMaterial.SetShaderParameter("jet_strength", 1.15f);
			shaderMaterial.SetShaderParameter("jet_speed", 2.0f);
			shaderMaterial.SetShaderParameter("jet_scale", 10.5f);
			shaderMaterial.SetShaderParameter("noise_scale", 7.8f);
			shaderMaterial.SetShaderParameter("distance_boost", 1.25f);
			return shaderMaterial;
		}

		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			AlbedoColor = new Color(1f, 0.58f, 0.18f, 0.08f),
			EmissionEnabled = true,
			Emission = new Color(1f, 0.4f, 0.1f),
			EmissionEnergyMultiplier = 1.2f,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			NoDepthTest = true
		};
	}

	private static StandardMaterial3D BuildStarShellMaterial()
	{
		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			CullMode = BaseMaterial3D.CullModeEnum.Front,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			AlbedoColor = new Color(0.05f, 0.08f, 0.15f, 0.22f),
			EmissionEnabled = true,
			Emission = new Color(0.08f, 0.16f, 0.3f),
			EmissionEnergyMultiplier = 0.12f,
			NoDepthTest = false
		};
	}


}
