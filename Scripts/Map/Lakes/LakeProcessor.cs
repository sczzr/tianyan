using System;
using System.Collections.Generic;
using System.Linq;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Map.Lakes;

/// <summary>
/// 湖泊处理器，计算湖泊的水文和气候数据
/// </summary>
public class LakeProcessor
{
	private readonly Cell[] _cells;
	private readonly List<Feature> _features;
	private readonly float _waterLevel;
	private const float LakeElevationDelta = 0.1f;
	private const float HeightExponent = 1.8f;

	public LakeProcessor(Cell[] cells, List<Feature> features, float waterLevel = 0.35f)
	{
		_cells = cells;
		_features = features;
		_waterLevel = waterLevel;
	}

	/// <summary>
	/// 处理所有湖泊
	/// </summary>
	public void Process()
	{
		foreach (var feature in _features)
		{
			if (feature.Type != FeatureType.Lake) continue;

			ExtractShoreline(feature);
			CalculateHeight(feature);
			CalculateClimateData(feature);
			DetectClosedLake(feature);
			ClassifyLake(feature);
		}
	}

	/// <summary>
	/// 提取湖泊的海岸线（相邻陆地Cell）
	/// </summary>
	private void ExtractShoreline(Feature lake)
	{
		var shoreline = new HashSet<int>();

		foreach (int cellId in lake.Cells)
		{
			foreach (int neighborId in _cells[cellId].NeighborIds)
			{
				if (_cells[neighborId].Height > _waterLevel)
				{
					shoreline.Add(neighborId);
				}
			}
		}

		lake.Shoreline.Clear();
		lake.Shoreline.AddRange(shoreline);
	}

	/// <summary>
	/// 计算湖泊水面高度
	/// </summary>
	private void CalculateHeight(Feature lake)
	{
		if (lake.Shoreline.Count == 0)
		{
			lake.Height = _waterLevel;
			return;
		}

		float minShoreHeight = lake.Shoreline.Min(cellId => _cells[cellId].Height);
		lake.Height = Math.Max(minShoreHeight - LakeElevationDelta / 100f, 0);
	}

	/// <summary>
	/// 计算湖泊气候数据（简化模型）
	/// </summary>
	private void CalculateClimateData(Feature lake)
	{
		// 计算流量（基于海岸线降水）
		lake.Flux = lake.Shoreline.Sum(cellId => _cells[cellId].Precipitation);

		// 计算温度（平均海岸线温度）
		if (lake.Shoreline.Count > 0)
		{
			lake.Temperature = (float)lake.Shoreline.Average(cellId => _cells[cellId].Temperature);
		}
		else
		{
			lake.Temperature = 15; // 默认温度
		}

		// 计算蒸发量（基于Penman公式简化版）
		float heightMeters = MathF.Pow(lake.Height * 100 - 18, HeightExponent);
		float evaporation = (700 * (lake.Temperature + 0.006f * heightMeters) / 50 + 75) / (80 - lake.Temperature);
		lake.Evaporation = Math.Max(evaporation * lake.CellCount, 0);
	}

	/// <summary>
	/// 检测湖泊是否为封闭湖泊（位于洼地中）
	/// </summary>
	private void DetectClosedLake(Feature lake)
	{
		const float ElevationLimit = 5f;

		float maxElevation = lake.Height + ElevationLimit / 100f;
		if (maxElevation > 0.99f)
		{
			lake.Closed = false;
			return;
		}

		// 从最低海岸线Cell开始BFS检查是否能到达海洋或更低湖泊
		if (lake.Shoreline.Count == 0)
		{
			lake.Closed = true;
			return;
		}

		int lowestShorelineCell = lake.Shoreline.OrderBy(cellId => _cells[cellId].Height).First();
		var queue = new Queue<int>();
		var checked_ = new bool[_cells.Length];

		queue.Enqueue(lowestShorelineCell);
		checked_[lowestShorelineCell] = true;

		bool isDeep = true;

		while (queue.Count > 0 && isDeep)
		{
			int cellId = queue.Dequeue();

			foreach (int neighborId in _cells[cellId].NeighborIds)
			{
				if (checked_[neighborId]) continue;
				if (_cells[neighborId].Height >= maxElevation) continue;

				// 检查是否到达水体
				if (_cells[neighborId].Height < _waterLevel)
				{
					// 找到相邻水体的特征
					int neighborFeatureId = _cells[neighborId].FeatureId;
					var neighborFeature = _features.FirstOrDefault(f => f.Id == neighborFeatureId);

					if (neighborFeature != null)
					{
						if (neighborFeature.Type == FeatureType.Ocean ||
						    (neighborFeature.Type == FeatureType.Lake && lake.Height > neighborFeature.Height))
						{
							isDeep = false;
						}
					}
				}

				checked_[neighborId] = true;
				queue.Enqueue(neighborId);
			}
		}

		lake.Closed = isDeep;

		// 设置出口Cell
		if (!lake.Closed)
		{
			lake.OutCell = lowestShorelineCell;
		}
	}

	/// <summary>
	/// 分类湖泊类型
	/// </summary>
	private void ClassifyLake(Feature lake)
	{
		// 冻结湖
		if (lake.Temperature < -3)
		{
			lake.Group = FeatureGroup.Frozen;
			return;
		}

		// 熔岩湖（高海拔小湖泊的随机可能）
		if (lake.Height > 0.6f && lake.CellCount < 10 && lake.FirstCell % 10 == 0)
		{
			lake.Group = FeatureGroup.Lava;
			return;
		}

		// 无入口无出口的湖泊
		if (lake.Inlets.Count == 0 && lake.Outlet < 0)
		{
			// 干涸湖
			if (lake.Evaporation > lake.Flux * 4)
			{
				lake.Group = FeatureGroup.Dry;
				return;
			}

			// 天坑
			if (lake.CellCount < 3 && lake.FirstCell % 10 == 0)
			{
				lake.Group = FeatureGroup.Sinkhole;
				return;
			}
		}

		// 盐湖（无出口且蒸发大于流入）
		if (lake.Outlet < 0 && lake.Evaporation > lake.Flux)
		{
			lake.Group = FeatureGroup.Salt;
			return;
		}

		// 默认为淡水湖
		lake.Group = FeatureGroup.Freshwater;
	}

	/// <summary>
	/// 获取指定Cell所属的湖泊
	/// </summary>
	public Feature GetLakeForCell(int cellId)
	{
		int featureId = _cells[cellId].FeatureId;
		return _features.FirstOrDefault(f => f.Id == featureId && f.Type == FeatureType.Lake);
	}

	/// <summary>
	/// 获取所有湖泊
	/// </summary>
	public IEnumerable<Feature> GetAllLakes()
	{
		return _features.Where(f => f.Type == FeatureType.Lake);
	}

	/// <summary>
	/// 清理河流生成后的湖泊数据
	/// </summary>
	public void CleanupAfterRiverGeneration()
	{
		foreach (var lake in GetAllLakes())
		{
			// 移除无效的入口河流
			lake.Inlets.RemoveAll(riverId => riverId <= 0);

			// 重新计算湖泊高度
			if (lake.Shoreline.Count > 0)
			{
				lake.Height = (float)Math.Round(lake.Height, 3);
			}
		}
	}
}
