using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Map.Features;

/// <summary>
/// 地貌特征识别器，使用Flood Fill算法识别海洋、湖泊、岛屿等连通区域
/// </summary>
public class FeatureDetector
{
	private readonly Cell[] _cells;
	private readonly int _totalCells;
	private readonly float _waterLevel;

	public FeatureDetector(Cell[] cells, float waterLevel = 0.35f)
	{
		_cells = cells;
		_totalCells = cells.Length;
		_waterLevel = waterLevel;
	}

	/// <summary>
	/// 检测所有地貌特征
	/// </summary>
	public List<Feature> Detect()
	{
		var features = new List<Feature>();
		var featureIds = new int[_totalCells];

		// 首先标记边界Cell
		MarkBorderCells();

		int featureId = 1;
		int firstUnmarked = 0;

		while (firstUnmarked >= 0)
		{
			var feature = FloodFill(firstUnmarked, featureId, featureIds);
			if (feature != null)
			{
				features.Add(feature);
				featureId++;
			}

			// 找下一个未标记的Cell
			firstUnmarked = FindUnmarked(featureIds);
		}

		// 分类特征（Ocean/Sea/Gulf, Continent/Island/Isle）
		ClassifyFeatures(features);

		return features;
	}

	/// <summary>
	/// 标记边界Cell
	/// </summary>
	private void MarkBorderCells()
	{
		float minX = float.MaxValue, minY = float.MaxValue;
		float maxX = float.MinValue, maxY = float.MinValue;

		foreach (var cell in _cells)
		{
			minX = Math.Min(minX, cell.Position.X);
			minY = Math.Min(minY, cell.Position.Y);
			maxX = Math.Max(maxX, cell.Position.X);
			maxY = Math.Max(maxY, cell.Position.Y);
		}

		float width = maxX - minX;
		float height = maxY - minY;
		
		// 边界边距：取地图尺寸的 2% 或 至少 1.0
		float marginX = Math.Max(width * 0.02f, 1.0f);
		float marginY = Math.Max(height * 0.02f, 1.0f);

		foreach (var cell in _cells)
		{
			cell.IsBorder = cell.Position.X < minX + marginX ||
			                cell.Position.Y < minY + marginY ||
			                cell.Position.X > maxX - marginX ||
			                cell.Position.Y > maxY - marginY;
		}
	}

	/// <summary>
	/// Flood Fill 算法填充连通区域
	/// </summary>
	private Feature FloodFill(int startCell, int featureId, int[] featureIds)
	{
		if (featureIds[startCell] != 0) return null;

		bool isLand = _cells[startCell].Height > _waterLevel;
		bool isBorder = false;
		var cellsList = new List<int>();

		var queue = new Queue<int>();
		queue.Enqueue(startCell);
		featureIds[startCell] = featureId;

		while (queue.Count > 0)
		{
			int cellId = queue.Dequeue();
			cellsList.Add(cellId);
			_cells[cellId].FeatureId = featureId;

			if (_cells[cellId].IsBorder)
				isBorder = true;

			foreach (int neighborId in _cells[cellId].NeighborIds)
			{
				bool neighborIsLand = _cells[neighborId].Height > _waterLevel;

				if (isLand == neighborIsLand && featureIds[neighborId] == 0)
				{
					featureIds[neighborId] = featureId;
					queue.Enqueue(neighborId);
				}
				else if (isLand && !neighborIsLand)
				{
					// 标记海岸线
					_cells[cellId].DistanceField = 1;  // 陆地海岸
					if (_cells[neighborId].DistanceField == 0)
						_cells[neighborId].DistanceField = -1;  // 水体海岸
				}
				else if (!isLand && neighborIsLand)
				{
					_cells[cellId].DistanceField = -1;  // 水体海岸
					if (_cells[neighborId].DistanceField == 0)
						_cells[neighborId].DistanceField = 1;  // 陆地海岸
				}
			}
		}

		var featureType = isLand ? FeatureType.Island : (isBorder ? FeatureType.Ocean : FeatureType.Lake);
		var feature = new Feature(featureId, featureType, isLand, isBorder)
		{
			CellCount = cellsList.Count,
			FirstCell = startCell
		};
		feature.Cells.AddRange(cellsList);

		return feature;
	}

	/// <summary>
	/// 找到第一个未标记的Cell
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int FindUnmarked(int[] featureIds)
	{
		for (int i = 0; i < _totalCells; i++)
		{
			if (featureIds[i] == 0)
				return i;
		}
		return -1;
	}

	/// <summary>
	/// 分类特征
	/// </summary>
	private void ClassifyFeatures(List<Feature> features)
	{
		float oceanMinSize = _totalCells / 25f;
		float seaMinSize = _totalCells / 1000f;
		float continentMinSize = _totalCells / 10f;
		float islandMinSize = _totalCells / 1000f;

		foreach (var feature in features)
		{
			if (feature.Type == FeatureType.Island)
			{
				// 陆地分类
				if (feature.CellCount > continentMinSize)
					feature.Group = FeatureGroup.Continent;
				else if (feature.CellCount > islandMinSize)
					feature.Group = FeatureGroup.Island;
				else
					feature.Group = FeatureGroup.Isle;
			}
			else if (feature.Type == FeatureType.Ocean)
			{
				// 海洋分类
				if (feature.CellCount > oceanMinSize)
					feature.Group = FeatureGroup.Ocean;
				else if (feature.CellCount > seaMinSize)
					feature.Group = FeatureGroup.Sea;
				else
					feature.Group = FeatureGroup.Gulf;
			}
			else if (feature.Type == FeatureType.Lake)
			{
				// 湖泊默认为淡水湖，后续由LakeProcessor进一步分类
				feature.Group = FeatureGroup.Freshwater;
			}
		}
	}

	/// <summary>
	/// 提取湖泊的海岸线
	/// </summary>
	public void ExtractShorelines(List<Feature> features)
	{
		foreach (var feature in features)
		{
			if (feature.Type != FeatureType.Lake) continue;

			var shoreline = new HashSet<int>();

			foreach (int cellId in feature.Cells)
			{
				foreach (int neighborId in _cells[cellId].NeighborIds)
				{
					if (_cells[neighborId].Height > _waterLevel)
					{
						shoreline.Add(neighborId);
					}
				}
			}

			feature.Shoreline.AddRange(shoreline);
		}
	}
}
