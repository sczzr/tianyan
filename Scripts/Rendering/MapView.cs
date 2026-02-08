using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Core;
using FantasyMapGenerator.Scripts.Data;
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

	// 颜色常量
	private static readonly Color OceanLayerColor = new Color(0.2f, 0.5f, 0.8f);
	private static readonly Color RiverColor = new Color(0.3f, 0.6f, 0.9f);
	private static readonly Color HeatmapLowColor = new Color(0.12f, 0.25f, 0.85f);
	private static readonly Color HeatmapHighColor = new Color(0.92f, 0.25f, 0.15f);
	private static readonly Color NameColor = new Color(0.12f, 0.1f, 0.08f, 0.85f);
	private static readonly Color CellOutlineColor = new Color(0.12f, 0.1f, 0.08f, 0.2f);
	private static readonly Color GridLineColor = new Color(0.12f, 0.1f, 0.08f, 0.18f);
	private static readonly Color CoordinateTextColor = new Color(0.12f, 0.1f, 0.08f, 0.65f);
	private static readonly Color CompassColor = new Color(0.12f, 0.1f, 0.08f, 0.85f);
	private static readonly Color ScaleBarColor = new Color(0.12f, 0.1f, 0.08f, 0.9f);
	private static readonly Color VignetteColor = new Color(0, 0, 0, 0.08f);
	private const float SharedVertexEpsilon = 0.01f;
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
	private MapGenerator _mapGenerator;
	private bool _isGenerating = false;
	private bool _useMultithreading = true;
	private float _viewScale = 1f;
	private Vector2 _canvasSize = new Vector2(512, 512);
	private Vector2 _cameraMapOffset = Vector2.Zero;
	private bool _showOceanLayers = true;
	private TerrainStyle _terrainStyleMode = TerrainStyle.Heightmap;
	private int _countryCount = 12;
	private int _minCountryCells = 3;
	private float _countryBorderWidth = 2f;
	private float _countryFillAlpha = 1f;
	private float _boundaryPaddingScale = 1.5f;
	private float _boundaryStepScale = 1f;
	private int[] _cellCountryIds;
	private List<Country> _countries = new();
	private readonly List<MapLayer> _layerOrder = new(DefaultLayerOrder);

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

	public override void _Ready()
	{
		_mapGenerator = new MapGenerator();
		_mapGenerator.MapWidth = _mapWidth;
		_mapGenerator.MapHeight = _mapHeight;
		_mapGenerator.RiverDensity = _riverDensity;
		_mapGenerator.UseMultithreading = _useMultithreading;
		_mapGenerator.BoundaryPaddingScale = _boundaryPaddingScale;
		_mapGenerator.BoundaryStepScale = _boundaryStepScale;
		_canvasSize = new Vector2(_mapWidth, _mapHeight);
		ApplyLayerPreset(_layerPresetMode);
		GenerateMap();
	}

	public void GenerateMap()
	{
		if (_isGenerating) return;
		_isGenerating = true;
		_selectedCellId = -1;

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

		_mapGenerator.Generate(seed, _cellCount);
		GenerateCountries();
		QueueRedraw();

		_isGenerating = false;
	}

	public void GenerateMapWithSeed(int seed)
	{
		GenerateMapWithSeed(seed.ToString());
	}

	public void SetWaterLevel(float level)
	{
		if (_mapGenerator?.Data != null)
		{
			var heightmap = _mapGenerator.Data.Heightmap;
			var cells = _mapGenerator.Data.Cells;
			int width = (int)_mapGenerator.Data.MapSize.X;
			int height = (int)_mapGenerator.Data.MapSize.Y;

			var processor = new HeightmapProcessor(_mapGenerator.PRNG);
			processor.WaterLevel = level;
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
		DrawRect(baseRect, OceanLayerColor, true);

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
			var layerColor = new Color(OceanLayerColor.R, OceanLayerColor.G, OceanLayerColor.B, opacity);

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

				DrawLine(p1, p2, RiverColor, width);
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				// 计算点击的Cell
				var localPos = GetLocalMousePosition();
				var mapPos = TransformToMapCoordinates(localPos);
				var cellId = GetCellAtPosition(mapPos);
				
				if (cellId != -1)
				{
					SelectCell(cellId);
				}
				else
				{
					// 点击空白处，重新生成（保留之前的功能，或者可以改成取消选择）
					// GenerateMap(); 
					// 此处改为左键点击只是选择，不再重新生成。如果需要重新生成可以加个按钮或右键。
					SelectCell(-1);
				}
			}
			else if (mouseButton.ButtonIndex == MouseButton.Right)
			{
				// 右键重新生成
				GenerateMap();
			}
		}
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
		if (_countries == null || _countries.Count < 2)
		{
			return;
		}

		for (int i = 0; i < _countries.Count - 1; i++)
		{
			var a = TransformToScreenCoordinates(_countries[i].Center);
			var b = TransformToScreenCoordinates(_countries[i + 1].Center);
			DrawLine(a, b, new Color(0.5f, 0.32f, 0.18f, 0.6f), 1.5f);
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

		QueueRedraw();
	}

	public void SetLayerEnabled(MapLayer layer, bool enabled)
	{
		SetLayer(layer, enabled);
		QueueRedraw();
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
				return color;
			}
		}

		return GetTerrainColor(cell);
	}

	private Color GetTerrainColor(Cell cell)
	{
		if (!cell.IsLand)
		{
			return new Color(OceanLayerColor.R, OceanLayerColor.G, OceanLayerColor.B, 0.8f);
		}

		float height = Mathf.Clamp(cell.Height, 0f, 1f);
		switch (_terrainStyleMode)
		{
			case TerrainStyle.Contour:
				const int bands = 8;
				float bandValue = Mathf.Floor(height * bands) / bands;
				return LerpColor(HeatmapLowColor, HeatmapHighColor, bandValue);
			case TerrainStyle.Heatmap:
				return LerpColor(HeatmapLowColor, HeatmapHighColor, height);
			default:
				return cell.RenderColor;
		}
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
	}

	private Color GenerateCountryColor(int index, int count)
	{
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

	private void SelectCell(int cellId)
	{
		_selectedCellId = cellId;
		QueueRedraw();
		EmitSignal(SignalName.CellSelected, cellId);
		
		if (cellId != -1 && _mapGenerator?.Data != null)
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
