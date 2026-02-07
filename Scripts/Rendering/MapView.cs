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
		get => _showRivers;
		set
		{
			_showRivers = value;
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
		get => _useBiomeColors;
		set
		{
			_useBiomeColors = value;
			QueueRedraw();
		}
	}

	[Export]
	public bool ShowTerrainLayer
	{
		get => _showTerrainLayer;
		set
		{
			_showTerrainLayer = value;
			QueueRedraw();
		}
	}

	[Export]
	public bool ShowNames
	{
		get => _showNames;
		set
		{
			_showNames = value;
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
		get => _showCountries;
		set
		{
			_showCountries = value;
			QueueRedraw();
		}
	}

	[Export]
	public bool ShowCountryBorders
	{
		get => _showCountryBorders;
		set
		{
			_showCountryBorders = value;
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

	[Export]
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

	[Export]
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
			_viewScale = Mathf.Clamp(value, 0.05f, 5.0f);
			QueueRedraw();
		}
	}

	// 颜色常量
	private static readonly Color OceanLayerColor = new Color(0.2f, 0.5f, 0.8f);
	private static readonly Color RiverColor = new Color(0.3f, 0.6f, 0.9f);
	private static readonly Color HeatmapLowColor = new Color(0.12f, 0.25f, 0.85f);
	private static readonly Color HeatmapHighColor = new Color(0.92f, 0.25f, 0.15f);
	private static readonly Color NameColor = new Color(0.12f, 0.1f, 0.08f, 0.85f);
	private const float SharedVertexEpsilon = 0.01f;

	private int _cellCount = 2000;
	private int _mapWidth = 512;
	private int _mapHeight = 512;
	private float _riverDensity = 1f;
	private MapGenerator _mapGenerator;
	private bool _isGenerating = false;
	private float _viewScale = 1f;
	private Vector2 _canvasSize = new Vector2(512, 512);
	private Vector2 _cameraMapOffset = Vector2.Zero;
	private bool _showRivers = true;
	private bool _showOceanLayers = true;
	private bool _useBiomeColors = true;
	private bool _showTerrainLayer = true;
	private bool _showNames = true;
	private TerrainStyle _terrainStyleMode = TerrainStyle.Heightmap;
	private bool _showCountries = true;
	private bool _showCountryBorders = true;
	private int _countryCount = 12;
	private float _countryBorderWidth = 2f;
	private float _countryFillAlpha = 0.85f;
	private int[] _cellCountryIds;
	private List<Country> _countries = new();

	// 选择状态
	private int _selectedCellId = -1;
	[Signal] public delegate void CellSelectedEventHandler(int cellId);

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
		_canvasSize = new Vector2(_mapWidth, _mapHeight);
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

		var cells = _mapGenerator.Data.Cells;
		int cellsDrawn = 0;

		// 调试：检查坐标范围
		float minX = float.MaxValue, maxX = float.MinValue;
		float minY = float.MaxValue, maxY = float.MinValue;

		// 1. 绘制基础地形（Cell多边形）
		foreach (var cell in cells)
		{
			if (cell.Vertices != null && cell.Vertices.Count >= 3)
			{
				cellsDrawn++;
				var points = new Vector2[cell.Vertices.Count];
				for (int i = 0; i < cell.Vertices.Count; i++)
				{
					// 将坐标转换到屏幕范围
					var screenPos = TransformToScreenCoordinates(cell.Vertices[i]);
					points[i] = screenPos;
					// 统计坐标范围
					if (screenPos.X < minX) minX = screenPos.X;
					if (screenPos.X > maxX) maxX = screenPos.X;
					if (screenPos.Y < minY) minY = screenPos.Y;
					if (screenPos.Y > maxY) maxY = points[i].Y;
				}

				// 使用固定颜色确保可见
				Color color = new Color(0.2f, 0.8f, 0.2f); // 绿色
				// 如果是选中状态，高亮显示
				if (cell.Id == _selectedCellId)
				{
					color = new Color(1f, 0.8f, 0.2f); // 金黄色选中
				}
				else
				{
					color = GetCellRenderColor(cell);
				}

				var colors = new Color[cell.Vertices.Count];
				for (int i = 0; i < cell.Vertices.Count; i++)
				{
					colors[i] = color;
				}
				DrawPolygon(points, colors);
				
				// 绘制选中框线
				if (cell.Id == _selectedCellId)
				{
					Vector2[] borderPoints = new Vector2[points.Length + 1];
					Array.Copy(points, borderPoints, points.Length);
					borderPoints[points.Length] = points[0];
					DrawPolyline(borderPoints, new Color(1, 1, 1), 2f);
				}
			}
			else if (cell.Vertices != null && cell.Vertices.Count == 2)
			{
				Color color = GetCellRenderColor(cell);
				DrawLine(cell.Vertices[0], cell.Vertices[1], color, 1f);
			}
		}

		GD.Print($"_Draw: cellsDrawn={cellsDrawn}, bounds=({minX},{minY})-({maxX},{maxY})");

		// 2. 绘制海洋分层效果
		if (ShowOceanLayers)
		{
			DrawOceanLayers();
		}

		// 2.5 绘制国家边界
		if (ShowCountries && ShowCountryBorders)
		{
			DrawCountryBorders();
		}

		// 3. 绘制河流
		if (ShowRivers)
		{
			DrawRivers();
		}

		if (ShowNames && ShowCountries)
		{
			DrawCountryNames();
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

	private Color GetCellRenderColor(Cell cell)
	{
		if (ShowCountries && _cellCountryIds != null && cell.Id >= 0 && cell.Id < _cellCountryIds.Length)
		{
			var countryId = _cellCountryIds[cell.Id];
			if (countryId >= 0 && countryId < _countries.Count)
			{
				var color = _countries[countryId].Color;
				color.A *= _countryFillAlpha;
				return color;
			}
		}

		if (UseBiomeColors)
		{
			return BiomeData.GetColor(cell.BiomeId);
		}

		if (ShowTerrainLayer)
		{
			return GetTerrainColor(cell);
		}

		return new Color(0, 0, 0, 0);
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

			Vector2 sum = Vector2.Zero;
			foreach (var cellId in country.CellIds)
			{
				sum += cells[cellId].Position;
			}

			country.Center = sum / country.CellIds.Count;
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

	private void UpdateTransformParameters()
	{
		// 获取当前节点的绘制边界
		var viewSize = Size;
		if (viewSize == Vector2.Zero)
		{
			viewSize = GetViewportRect().Size;
		}
		var mapSize = _canvasSize;
		if (mapSize == Vector2.Zero)
		{
			mapSize = _mapGenerator?.Data?.MapSize ?? new Vector2(1024, 1024);
		}

		// 计算缩放比例（留一点边距）
		float margin = 20f;
		float scaleX = (viewSize.X - margin * 2) / mapSize.X;
		float scaleY = (viewSize.Y - margin * 2) / mapSize.Y;
		_currentScale = Mathf.Min(scaleX, scaleY) * _viewScale;

		// 计算中心偏移
		var scaledMapSize = mapSize * _currentScale;
		// 偏移量 = 边距 + (视口剩余空间的一半)
		_currentOffset = new Vector2(margin, margin) + (viewSize - scaledMapSize) / 2;
		_currentOffset -= _cameraMapOffset * _currentScale;
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
