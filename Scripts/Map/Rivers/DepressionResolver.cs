using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Map.Rivers;

/// <summary>
/// 洼地填充算法，确保水流可以从高处流向低处
/// </summary>
public class DepressionResolver
{
	private readonly Cell[] _cells;
	private readonly List<Feature> _features;
	private readonly float _waterLevel;
	private const int MaxIterations = 1000;
	private const float ElevationIncrement = 0.001f;

	public DepressionResolver(Cell[] cells, List<Feature> features, float waterLevel = 0.35f)
	{
		_cells = cells;
		_features = features;
		_waterLevel = waterLevel;
	}

	/// <summary>
	/// 解决洼地问题，返回修改后的高度数组
	/// </summary>
	public float[] Resolve()
	{
		// 创建工作高度数组
		var heights = new float[_cells.Length];
		for (int i = 0; i < _cells.Length; i++)
		{
			heights[i] = _cells[i].Height;
		}

		// 对于湖泊，使用湖泊高度
		foreach (var feature in _features)
		{
			if (feature.Type == FeatureType.Lake)
			{
				foreach (int cellId in feature.Cells)
				{
					heights[cellId] = feature.Height;
				}
			}
		}

		// 获取所有陆地Cell（非边界）
		var land = new List<int>();
		for (int i = 0; i < _cells.Length; i++)
		{
			if (heights[i] >= _waterLevel && !_cells[i].IsBorder)
			{
				land.Add(i);
			}
		}

		// 按高度排序（从低到高）
		land.Sort((a, b) => heights[a].CompareTo(heights[b]));

		int depressions = int.MaxValue;
		int prevDepressions = -1;
		var progress = new List<int>();

		for (int iteration = 0; depressions > 0 && iteration < MaxIterations; iteration++)
		{
			// 检测进度是否停滞
            // 如果连续50次迭代没有减少 depressions 数量，才认为是停滞
            // 注意：填坑过程中 depressions 数量可能会暂时上升，这是正常的，所以不能用 Sum() > 0 判断
			if (progress.Count > 50 && progress.TakeLast(50).All(x => x >= 0))
			{
				GD.PrintErr($"Depression resolution progress stalled (no reduction for 50 steps), aborting after {iteration} iterations");
				break;
			}

			depressions = 0;

			// 处理湖泊
			foreach (var lake in _features.Where(f => f.Type == FeatureType.Lake))
			{
				if (lake.Closed) continue;
				if (lake.Shoreline.Count == 0) continue;

				float minShoreHeight = lake.Shoreline.Min(cellId => heights[cellId]);
				if (minShoreHeight >= 1.0f || lake.Height > minShoreHeight) continue;

				depressions++;
				lake.Height = minShoreHeight + 0.002f;

				// 更新湖泊Cell的高度
				foreach (int cellId in lake.Cells)
				{
					heights[cellId] = lake.Height;
				}
			}

			// 处理陆地洼地
			foreach (int cellId in land)
			{
				float minNeighborHeight = GetMinNeighborHeight(cellId, heights);
				if (minNeighborHeight >= 1.0f || heights[cellId] > minNeighborHeight) continue;

				depressions++;
				heights[cellId] = minNeighborHeight + ElevationIncrement;
			}

			if (prevDepressions >= 0)
			{
				progress.Add(depressions - prevDepressions);
				if (progress.Count > 10)
					progress.RemoveAt(0);
			}
			prevDepressions = depressions;
		}

		if (depressions > 0)
		{
			GD.PrintErr($"Unresolved depressions: {depressions}. Edit heightmap to fix.");
		}

		return heights;
	}

	/// <summary>
	/// 获取邻居Cell的最小高度
	/// </summary>
	private float GetMinNeighborHeight(int cellId, float[] heights)
	{
		float minHeight = float.MaxValue;

		foreach (int neighborId in _cells[cellId].NeighborIds)
		{
			// 考虑湖泊高度
			float neighborHeight = heights[neighborId];
			var lake = GetLakeForCell(neighborId);
			if (lake != null)
			{
				neighborHeight = lake.Height;
			}

			minHeight = Math.Min(minHeight, neighborHeight);
		}

		return minHeight;
	}

	/// <summary>
	/// 获取Cell所属的湖泊
	/// </summary>
	private Feature GetLakeForCell(int cellId)
	{
		if (_cells[cellId].Height >= _waterLevel) return null;

		int featureId = _cells[cellId].FeatureId;
		return _features.FirstOrDefault(f => f.Id == featureId && f.Type == FeatureType.Lake);
	}

	/// <summary>
	/// 应用解决后的高度到Cell
	/// </summary>
	public void ApplyHeights(float[] heights)
	{
		for (int i = 0; i < _cells.Length; i++)
		{
			// 只更新陆地Cell的高度
			if (_cells[i].Height >= _waterLevel)
			{
				_cells[i].Height = heights[i];
			}
		}
	}
}
