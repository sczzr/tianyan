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
public partial class MapView : Node2D
{
	[Export]
	public int CellCount
	{
		get => _cellCount;
		set
		{
			_cellCount = value;
			if (_mapGenerator != null)
			{
				GenerateMap();
				QueueRedraw();
			}
		}
	}

	[Export]
	public bool AutoRegenerate { get; set; } = false;

	[Export]
	public bool ShowRivers { get; set; } = true;

	[Export]
	public bool ShowOceanLayers { get; set; } = true;

	[Export]
	public bool UseBiomeColors { get; set; } = true;

	// 颜色常量
	private static readonly Color OceanLayerColor = new Color(0.2f, 0.5f, 0.8f);
	private static readonly Color RiverColor = new Color(0.3f, 0.6f, 0.9f);

	private int _cellCount = 2000;
	private MapGenerator _mapGenerator;
	private bool _isGenerating = false;

	// 选择状态
	private int _selectedCellId = -1;
	[Signal] public delegate void CellSelectedEventHandler(int cellId);

	public override void _Ready()
	{
		_mapGenerator = new MapGenerator();
		GenerateMap();
	}

	public void GenerateMap()
	{
		if (_isGenerating) return;
		_isGenerating = true;
		_selectedCellId = -1;

		_mapGenerator.GenerateWithNewSeed(_cellCount);
		QueueRedraw();

		_isGenerating = false;
	}

	public void GenerateMapWithSeed(string seed)
	{
		if (_isGenerating) return;
		_isGenerating = true;
		_selectedCellId = -1;

		_mapGenerator.Generate(seed, _cellCount);
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
					color = UseBiomeColors ? BiomeData.GetColor(cell.BiomeId) : cell.RenderColor;
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
				Color color = UseBiomeColors ? BiomeData.GetColor(cell.BiomeId) : cell.RenderColor;
				DrawLine(cell.Vertices[0], cell.Vertices[1], color, 1f);
			}
		}

		GD.Print($"_Draw: cellsDrawn={cellsDrawn}, bounds=({minX},{minY})-({maxX},{maxY})");

		// 2. 绘制海洋分层效果
		if (ShowOceanLayers)
		{
			DrawOceanLayers();
		}

		// 3. 绘制河流
		if (ShowRivers)
		{
			DrawRivers();
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
				var mapPos = TransformToMapCoordinates(mouseButton.Position);
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
		var viewSize = GetViewportRect().Size;
		var mapSize = _mapGenerator?.Data?.MapSize ?? new Vector2(1024, 1024);

		// 计算缩放比例（留一点边距）
		float margin = 20f;
		float scaleX = (viewSize.X - margin * 2) / mapSize.X;
		float scaleY = (viewSize.Y - margin * 2) / mapSize.Y;
		_currentScale = Mathf.Min(scaleX, scaleY);

		// 计算中心偏移
		var scaledMapSize = mapSize * _currentScale;
		// 偏移量 = 边距 + (视口剩余空间的一半)
		_currentOffset = new Vector2(margin, margin) + (viewSize - scaledMapSize) / 2;
	}
}
