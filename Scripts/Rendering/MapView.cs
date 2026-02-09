using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using FantasyMapGenerator.Scripts.Core;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Map;
using FantasyMapGenerator.Scripts.Map.Heightmap;

namespace FantasyMapGenerator.Scripts.Rendering;

/// <summary>
/// 地图视图节点，负责渲染地图
/// </summary>
public partial class MapView : Control
{
	public enum MapLayer
	{
		Texture,
		Heightmap,
		Biomes,
		Cells,
		Grid,
		Coordinates,
		Compass,
		Rivers,
		Relief,
		Religions,
		Cultures,
		States,
		Provinces,
		Zones,
		Borders,
		Routes,
		Temperature,
		Population,
		Ice,
		Precipitation,
		Emblems,
		BurgIcons,
		Labels,
		Military,
		Markers,
		Rulers,
		ScaleBar,
		Vignette
	}

	public enum EditableElement
	{
		None,
		Biomes,
		Burgs,
		Cultures,
		Diplomacy,
		Emblems,
		Heightmap,
		Markers,
		Military,
		Namesbase,
		Notes,
		Provinces,
		Religions,
		Rivers,
		Routes,
		States,
		Units,
		Zones
	}

	public enum LayerPreset
	{
		Political,
		Cultural,
		Religions,
		Provinces,
		Biomes,
		Heightmap,
		Physical,
		Poi,
		Military,
		Emblems,
		Landmass,
		Custom
	}

	[Export]
	public int CellCount
	{
		get => _cellCount;
		set
		{
			_cellCount = value;
			if (_mapGenerator != null && AutoRegenerate)
			{
				GenerateMap();
				QueueRedraw();
			}
		}
	}

	[Export]
	public bool AutoRegenerate { get; set; } = true;

	[Export]
	public bool ShowRivers
	{
		get => _layerRivers;
		set
		{
			_layerRivers = value;
			QueueRedraw();
		}
	}

	[Export]
	public bool ShowOceanLayers
	{
		get => _showOceanLayers;
		set
		{
			_showOceanLayers = value;
			QueueRedraw();
		}
	}

	[Export]
	public bool UseBiomeColors
	{
		get => _layerBiomes;
		set
		{
			_layerBiomes = value;
			QueueRedraw();
		}
	}

	[Export]
	public bool ShowTerrainLayer
	{
		get => _layerHeightmap;
		set
		{
			_layerHeightmap = value;
			QueueRedraw();
		}
	}

	[Export]
	public bool ShowNames
	{
		get => _layerLabels;
		set
		{
			_layerLabels = value;
			QueueRedraw();
		}
	}

	public enum TerrainStyle
	{
		Heightmap = 0,
		Contour = 1,
		Heatmap = 2
	}

	[Export]
	public MapVisualStyleSelection VisualStyleMode
	{
		get => _visualStyleMode;
		set
		{
			if (_visualStyleMode == value)
			{
				return;
			}

			_visualStyleMode = value;
			ApplyVisualStyle(value);
		}
	}

	[Export]
	public TerrainStyle TerrainStyleMode
	{
		get => _terrainStyleMode;
		set
		{
			_terrainStyleMode = value;
			QueueRedraw();
		}
	}

	[Export]
	public float GenerationWaterLevel
	{
		get => _generationWaterLevel;
		set
		{
			_generationWaterLevel = Mathf.Clamp(value, 0.05f, 0.95f);
			if (_mapGenerator != null)
			{
				_mapGenerator.WaterLevel = _generationWaterLevel;
			}
		}
	}

	[Export]
	public bool UseTemplateGeneration
	{
		get => _useTemplateGeneration;
		set
		{
			_useTemplateGeneration = value;
			if (_mapGenerator != null)
			{
				_mapGenerator.UseTemplate = _useTemplateGeneration;
			}
		}
	}

	[Export]
	public bool UseRandomTemplateGeneration
	{
		get => _useRandomTemplateGeneration;
		set
		{
			_useRandomTemplateGeneration = value;
			if (_mapGenerator != null)
			{
				_mapGenerator.RandomTemplate = _useRandomTemplateGeneration;
			}
		}
	}

	[Export]
	public HeightmapTemplateType GenerationTemplateType
	{
		get => _generationTemplateType;
		set
		{
			_generationTemplateType = value;
			if (_mapGenerator != null)
			{
				_mapGenerator.TemplateType = _generationTemplateType;
			}
		}
	}

	[Export]
	public bool ShowCountries
	{
		get => _layerStates;
		set
		{
			_layerStates = value;
			QueueRedraw();
		}
	}

	[Export]
	public bool ShowCountryBorders
	{
		get => _layerBorders;
		set
		{
			_layerBorders = value;
			QueueRedraw();
		}
	}

	[Export]
	public int CountryCount
	{
		get => _countryCount;
		set
		{
			_countryCount = Mathf.Clamp(value, 1, 128);
			if (_mapGenerator?.Data?.Cells != null)
			{
				GenerateCountries();
				QueueRedraw();
			}
		}
	}

	[Export]
	public int MinCountryCells
	{
		get => _minCountryCells;
		set
		{
			_minCountryCells = Mathf.Clamp(value, 1, 2048);
			if (_mapGenerator?.Data?.Cells != null)
			{
				GenerateCountries();
				QueueRedraw();
			}
		}
	}

	[Export]
	public float CountryBorderWidth
	{
		get => _countryBorderWidth;
		set
		{
			_countryBorderWidth = Mathf.Max(0.5f, value);
			QueueRedraw();
		}
	}

	[Export]
	public float CountryFillAlpha
	{
		get => _countryFillAlpha;
		set
		{
			_countryFillAlpha = Mathf.Clamp(value, 0.1f, 1.0f);
			QueueRedraw();
		}
	}

	[Export]
	public Color CountryBorderColor { get; set; } = new Color(0.12f, 0.1f, 0.08f, 0.9f);

	public int MapWidth
	{
		get => _mapWidth;
		set
		{
			_mapWidth = Mathf.Clamp(value, 128, 4096);
			_canvasSize = new Vector2(_mapWidth, _canvasSize.Y);
			if (_mapGenerator != null)
			{
				_mapGenerator.MapWidth = _mapWidth;
				if (AutoRegenerate)
				{
					GenerateMap();
				}
			}
			QueueRedraw();
		}
	}

	public int MapHeight
	{
		get => _mapHeight;
		set
		{
			_mapHeight = Mathf.Clamp(value, 128, 4096);
			_canvasSize = new Vector2(_canvasSize.X, _mapHeight);
			if (_mapGenerator != null)
			{
				_mapGenerator.MapHeight = _mapHeight;
				if (AutoRegenerate)
				{
					GenerateMap();
				}
			}
			QueueRedraw();
		}
	}

	[Export]
	public float RiverDensity
	{
		get => _riverDensity;
		set
		{
			_riverDensity = Mathf.Clamp(value, 0.25f, 3f);
			if (_mapGenerator != null)
			{
				_mapGenerator.RiverDensity = _riverDensity;
				if (AutoRegenerate)
				{
					GenerateMap();
					QueueRedraw();
				}
			}
		}
	}

	[Export]
	public float ViewScale
	{
		get => _viewScale;
		set
		{
			_viewScale = ClampViewScale(value);
			QueueRedraw();
		}
	}

	[Export]
	public bool UseMultithreading
	{
		get => _useMultithreading;
		set
		{
			_useMultithreading = value;
			if (_mapGenerator != null)
			{
				_mapGenerator.UseMultithreading = _useMultithreading;
			}
		}
	}

	[Export]
	public float BoundaryPaddingScale
	{
		get => _boundaryPaddingScale;
		set
		{
			_boundaryPaddingScale = Mathf.Clamp(value, 0.5f, 4f);
			if (_mapGenerator != null)
			{
				_mapGenerator.BoundaryPaddingScale = _boundaryPaddingScale;
				if (AutoRegenerate)
				{
					GenerateMap();
					QueueRedraw();
				}
			}
		}
	}

	[Export]
	public float BoundaryStepScale
	{
		get => _boundaryStepScale;
		set
		{
			_boundaryStepScale = Mathf.Clamp(value, 0.2f, 2f);
			if (_mapGenerator != null)
			{
				_mapGenerator.BoundaryStepScale = _boundaryStepScale;
				if (AutoRegenerate)
				{
					GenerateMap();
					QueueRedraw();
				}
			}
		}
	}

	[Export]
	public int BurgsPerCountryMin
	{
		get => _burgsPerCountryMin;
		set
		{
			_burgsPerCountryMin = Mathf.Clamp(value, 1, 6);
			if (_burgsPerCountryMax < _burgsPerCountryMin)
			{
				_burgsPerCountryMax = _burgsPerCountryMin;
			}
			if (_mapGenerator?.Data?.Cells != null)
			{
				GenerateBurgs();
				GenerateRoutes();
				QueueRedraw();
			}
		}
	}

	[Export]
	public int BurgsPerCountryMax
	{
		get => _burgsPerCountryMax;
		set
		{
			_burgsPerCountryMax = Mathf.Clamp(value, 1, 8);
			if (_burgsPerCountryMax < _burgsPerCountryMin)
			{
				_burgsPerCountryMin = _burgsPerCountryMax;
			}
			if (_mapGenerator?.Data?.Cells != null)
			{
				GenerateBurgs();
				GenerateRoutes();
				QueueRedraw();
			}
		}
	}

	[Export]
	public float RouteExtraConnectionChance
	{
		get => _routeExtraConnectionChance;
		set
		{
			_routeExtraConnectionChance = Mathf.Clamp(value, 0f, 1f);
			if (_mapGenerator?.Data?.Cells != null)
			{
				GenerateRoutes();
				QueueRedraw();
			}
		}
	}

	[Export]
	public float RouteExtraConnectionScale
	{
		get => _routeExtraConnectionScale;
		set
		{
			_routeExtraConnectionScale = Mathf.Clamp(value, 1.1f, 3f);
			if (_mapGenerator?.Data?.Cells != null)
			{
				GenerateRoutes();
				QueueRedraw();
			}
		}
	}

	[Export]
	public float RoutePrimaryWidth
	{
		get => _routePrimaryWidth;
		set
		{
			_routePrimaryWidth = Mathf.Clamp(value, 0.6f, 5f);
			QueueRedraw();
		}
	}

	[Export]
	public float RouteSecondaryWidth
	{
		get => _routeSecondaryWidth;
		set
		{
			_routeSecondaryWidth = Mathf.Clamp(value, 0.4f, 4f);
			QueueRedraw();
		}
	}

	[Export]
	public float RouteSlopeWeight
	{
		get => _routeSlopeWeight;
		set
		{
			_routeSlopeWeight = Mathf.Clamp(value, 1f, 12f);
			if (_mapGenerator?.Data?.Cells != null)
			{
				GenerateRoutes();
				QueueRedraw();
			}
		}
	}

	[Export]
	public float RouteElevationWeight
	{
		get => _routeElevationWeight;
		set
		{
			_routeElevationWeight = Mathf.Clamp(value, 1f, 12f);
			if (_mapGenerator?.Data?.Cells != null)
			{
				GenerateRoutes();
				QueueRedraw();
			}
		}
	}

	[Export]
	public float RouteWaterPenalty
	{
		get => _routeWaterPenalty;
		set
		{
			_routeWaterPenalty = Mathf.Clamp(value, 50f, 1500f);
			if (_mapGenerator?.Data?.Cells != null)
			{
				GenerateRoutes();
				QueueRedraw();
			}
		}
	}

	[Export]
	public int RouteBridgeFluxThreshold
	{
		get => _routeBridgeFluxThreshold;
		set
		{
			_routeBridgeFluxThreshold = Mathf.Clamp(value, 1, 500);
			if (_mapGenerator?.Data?.Cells != null)
			{
				GenerateRoutes();
				QueueRedraw();
			}
		}
	}

	[Export]
	public float RouteBridgePenaltyMultiplier
	{
		get => _routeBridgePenaltyMultiplier;
		set
		{
			_routeBridgePenaltyMultiplier = Mathf.Clamp(value, 0.6f, 1.2f);
			if (_mapGenerator?.Data?.Cells != null)
			{
				GenerateRoutes();
				QueueRedraw();
			}
		}
	}

	// 颜色常量
	private static readonly Color OceanLayerColor = new Color(0.2f, 0.5f, 0.8f);
	private static readonly Color RiverColor = new Color(0.3f, 0.6f, 0.9f);
	private static readonly Color RoutePrimaryColor = new Color(0.45f, 0.28f, 0.16f, 0.7f);
	private static readonly Color RouteSecondaryColor = new Color(0.6f, 0.42f, 0.26f, 0.55f);
	private static readonly Color HeatmapLowColor = new Color(0.12f, 0.25f, 0.85f);
	private static readonly Color HeatmapHighColor = new Color(0.92f, 0.25f, 0.15f);
	private static readonly Color NameColor = new Color(0.12f, 0.1f, 0.08f, 0.85f);
	private static readonly Color CellOutlineColor = new Color(0.12f, 0.1f, 0.08f, 0.2f);
	private static readonly Color GridLineColor = new Color(0.12f, 0.1f, 0.08f, 0.18f);
	private static readonly Color CoordinateTextColor = new Color(0.12f, 0.1f, 0.08f, 0.65f);
	private static readonly Color CompassColor = new Color(0.12f, 0.1f, 0.08f, 0.85f);
	private static readonly Color ScaleBarColor = new Color(0.12f, 0.1f, 0.08f, 0.9f);
	private static readonly Color VignetteColor = new Color(0, 0, 0, 0.08f);
	private const float RouteRiverCrossPenalty = 1.18f;
	private const float RouteRiverValleyBonus = 0.92f;
	private const float RouteTurnDotThreshold = 0.995f;
	private const float SharedVertexEpsilon = 0.01f;
	private const string ControlsConfigPath = "user://settings.cfg";
	private const string ControlsConfigSection = "controls";
	private const float DefaultZoomStep = 0.05f;
	private const float DefaultFineZoomStep = 0.01f;
	private const float DefaultCoarseZoomStep = 0.1f;
	private const float DefaultMoveSpeed = 240f;
	private const float ZoomOutMinFactor = 0.2f;
	private const float ZoomOutMaxFactor = 2.0f;
	private const float ZoomOutCurvePower = 1.6f;
	private static readonly MapLayer[] DefaultLayerOrder =
	{
		MapLayer.Texture,
		MapLayer.Heightmap,
		MapLayer.Biomes,
		MapLayer.Cells,
		MapLayer.Grid,
		MapLayer.Coordinates,
		MapLayer.Compass,
		MapLayer.Rivers,
		MapLayer.Relief,
		MapLayer.Religions,
		MapLayer.Cultures,
		MapLayer.States,
		MapLayer.Provinces,
		MapLayer.Zones,
		MapLayer.Borders,
		MapLayer.Routes,
		MapLayer.Temperature,
		MapLayer.Population,
		MapLayer.Ice,
		MapLayer.Precipitation,
		MapLayer.Emblems,
		MapLayer.BurgIcons,
		MapLayer.Labels,
		MapLayer.Military,
		MapLayer.Markers,
		MapLayer.Rulers,
		MapLayer.ScaleBar,
		MapLayer.Vignette
	};

	private int _cellCount = 2000;
	private int _mapWidth = 512;
	private int _mapHeight = 512;
	private float _riverDensity = 1f;
	private static MapGenerator _cachedMapGenerator;
	private static bool _suppressGenerateOnReadyOnce;
	private MapGenerator _mapGenerator;
	private bool _isGenerating = false;
	private bool _useMultithreading = true;
	private float _viewScale = 1f;
	private Vector2 _canvasSize = new Vector2(512, 512);
	private Vector2 _cameraMapOffset = Vector2.Zero;
	private bool _showOceanLayers = true;
	private MapVisualStyleSelection _visualStyleMode = MapVisualStyleSelection.InkFantasy;
	private TerrainStyle _terrainStyleMode = TerrainStyle.Heightmap;
	private float _generationWaterLevel = 0.35f;
	private bool _useTemplateGeneration = true;
	private bool _useRandomTemplateGeneration = false;
	private HeightmapTemplateType _generationTemplateType = HeightmapTemplateType.Continents;
	private float[] _genesisTerrainHeightmap;
	private int _genesisTerrainWidth;
	private int _genesisTerrainHeight;
	private bool _useGenesisPlanetTint = false;
	private Color _dynamicOceanColor = OceanLayerColor;
	private Color _dynamicRiverColor = RiverColor;
	private Color _dynamicTerrainLowColor = new Color(0.24f, 0.5f, 0.3f, 0.92f);
	private Color _dynamicTerrainHighColor = new Color(0.75f, 0.82f, 0.62f, 0.96f);
	private float _dynamicTerrainBlend = 0.45f;
	private int _countryCount = 12;
	private int _minCountryCells = 3;
	private float _countryBorderWidth = 2f;
	private float _countryFillAlpha = 1f;
	private float _boundaryPaddingScale = 1.5f;
	private float _boundaryStepScale = 1f;
	private int _burgsPerCountryMin = 1;
	private int _burgsPerCountryMax = 3;
	private float _routeExtraConnectionChance = 0.4f;
	private float _routeExtraConnectionScale = 1.6f;
	private float _routePrimaryWidth = 2.1f;
	private float _routeSecondaryWidth = 1.4f;
	private float _routeSlopeWeight = 7f;
	private float _routeElevationWeight = 7.5f;
	private float _routeWaterPenalty = 800f;
	private int _routeBridgeFluxThreshold = 45;
	private float _routeBridgePenaltyMultiplier = 0.78f;
	private float _zoomStep = DefaultZoomStep;
	private float _fineZoomStep = DefaultFineZoomStep;
	private float _coarseZoomStep = DefaultCoarseZoomStep;
	private bool _enableKeyboardPan = true;
	private bool _scaleMoveSpeedByZoom = true;
	private float _moveSpeed = DefaultMoveSpeed;
	private bool _isMiddleDragging = false;
	private Vector2 _lastPointerCanvasPos = Vector2.Zero;
	private int[] _cellCountryIds;
	private List<Country> _countries = new();
	private readonly List<MapLayer> _layerOrder = new(DefaultLayerOrder);
	private readonly List<RoutePath> _routePaths = new();
	private readonly List<List<int>> _countryBurgCellIds = new();

	private EditableElement _currentEditableElement = EditableElement.None;
	private LayerPreset _layerPresetMode = LayerPreset.Political;

	private bool _layerTexture = false;
	private bool _layerHeightmap = true;
	private bool _layerBiomes = true;
	private bool _layerCells = false;
	private bool _layerGrid = false;
	private bool _layerCoordinates = false;
	private bool _layerCompass = false;
	private bool _layerRivers = true;
	private bool _layerRelief = false;
	private bool _layerReligions = false;
	private bool _layerCultures = false;
	private bool _layerStates = true;
	private bool _layerProvinces = false;
	private bool _layerZones = false;
	private bool _layerBorders = true;
	private bool _layerRoutes = false;
	private bool _layerTemperature = false;
	private bool _layerPopulation = false;
	private bool _layerIce = false;
	private bool _layerPrecipitation = false;
	private bool _layerEmblems = false;
	private bool _layerBurgIcons = false;
	private bool _layerLabels = true;
	private bool _layerMilitary = false;
	private bool _layerMarkers = false;
	private bool _layerRulers = false;
	private bool _layerScaleBar = false;
	private bool _layerVignette = false;

	// 选择状态
	private int _selectedCellId = -1;
	[Signal] public delegate void CellSelectedEventHandler(int cellId);

	[Signal] public delegate void EditableElementChangedEventHandler(EditableElement element);

	[Export]
	public EditableElement CurrentEditableElement
	{
		get => _currentEditableElement;
		set
		{
			if (_currentEditableElement == value)
			{
				return;
			}

			_currentEditableElement = value;
			EmitSignal(SignalName.EditableElementChanged, (int)value);
		}
	}

	[Export]
	public LayerPreset LayerPresetMode
	{
		get => _layerPresetMode;
		set
		{
			if (_layerPresetMode == value)
			{
				return;
			}

			_layerPresetMode = value;
			if (_layerPresetMode != LayerPreset.Custom)
			{
				ApplyLayerPreset(_layerPresetMode);
			}
		}
	}

	[Export] public bool LayerTexture { get => _layerTexture; set { _layerTexture = value; QueueRedraw(); } }
	[Export] public bool LayerHeightmap { get => _layerHeightmap; set { _layerHeightmap = value; QueueRedraw(); } }
	[Export] public bool LayerBiomes { get => _layerBiomes; set { _layerBiomes = value; QueueRedraw(); } }
	[Export] public bool LayerCells { get => _layerCells; set { _layerCells = value; QueueRedraw(); } }
	[Export] public bool LayerGrid { get => _layerGrid; set { _layerGrid = value; QueueRedraw(); } }
	[Export] public bool LayerCoordinates { get => _layerCoordinates; set { _layerCoordinates = value; QueueRedraw(); } }
	[Export] public bool LayerCompass { get => _layerCompass; set { _layerCompass = value; QueueRedraw(); } }
	[Export] public bool LayerRivers { get => _layerRivers; set { _layerRivers = value; QueueRedraw(); } }
	[Export] public bool LayerRelief { get => _layerRelief; set { _layerRelief = value; QueueRedraw(); } }
	[Export] public bool LayerReligions { get => _layerReligions; set { _layerReligions = value; QueueRedraw(); } }
	[Export] public bool LayerCultures { get => _layerCultures; set { _layerCultures = value; QueueRedraw(); } }
	[Export] public bool LayerStates { get => _layerStates; set { _layerStates = value; QueueRedraw(); } }
	[Export] public bool LayerProvinces { get => _layerProvinces; set { _layerProvinces = value; QueueRedraw(); } }
	[Export] public bool LayerZones { get => _layerZones; set { _layerZones = value; QueueRedraw(); } }
	[Export] public bool LayerBorders { get => _layerBorders; set { _layerBorders = value; QueueRedraw(); } }
	[Export] public bool LayerRoutes { get => _layerRoutes; set { _layerRoutes = value; QueueRedraw(); } }
	[Export] public bool LayerTemperature { get => _layerTemperature; set { _layerTemperature = value; QueueRedraw(); } }
	[Export] public bool LayerPopulation { get => _layerPopulation; set { _layerPopulation = value; QueueRedraw(); } }
	[Export] public bool LayerIce { get => _layerIce; set { _layerIce = value; QueueRedraw(); } }
	[Export] public bool LayerPrecipitation { get => _layerPrecipitation; set { _layerPrecipitation = value; QueueRedraw(); } }
	[Export] public bool LayerEmblems { get => _layerEmblems; set { _layerEmblems = value; QueueRedraw(); } }
	[Export] public bool LayerBurgIcons { get => _layerBurgIcons; set { _layerBurgIcons = value; QueueRedraw(); } }
	[Export] public bool LayerLabels { get => _layerLabels; set { _layerLabels = value; QueueRedraw(); } }
	[Export] public bool LayerMilitary { get => _layerMilitary; set { _layerMilitary = value; QueueRedraw(); } }
	[Export] public bool LayerMarkers { get => _layerMarkers; set { _layerMarkers = value; QueueRedraw(); } }
	[Export] public bool LayerRulers { get => _layerRulers; set { _layerRulers = value; QueueRedraw(); } }
	[Export] public bool LayerScaleBar { get => _layerScaleBar; set { _layerScaleBar = value; QueueRedraw(); } }
	[Export] public bool LayerVignette { get => _layerVignette; set { _layerVignette = value; QueueRedraw(); } }

	[Export]
	public Vector2 CanvasSize
	{
		get => _canvasSize;
		set
		{
			_canvasSize = new Vector2(
				Mathf.Clamp(value.X, 128, 4096),
				Mathf.Clamp(value.Y, 128, 4096)
			);
			QueueRedraw();
		}
	}

	[Export]
	public Vector2 CameraMapOffset
	{
		get => _cameraMapOffset;
		set
		{
			_cameraMapOffset = value;
			QueueRedraw();
		}
	}

	public bool HasMapData => _mapGenerator?.Data != null;

	public static void ClearCachedMap()
	{
		_cachedMapGenerator = null;
	}

	public static void SuppressGenerateOnNextReady()
	{
		_suppressGenerateOnReadyOnce = true;
	}

	public override void _Ready()
	{
		SetProcess(true);
		SetProcessInput(true);
		LoadControlSettings();

		bool suppressGenerateOnReady = _suppressGenerateOnReadyOnce;
		_suppressGenerateOnReadyOnce = false;

		if (_cachedMapGenerator?.Data != null)
		{
			_mapGenerator = _cachedMapGenerator;
			_mapWidth = _mapGenerator.MapWidth;
			_mapHeight = _mapGenerator.MapHeight;
			_riverDensity = _mapGenerator.RiverDensity;
			_useMultithreading = _mapGenerator.UseMultithreading;
			_boundaryPaddingScale = _mapGenerator.BoundaryPaddingScale;
			_boundaryStepScale = _mapGenerator.BoundaryStepScale;
			_generationWaterLevel = _mapGenerator.WaterLevel;
			_useTemplateGeneration = _mapGenerator.UseTemplate;
			_useRandomTemplateGeneration = _mapGenerator.RandomTemplate;
			_generationTemplateType = _mapGenerator.TemplateType;
			if (_mapGenerator.CellCount > 0)
			{
				_cellCount = _mapGenerator.CellCount;
			}

			_canvasSize = new Vector2(_mapWidth, _mapHeight);
			ApplyLayerPreset(_layerPresetMode);
			QueueRedraw();
			return;
		}

		_mapGenerator = new MapGenerator();
		_mapGenerator.MapWidth = _mapWidth;
		_mapGenerator.MapHeight = _mapHeight;
		_mapGenerator.RiverDensity = _riverDensity;
		_mapGenerator.UseMultithreading = _useMultithreading;
		_mapGenerator.BoundaryPaddingScale = _boundaryPaddingScale;
		_mapGenerator.BoundaryStepScale = _boundaryStepScale;
		_mapGenerator.WaterLevel = _generationWaterLevel;
		_mapGenerator.UseTemplate = _useTemplateGeneration;
		_mapGenerator.RandomTemplate = _useRandomTemplateGeneration;
		_mapGenerator.TemplateType = _generationTemplateType;
		ApplyGenesisTerrainHeightmapToGenerator();
		_canvasSize = new Vector2(_mapWidth, _mapHeight);
		ApplyLayerPreset(_layerPresetMode);
		ApplyVisualStyle(_visualStyleMode);
		if (!suppressGenerateOnReady)
		{
			GenerateMap();
		}
	}

	public override void _ExitTree()
	{
		if (_mapGenerator?.Data != null)
		{
			_cachedMapGenerator = _mapGenerator;
		}
	}

	public void GenerateMap()
	{
		if (_isGenerating) return;
		_isGenerating = true;
		_selectedCellId = -1;
		SyncGeneratorSettings();

		_mapGenerator.GenerateWithNewSeed(_cellCount);
		GenerateCountries();
		QueueRedraw();

		_isGenerating = false;
	}

	public void GenerateMapWithSeed(string seed)
	{
		if (_isGenerating) return;
		_isGenerating = true;
		_selectedCellId = -1;
		SyncGeneratorSettings();

		_mapGenerator.Generate(seed, _cellCount);
		GenerateCountries();
		QueueRedraw();

		_isGenerating = false;
	}

	public void GenerateMapWithSeed(int seed)
	{
		GenerateMapWithSeed(seed.ToString());
	}

	public async Task GenerateMapForContextAsync(MapContext context, Action<float> progressCallback = null)
	{
		if (context == null)
		{
			return;
		}

		if (context.ParentMapData != null && context.ParentCellId.HasValue)
		{
			progressCallback?.Invoke(0f);
			GenerateChildMapFromParent(context);
			progressCallback?.Invoke(1f);
			return;
		}

		SetCellCountWithoutRegenerate(context.CellCount);
		await GenerateMapWithSeedAsync(context.Seed, _cellCount, progressCallback);
	}

	public void GenerateMapForContext(MapContext context)
	{
		if (context == null)
		{
			return;
		}

		if (context.ParentMapData != null && context.ParentCellId.HasValue)
		{
			GenerateChildMapFromParent(context);
			return;
		}

		SetCellCountWithoutRegenerate(context.CellCount);
		GenerateMapWithSeed(context.Seed);
	}

	public async Task GenerateMapWithSeedAsync(string seed, int cellCount, Action<float> progressCallback = null)
	{
		if (_isGenerating || _mapGenerator == null)
		{
			return;
		}

		_isGenerating = true;
		_selectedCellId = -1;
		SetCellCountWithoutRegenerate(cellCount);
		SyncGeneratorSettings();

		object progressLock = new object();
		float latestProgress = 0f;

		void OnProgress(float value)
		{
			lock (progressLock)
			{
				latestProgress = Mathf.Clamp(value, 0f, 1f);
			}
		}

		_mapGenerator.ProgressChanged += OnProgress;
		progressCallback?.Invoke(0f);

		try
		{
			var generationTask = Task.Run(() => _mapGenerator.Generate(seed, _cellCount));

			while (!generationTask.IsCompleted)
			{
				float currentProgress;
				lock (progressLock)
				{
					currentProgress = latestProgress;
				}

				progressCallback?.Invoke(currentProgress);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			await generationTask;
			progressCallback?.Invoke(1f);
			GenerateCountries();
			QueueRedraw();
		}
		finally
		{
			_mapGenerator.ProgressChanged -= OnProgress;
			_isGenerating = false;
		}
	}

	public void SetCellCountWithoutGeneration(int value)
	{
		SetCellCountWithoutRegenerate(value);
	}

	private void SetCellCountWithoutRegenerate(int value)
	{
		_cellCount = Mathf.Max(1, value);
	}

	public void SetWaterLevel(float level)
	{
		_generationWaterLevel = Mathf.Clamp(level, 0.05f, 0.95f);
		if (_mapGenerator != null)
		{
			_mapGenerator.WaterLevel = _generationWaterLevel;
		}

		if (_mapGenerator?.Data != null)
		{
			var heightmap = _mapGenerator.Data.Heightmap;
			var cells = _mapGenerator.Data.Cells;
			int width = (int)_mapGenerator.Data.MapSize.X;
			int height = (int)_mapGenerator.Data.MapSize.Y;

			var processor = new HeightmapProcessor(_mapGenerator.PRNG);
			processor.WaterLevel = _generationWaterLevel;
			processor.ApplyToCells(cells, heightmap, width, height);
			processor.AssignColors(cells);

			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		if (_mapGenerator?.Data == null) return;

		// Draw a solid base to ensure straight map edges even if boundary cells don't reach the border.
		var mapSize = GetMapSize();
		var topLeft = TransformToScreenCoordinates(Vector2.Zero);
		var bottomRight = TransformToScreenCoordinates(mapSize);
		var baseRect = new Rect2(topLeft, bottomRight - topLeft);
		if (_visualStyleMode == MapVisualStyleSelection.Parchment)
		{
			DrawParchmentBackground(baseRect);
		}
		else
		{
			DrawRect(baseRect, _dynamicOceanColor, true);
		}

		foreach (var layer in _layerOrder)
		{
			if (!IsLayerEnabled(layer))
			{
				continue;
			}

			switch (layer)
			{
				case MapLayer.Texture:
					break;
				case MapLayer.Heightmap:
					DrawCellPolygons(GetTerrainColor);
					if (ShowOceanLayers)
					{
						DrawOceanLayers();
					}
					break;
				case MapLayer.Biomes:
					DrawCellPolygons(GetBiomeColor);
					break;
				case MapLayer.Cells:
					DrawCellStructure();
					break;
				case MapLayer.Grid:
					DrawGrid();
					break;
				case MapLayer.Coordinates:
					DrawCoordinates();
					break;
				case MapLayer.Compass:
					DrawCompass();
					break;
				case MapLayer.Rivers:
					DrawRivers();
					break;
				case MapLayer.Relief:
					DrawReliefMarkers();
					break;
				case MapLayer.Religions:
					DrawCellPolygons(GetReligionColor);
					break;
				case MapLayer.Cultures:
					DrawCellPolygons(GetCultureColor);
					break;
				case MapLayer.States:
					DrawCellPolygons(GetCountryColor);
					break;
				case MapLayer.Provinces:
					DrawCellPolygons(GetProvinceColor);
					break;
				case MapLayer.Zones:
					DrawCellPolygons(GetZoneColor);
					break;
				case MapLayer.Borders:
					DrawCountryBorders();
					break;
				case MapLayer.Routes:
					DrawRoutes();
					break;
				case MapLayer.Temperature:
					DrawCellPolygons(GetTemperatureColor);
					break;
				case MapLayer.Population:
					DrawCellPolygons(GetPopulationColor);
					break;
				case MapLayer.Ice:
					DrawIceOverlay();
					break;
				case MapLayer.Precipitation:
					DrawCellPolygons(GetPrecipitationColor);
					break;
				case MapLayer.Emblems:
					DrawEmblems();
					break;
				case MapLayer.BurgIcons:
					DrawBurgIcons();
					break;
				case MapLayer.Labels:
					DrawCountryNames();
					break;
				case MapLayer.Military:
					DrawMilitary();
					break;
				case MapLayer.Markers:
					DrawMarkers();
					break;
				case MapLayer.Rulers:
					DrawRulers();
					break;
				case MapLayer.ScaleBar:
					DrawScaleBar();
					break;
				case MapLayer.Vignette:
					DrawVignette();
					break;
			}
		}

		if (_visualStyleMode == MapVisualStyleSelection.Parchment)
		{
			DrawParchmentPostEffects(baseRect);
		}
	}

	private void DrawParchmentBackground(Rect2 mapRect)
	{
		var light = new Color(0.96f, 0.88f, 0.68f, 1f);
		var dark = new Color(0.78f, 0.65f, 0.42f, 1f);

		const int bands = 28;
		for (int i = 0; i < bands; i++)
		{
			float y0 = (float)i / bands;
			float y1 = (float)(i + 1) / bands;
			float tintNoise = Hash01(i, _cellCount, 71) - 0.5f;
			float blend = Mathf.Clamp(y0 * 0.78f + tintNoise * 0.16f, 0f, 1f);
			var bandColor = LerpColor(light, dark, blend);
			var bandRect = new Rect2(
				mapRect.Position.X,
				mapRect.Position.Y + mapRect.Size.Y * y0,
				mapRect.Size.X,
				mapRect.Size.Y * (y1 - y0));
			DrawRect(bandRect, bandColor, true);
		}

		const int blotchCols = 14;
		const int blotchRows = 10;
		float blotchWidth = mapRect.Size.X / blotchCols;
		float blotchHeight = mapRect.Size.Y / blotchRows;
		for (int row = 0; row < blotchRows; row++)
		{
			for (int col = 0; col < blotchCols; col++)
			{
				float n = Hash01(col, row, 113);
				float n2 = Hash01(col, row, 127);
				float alpha = 0.03f + n * 0.06f;
				var tone = new Color(0.52f + n2 * 0.18f, 0.43f + n2 * 0.15f, 0.28f + n2 * 0.1f, alpha);

				var blotchRect = new Rect2(
					mapRect.Position.X + col * blotchWidth + (n - 0.5f) * blotchWidth * 0.35f,
					mapRect.Position.Y + row * blotchHeight + (n2 - 0.5f) * blotchHeight * 0.35f,
					blotchWidth * (0.62f + n * 0.35f),
					blotchHeight * (0.62f + n2 * 0.35f));
				DrawRect(blotchRect, tone, true);
			}
		}

		int fiberCount = Mathf.Clamp((int)(mapRect.Size.X * mapRect.Size.Y / 4200f), 120, 360);
		for (int i = 0; i < fiberCount; i++)
		{
			float px = mapRect.Position.X + Hash01(i, fiberCount, 211) * mapRect.Size.X;
			float py = mapRect.Position.Y + Hash01(i, fiberCount, 223) * mapRect.Size.Y;
			float angle = (Hash01(i, fiberCount, 239) - 0.5f) * 0.9f;
			float length = 4f + Hash01(i, fiberCount, 251) * 10f;
			var start = new Vector2(px, py);
			var end = start + new Vector2(length, 0f).Rotated(angle);
			DrawLine(start, end, new Color(0.29f, 0.22f, 0.14f, 0.08f), 1f);
		}
	}

	private void DrawParchmentPostEffects(Rect2 mapRect)
	{
		DrawParchmentCoastlineInk();
		DrawParchmentDecorations(mapRect);
		DrawParchmentFrame(mapRect);
	}

	private void DrawParchmentDecorations(Rect2 mapRect)
	{
		DrawParchmentLatitudeGuides(mapRect);
		DrawParchmentCompassRose(mapRect);
		DrawParchmentCornerCurls(mapRect);
	}

	private void DrawParchmentLatitudeGuides(Rect2 mapRect)
	{
		const int guideCount = 4;
		const int segments = 30;
		for (int guide = 1; guide <= guideCount; guide++)
		{
			float t = guide / (guideCount + 1f);
			float baseY = mapRect.Position.Y + mapRect.Size.Y * t;
			float amplitude = 2f + guide * 0.45f;
			for (int segment = 0; segment < segments; segment++)
			{
				if ((segment & 1) != 0)
				{
					continue;
				}

				float x0t = (float)segment / segments;
				float x1t = (float)(segment + 1) / segments;
				float x0 = mapRect.Position.X + mapRect.Size.X * x0t;
				float x1 = mapRect.Position.X + mapRect.Size.X * x1t;
				float y0 = baseY + Mathf.Sin(x0t * Mathf.Pi * 2f + guide * 0.7f) * amplitude;
				float y1 = baseY + Mathf.Sin(x1t * Mathf.Pi * 2f + guide * 0.7f) * amplitude;
				DrawLine(new Vector2(x0, y0), new Vector2(x1, y1), new Color(0.22f, 0.16f, 0.1f, 0.11f), 1f);
			}
		}
	}

	private void DrawParchmentCompassRose(Rect2 mapRect)
	{
		float radiusOuter = Mathf.Clamp(Mathf.Min(mapRect.Size.X, mapRect.Size.Y) * 0.055f, 22f, 34f);
		float margin = radiusOuter + 20f;
		var center = new Vector2(mapRect.End.X - margin, mapRect.Position.Y + margin);

		if (!mapRect.HasPoint(center))
		{
			return;
		}

		const int rays = 16;
		for (int i = 0; i < rays; i++)
		{
			float angle = i / (float)rays * Mathf.Pi * 2f - Mathf.Pi * 0.5f;
			var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			float length = (i % 2 == 0) ? radiusOuter : radiusOuter * 0.58f;
			float width = (i % 4 == 0) ? 2.2f : 1.2f;
			DrawLine(center, center + dir * length, new Color(0.16f, 0.11f, 0.07f, 0.78f), width);
		}

		DrawCircle(center, radiusOuter + 3f, new Color(0.19f, 0.13f, 0.08f, 0.55f), false, 1.2f);
		DrawCircle(center, radiusOuter * 0.55f, new Color(0.19f, 0.13f, 0.08f, 0.45f), false, 1f);

		var font = GetThemeFont("font", "Label");
		if (font != null)
		{
			int fontSize = Mathf.Max(12, GetThemeFontSize("font_size", "Label") - 1);
			var northPos = center + new Vector2(0f, -radiusOuter - 5f);
			DrawString(font, northPos, "N", HorizontalAlignment.Center, -1, fontSize, new Color(0.16f, 0.11f, 0.07f, 0.9f));
		}
	}

	private void DrawParchmentCornerCurls(Rect2 mapRect)
	{
		float curl = Mathf.Clamp(Mathf.Min(mapRect.Size.X, mapRect.Size.Y) * 0.09f, 26f, 46f);
		var shadow = new Color(0.18f, 0.12f, 0.07f, 0.09f);
		var highlight = new Color(0.96f, 0.9f, 0.74f, 0.32f);

		DrawPolygon(
			new[]
			{
				mapRect.Position,
				mapRect.Position + new Vector2(curl, 0f),
				mapRect.Position + new Vector2(0f, curl)
			},
			new[] { shadow, shadow, shadow });

		DrawPolygon(
			new[]
			{
				new Vector2(mapRect.End.X, mapRect.Position.Y),
				new Vector2(mapRect.End.X - curl, mapRect.Position.Y),
				new Vector2(mapRect.End.X, mapRect.Position.Y + curl)
			},
			new[] { shadow, shadow, shadow });

		DrawPolygon(
			new[]
			{
				new Vector2(mapRect.Position.X, mapRect.End.Y),
				new Vector2(mapRect.Position.X + curl, mapRect.End.Y),
				new Vector2(mapRect.Position.X, mapRect.End.Y - curl)
			},
			new[] { shadow, shadow, shadow });

		DrawPolygon(
			new[]
			{
				mapRect.End,
				new Vector2(mapRect.End.X - curl, mapRect.End.Y),
				new Vector2(mapRect.End.X, mapRect.End.Y - curl)
			},
			new[] { shadow, shadow, shadow });

		DrawLine(mapRect.Position + new Vector2(2f, curl * 0.68f), mapRect.Position + new Vector2(curl * 0.68f, 2f), highlight, 1.4f);
		DrawLine(new Vector2(mapRect.End.X - curl * 0.68f, mapRect.Position.Y + 2f), new Vector2(mapRect.End.X - 2f, mapRect.Position.Y + curl * 0.68f), highlight, 1.4f);
		DrawLine(new Vector2(mapRect.Position.X + 2f, mapRect.End.Y - curl * 0.68f), new Vector2(mapRect.Position.X + curl * 0.68f, mapRect.End.Y - 2f), highlight, 1.4f);
		DrawLine(new Vector2(mapRect.End.X - curl * 0.68f, mapRect.End.Y - 2f), new Vector2(mapRect.End.X - 2f, mapRect.End.Y - curl * 0.68f), highlight, 1.4f);
	}

	private void DrawParchmentCoastlineInk()
	{
		if (_mapGenerator?.Data?.Cells == null)
		{
			return;
		}

		var cells = _mapGenerator.Data.Cells;
		for (int i = 0; i < cells.Length; i++)
		{
			var cell = cells[i];
			if (cell.Vertices == null || cell.Vertices.Count < 2)
			{
				continue;
			}

			foreach (var neighborId in cell.NeighborIds)
			{
				if (neighborId <= cell.Id || neighborId < 0 || neighborId >= cells.Length)
				{
					continue;
				}

				var neighbor = cells[neighborId];
				if (neighbor.Vertices == null || neighbor.Vertices.Count < 2)
				{
					continue;
				}

				if (cell.IsLand == neighbor.IsLand)
				{
					continue;
				}

				if (!TryGetSharedEdge(cell.Vertices, neighbor.Vertices, out var v1, out var v2))
				{
					continue;
				}

				var p1 = TransformToScreenCoordinates(v1);
				var p2 = TransformToScreenCoordinates(v2);
				DrawLine(p1, p2, new Color(0.15f, 0.1f, 0.06f, 0.9f), 2.2f);
				DrawLine(p1, p2, new Color(0.96f, 0.87f, 0.68f, 0.45f), 0.9f);
			}
		}
	}

	private void DrawParchmentFrame(Rect2 mapRect)
	{
		const int ringCount = 5;
		for (int i = 0; i < ringCount; i++)
		{
			float inset = 2f + i * 2.2f;
			var frameRect = new Rect2(
				mapRect.Position.X + inset,
				mapRect.Position.Y + inset,
				mapRect.Size.X - inset * 2f,
				mapRect.Size.Y - inset * 2f);

			if (frameRect.Size.X <= 2f || frameRect.Size.Y <= 2f)
			{
				continue;
			}

			float alpha = 0.18f - i * 0.028f;
			var frameColor = new Color(0.19f, 0.13f, 0.08f, Mathf.Max(0.03f, alpha));
			DrawRect(frameRect, frameColor, false, 1.2f);
		}

		int stainCount = 8;
		for (int i = 0; i < stainCount; i++)
		{
			float px = mapRect.Position.X + Hash01(i, stainCount, 307) * mapRect.Size.X;
			float py = mapRect.Position.Y + Hash01(i, stainCount, 311) * mapRect.Size.Y;
			float radius = 8f + Hash01(i, stainCount, 313) * 18f;
			DrawCircle(new Vector2(px, py), radius, new Color(0.22f, 0.15f, 0.08f, 0.035f));
		}
	}

	private static float Hash01(int x, int y, int salt)
	{
		unchecked
		{
			uint h = (uint)(x * 374761393 + y * 668265263 + salt * 1274126177);
			h ^= h >> 13;
			h *= 1274126177;
			h ^= h >> 16;
			return (h & 0x00FFFFFF) / 16777215f;
		}
	}

	/// <summary>
	/// 绘制海洋分层效果
	/// </summary>
	private void DrawOceanLayers()
	{
		var cells = _mapGenerator.Data.Cells;

		// 绘制不同深度的海洋层
		sbyte[] depths = { -2, -4, -6, -8 };
		float baseOpacity = 0.1f;

		foreach (sbyte depth in depths)
		{
			float opacity = baseOpacity * (1 + Mathf.Abs(depth) * 0.1f);
			var layerColor = new Color(_dynamicOceanColor.R, _dynamicOceanColor.G, _dynamicOceanColor.B, opacity);

			foreach (var cell in cells)
			{
				if (cell.DistanceField <= depth && cell.Vertices != null && cell.Vertices.Count >= 3)
				{
					var points = new Vector2[cell.Vertices.Count];
					bool valid = true;
					for (int i = 0; i < cell.Vertices.Count; i++)
					{
						points[i] = TransformToScreenCoordinates(cell.Vertices[i]);
						if (float.IsNaN(points[i].X) || float.IsNaN(points[i].Y)) valid = false;
					}
					
					if (valid)
					{
						var colors = new Color[cell.Vertices.Count];
						for (int i = 0; i < cell.Vertices.Count; i++)
						{
							colors[i] = layerColor;
						}
						DrawPolygon(points, colors);
					}
				}
			}
		}
	}

	/// <summary>
	/// 绘制河流
	/// </summary>
	private void DrawRivers()
	{
		var rivers = _mapGenerator.Data.Rivers;
		if (rivers == null) return;

		foreach (var river in rivers)
		{
			if (river.MeanderedPoints == null || river.MeanderedPoints.Count < 2)
				continue;

			// 绘制河流为线条（简化版本）
			for (int i = 1; i < river.MeanderedPoints.Count; i++)
			{
				var p1 = TransformToScreenCoordinates(new Vector2(river.MeanderedPoints[i - 1].X, river.MeanderedPoints[i - 1].Y));
				var p2 = TransformToScreenCoordinates(new Vector2(river.MeanderedPoints[i].X, river.MeanderedPoints[i].Y));

				if (float.IsNaN(p1.X) || float.IsNaN(p1.Y) || float.IsNaN(p2.X) || float.IsNaN(p2.Y))
					continue;

				// 根据位置计算宽度
				float flux = river.MeanderedPoints[i].Z;
				if (float.IsNaN(flux) || flux < 0) flux = 0;

				float width = Mathf.Max(0.5f, Mathf.Sqrt(flux) / 10f);

				DrawLine(p1, p2, _dynamicRiverColor, width);
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Middle)
			{
				if (mouseButton.Pressed && IsPointerOverMapView())
				{
					_isMiddleDragging = true;
					_lastPointerCanvasPos = mouseButton.Position;
				}
				else if (!mouseButton.Pressed)
				{
					_isMiddleDragging = false;
				}
			}

			if (!mouseButton.Pressed)
			{
				return;
			}

			if (!IsPointerOverMapView())
			{
				return;
			}

			if (mouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				ZoomAtPointer(1, GetZoomStep(mouseButton), mouseButton.Position);
				return;
			}

			if (mouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				ZoomAtPointer(-1, GetZoomStep(mouseButton), mouseButton.Position);
				return;
			}

			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				var localPos = GetLocalMousePosition();
				var mapPos = TransformToMapCoordinates(localPos);
				var cellId = GetCellAtPosition(mapPos);

				if (cellId != -1)
				{
					SelectCell(cellId, emitSignal: mouseButton.DoubleClick);
				}
				else
				{
					SelectCell(-1, emitSignal: false);
				}
			}
			else if (mouseButton.ButtonIndex == MouseButton.Right)
			{
				GenerateMap();
			}

			return;
		}

		if (@event is InputEventMouseMotion mouseMotion && _isMiddleDragging)
		{
			var previousLocalPos = GetGlobalTransformWithCanvas().AffineInverse() * _lastPointerCanvasPos;
			var currentLocalPos = GetGlobalTransformWithCanvas().AffineInverse() * mouseMotion.Position;
			var previousMapPos = ScreenToMap(previousLocalPos);
			var currentMapPos = ScreenToMap(currentLocalPos);

			CameraMapOffset += previousMapPos - currentMapPos;
			_lastPointerCanvasPos = mouseMotion.Position;
			return;
		}

		if (@event is InputEventMagnifyGesture magnifyGesture && IsPointerOverMapView())
		{
			float intensity = Mathf.Max(0.05f, Mathf.Abs(magnifyGesture.Factor - 1f));
			float step = _zoomStep * intensity;
			int direction = magnifyGesture.Factor >= 1f ? 1 : -1;
			var viewport = GetViewport();
			var pointerPos = viewport?.GetMousePosition() ?? Vector2.Zero;
			ZoomAtPointer(direction, step, pointerPos);
		}
	}

	public override void _Process(double delta)
	{
		if (!_enableKeyboardPan || _isGenerating)
		{
			return;
		}

		var direction = GetMoveDirection();
		if (direction == Vector2.Zero)
		{
			return;
		}

		float speed = _moveSpeed;
		if (_scaleMoveSpeedByZoom && ViewScale > 0.001f)
		{
			speed /= ViewScale;
		}

		CameraMapOffset += direction * speed * (float)delta;
	}

	private Vector2 GetMoveDirection()
	{
		float x = 0f;
		float y = 0f;

		if (Input.IsActionPressed("ui_right") || Input.IsKeyPressed(Key.D))
		{
			x += 1f;
		}

		if (Input.IsActionPressed("ui_left") || Input.IsKeyPressed(Key.A))
		{
			x -= 1f;
		}

		if (Input.IsActionPressed("ui_down") || Input.IsKeyPressed(Key.S))
		{
			y += 1f;
		}

		if (Input.IsActionPressed("ui_up") || Input.IsKeyPressed(Key.W))
		{
			y -= 1f;
		}

		var direction = new Vector2(x, y);
		return direction.LengthSquared() > 1f ? direction.Normalized() : direction;
	}

	private void ZoomAtPointer(int direction, float step, Vector2 pointerCanvasPos)
	{
		if (direction == 0 || step <= 0f)
		{
			return;
		}

		float currentScale = ViewScale;
		float adjustedStep = direction < 0 ? step * GetZoomOutFactor(currentScale) : step;
		float targetScale = ClampViewScale(currentScale + direction * adjustedStep);
		if (Mathf.IsEqualApprox(currentScale, targetScale))
		{
			return;
		}

		var localPointerPos = GetGlobalTransformWithCanvas().AffineInverse() * pointerCanvasPos;
		var mapPos = ScreenToMap(localPointerPos);
		ViewScale = targetScale;
		CameraMapOffset = GetCameraOffsetForZoomFocus(mapPos, localPointerPos, targetScale);
	}

	private float GetZoomStep(InputEventWithModifiers inputEvent)
	{
		if (inputEvent != null && inputEvent.ShiftPressed)
		{
			return _coarseZoomStep;
		}

		if (inputEvent != null && inputEvent.CtrlPressed)
		{
			return _fineZoomStep;
		}

		return _zoomStep;
	}

	private float GetZoomOutFactor(float currentScale)
	{
		var minScale = Mathf.Max(0.05f, GetMinViewScaleForArea());
		if (currentScale <= minScale)
		{
			return ZoomOutMinFactor;
		}

		float maxScale = Mathf.Max(5.0f, minScale);
		float t = Mathf.Clamp((currentScale - minScale) / Mathf.Max(0.0001f, maxScale - minScale), 0f, 1f);
		float curved = Mathf.Pow(t, ZoomOutCurvePower);
		return Mathf.Lerp(ZoomOutMinFactor, ZoomOutMaxFactor, curved);
	}

	private bool IsPointerOverMapView()
	{
		var viewport = GetViewport();
		if (viewport == null)
		{
			return true;
		}

		var pointerPos = viewport.GetMousePosition();
		if (!GetGlobalRect().HasPoint(pointerPos))
		{
			return false;
		}

		var hovered = viewport.GuiGetHoveredControl();
		if (hovered == null)
		{
			return true;
		}

		if (hovered == this || IsAncestorOf(hovered) || hovered.IsAncestorOf(this))
		{
			return true;
		}

		return hovered.MouseFilter != Control.MouseFilterEnum.Stop;
	}

	private void LoadControlSettings()
	{
		var config = new ConfigFile();
		if (config.Load(ControlsConfigPath) != Error.Ok)
		{
			return;
		}

		_zoomStep = Mathf.Max(0.001f, ReadFloat(config, "zoom_step", DefaultZoomStep));
		_fineZoomStep = Mathf.Max(0.001f, ReadFloat(config, "fine_zoom_step", DefaultFineZoomStep));
		_coarseZoomStep = Mathf.Max(0.001f, ReadFloat(config, "coarse_zoom_step", DefaultCoarseZoomStep));
		_enableKeyboardPan = ReadBool(config, "enable_keyboard_pan", _enableKeyboardPan);
		_scaleMoveSpeedByZoom = ReadBool(config, "scale_move_speed_by_zoom", _scaleMoveSpeedByZoom);
		_moveSpeed = Mathf.Max(0f, ReadFloat(config, "move_speed", DefaultMoveSpeed));
	}

	private float ReadFloat(ConfigFile config, string key, float defaultValue)
	{
		if (!config.HasSectionKey(ControlsConfigSection, key))
		{
			return defaultValue;
		}

		var value = config.GetValue(ControlsConfigSection, key, defaultValue);
		var text = value.AsString();
		if (float.TryParse(text, out var parsed))
		{
			return parsed;
		}

		if (bool.TryParse(text, out var parsedBool))
		{
			return parsedBool ? 1f : 0f;
		}

		return defaultValue;
	}

	private bool ReadBool(ConfigFile config, string key, bool defaultValue)
	{
		if (!config.HasSectionKey(ControlsConfigSection, key))
		{
			return defaultValue;
		}

		var value = config.GetValue(ControlsConfigSection, key, defaultValue);
		var text = value.AsString();
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
	private void DrawCellPolygons(Func<Cell, Color> colorSelector)
	{
		var cells = _mapGenerator.Data.Cells;
		foreach (var cell in cells)
		{
			if (cell.Vertices == null || cell.Vertices.Count < 2)
			{
				continue;
			}

			if (cell.Vertices.Count == 2)
			{
				var p1 = TransformToScreenCoordinates(cell.Vertices[0]);
				var p2 = TransformToScreenCoordinates(cell.Vertices[1]);
				DrawLine(p1, p2, colorSelector(cell), 1f);
				continue;
			}

			if (!TryBuildScreenPolygon(cell.Vertices, out var points))
			{
				continue;
			}

			Color color = cell.Id == _selectedCellId
				? new Color(1f, 0.8f, 0.2f)
				: colorSelector(cell);

			var colors = new Color[cell.Vertices.Count];
			for (int i = 0; i < cell.Vertices.Count; i++)
			{
				colors[i] = color;
			}

			DrawPolygon(points, colors);

			if (cell.Id == _selectedCellId)
			{
				Vector2[] borderPoints = new Vector2[points.Length + 1];
				Array.Copy(points, borderPoints, points.Length);
				borderPoints[points.Length] = points[0];
				DrawPolyline(borderPoints, new Color(1, 1, 1), 2f);
			}
		}
	}

	private void DrawCellStructure()
	{
		var cells = _mapGenerator.Data.Cells;
		foreach (var cell in cells)
		{
			if (cell.Vertices == null || cell.Vertices.Count < 2)
			{
				continue;
			}

			var points = new Vector2[cell.Vertices.Count + 1];
			for (int i = 0; i < cell.Vertices.Count; i++)
			{
				points[i] = TransformToScreenCoordinates(cell.Vertices[i]);
			}
			points[cell.Vertices.Count] = points[0];
			DrawPolyline(points, CellOutlineColor, 1f);
		}
	}

	private void DrawGrid()
	{
		var mapSize = GetMapSize();
		int divisions = 10;
		for (int i = 0; i <= divisions; i++)
		{
			float t = i / (float)divisions;
			float x = mapSize.X * t;
			float y = mapSize.Y * t;

			var top = TransformToScreenCoordinates(new Vector2(x, 0));
			var bottom = TransformToScreenCoordinates(new Vector2(x, mapSize.Y));
			DrawLine(top, bottom, GridLineColor, 1f);

			var left = TransformToScreenCoordinates(new Vector2(0, y));
			var right = TransformToScreenCoordinates(new Vector2(mapSize.X, y));
			DrawLine(left, right, GridLineColor, 1f);
		}
	}

	private void DrawCoordinates()
	{
		var font = GetThemeFont("font", "Label");
		if (font == null)
		{
			return;
		}

		int fontSize = GetThemeFontSize("font_size", "Label");
		if (fontSize <= 0)
		{
			fontSize = 12;
		}

		var mapSize = GetMapSize();
		int divisions = 5;
		for (int i = 0; i <= divisions; i++)
		{
			float t = i / (float)divisions;
			float x = mapSize.X * t;
			float y = mapSize.Y * t;

			var top = TransformToScreenCoordinates(new Vector2(x, 0));
			var left = TransformToScreenCoordinates(new Vector2(0, y));

			DrawString(font, top + new Vector2(0, -4), $"{Mathf.RoundToInt(x)}", HorizontalAlignment.Center, -1, fontSize, CoordinateTextColor);
			DrawString(font, left + new Vector2(-6, 0), $"{Mathf.RoundToInt(y)}", HorizontalAlignment.Right, -1, fontSize, CoordinateTextColor);
		}
	}

	private void DrawCompass()
	{
		var font = GetThemeFont("font", "Label");
		if (font == null)
		{
			return;
		}

		int fontSize = GetThemeFontSize("font_size", "Label");
		if (fontSize <= 0)
		{
			fontSize = 16;
		}

		var anchor = new Vector2(ViewMargin + 10, ViewMargin + 20);
		DrawString(font, anchor, "N", HorizontalAlignment.Center, -1, fontSize, CompassColor);
		DrawLine(anchor + new Vector2(0, 4), anchor + new Vector2(0, 26), CompassColor, 2f);
	}

	private void DrawScaleBar()
	{
		UpdateTransformParameters();
		var viewSize = GetViewSize();
		float targetLength = 120f;
		float mapLength = targetLength / Mathf.Max(0.001f, _currentScale);

		var start = new Vector2(ViewMargin + 10, viewSize.Y - ViewMargin - 10);
		var end = start + new Vector2(targetLength, 0);
		DrawLine(start, end, ScaleBarColor, 3f);
		DrawLine(start, start + new Vector2(0, -6), ScaleBarColor, 3f);
		DrawLine(end, end + new Vector2(0, -6), ScaleBarColor, 3f);

		var font = GetThemeFont("font", "Label");
		if (font == null)
		{
			return;
		}

		int fontSize = GetThemeFontSize("font_size", "Label");
		if (fontSize <= 0)
		{
			fontSize = 12;
		}

		DrawString(font, start + new Vector2(0, -8), $"{Mathf.RoundToInt(mapLength)}", HorizontalAlignment.Left, -1, fontSize, ScaleBarColor);
	}

	private void DrawVignette()
	{
		var viewSize = GetViewSize();
		float thickness = 24f;
		DrawRect(new Rect2(0, 0, viewSize.X, thickness), VignetteColor, true);
		DrawRect(new Rect2(0, viewSize.Y - thickness, viewSize.X, thickness), VignetteColor, true);
		DrawRect(new Rect2(0, 0, thickness, viewSize.Y), VignetteColor, true);
		DrawRect(new Rect2(viewSize.X - thickness, 0, thickness, viewSize.Y), VignetteColor, true);
	}

	private void DrawReliefMarkers()
	{
		var cells = _mapGenerator.Data.Cells;
		foreach (var cell in cells)
		{
			if (!cell.IsLand || cell.Height < 0.72f || cell.Id % 18 != 0)
			{
				continue;
			}

			var pos = TransformToScreenCoordinates(cell.Position);
			var size = 6f + cell.Height * 6f;
			Vector2[] points =
			{
				pos + new Vector2(0, -size),
				pos + new Vector2(size * 0.6f, size),
				pos + new Vector2(-size * 0.6f, size)
			};
			DrawPolygon(points, new[] { new Color(0.35f, 0.3f, 0.2f, 0.5f), new Color(0.35f, 0.3f, 0.2f, 0.5f), new Color(0.35f, 0.3f, 0.2f, 0.5f) });
		}
	}

	private void DrawRoutes()
	{
		if (_routePaths.Count == 0)
		{
			return;
		}

		foreach (var route in _routePaths)
		{
			if (route == null || route.Points == null || route.Points.Count < 2)
			{
				continue;
			}

			var screenPoints = new Vector2[route.Points.Count];
			bool valid = true;
			for (int i = 0; i < route.Points.Count; i++)
			{
				screenPoints[i] = TransformToScreenCoordinates(route.Points[i]);
				if (float.IsNaN(screenPoints[i].X) || float.IsNaN(screenPoints[i].Y))
				{
					valid = false;
					break;
				}
			}

			if (!valid)
			{
				continue;
			}

			var color = route.IsPrimary ? RoutePrimaryColor : RouteSecondaryColor;
			float width = route.IsPrimary ? _routePrimaryWidth : _routeSecondaryWidth;
			DrawPolyline(screenPoints, color, width);
		}
	}

	private void DrawBurgIcons()
	{
		if (_countries == null || _countries.Count == 0)
		{
			return;
		}

		foreach (var country in _countries)
		{
			var center = country.Center;
			if (country.CapitalCellId >= 0 && _mapGenerator?.Data?.Cells != null && country.CapitalCellId < _mapGenerator.Data.Cells.Length)
			{
				center = _mapGenerator.Data.Cells[country.CapitalCellId].Position;
			}

			var pos = TransformToScreenCoordinates(center);
			DrawCircle(pos, 4f, new Color(0.2f, 0.15f, 0.1f, 0.8f));
			DrawCircle(pos, 2f, new Color(0.9f, 0.85f, 0.7f, 0.9f));
		}
	}

	private void DrawEmblems()
	{
		if (_countries == null || _countries.Count == 0)
		{
			return;
		}

		foreach (var country in _countries)
		{
			var pos = TransformToScreenCoordinates(country.Center);
			var size = new Vector2(8, 8);
			DrawRect(new Rect2(pos - size * 0.5f, size), new Color(0.65f, 0.2f, 0.2f, 0.7f), true);
		}
	}

	private void DrawMilitary()
	{
		if (_countries == null || _countries.Count == 0)
		{
			return;
		}

		foreach (var country in _countries)
		{
			var pos = TransformToScreenCoordinates(country.Center) + new Vector2(0, 10);
			Vector2[] points =
			{
				pos + new Vector2(0, -6),
				pos + new Vector2(5, 4),
				pos + new Vector2(-5, 4)
			};
			DrawPolygon(points, new[] { new Color(0.2f, 0.25f, 0.55f, 0.65f), new Color(0.2f, 0.25f, 0.55f, 0.65f), new Color(0.2f, 0.25f, 0.55f, 0.65f) });
		}
	}

	private void DrawMarkers()
	{
		if (_countries == null || _countries.Count == 0)
		{
			return;
		}

		foreach (var country in _countries)
		{
			var pos = TransformToScreenCoordinates(country.Center) + new Vector2(0, -10);
			Vector2[] points =
			{
				pos + new Vector2(0, -5),
				pos + new Vector2(5, 0),
				pos + new Vector2(0, 5),
				pos + new Vector2(-5, 0)
			};
			DrawPolygon(points, new[] { new Color(0.1f, 0.45f, 0.35f, 0.7f), new Color(0.1f, 0.45f, 0.35f, 0.7f), new Color(0.1f, 0.45f, 0.35f, 0.7f), new Color(0.1f, 0.45f, 0.35f, 0.7f) });
		}
	}

	private void DrawRulers()
	{
		if (_countries == null || _countries.Count == 0)
		{
			return;
		}

		var font = GetThemeFont("font", "Label");
		if (font == null)
		{
			return;
		}

		int fontSize = GetThemeFontSize("font_size", "Label");
		if (fontSize <= 0)
		{
			fontSize = 12;
		}

		foreach (var country in _countries)
		{
			var pos = TransformToScreenCoordinates(country.Center) + new Vector2(0, 18);
			DrawString(font, pos, "R", HorizontalAlignment.Center, -1, fontSize, new Color(0.1f, 0.1f, 0.1f, 0.7f));
		}
	}

	private void DrawIceOverlay()
	{
		var cells = _mapGenerator.Data.Cells;
		foreach (var cell in cells)
		{
			bool isCold = cell.Temperature <= 0 || cell.Height > 0.85f;
			if (!isCold || cell.Vertices == null || cell.Vertices.Count < 3)
			{
				continue;
			}

			if (!TryBuildScreenPolygon(cell.Vertices, out var points))
			{
				continue;
			}

			var colors = new Color[cell.Vertices.Count];
			for (int i = 0; i < cell.Vertices.Count; i++)
			{
				colors[i] = new Color(0.92f, 0.95f, 1f, 0.5f);
			}
			DrawPolygon(points, colors);
		}
	}

	private Color GetCultureColor(Cell cell)
	{
		return GetHashedColor(17, cell.BiomeId, 0.6f);
	}

	private Color GetReligionColor(Cell cell)
	{
		return GetHashedColor(23, cell.FeatureId, 0.6f);
	}

	private Color GetProvinceColor(Cell cell)
	{
		return GetHashedColor(31, cell.GridCellId, 0.55f);
	}

	private Color GetZoneColor(Cell cell)
	{
		return GetHashedColor(41, cell.DistanceField, 0.45f);
	}

	private Color GetTemperatureColor(Cell cell)
	{
		float temp = Mathf.Clamp(cell.Temperature, -30, 30);
		float t = (temp + 30f) / 60f;
		return LerpColor(new Color(0.2f, 0.35f, 0.75f, 0.7f), new Color(0.9f, 0.35f, 0.2f, 0.7f), t);
	}

	private Color GetPrecipitationColor(Cell cell)
	{
		float t = Mathf.Clamp(cell.Precipitation / 255f, 0f, 1f);
		return LerpColor(new Color(0.85f, 0.78f, 0.5f, 0.6f), new Color(0.2f, 0.5f, 0.8f, 0.7f), t);
	}

	private Color GetPopulationColor(Cell cell)
	{
		float t = Mathf.Clamp(cell.Height, 0f, 1f);
		return LerpColor(new Color(0.95f, 0.9f, 0.8f, 0.5f), new Color(0.45f, 0.2f, 0.1f, 0.8f), t);
	}

	private Color GetHashedColor(int seed, int value, float alpha)
	{
		int hash = value * 73856093 ^ seed * 19349663;
		hash = (hash ^ (hash >> 13)) * 1274126177;
		float hue = Mathf.Abs(hash % 360) / 360f;
		return Color.FromHsv(hue, 0.45f, 0.85f, alpha);
	}

	private bool TryBuildScreenPolygon(List<Vector2> vertices, out Vector2[] points)
	{
		points = null;
		if (vertices == null || vertices.Count < 3)
		{
			return false;
		}

		points = new Vector2[vertices.Count];
		double area = 0.0;
		for (int i = 0; i < vertices.Count; i++)
		{
			var screenPos = TransformToScreenCoordinates(vertices[i]);
			if (float.IsNaN(screenPos.X) || float.IsNaN(screenPos.Y) || float.IsInfinity(screenPos.X) || float.IsInfinity(screenPos.Y))
			{
				points = null;
				return false;
			}
			points[i] = screenPos;
		}

		for (int i = 0; i < points.Length; i++)
		{
			var a = points[i];
			var b = points[(i + 1) % points.Length];
			area += a.X * b.Y - b.X * a.Y;
		}

		return Math.Abs(area) > 0.001;
	}

	public bool IsLayerEnabled(MapLayer layer)
	{
		return layer switch
		{
			MapLayer.Texture => _layerTexture,
			MapLayer.Heightmap => _layerHeightmap,
			MapLayer.Biomes => _layerBiomes,
			MapLayer.Cells => _layerCells,
			MapLayer.Grid => _layerGrid,
			MapLayer.Coordinates => _layerCoordinates,
			MapLayer.Compass => _layerCompass,
			MapLayer.Rivers => _layerRivers,
			MapLayer.Relief => _layerRelief,
			MapLayer.Religions => _layerReligions,
			MapLayer.Cultures => _layerCultures,
			MapLayer.States => _layerStates,
			MapLayer.Provinces => _layerProvinces,
			MapLayer.Zones => _layerZones,
			MapLayer.Borders => _layerBorders,
			MapLayer.Routes => _layerRoutes,
			MapLayer.Temperature => _layerTemperature,
			MapLayer.Population => _layerPopulation,
			MapLayer.Ice => _layerIce,
			MapLayer.Precipitation => _layerPrecipitation,
			MapLayer.Emblems => _layerEmblems,
			MapLayer.BurgIcons => _layerBurgIcons,
			MapLayer.Labels => _layerLabels,
			MapLayer.Military => _layerMilitary,
			MapLayer.Markers => _layerMarkers,
			MapLayer.Rulers => _layerRulers,
			MapLayer.ScaleBar => _layerScaleBar,
			MapLayer.Vignette => _layerVignette,
			_ => false
		};
	}

	public void ApplyLayerPreset(LayerPreset preset)
	{
		SetAllLayers(false);
		switch (preset)
		{
			case LayerPreset.Political:
				EnableLayers(MapLayer.Borders, MapLayer.BurgIcons, MapLayer.Ice, MapLayer.Labels, MapLayer.Rivers, MapLayer.Routes, MapLayer.ScaleBar, MapLayer.States, MapLayer.Vignette);
				break;
			case LayerPreset.Cultural:
				EnableLayers(MapLayer.Borders, MapLayer.BurgIcons, MapLayer.Cultures, MapLayer.Labels, MapLayer.Rivers, MapLayer.Routes, MapLayer.ScaleBar, MapLayer.Vignette);
				break;
			case LayerPreset.Religions:
				EnableLayers(MapLayer.Borders, MapLayer.BurgIcons, MapLayer.Labels, MapLayer.Religions, MapLayer.Rivers, MapLayer.Routes, MapLayer.ScaleBar, MapLayer.Vignette);
				break;
			case LayerPreset.Provinces:
				EnableLayers(MapLayer.Borders, MapLayer.BurgIcons, MapLayer.Provinces, MapLayer.Rivers, MapLayer.ScaleBar, MapLayer.Vignette);
				break;
			case LayerPreset.Biomes:
				EnableLayers(MapLayer.Biomes, MapLayer.Ice, MapLayer.Rivers, MapLayer.ScaleBar, MapLayer.Vignette);
				break;
			case LayerPreset.Heightmap:
				EnableLayers(MapLayer.Heightmap, MapLayer.Rivers, MapLayer.Vignette);
				break;
			case LayerPreset.Physical:
				EnableLayers(MapLayer.Coordinates, MapLayer.Heightmap, MapLayer.Ice, MapLayer.Rivers, MapLayer.ScaleBar, MapLayer.Vignette);
				break;
			case LayerPreset.Poi:
				EnableLayers(MapLayer.Borders, MapLayer.BurgIcons, MapLayer.Heightmap, MapLayer.Ice, MapLayer.Markers, MapLayer.Rivers, MapLayer.Routes, MapLayer.ScaleBar, MapLayer.Vignette);
				break;
			case LayerPreset.Military:
				EnableLayers(MapLayer.Borders, MapLayer.BurgIcons, MapLayer.Labels, MapLayer.Military, MapLayer.Rivers, MapLayer.Routes, MapLayer.ScaleBar, MapLayer.States, MapLayer.Vignette);
				break;
			case LayerPreset.Emblems:
				EnableLayers(MapLayer.Borders, MapLayer.BurgIcons, MapLayer.Ice, MapLayer.Emblems, MapLayer.Rivers, MapLayer.Routes, MapLayer.ScaleBar, MapLayer.States, MapLayer.Vignette);
				break;
			case LayerPreset.Landmass:
				EnableLayers(MapLayer.ScaleBar);
				break;
			case LayerPreset.Custom:
				break;
		}

		SyncGeneratorSettings();
		QueueRedraw();
	}

	public void SetLayerEnabled(MapLayer layer, bool enabled)
	{
		SetLayer(layer, enabled);
		QueueRedraw();
	}

	public void ApplyVisualStyle(MapVisualStyleSelection style)
	{
		_visualStyleMode = style;

		switch (style)
		{
			case MapVisualStyleSelection.InkFantasy:
				TerrainStyleMode = TerrainStyle.Heightmap;
				UseBiomeColors = true;
				ShowOceanLayers = true;
				CountryFillAlpha = 0.88f;
				CountryBorderWidth = 2f;
				CountryBorderColor = new Color(0.19f, 0.13f, 0.08f, 0.9f);
				_riverDensity = 1.05f;
				GenerationWaterLevel = 0.35f;
				UseTemplateGeneration = true;
				UseRandomTemplateGeneration = false;
				GenerationTemplateType = HeightmapTemplateType.Continents;
				break;
			case MapVisualStyleSelection.Parchment:
				TerrainStyleMode = TerrainStyle.Contour;
				UseBiomeColors = false;
				ShowOceanLayers = false;
				CountryFillAlpha = 0.52f;
				CountryBorderWidth = 2.4f;
				CountryBorderColor = new Color(0.28f, 0.2f, 0.12f, 0.92f);
				_riverDensity = 0.78f;
				GenerationWaterLevel = 0.28f;
				UseTemplateGeneration = true;
				UseRandomTemplateGeneration = false;
				GenerationTemplateType = HeightmapTemplateType.Pangea;
				break;
			case MapVisualStyleSelection.NavalChart:
				TerrainStyleMode = TerrainStyle.Heightmap;
				UseBiomeColors = false;
				ShowOceanLayers = true;
				CountryFillAlpha = 0.68f;
				CountryBorderWidth = 1.4f;
				CountryBorderColor = new Color(0.1f, 0.23f, 0.39f, 0.9f);
				_riverDensity = 1.35f;
				GenerationWaterLevel = 0.42f;
				UseTemplateGeneration = true;
				UseRandomTemplateGeneration = false;
				GenerationTemplateType = HeightmapTemplateType.Archipelago;
				break;
			case MapVisualStyleSelection.Relief:
				TerrainStyleMode = TerrainStyle.Contour;
				UseBiomeColors = true;
				ShowOceanLayers = true;
				CountryFillAlpha = 0.74f;
				CountryBorderWidth = 1.8f;
				CountryBorderColor = new Color(0.24f, 0.18f, 0.08f, 0.85f);
				_riverDensity = 1.15f;
				GenerationWaterLevel = 0.33f;
				UseTemplateGeneration = true;
				UseRandomTemplateGeneration = false;
				GenerationTemplateType = HeightmapTemplateType.Peninsula;
				break;
			case MapVisualStyleSelection.Heatmap:
				TerrainStyleMode = TerrainStyle.Heatmap;
				UseBiomeColors = false;
				ShowOceanLayers = false;
				CountryFillAlpha = 0.55f;
				CountryBorderWidth = 1.2f;
				CountryBorderColor = new Color(0.18f, 0.09f, 0.12f, 0.85f);
				_riverDensity = 0.92f;
				GenerationWaterLevel = 0.24f;
				UseTemplateGeneration = true;
				UseRandomTemplateGeneration = false;
				GenerationTemplateType = HeightmapTemplateType.Fractious;
				break;
			case MapVisualStyleSelection.Monochrome:
				TerrainStyleMode = TerrainStyle.Contour;
				UseBiomeColors = false;
				ShowOceanLayers = false;
				CountryFillAlpha = 0.4f;
				CountryBorderWidth = 2.1f;
				CountryBorderColor = new Color(0.08f, 0.08f, 0.08f, 0.95f);
				_riverDensity = 0.6f;
				GenerationWaterLevel = 0.2f;
				UseTemplateGeneration = true;
				UseRandomTemplateGeneration = false;
				GenerationTemplateType = HeightmapTemplateType.OldWorld;
				break;
		}

		SyncGeneratorSettings();
		QueueRedraw();
	}

	public void ApplyGenesisPlanetInfluence(PlanetData planetData, int lawAlignment)
	{
		if (planetData == null)
		{
			ClearGenesisPlanetInfluence();
			return;
		}

		float temperature = Mathf.Clamp(planetData.Temperature, 0f, 1f);
		float atmosphere = Mathf.Clamp(planetData.AtmosphereDensity, 0f, 1f);
		float oceanCoverage = Mathf.Clamp(planetData.OceanCoverage, 0f, 1f);
		float lawFactor = Mathf.Clamp(lawAlignment / 100f, 0f, 1f);

		Color landBase;
		Color waterBase;
		switch (planetData.Element)
		{
			case PlanetElement.Pyro:
				landBase = new Color(0.62f, 0.24f, 0.14f, 1f);
				waterBase = new Color(0.28f, 0.1f, 0.1f, 1f);
				break;
			case PlanetElement.Cryo:
				landBase = new Color(0.74f, 0.84f, 0.92f, 1f);
				waterBase = new Color(0.38f, 0.54f, 0.74f, 1f);
				break;
			case PlanetElement.Aero:
				landBase = new Color(0.66f, 0.74f, 0.86f, 1f);
				waterBase = new Color(0.33f, 0.48f, 0.68f, 1f);
				break;
			case PlanetElement.Terra:
			default:
				landBase = new Color(0.24f, 0.56f, 0.34f, 1f);
				waterBase = new Color(0.12f, 0.3f, 0.53f, 1f);
				break;
		}

		Color coldTint = new Color(0.82f, 0.9f, 1.1f, 1f);
		Color hotTint = new Color(1.15f, 0.95f, 0.82f, 1f);
		Color tempTint = coldTint.Lerp(hotTint, temperature);
		landBase = new Color(landBase.R * tempTint.R, landBase.G * tempTint.G, landBase.B * tempTint.B, 1f);
		waterBase = new Color(waterBase.R * tempTint.R, waterBase.G * tempTint.G, waterBase.B * tempTint.B, 1f);

		_dynamicTerrainLowColor = landBase.Darkened(0.28f + oceanCoverage * 0.1f);
		_dynamicTerrainHighColor = landBase.Lightened(0.16f + (1f - oceanCoverage) * 0.2f);
		_dynamicOceanColor = waterBase.Lerp(new Color(0.0f, 0.92f, 1.0f, 1f), lawFactor * 0.12f);
		_dynamicRiverColor = _dynamicOceanColor.Lightened(0.16f + atmosphere * 0.2f);
		_dynamicTerrainBlend = Mathf.Lerp(0.2f, 0.64f, atmosphere * 0.55f + oceanCoverage * 0.45f);
		_useGenesisPlanetTint = true;
		QueueRedraw();
	}

	public void ClearGenesisPlanetInfluence()
	{
		_useGenesisPlanetTint = false;
		_dynamicOceanColor = OceanLayerColor;
		_dynamicRiverColor = RiverColor;
		_dynamicTerrainLowColor = new Color(0.24f, 0.5f, 0.3f, 0.92f);
		_dynamicTerrainHighColor = new Color(0.75f, 0.82f, 0.62f, 0.96f);
		_dynamicTerrainBlend = 0.45f;
		QueueRedraw();
	}

	public void SetGenesisTerrainHeightmap(float[] heightmap, int width, int height)
	{
		if (heightmap == null || width <= 1 || height <= 1)
		{
			_genesisTerrainHeightmap = null;
			_genesisTerrainWidth = 0;
			_genesisTerrainHeight = 0;
			_mapGenerator?.ClearExternalHeightmap();
			return;
		}

		int expectedLength = width * height;
		if (heightmap.Length < expectedLength)
		{
			_genesisTerrainHeightmap = null;
			_genesisTerrainWidth = 0;
			_genesisTerrainHeight = 0;
			_mapGenerator?.ClearExternalHeightmap();
			return;
		}

		_genesisTerrainHeightmap = new float[expectedLength];
		Array.Copy(heightmap, _genesisTerrainHeightmap, expectedLength);
		_genesisTerrainWidth = width;
		_genesisTerrainHeight = height;

		ApplyGenesisTerrainHeightmapToGenerator();
	}

	private void ApplyGenesisTerrainHeightmapToGenerator()
	{
		if (_mapGenerator == null)
		{
			return;
		}

		if (_genesisTerrainHeightmap == null || _genesisTerrainWidth <= 1 || _genesisTerrainHeight <= 1)
		{
			_mapGenerator.ClearExternalHeightmap();
			return;
		}

		_mapGenerator.SetExternalHeightmap(_genesisTerrainHeightmap, _genesisTerrainWidth, _genesisTerrainHeight);
	}

	private void SyncGeneratorSettings()
	{
		if (_mapGenerator == null)
		{
			return;
		}

		_mapGenerator.MapWidth = _mapWidth;
		_mapGenerator.MapHeight = _mapHeight;
		_mapGenerator.RiverDensity = _riverDensity;
		_mapGenerator.UseMultithreading = _useMultithreading;
		_mapGenerator.BoundaryPaddingScale = _boundaryPaddingScale;
		_mapGenerator.BoundaryStepScale = _boundaryStepScale;
		_mapGenerator.WaterLevel = _generationWaterLevel;
		_mapGenerator.UseTemplate = _useTemplateGeneration;
		_mapGenerator.RandomTemplate = _useRandomTemplateGeneration;
		_mapGenerator.TemplateType = _generationTemplateType;
		ApplyGenesisTerrainHeightmapToGenerator();
	}

	private void EnableLayers(params MapLayer[] layers)
	{
		foreach (var layer in layers)
		{
			SetLayer(layer, true);
		}
	}

	private void SetAllLayers(bool enabled)
	{
		SetLayer(MapLayer.Texture, enabled);
		SetLayer(MapLayer.Heightmap, enabled);
		SetLayer(MapLayer.Biomes, enabled);
		SetLayer(MapLayer.Cells, enabled);
		SetLayer(MapLayer.Grid, enabled);
		SetLayer(MapLayer.Coordinates, enabled);
		SetLayer(MapLayer.Compass, enabled);
		SetLayer(MapLayer.Rivers, enabled);
		SetLayer(MapLayer.Relief, enabled);
		SetLayer(MapLayer.Religions, enabled);
		SetLayer(MapLayer.Cultures, enabled);
		SetLayer(MapLayer.States, enabled);
		SetLayer(MapLayer.Provinces, enabled);
		SetLayer(MapLayer.Zones, enabled);
		SetLayer(MapLayer.Borders, enabled);
		SetLayer(MapLayer.Routes, enabled);
		SetLayer(MapLayer.Temperature, enabled);
		SetLayer(MapLayer.Population, enabled);
		SetLayer(MapLayer.Ice, enabled);
		SetLayer(MapLayer.Precipitation, enabled);
		SetLayer(MapLayer.Emblems, enabled);
		SetLayer(MapLayer.BurgIcons, enabled);
		SetLayer(MapLayer.Labels, enabled);
		SetLayer(MapLayer.Military, enabled);
		SetLayer(MapLayer.Markers, enabled);
		SetLayer(MapLayer.Rulers, enabled);
		SetLayer(MapLayer.ScaleBar, enabled);
		SetLayer(MapLayer.Vignette, enabled);
	}

	private void SetLayer(MapLayer layer, bool enabled)
	{
		switch (layer)
		{
			case MapLayer.Texture: _layerTexture = enabled; break;
			case MapLayer.Heightmap: _layerHeightmap = enabled; break;
			case MapLayer.Biomes: _layerBiomes = enabled; break;
			case MapLayer.Cells: _layerCells = enabled; break;
			case MapLayer.Grid: _layerGrid = enabled; break;
			case MapLayer.Coordinates: _layerCoordinates = enabled; break;
			case MapLayer.Compass: _layerCompass = enabled; break;
			case MapLayer.Rivers: _layerRivers = enabled; break;
			case MapLayer.Relief: _layerRelief = enabled; break;
			case MapLayer.Religions: _layerReligions = enabled; break;
			case MapLayer.Cultures: _layerCultures = enabled; break;
			case MapLayer.States: _layerStates = enabled; break;
			case MapLayer.Provinces: _layerProvinces = enabled; break;
			case MapLayer.Zones: _layerZones = enabled; break;
			case MapLayer.Borders: _layerBorders = enabled; break;
			case MapLayer.Routes: _layerRoutes = enabled; break;
			case MapLayer.Temperature: _layerTemperature = enabled; break;
			case MapLayer.Population: _layerPopulation = enabled; break;
			case MapLayer.Ice: _layerIce = enabled; break;
			case MapLayer.Precipitation: _layerPrecipitation = enabled; break;
			case MapLayer.Emblems: _layerEmblems = enabled; break;
			case MapLayer.BurgIcons: _layerBurgIcons = enabled; break;
			case MapLayer.Labels: _layerLabels = enabled; break;
			case MapLayer.Military: _layerMilitary = enabled; break;
			case MapLayer.Markers: _layerMarkers = enabled; break;
			case MapLayer.Rulers: _layerRulers = enabled; break;
			case MapLayer.ScaleBar: _layerScaleBar = enabled; break;
			case MapLayer.Vignette: _layerVignette = enabled; break;
		}
	}

	private Color GetCellRenderColor(Cell cell)
	{
		if (ShowCountries)
		{
			return GetCountryColor(cell);
		}

		if (UseBiomeColors)
		{
			return GetBiomeColor(cell);
		}

		if (ShowTerrainLayer)
		{
			return GetTerrainColor(cell);
		}

		return new Color(0, 0, 0, 0);
	}

	private Color GetBiomeColor(Cell cell)
	{
		return BiomeData.GetColor(cell.BiomeId);
	}

	private Color GetCountryColor(Cell cell)
	{
		if (_cellCountryIds != null && cell.Id >= 0 && cell.Id < _cellCountryIds.Length)
		{
			var countryId = _cellCountryIds[cell.Id];
			if (countryId >= 0 && countryId < _countries.Count)
			{
				var color = _countries[countryId].Color;
				color.A *= _countryFillAlpha;
				if (_visualStyleMode == MapVisualStyleSelection.Parchment)
				{
					color = color.Lerp(new Color(0.83f, 0.74f, 0.56f, color.A), 0.42f);
				}
				else if (_visualStyleMode == MapVisualStyleSelection.NavalChart)
				{
					color = color.Lerp(new Color(0.6f, 0.78f, 0.9f, color.A), 0.35f);
				}
				else if (_visualStyleMode == MapVisualStyleSelection.Monochrome)
				{
					float gray = color.R * 0.3f + color.G * 0.59f + color.B * 0.11f;
					color = new Color(gray, gray, gray, color.A);
				}
				return color;
			}
		}

		return GetTerrainColor(cell);
	}

	private Color GetTerrainColor(Cell cell)
	{
		if (!cell.IsLand)
		{
			return GetWaterColor(cell.Height);
		}

		float height = Mathf.Clamp(cell.Height, 0f, 1f);
		var baseHeightmapColor = cell.RenderColor;
		if (_visualStyleMode == MapVisualStyleSelection.Parchment)
		{
			var parchmentLow = new Color(0.66f, 0.57f, 0.41f, 0.92f);
			var parchmentHigh = new Color(0.95f, 0.87f, 0.66f, 0.96f);
			baseHeightmapColor = LerpColor(parchmentLow, parchmentHigh, height);
		}
		else if (_visualStyleMode == MapVisualStyleSelection.NavalChart)
		{
			var coastTint = new Color(0.79f, 0.88f, 0.74f, 0.9f);
			var uplandTint = new Color(0.52f, 0.72f, 0.58f, 0.9f);
			baseHeightmapColor = LerpColor(coastTint, uplandTint, height);
		}
		else if (_visualStyleMode == MapVisualStyleSelection.Monochrome)
		{
			var lowGray = new Color(0.42f, 0.42f, 0.42f, 0.92f);
			var highGray = new Color(0.88f, 0.88f, 0.88f, 0.94f);
			baseHeightmapColor = LerpColor(lowGray, highGray, height);
		}
		if (_useGenesisPlanetTint && _visualStyleMode != MapVisualStyleSelection.Monochrome)
		{
			var dynamicTerrainColor = LerpColor(_dynamicTerrainLowColor, _dynamicTerrainHighColor, height);
			baseHeightmapColor = baseHeightmapColor.Lerp(dynamicTerrainColor, _dynamicTerrainBlend);
		}

		switch (_terrainStyleMode)
		{
			case TerrainStyle.Contour:
				const int bands = 8;
				float bandValue = Mathf.Floor(height * bands) / bands;
				if (_visualStyleMode == MapVisualStyleSelection.Parchment)
				{
					return LerpColor(new Color(0.53f, 0.44f, 0.31f, 0.95f), new Color(0.9f, 0.8f, 0.58f, 0.96f), bandValue);
				}
				if (_visualStyleMode == MapVisualStyleSelection.Monochrome)
				{
					return LerpColor(new Color(0.35f, 0.35f, 0.35f, 0.95f), new Color(0.88f, 0.88f, 0.88f, 0.96f), bandValue);
				}
				var contourLow = _useGenesisPlanetTint ? _dynamicTerrainLowColor : HeatmapLowColor;
				var contourHigh = _useGenesisPlanetTint ? _dynamicTerrainHighColor : HeatmapHighColor;
				return LerpColor(contourLow, contourHigh, bandValue);
			case TerrainStyle.Heatmap:
				var heatLow = _useGenesisPlanetTint ? _dynamicTerrainLowColor : HeatmapLowColor;
				return LerpColor(heatLow, _useGenesisPlanetTint ? _dynamicTerrainHighColor : HeatmapHighColor, height);
			default:
				return baseHeightmapColor;
		}
	}

	private Color GetWaterColor(float cellHeight)
	{
		float depth = Mathf.Clamp(1f - Mathf.Max(0f, cellHeight), 0f, 1f);
		return _visualStyleMode switch
		{
			MapVisualStyleSelection.Parchment => LerpColor(new Color(0.43f, 0.36f, 0.24f, 0.82f), new Color(0.61f, 0.5f, 0.31f, 0.88f), depth),
			MapVisualStyleSelection.NavalChart => LerpColor(new Color(0.12f, 0.28f, 0.49f, 0.88f), new Color(0.25f, 0.52f, 0.75f, 0.95f), depth),
			MapVisualStyleSelection.Monochrome => LerpColor(new Color(0.25f, 0.25f, 0.25f, 0.82f), new Color(0.55f, 0.55f, 0.55f, 0.88f), depth),
			_ => new Color(_dynamicOceanColor.R, _dynamicOceanColor.G, _dynamicOceanColor.B, 0.82f)
		};
	}

	private int GetCountryId(int cellId)
	{
		if (_cellCountryIds == null || cellId < 0 || cellId >= _cellCountryIds.Length)
		{
			return -1;
		}

		return _cellCountryIds[cellId];
	}

	private void GenerateCountries()
	{
		if (_mapGenerator?.Data?.Cells == null)
		{
			return;
		}

		var cells = _mapGenerator.Data.Cells;
		_cellCountryIds = new int[cells.Length];
		Array.Fill(_cellCountryIds, -1);
		_countries = new List<Country>();

		var landCellIds = new List<int>();
		for (int i = 0; i < cells.Length; i++)
		{
			if (cells[i].IsLand)
			{
				landCellIds.Add(i);
			}
		}

		if (landCellIds.Count == 0)
		{
			_mapGenerator.Data.Countries = Array.Empty<Country>();
			_mapGenerator.Data.CellCountryIds = _cellCountryIds;
			return;
		}

		var countryCount = Mathf.Clamp(_countryCount, 1, landCellIds.Count);
		var available = new List<int>(landCellIds);
		var seedCells = new List<int>();

		for (int i = 0; i < countryCount; i++)
		{
			var index = _mapGenerator.PRNG.NextInt(0, available.Count - 1);
			var seedCellId = available[index];
			available.RemoveAt(index);
			seedCells.Add(seedCellId);

			var country = new Country
			{
				Id = i,
				Name = $"国家 {i + 1}",
				Color = GenerateCountryColor(i, countryCount),
				CapitalCellId = seedCellId
			};

			_countries.Add(country);
			_cellCountryIds[seedCellId] = i;
		}

		var queue = new Queue<int>(seedCells);
		while (queue.Count > 0)
		{
			var cellId = queue.Dequeue();
			var countryId = _cellCountryIds[cellId];
			var cell = cells[cellId];
			foreach (var neighborId in cell.NeighborIds)
			{
				if (neighborId < 0 || neighborId >= cells.Length)
				{
					continue;
				}

				if (!cells[neighborId].IsLand || _cellCountryIds[neighborId] != -1)
				{
					continue;
				}

				_cellCountryIds[neighborId] = countryId;
				queue.Enqueue(neighborId);
			}
		}

		for (int i = 0; i < cells.Length; i++)
		{
			if (!cells[i].IsLand || _cellCountryIds[i] != -1)
			{
				continue;
			}

			var closestCountry = 0;
			var closestDist = float.MaxValue;
			for (int c = 0; c < seedCells.Count; c++)
			{
				var seedCellId = seedCells[c];
				var dist = cells[i].Position.DistanceSquaredTo(cells[seedCellId].Position);
				if (dist < closestDist)
				{
					closestDist = dist;
					closestCountry = c;
				}
			}

			_cellCountryIds[i] = closestCountry;
		}

		MergeSmallCountries(cells);

		foreach (var country in _countries)
		{
			country.CellIds.Clear();
		}

		for (int i = 0; i < _cellCountryIds.Length; i++)
		{
			var countryId = _cellCountryIds[i];
			if (countryId >= 0 && countryId < _countries.Count)
			{
				_countries[countryId].CellIds.Add(i);
			}
		}

		for (int i = 0; i < _countries.Count; i++)
		{
			var country = _countries[i];
			if (country.CellIds.Count == 0)
			{
				continue;
			}

			int bestCellId = -1;
			float bestArea = -1f;
			foreach (var cellId in country.CellIds)
			{
				var cell = cells[cellId];
				if (cell.Vertices == null || cell.Vertices.Count < 3)
				{
					continue;
				}

				float area = Mathf.Abs(ComputePolygonArea(cell.Vertices));
				if (area > bestArea)
				{
					bestArea = area;
					bestCellId = cellId;
				}
			}

			if (bestCellId != -1)
			{
				country.Center = ComputePolygonCentroid(cells[bestCellId].Vertices);
			}
			else
			{
				Vector2 sum = Vector2.Zero;
				foreach (var cellId in country.CellIds)
				{
					sum += cells[cellId].Position;
				}
				country.Center = sum / country.CellIds.Count;
			}
		}

		_mapGenerator.Data.Countries = _countries.ToArray();
		_mapGenerator.Data.CellCountryIds = _cellCountryIds;

		GenerateBurgs();
		GenerateRoutes();
	}

	private void GenerateBurgs()
	{
		_countryBurgCellIds.Clear();
		if (_countries == null || _countries.Count == 0 || _mapGenerator?.Data?.Cells == null)
		{
			return;
		}

		var cells = _mapGenerator.Data.Cells;
		for (int i = 0; i < _countries.Count; i++)
		{
			var country = _countries[i];
			var burgs = new List<int>();

			if (country.CapitalCellId >= 0 && country.CapitalCellId < cells.Length)
			{
				burgs.Add(country.CapitalCellId);
			}

			if (country.CellIds == null || country.CellIds.Count == 0)
			{
				_countryBurgCellIds.Add(burgs);
				continue;
			}

			int desiredCount = _mapGenerator.PRNG.NextInt(_burgsPerCountryMin, _burgsPerCountryMax);
			if (country.CellIds.Count < 12)
			{
				desiredCount = Math.Min(desiredCount, 1);
			}

			int safety = 0;
			while (burgs.Count < desiredCount && safety < 200)
			{
				safety++;
				int cellId = PickWeightedBurgCell(country, cells);
				if (cellId < 0 || cellId >= cells.Length || !cells[cellId].IsLand)
				{
					continue;
				}

				if (burgs.Contains(cellId))
				{
					continue;
				}

				var cell = cells[cellId];
				if (cell.Height > 0.88f)
				{
					continue;
				}

				burgs.Add(cellId);
			}

			_countryBurgCellIds.Add(burgs);
		}
	}

	private int PickWeightedBurgCell(Country country, Cell[] cells)
	{
		if (country.CellIds == null || country.CellIds.Count == 0)
		{
			return -1;
		}

		float totalWeight = 0f;
		var weights = new float[country.CellIds.Count];
		for (int i = 0; i < country.CellIds.Count; i++)
		{
			int cellId = country.CellIds[i];
			if (cellId < 0 || cellId >= cells.Length || !cells[cellId].IsLand)
			{
				weights[i] = 0f;
				continue;
			}

			var cell = cells[cellId];
			float weight = 1f;
			if (cell.Height < 0.45f)
			{
				weight += 0.6f;
			}
			else if (cell.Height > 0.75f)
			{
				weight *= 0.4f;
			}

			if (cell.RiverId > 0)
			{
				weight += 0.8f;
			}

			if (cell.DistanceField > 0 && cell.DistanceField <= 3)
			{
				weight += 0.5f;
			}

			weights[i] = Mathf.Max(0.05f, weight);
			totalWeight += weights[i];
		}

		if (totalWeight <= 0f)
		{
			return country.CellIds[_mapGenerator.PRNG.NextInt(0, country.CellIds.Count - 1)];
		}

		float pick = _mapGenerator.PRNG.NextFloat() * totalWeight;
		for (int i = 0; i < country.CellIds.Count; i++)
		{
			pick -= weights[i];
			if (pick <= 0f)
			{
				return country.CellIds[i];
			}
		}

		return country.CellIds[^1];
	}

	private void GenerateRoutes()
	{
		_routePaths.Clear();

		if (_countries == null || _countries.Count < 2 || _mapGenerator?.Data?.Cells == null)
		{
			return;
		}

		var cells = _mapGenerator.Data.Cells;
		var routeNodeCountries = new List<int>();
		var routeNodeCells = new List<int>();

		for (int i = 0; i < _countries.Count; i++)
		{
			if (i >= _countryBurgCellIds.Count || _countryBurgCellIds[i].Count == 0)
			{
				int fallbackCellId = GetRouteEndpointCellId(_countries[i], cells);
				if (fallbackCellId != -1)
				{
					routeNodeCountries.Add(i);
					routeNodeCells.Add(fallbackCellId);
				}
				continue;
			}

			foreach (var cellId in _countryBurgCellIds[i])
			{
				if (cellId == -1)
				{
					continue;
				}

				routeNodeCountries.Add(i);
				routeNodeCells.Add(cellId);
			}
		}

		if (routeNodeCells.Count < 2)
		{
			return;
		}

		var edges = BuildRouteCandidateEdges(routeNodeCells, cells);
		var selectedEdges = BuildRouteNetwork(routeNodeCells.Count, edges);

		foreach (var edge in selectedEdges)
		{
			int startCellId = routeNodeCells[edge.Edge.A];
			int endCellId = routeNodeCells[edge.Edge.B];
			if (TryBuildRoutePath(startCellId, endCellId, cells, out var path))
			{
				_routePaths.Add(new RoutePath(SimplifyRoute(path), edge.IsPrimary));
			}
		}
	}

	private sealed class RoutePath
	{
		public List<Vector2> Points { get; }
		public bool IsPrimary { get; }

		public RoutePath(List<Vector2> points, bool isPrimary)
		{
			Points = points;
			IsPrimary = isPrimary;
		}
	}

	private int GetRouteEndpointCellId(Country country, Cell[] cells)
	{
		if (country == null || cells == null || cells.Length == 0)
		{
			return -1;
		}

		if (country.CapitalCellId >= 0 && country.CapitalCellId < cells.Length && cells[country.CapitalCellId].IsLand)
		{
			return country.CapitalCellId;
		}

		if (country.CellIds == null || country.CellIds.Count == 0)
		{
			return -1;
		}

		int bestCellId = -1;
		float bestDist = float.MaxValue;
		foreach (var cellId in country.CellIds)
		{
			if (cellId < 0 || cellId >= cells.Length || !cells[cellId].IsLand)
			{
				continue;
			}

			float dist = cells[cellId].Position.DistanceSquaredTo(country.Center);
			if (dist < bestDist)
			{
				bestDist = dist;
				bestCellId = cellId;
			}
		}

		return bestCellId;
	}

	private readonly struct RouteEdge
	{
		public readonly int A;
		public readonly int B;
		public readonly float Distance;

		public RouteEdge(int a, int b, float distance)
		{
			A = a;
			B = b;
			Distance = distance;
		}
	}

	private readonly struct RouteLink
	{
		public readonly RouteEdge Edge;
		public readonly bool IsPrimary;

		public RouteLink(RouteEdge edge, bool isPrimary)
		{
			Edge = edge;
			IsPrimary = isPrimary;
		}
	}

	private List<RouteEdge> BuildRouteCandidateEdges(List<int> routeNodeCells, Cell[] cells)
	{
		var edges = new List<RouteEdge>();
		for (int i = 0; i < routeNodeCells.Count; i++)
		{
			var posA = cells[routeNodeCells[i]].Position;
			for (int j = i + 1; j < routeNodeCells.Count; j++)
			{
				var posB = cells[routeNodeCells[j]].Position;
				float distance = posA.DistanceTo(posB);
				edges.Add(new RouteEdge(i, j, distance));
			}
		}

		edges.Sort((a, b) => a.Distance.CompareTo(b.Distance));
		return edges;
	}

	private List<RouteLink> BuildRouteNetwork(int nodeCount, List<RouteEdge> edges)
	{
		var selected = new List<RouteLink>();
		if (nodeCount <= 1)
		{
			return selected;
		}

		var unionFind = new UnionFind(nodeCount);
		for (int i = 0; i < edges.Count; i++)
		{
			var edge = edges[i];
			if (unionFind.Union(edge.A, edge.B))
			{
				selected.Add(new RouteLink(edge, true));
				if (selected.Count == nodeCount - 1)
				{
					break;
				}
			}
		}

		var nearestDistance = new float[nodeCount];
		Array.Fill(nearestDistance, float.MaxValue);
		foreach (var edge in edges)
		{
			if (edge.Distance < nearestDistance[edge.A])
			{
				nearestDistance[edge.A] = edge.Distance;
			}
			if (edge.Distance < nearestDistance[edge.B])
			{
				nearestDistance[edge.B] = edge.Distance;
			}
		}

		float avgNearest = 0f;
		int count = 0;
		for (int i = 0; i < nearestDistance.Length; i++)
		{
			if (!float.IsInfinity(nearestDistance[i]) && !float.IsNaN(nearestDistance[i]))
			{
				avgNearest += nearestDistance[i];
				count++;
			}
		}
		if (count > 0)
		{
			avgNearest /= count;
		}

		float extraThreshold = avgNearest * _routeExtraConnectionScale;
		var selectedLookup = new HashSet<(int, int)>();
		foreach (var edge in selected)
		{
			selectedLookup.Add(NormalizeEdge(edge.Edge.A, edge.Edge.B));
		}

		foreach (var edge in edges)
		{
			if (edge.Distance > extraThreshold)
			{
				break;
			}

			var key = NormalizeEdge(edge.A, edge.B);
			if (selectedLookup.Contains(key))
			{
				continue;
			}

			if (_mapGenerator?.PRNG != null && _mapGenerator.PRNG.NextFloat() > _routeExtraConnectionChance)
			{
				continue;
			}

			selected.Add(new RouteLink(edge, false));
			selectedLookup.Add(key);
		}

		return selected;
	}

	private static (int, int) NormalizeEdge(int a, int b)
	{
		return a < b ? (a, b) : (b, a);
	}

	private bool TryBuildRoutePath(int startCellId, int goalCellId, Cell[] cells, out List<Vector2> path)
	{
		path = null;
		if (startCellId == goalCellId || startCellId < 0 || goalCellId < 0)
		{
			return false;
		}

		int cellCount = cells.Length;
		var cameFrom = new int[cellCount];
		var gScore = new float[cellCount];
		var closed = new bool[cellCount];
		for (int i = 0; i < cellCount; i++)
		{
			cameFrom[i] = -1;
			gScore[i] = float.PositiveInfinity;
		}

		var openSet = new PriorityQueue<int, float>();
		gScore[startCellId] = 0f;
		openSet.Enqueue(startCellId, Heuristic(cells[startCellId], cells[goalCellId]));

		while (openSet.Count > 0)
		{
			openSet.TryDequeue(out int current, out _);
			if (closed[current])
			{
				continue;
			}

			if (current == goalCellId)
			{
				path = ReconstructPath(cameFrom, current, cells);
				return path.Count >= 2;
			}

			closed[current] = true;
			var currentCell = cells[current];
			for (int i = 0; i < currentCell.NeighborIds.Count; i++)
			{
				int neighborId = currentCell.NeighborIds[i];
				if (neighborId < 0 || neighborId >= cellCount)
				{
					continue;
				}

				if (closed[neighborId])
				{
					continue;
				}

				var neighborCell = cells[neighborId];
				float stepCost = ComputeRouteStepCost(currentCell, neighborCell);
				float tentativeScore = gScore[current] + stepCost;
				if (tentativeScore < gScore[neighborId])
				{
					cameFrom[neighborId] = current;
					gScore[neighborId] = tentativeScore;
					float estimate = tentativeScore + Heuristic(neighborCell, cells[goalCellId]);
					openSet.Enqueue(neighborId, estimate);
				}
			}
		}

		return false;
	}

	private float ComputeRouteStepCost(Cell from, Cell to)
	{
		float distance = from.Position.DistanceTo(to.Position);
		float slope = Mathf.Abs(to.Height - from.Height);
		float elevation = (from.Height + to.Height) * 0.5f;
		float penalty = 1f + slope * _routeSlopeWeight + Mathf.Pow(Mathf.Max(0f, elevation - 0.52f), 2f) * _routeElevationWeight;
		if (elevation < 0.55f)
		{
			penalty *= 0.92f;
		}

		if (!from.IsLand || !to.IsLand)
		{
			penalty += _routeWaterPenalty;
		}

		if (from.RiverId > 0 || to.RiverId > 0)
		{
			if (from.RiverId > 0 && from.RiverId == to.RiverId)
			{
				penalty *= RouteRiverValleyBonus;
			}
			else
			{
				penalty *= RouteRiverCrossPenalty;
				if (IsBridgeCandidate(from, to))
				{
					penalty *= _routeBridgePenaltyMultiplier;
				}
			}
		}

		return distance * penalty;
	}

	private bool IsBridgeCandidate(Cell from, Cell to)
	{
		if (!from.IsLand || !to.IsLand)
		{
			return false;
		}

		if (from.RiverId == 0 || to.RiverId == 0)
		{
			return false;
		}

		int flux = Math.Max(from.Flux, to.Flux);
		return flux <= _routeBridgeFluxThreshold;
	}

	private float Heuristic(Cell from, Cell to)
	{
		return from.Position.DistanceTo(to.Position);
	}

	private static List<Vector2> ReconstructPath(int[] cameFrom, int current, Cell[] cells)
	{
		var path = new List<Vector2>();
		while (current != -1)
		{
			path.Add(cells[current].Position);
			current = cameFrom[current];
		}

		path.Reverse();
		return path;
	}

	private static List<Vector2> SimplifyRoute(List<Vector2> points)
	{
		if (points == null || points.Count <= 2)
		{
			return points ?? new List<Vector2>();
		}

		var simplified = new List<Vector2> { points[0] };
		var prevDir = (points[1] - points[0]);
		if (prevDir.LengthSquared() > 0.0001f)
		{
			prevDir = prevDir.Normalized();
		}

		for (int i = 1; i < points.Count - 1; i++)
		{
			var dir = (points[i + 1] - points[i]);
			if (dir.LengthSquared() <= 0.0001f)
			{
				continue;
			}

			dir = dir.Normalized();
			if (prevDir.LengthSquared() == 0 || dir.Dot(prevDir) < RouteTurnDotThreshold)
			{
				simplified.Add(points[i]);
				prevDir = dir;
			}
		}

		simplified.Add(points[^1]);
		return simplified;
	}

	private sealed class UnionFind
	{
		private readonly int[] _parent;
		private readonly int[] _rank;

		public UnionFind(int size)
		{
			_parent = new int[size];
			_rank = new int[size];
			for (int i = 0; i < size; i++)
			{
				_parent[i] = i;
				_rank[i] = 0;
			}
		}

		public int Find(int x)
		{
			if (_parent[x] != x)
			{
				_parent[x] = Find(_parent[x]);
			}
			return _parent[x];
		}

		public bool Union(int a, int b)
		{
			int rootA = Find(a);
			int rootB = Find(b);
			if (rootA == rootB)
			{
				return false;
			}

			if (_rank[rootA] < _rank[rootB])
			{
				_parent[rootA] = rootB;
			}
			else if (_rank[rootA] > _rank[rootB])
			{
				_parent[rootB] = rootA;
			}
			else
			{
				_parent[rootB] = rootA;
				_rank[rootA]++;
			}

			return true;
		}
	}

	private Color GenerateCountryColor(int index, int count)
	{
		if (_visualStyleMode == MapVisualStyleSelection.Monochrome)
		{
			var grayBase = 0.3f + 0.6f * (count > 1 ? (float)index / (count - 1) : 0.5f);
			var jitter = _mapGenerator.PRNG.NextRange(-0.06f, 0.06f);
			var gray = Mathf.Clamp(grayBase + jitter, 0.15f, 0.92f);
			return new Color(gray, gray, gray, 0.95f);
		}

		if (_visualStyleMode == MapVisualStyleSelection.Parchment)
		{
			var parchmentHue = 0.08f + 0.1f * (count > 0 ? (float)index / count : 0f);
			var parchmentSaturation = 0.26f + _mapGenerator.PRNG.NextRange(-0.04f, 0.06f);
			var parchmentValue = 0.74f + _mapGenerator.PRNG.NextRange(-0.04f, 0.05f);
			return Color.FromHsv(parchmentHue, Mathf.Clamp(parchmentSaturation, 0.18f, 0.4f), Mathf.Clamp(parchmentValue, 0.58f, 0.88f), 0.9f);
		}

		if (_visualStyleMode == MapVisualStyleSelection.NavalChart)
		{
			var navalHue = 0.48f + 0.18f * (count > 0 ? (float)index / count : 0f);
			var navalSaturation = 0.34f + _mapGenerator.PRNG.NextRange(-0.05f, 0.08f);
			var navalValue = 0.82f + _mapGenerator.PRNG.NextRange(-0.05f, 0.08f);
			return Color.FromHsv(Mathf.PosMod(navalHue, 1f), Mathf.Clamp(navalSaturation, 0.2f, 0.62f), Mathf.Clamp(navalValue, 0.62f, 0.95f), 0.9f);
		}

		var hue = count > 0 ? (float)index / count : 0f;
		var hueOffset = _mapGenerator.PRNG.NextRange(-0.03f, 0.03f);
		hue = Mathf.PosMod(hue + hueOffset, 1f);
		var saturation = 0.45f + _mapGenerator.PRNG.NextRange(-0.05f, 0.1f);
		var value = 0.85f + _mapGenerator.PRNG.NextRange(-0.05f, 0.05f);
		return Color.FromHsv(hue, Mathf.Clamp(saturation, 0.25f, 0.9f), Mathf.Clamp(value, 0.6f, 0.95f));
	}

	private void MergeSmallCountries(Cell[] cells)
	{
		if (_cellCountryIds == null || _countries.Count == 0)
		{
			return;
		}

		int landCellCount = 0;
		for (int i = 0; i < cells.Length; i++)
		{
			if (cells[i].IsLand)
			{
				landCellCount++;
			}
		}

		if (landCellCount == 0)
		{
			return;
		}

		int targetSize = Mathf.Max(1, landCellCount / Mathf.Max(1, _countries.Count));
		int minSize = Mathf.Max(1, Mathf.Max(_minCountryCells, Mathf.CeilToInt(targetSize * 0.4f)));

		var sizes = new int[_countries.Count];
		var countryCells = new List<int>[_countries.Count];
		for (int i = 0; i < _countries.Count; i++)
		{
			countryCells[i] = new List<int>();
		}

		for (int i = 0; i < cells.Length; i++)
		{
			if (!cells[i].IsLand)
			{
				continue;
			}

			var countryId = _cellCountryIds[i];
			if (countryId >= 0 && countryId < sizes.Length)
			{
				sizes[countryId]++;
				countryCells[countryId].Add(i);
			}
		}

		bool merged = true;
		while (merged)
		{
			merged = false;
			for (int countryId = 0; countryId < sizes.Length; countryId++)
			{
				if (sizes[countryId] == 0 || sizes[countryId] >= minSize)
				{
					continue;
				}

				var neighborCounts = new int[_countries.Count];
				int bestNeighbor = -1;
				int bestBorder = -1;
				int bestSize = -1;

				foreach (var cellId in countryCells[countryId])
				{
					foreach (var neighborId in cells[cellId].NeighborIds)
					{
						if (neighborId < 0 || neighborId >= cells.Length || !cells[neighborId].IsLand)
						{
							continue;
						}

						int neighborCountry = _cellCountryIds[neighborId];
						if (neighborCountry == countryId || neighborCountry < 0 || neighborCountry >= sizes.Length)
						{
							continue;
						}

						int count = ++neighborCounts[neighborCountry];
						int size = sizes[neighborCountry];
						if (count > bestBorder || (count == bestBorder && size > bestSize))
						{
							bestBorder = count;
							bestSize = size;
							bestNeighbor = neighborCountry;
						}
					}
				}

				if (bestNeighbor == -1)
				{
					for (int i = 0; i < sizes.Length; i++)
					{
						if (i == countryId || sizes[i] == 0)
						{
							continue;
						}

						if (sizes[i] > bestSize)
						{
							bestSize = sizes[i];
							bestNeighbor = i;
						}
					}
				}

				if (bestNeighbor == -1)
				{
					continue;
				}

				var cellsToMove = countryCells[countryId];
				for (int i = 0; i < cellsToMove.Count; i++)
				{
					int cellId = cellsToMove[i];
					_cellCountryIds[cellId] = bestNeighbor;
					countryCells[bestNeighbor].Add(cellId);
				}

				sizes[bestNeighbor] += sizes[countryId];
				sizes[countryId] = 0;
				countryCells[countryId] = new List<int>();
				merged = true;
			}
		}
	}

	private static float ComputePolygonArea(List<Vector2> vertices)
	{
		double area = 0.0;
		int count = vertices.Count;
		for (int i = 0; i < count; i++)
		{
			var a = vertices[i];
			var b = vertices[(i + 1) % count];
			area += a.X * b.Y - b.X * a.Y;
		}

		return (float)(area * 0.5);
	}

	private static Vector2 ComputePolygonCentroid(List<Vector2> vertices)
	{
		double area = 0.0;
		double cx = 0.0;
		double cy = 0.0;
		int count = vertices.Count;
		for (int i = 0; i < count; i++)
		{
			var a = vertices[i];
			var b = vertices[(i + 1) % count];
			double cross = a.X * b.Y - b.X * a.Y;
			area += cross;
			cx += (a.X + b.X) * cross;
			cy += (a.Y + b.Y) * cross;
		}

		if (Math.Abs(area) < 0.00001)
		{
			Vector2 sum = Vector2.Zero;
			foreach (var v in vertices)
			{
				sum += v;
			}
			return sum / vertices.Count;
		}

		area *= 0.5;
		double factor = 1.0 / (6.0 * area);
		return new Vector2((float)(cx * factor), (float)(cy * factor));
	}

	private bool TryGetSharedEdge(List<Vector2> verticesA, List<Vector2> verticesB, out Vector2 v1, out Vector2 v2)
	{
		v1 = Vector2.Zero;
		v2 = Vector2.Zero;
		int found = 0;
		float epsilonSq = SharedVertexEpsilon * SharedVertexEpsilon;

		for (int i = 0; i < verticesA.Count; i++)
		{
			var a = verticesA[i];
			for (int j = 0; j < verticesB.Count; j++)
			{
				var b = verticesB[j];
				if (a.DistanceSquaredTo(b) <= epsilonSq)
				{
					if (found == 0)
					{
						v1 = a;
						found = 1;
					}
					else if (found == 1 && a.DistanceSquaredTo(v1) > epsilonSq)
					{
						v2 = a;
						return true;
					}
				}
			}
		}

		return false;
	}

	private void SelectCell(int cellId, bool emitSignal = true)
	{
		bool selectionChanged = _selectedCellId != cellId;
		_selectedCellId = cellId;
		if (selectionChanged)
		{
			QueueRedraw();
		}

		if (emitSignal)
		{
			EmitSignal(SignalName.CellSelected, cellId);
		}

		if (cellId != -1 && _mapGenerator?.Data != null && cellId >= 0 && cellId < _mapGenerator.Data.Cells.Length)
		{
			var cell = _mapGenerator.Data.Cells[cellId];
			GD.Print($"Selected Cell {cellId}: Pos={cell.Position}, Height={cell.Height:F3}, Biome={cell.BiomeId}, IsLand={cell.IsLand}");
		}
	}

	private int GetCellAtPosition(Vector2 mapPos)
	{
		if (_mapGenerator?.Data?.Cells == null) return -1;

		float minDistanceSq = float.MaxValue;
		int closestCellId = -1;

		// 简单的遍历查找最近的种子点 (Voronoi 性质)
		// 对于几千个点，遍历是可以接受的。如果点非常多，可以使用空间分区优化。
		foreach (var cell in _mapGenerator.Data.Cells)
		{
			float distSq = cell.Position.DistanceSquaredTo(mapPos);
			if (distSq < minDistanceSq)
			{
				minDistanceSq = distSq;
				closestCellId = cell.Id;
			}
		}

		// 检查是否在有效距离内（防止点击太远的地方也选中边缘点）
		// 这里的阈值可以根据地图大小调整，或者如果确实是全覆盖Voronoi，只要在包围盒内就应该选中
		return closestCellId;
	}

	public MapData GetMapData()
	{
		return _mapGenerator?.Data;
	}

	private void GenerateChildMapFromParent(MapContext context)
	{
		var parentData = context.ParentMapData;
		if (parentData?.Cells == null || parentData.Cells.Length == 0 || !context.ParentCellId.HasValue)
		{
			CellCount = context.CellCount;
			GenerateMapWithSeed(context.Seed);
			return;
		}

		int parentCellId = context.ParentCellId.Value;
		if (parentCellId < 0 || parentCellId >= parentData.Cells.Length)
		{
			CellCount = context.CellCount;
			GenerateMapWithSeed(context.Seed);
			return;
		}

		CellCount = context.CellCount;
		GenerateMapWithSeed(context.Seed);

		if (_mapGenerator?.Data?.Cells == null)
		{
			return;
		}

		var parentCell = parentData.Cells[parentCellId];
		var parentCellVertices = parentCell.Vertices;
		if (parentCellVertices == null || parentCellVertices.Count < 3)
		{
			return;
		}

		var childCells = _mapGenerator.Data.Cells;
		var childMapSize = _mapGenerator.Data.MapSize;
		var linkedParentIds = BuildLinkedParentCellIds(parentData, parentCellId);
		int maxParentNeighborId = parentData.Cells.Length - 1;

		for (int i = 0; i < childCells.Length; i++)
		{
			var child = childCells[i];
			var parentSpacePos = MapChildToParentSpace(child.Position, childMapSize, parentCellVertices);
			bool insideParentCell = IsPointInPolygon(parentSpacePos, parentCellVertices);
			int closestParentCellId = FindClosestParentCell(parentData, parentSpacePos, linkedParentIds);
			if (closestParentCellId < 0)
			{
				closestParentCellId = parentCellId;
			}

			if (insideParentCell)
			{
				closestParentCellId = parentCellId;
			}

			int linkedParentCellId = Mathf.Clamp(closestParentCellId, 0, maxParentNeighborId);
			child.GridCellId = linkedParentCellId;

			var linkedParentCell = parentData.Cells[linkedParentCellId];
			if (!linkedParentCell.IsLand)
			{
				child.IsLand = false;
				child.Height = Mathf.Min(child.Height, _generationWaterLevel - 0.03f);
				child.BiomeId = linkedParentCell.BiomeId;
				child.RenderColor = linkedParentCell.RenderColor;
			}
			else
			{
				float parentHeight = Mathf.Clamp(linkedParentCell.Height, 0.05f, 0.98f);
				float mixedHeight = Mathf.Clamp(parentHeight * 0.78f + child.Height * 0.22f, 0.05f, 0.99f);
				child.Height = mixedHeight;
				child.IsLand = mixedHeight > _generationWaterLevel;
				child.BiomeId = linkedParentCell.BiomeId;
				if (child.IsLand)
				{
					child.RenderColor = linkedParentCell.RenderColor;
				}
			}
		}

		GenerateCountries();
		QueueRedraw();
	}

	private static Vector2 MapChildToParentSpace(Vector2 childPos, Vector2 childMapSize, List<Vector2> parentPolygon)
	{
		var parentRect = GetPolygonBounds(parentPolygon);
		float normalizedWidth = Mathf.Max(1f, childMapSize.X);
		float normalizedHeight = Mathf.Max(1f, childMapSize.Y);
		float nx = Mathf.Clamp(childPos.X / normalizedWidth, 0f, 1f);
		float ny = Mathf.Clamp(childPos.Y / normalizedHeight, 0f, 1f);
		return new Vector2(
			Mathf.Lerp(parentRect.Position.X, parentRect.End.X, nx),
			Mathf.Lerp(parentRect.Position.Y, parentRect.End.Y, ny));
	}

	private static Rect2 GetPolygonBounds(List<Vector2> polygon)
	{
		if (polygon == null || polygon.Count == 0)
		{
			return new Rect2(Vector2.Zero, Vector2.One);
		}

		var min = polygon[0];
		var max = polygon[0];
		for (int i = 1; i < polygon.Count; i++)
		{
			var p = polygon[i];
			min = new Vector2(Mathf.Min(min.X, p.X), Mathf.Min(min.Y, p.Y));
			max = new Vector2(Mathf.Max(max.X, p.X), Mathf.Max(max.Y, p.Y));
		}

		var size = max - min;
		if (size.X < 0.001f)
		{
			size.X = 0.001f;
		}
		if (size.Y < 0.001f)
		{
			size.Y = 0.001f;
		}

		return new Rect2(min, size);
	}

	private static int FindClosestParentCell(MapData parentData, Vector2 parentSpacePos, List<int> allowedCellIds)
	{
		if (parentData?.Cells == null || parentData.Cells.Length == 0)
		{
			return -1;
		}

		if (allowedCellIds == null || allowedCellIds.Count == 0)
		{
			return -1;
		}

		float minDistanceSq = float.MaxValue;
		int bestId = -1;
		for (int i = 0; i < allowedCellIds.Count; i++)
		{
			int cellId = allowedCellIds[i];
			if (cellId < 0 || cellId >= parentData.Cells.Length)
			{
				continue;
			}

			var parentCell = parentData.Cells[cellId];
			float distSq = parentCell.Position.DistanceSquaredTo(parentSpacePos);
			if (distSq < minDistanceSq)
			{
				minDistanceSq = distSq;
				bestId = parentCell.Id;
			}
		}

		return bestId;
	}

	private static List<int> BuildLinkedParentCellIds(MapData parentData, int parentCellId)
	{
		var result = new List<int>();
		if (parentData?.Cells == null || parentCellId < 0 || parentCellId >= parentData.Cells.Length)
		{
			return result;
		}

		result.Add(parentCellId);
		var parentCell = parentData.Cells[parentCellId];
		if (parentCell.NeighborIds != null)
		{
			for (int i = 0; i < parentCell.NeighborIds.Count; i++)
			{
				int neighborId = parentCell.NeighborIds[i];
				if (neighborId < 0 || neighborId >= parentData.Cells.Length)
				{
					continue;
				}

				if (!result.Contains(neighborId))
				{
					result.Add(neighborId);
				}
			}
		}

		return result;
	}

	private static bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
	{
		if (polygon == null || polygon.Count < 3)
		{
			return false;
		}

		bool inside = false;
		for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
		{
			var a = polygon[i];
			var b = polygon[j];
			bool intersects = ((a.Y > point.Y) != (b.Y > point.Y))
				&& (point.X < (b.X - a.X) * (point.Y - a.Y) / Mathf.Max(0.000001f, b.Y - a.Y) + a.X);
			if (intersects)
			{
				inside = !inside;
			}
		}

		return inside;
	}

	private void DrawCountryNames()
	{
		if (_countries == null || _countries.Count == 0)
		{
			return;
		}

		var font = GetThemeFont("font", "Label");
		if (font == null)
		{
			return;
		}

		int fontSize = GetThemeFontSize("font_size", "Label");
		if (fontSize <= 0)
		{
			fontSize = 14;
		}

		foreach (var country in _countries)
		{
			if (country.CellIds == null || country.CellIds.Count == 0)
			{
				continue;
			}

			var screenPos = TransformToScreenCoordinates(country.Center);
			DrawString(font, screenPos, country.Name, HorizontalAlignment.Center, -1, fontSize, NameColor);
		}
	}

	private static Color LerpColor(Color from, Color to, float t)
	{
		t = Mathf.Clamp(t, 0f, 1f);
		return new Color(
			Mathf.Lerp(from.R, to.R, t),
			Mathf.Lerp(from.G, to.G, t),
			Mathf.Lerp(from.B, to.B, t),
			Mathf.Lerp(from.A, to.A, t)
		);
	}

	// 缓存变换参数
	private const float ViewMargin = 20f;
	private float _currentScale = 1f;
	private Vector2 _currentOffset = Vector2.Zero;

	/// <summary>
	/// 将地图坐标转换为屏幕坐标
	/// 地图坐标范围是 (0,0) 到 (512,512)，屏幕坐标需要适应 Node2D 的绘制区域
	/// </summary>
	private Vector2 TransformToScreenCoordinates(Vector2 mapPos)
	{
		UpdateTransformParameters();
		return new Vector2(
			_currentOffset.X + mapPos.X * _currentScale,
			_currentOffset.Y + mapPos.Y * _currentScale
		);
	}

	/// <summary>
	/// 将屏幕坐标转换为地图坐标
	/// </summary>
	private Vector2 TransformToMapCoordinates(Vector2 screenPos)
	{
		UpdateTransformParameters();
		return new Vector2(
			(screenPos.X - _currentOffset.X) / _currentScale,
			(screenPos.Y - _currentOffset.Y) / _currentScale
		);
	}

	public Vector2 ScreenToMap(Vector2 screenPos)
	{
		return TransformToMapCoordinates(screenPos);
	}

	public float GetMinViewScaleForArea()
	{
		var viewSize = GetViewSize();
		var mapSize = GetMapSize();
		if (viewSize.X <= 0f || viewSize.Y <= 0f || mapSize.X <= 0f || mapSize.Y <= 0f)
		{
			return 0.05f;
		}

		var contentSize = new Vector2(
			Mathf.Max(1f, viewSize.X - ViewMargin * 2f),
			Mathf.Max(1f, viewSize.Y - ViewMargin * 2f)
		);
		var fitScale = Mathf.Min(contentSize.X / mapSize.X, contentSize.Y / mapSize.Y);
		if (fitScale <= 0.000001f)
		{
			return 0.05f;
		}

		var viewArea = viewSize.X * viewSize.Y;
		var mapArea = mapSize.X * mapSize.Y;
		var minCurrentScale = Mathf.Sqrt(viewArea / (mapArea * 3f));
		return Mathf.Max(0.05f, minCurrentScale / fitScale);
	}

	public Vector2 GetCameraOffsetForZoomFocus(Vector2 mapPos, Vector2 screenPos, float viewScale)
	{
		ComputeBaseTransform(viewScale, out var scale, out var baseOffset);
		if (scale <= 0.000001f)
		{
			return _cameraMapOffset;
		}

		return mapPos - (screenPos - baseOffset) / scale;
	}

	private void UpdateTransformParameters()
	{
		var viewSize = GetViewSize();
		var mapSize = GetMapSize();
		_viewScale = ClampViewScale(_viewScale);

		ComputeBaseTransform(_viewScale, out _currentScale, out var baseOffset);
		var mapSizeScaled = mapSize * _currentScale;
		_currentOffset = baseOffset;
		if (mapSizeScaled.X > viewSize.X)
		{
			_currentOffset.X -= _cameraMapOffset.X * _currentScale;
		}

		if (mapSizeScaled.Y > viewSize.Y)
		{
			_currentOffset.Y -= _cameraMapOffset.Y * _currentScale;
		}
	}

	private float ClampViewScale(float value)
	{
		var minScale = GetMinViewScaleForArea();
		var clampedMin = Mathf.Max(0.05f, minScale);
		var clampedMax = Mathf.Max(5.0f, clampedMin);
		return Mathf.Clamp(value, clampedMin, clampedMax);
	}

	private Vector2 GetViewSize()
	{
		var viewSize = Size;
		if (viewSize == Vector2.Zero)
		{
			viewSize = GetViewportRect().Size;
		}
		return viewSize;
	}

	private Vector2 GetMapSize()
	{
		var mapSize = _canvasSize;
		if (mapSize == Vector2.Zero)
		{
			mapSize = _mapGenerator?.Data?.MapSize ?? new Vector2(1024, 1024);
		}
		return mapSize;
	}

	private void ComputeBaseTransform(float viewScale, out float scale, out Vector2 baseOffset)
	{
		var viewSize = GetViewSize();
		var mapSize = GetMapSize();

		var contentWidth = Mathf.Max(1f, viewSize.X - ViewMargin * 2f);
		var contentHeight = Mathf.Max(1f, viewSize.Y - ViewMargin * 2f);
		float scaleX = contentWidth / mapSize.X;
		float scaleY = contentHeight / mapSize.Y;
		scale = Mathf.Min(scaleX, scaleY) * viewScale;

		var scaledMapSize = mapSize * scale;
		baseOffset = new Vector2(ViewMargin, ViewMargin) + (viewSize - scaledMapSize) / 2;
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
		{
			QueueRedraw();
		}
	}

	private void DrawCountryBorders()
	{
		if (_cellCountryIds == null || _mapGenerator?.Data?.Cells == null)
		{
			return;
		}

		var cells = _mapGenerator.Data.Cells;
		for (int i = 0; i < cells.Length; i++)
		{
			var cell = cells[i];
			if (cell.Vertices == null || cell.Vertices.Count < 2)
			{
				continue;
			}

			var countryId = GetCountryId(cell.Id);
			if (countryId < 0)
			{
				continue;
			}

			foreach (var neighborId in cell.NeighborIds)
			{
				if (neighborId <= cell.Id)
				{
					continue;
				}

				if (neighborId < 0 || neighborId >= cells.Length)
				{
					continue;
				}

				if (_cellCountryIds[neighborId] == countryId)
				{
					continue;
				}

				var neighbor = cells[neighborId];
				if (neighbor.Vertices == null || neighbor.Vertices.Count < 2)
				{
					continue;
				}

				if (TryGetSharedEdge(cell.Vertices, neighbor.Vertices, out var v1, out var v2))
				{
					var p1 = TransformToScreenCoordinates(v1);
					var p2 = TransformToScreenCoordinates(v2);
					DrawLine(p1, p2, CountryBorderColor, _countryBorderWidth);
				}
			}
		}
	}
}
