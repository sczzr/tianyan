using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Map.Rivers;

/// <summary>
/// 河流生成器，实现完整的水文循环
/// </summary>
public class RiverGenerator
{
	private readonly Cell[] _cells;
	private readonly List<Feature> _features;
	private readonly AleaPRNG _prng;
	private readonly float _waterLevel;
	private readonly float[] _heights;

	private const int MinFluxToFormRiver = 30;
	private const float FluxFactor = 500f;
	private const float MaxFluxWidth = 1f;
	private const float LengthFactor = 200f;

	private readonly Dictionary<int, List<int>> _riversData = new();
	private readonly Dictionary<int, int> _riverParents = new();
	private int _riverNext = 1;

	public RiverGenerator(Cell[] cells, List<Feature> features, AleaPRNG prng, float[] heights, float waterLevel = 0.35f)
	{
		_cells = cells;
		_features = features;
		_prng = prng;
		_heights = heights;
		_waterLevel = waterLevel;
	}

	/// <summary>
	/// 生成河流
	/// </summary>
	public List<River> Generate()
	{
		// 初始化数组
		for (int i = 0; i < _cells.Length; i++)
		{
			_cells[i].Flux = 0;
			_cells[i].RiverId = 0;
			_cells[i].Confluence = 0;
		}

		// 水流累积
		DrainWater();

		// 定义河流
		var rivers = DefineRivers();

		// 计算汇流量
		CalculateConfluenceFlux();

		return rivers;
	}

	/// <summary>
	/// 水流累积算法
	/// </summary>
	private void DrainWater()
	{
		// 计算Cell数量修正因子
		float cellsModifier = MathF.Pow(_cells.Length / 10000f, 0.25f);

		// 获取所有陆地Cell，按高度从高到低排序
		var land = new List<int>();
		for (int i = 0; i < _cells.Length; i++)
		{
			if (_heights[i] >= _waterLevel)
			{
				land.Add(i);
			}
		}
		land.Sort((a, b) => _heights[b].CompareTo(_heights[a]));

		// 识别湖泊出口Cell
		var lakeOutCells = new Dictionary<int, List<Feature>>();
		foreach (var feature in _features.Where(f => f.Type == FeatureType.Lake && f.OutCell >= 0))
		{
			if (!lakeOutCells.ContainsKey(feature.OutCell))
				lakeOutCells[feature.OutCell] = new List<Feature>();
			lakeOutCells[feature.OutCell].Add(feature);
		}

		// 处理每个陆地Cell
		foreach (int cellId in land)
		{
			// 添加降水贡献的流量
			_cells[cellId].Flux += (ushort)Math.Min(_cells[cellId].Precipitation / cellsModifier, ushort.MaxValue);

			// 处理湖泊出口
			if (lakeOutCells.TryGetValue(cellId, out var lakes))
			{
				foreach (var lake in lakes.Where(l => l.Flux > l.Evaporation))
				{
					// 找到湖泊中相邻的水Cell
					int lakeCell = _cells[cellId].NeighborIds
						.FirstOrDefault(c => _heights[c] < _waterLevel && _cells[c].FeatureId == lake.Id, -1);

					if (lakeCell >= 0)
					{
						// 添加湖泊流出的水量
						float outFlow = Math.Max(lake.Flux - lake.Evaporation, 0);
						_cells[lakeCell].Flux = (ushort)Math.Min(_cells[lakeCell].Flux + outFlow, ushort.MaxValue);

						// 分配河流ID
						if (_cells[lakeCell].RiverId == 0)
						{
							_cells[lakeCell].RiverId = (ushort)_riverNext;
							AddCellToRiver(lakeCell, _riverNext);
							_riverNext++;
						}

						lake.Outlet = _cells[lakeCell].RiverId;
						FlowDown(cellId, _cells[lakeCell].Flux, _cells[lakeCell].RiverId);
					}
				}

				// 将所有支流分配到出口流域
				var outlet = lakes.FirstOrDefault()?.Outlet ?? 0;
				foreach (var lake in lakes)
				{
					foreach (int inlet in lake.Inlets)
					{
						_riverParents[inlet] = outlet;
					}
				}
			}

			// 边界Cell：水流出地图
			if (_cells[cellId].IsBorder && _cells[cellId].RiverId > 0)
			{
				AddCellToRiver(-1, _cells[cellId].RiverId);
				continue;
			}

			// 找到下坡Cell
			int downhill = FindDownhillCell(cellId, lakeOutCells.ContainsKey(cellId) ? lakes : null);

			// 如果是洼地则跳过
			if (downhill < 0 || _heights[cellId] <= _heights[downhill]) continue;

			// 流量不足以形成河流
			if (_cells[cellId].Flux < MinFluxToFormRiver)
			{
				if (_heights[downhill] >= _waterLevel)
					_cells[downhill].Flux = (ushort)Math.Min(_cells[downhill].Flux + _cells[cellId].Flux, ushort.MaxValue);
				continue;
			}

			// 声明新河流
			if (_cells[cellId].RiverId == 0)
			{
				_cells[cellId].RiverId = (ushort)_riverNext;
				AddCellToRiver(cellId, _riverNext);
				_riverNext++;
			}

			FlowDown(downhill, _cells[cellId].Flux, _cells[cellId].RiverId);
		}
	}

	/// <summary>
	/// 找到下坡Cell
	/// </summary>
	private int FindDownhillCell(int cellId, List<Feature> excludeLakes)
	{
		// 如果有港口（相邻水域），优先流向那里
		if (_cells[cellId].Haven >= 0)
			return _cells[cellId].Haven;

		var candidates = _cells[cellId].NeighborIds.ToList();

		// 排除特定湖泊
		if (excludeLakes != null)
		{
			var excludeFeatureIds = excludeLakes.Select(l => l.Id).ToHashSet();
			candidates = candidates.Where(c => !excludeFeatureIds.Contains(_cells[c].FeatureId)).ToList();
		}

		if (candidates.Count == 0) return -1;

		// 返回最低的邻居
		return candidates.OrderBy(c => _heights[c]).First();
	}

	/// <summary>
	/// 水流向下流动
	/// </summary>
	private void FlowDown(int toCell, ushort fromFlux, int riverId)
	{
		if (toCell < 0) return;

		ushort toFlux = (ushort)(_cells[toCell].Flux - _cells[toCell].Confluence);
		int toRiver = _cells[toCell].RiverId;

		if (toRiver > 0)
		{
			// 下游Cell已有河流
			if (fromFlux > toFlux)
			{
				_cells[toCell].Confluence = (ushort)Math.Min(_cells[toCell].Confluence + _cells[toCell].Flux, ushort.MaxValue);
				if (_heights[toCell] >= _waterLevel)
					_riverParents[toRiver] = riverId;
				_cells[toCell].RiverId = (ushort)riverId;
			}
			else
			{
				_cells[toCell].Confluence = (ushort)Math.Min(_cells[toCell].Confluence + fromFlux, ushort.MaxValue);
				if (_heights[toCell] >= _waterLevel)
					_riverParents[riverId] = toRiver;
			}
		}
		else
		{
			_cells[toCell].RiverId = (ushort)riverId;
		}

		// 流入水体
		if (_heights[toCell] < _waterLevel)
		{
			var waterBody = _features.FirstOrDefault(f => f.Id == _cells[toCell].FeatureId);
			if (waterBody?.Type == FeatureType.Lake)
			{
				if (waterBody.Inlets.Count == 0 || fromFlux > waterBody.Flux)
				{
					waterBody.Flux = fromFlux;
				}
				if (!waterBody.Inlets.Contains(riverId))
					waterBody.Inlets.Add(riverId);
			}
		}
		else
		{
			// 传播流量
			_cells[toCell].Flux = (ushort)Math.Min(_cells[toCell].Flux + fromFlux, ushort.MaxValue);
		}

		AddCellToRiver(toCell, riverId);
	}

	/// <summary>
	/// 添加Cell到河流路径
	/// </summary>
	private void AddCellToRiver(int cellId, int riverId)
	{
		if (!_riversData.ContainsKey(riverId))
			_riversData[riverId] = new List<int>();
		_riversData[riverId].Add(cellId);
	}

	/// <summary>
	/// 从Cell数据定义River对象
	/// </summary>
	private List<River> DefineRivers()
	{
		var rivers = new List<River>();

		float defaultWidthFactor = MathF.Round(1f / MathF.Pow(_cells.Length / 10000f, 0.25f), 2);
		float mainStemWidthFactor = defaultWidthFactor * 1.2f;

		foreach (var kvp in _riversData)
		{
			var riverCells = kvp.Value;
			if (riverCells.Count < 3) continue; // 排除太短的河流

			int riverId = kvp.Key;

			// 重新标记Cell的河流归属
			foreach (int cellId in riverCells)
			{
				if (cellId < 0 || _cells[cellId].Height < _waterLevel) continue;

				if (_cells[cellId].RiverId > 0 && _cells[cellId].RiverId != riverId)
					_cells[cellId].Confluence = 1;
				else
					_cells[cellId].RiverId = (ushort)riverId;
			}

			int source = riverCells[0];
			int mouth = riverCells.Count > 1 ? riverCells[^2] : source;
			int parent = _riverParents.GetValueOrDefault(riverId, 0);

			float widthFactor = (parent == 0 || parent == riverId) ? mainStemWidthFactor : defaultWidthFactor;
			float discharge = mouth >= 0 ? _cells[mouth].Flux : 0;
			float sourceWidth = GetSourceWidth(source >= 0 ? _cells[source].Flux : 0);

			var river = new River(riverId)
			{
				Source = source,
				Mouth = mouth,
				Parent = parent,
				WidthFactor = widthFactor,
				Discharge = discharge,
				SourceWidth = sourceWidth
			};
			river.Cells.AddRange(riverCells);

			// 计算长度和宽度
			river.Length = CalculateLength(river);
			river.Width = GetWidth(GetOffset(discharge, river.MeanderedPoints.Count, widthFactor, sourceWidth));

			rivers.Add(river);
		}

		// 设置流域ID
		foreach (var river in rivers)
		{
			river.Basin = GetBasin(river.Id, rivers);
		}

		return rivers;
	}

	/// <summary>
	/// 获取源头宽度
	/// </summary>
	private float GetSourceWidth(float flux)
	{
		return MathF.Round(MathF.Min(MathF.Pow(flux, 0.9f) / FluxFactor, MaxFluxWidth), 2);
	}

	/// <summary>
	/// 获取宽度偏移
	/// </summary>
	private float GetOffset(float flux, int pointIndex, float widthFactor, float startingWidth)
	{
		if (pointIndex == 0) return startingWidth;

		float fluxWidth = MathF.Min(MathF.Pow(flux, 0.7f) / FluxFactor, MaxFluxWidth);
		float lengthWidth = pointIndex / LengthFactor;

		return widthFactor * (lengthWidth + fluxWidth) + startingWidth;
	}

	/// <summary>
	/// 获取河口宽度
	/// </summary>
	private float GetWidth(float offset)
	{
		return MathF.Round(MathF.Pow(offset / 1.5f, 1.8f), 2);
	}

	/// <summary>
	/// 计算河流长度
	/// </summary>
	private float CalculateLength(River river)
	{
		float length = 0;

		for (int i = 1; i < river.Cells.Count; i++)
		{
			int prev = river.Cells[i - 1];
			int curr = river.Cells[i];
			if (prev < 0 || curr < 0) continue;

			float dx = _cells[curr].Position.X - _cells[prev].Position.X;
			float dy = _cells[curr].Position.Y - _cells[prev].Position.Y;
			length += MathF.Sqrt(dx * dx + dy * dy);
		}

		return MathF.Round(length, 2);
	}

	/// <summary>
	/// 获取河流流域
	/// </summary>
	private int GetBasin(int riverId, List<River> rivers)
	{
		var river = rivers.FirstOrDefault(r => r.Id == riverId);
		if (river == null) return riverId;

		int parent = river.Parent;
		if (parent == 0 || parent == riverId) return riverId;

		return GetBasin(parent, rivers);
	}

	/// <summary>
	/// 计算汇流量
	/// </summary>
	private void CalculateConfluenceFlux()
	{
		for (int i = 0; i < _cells.Length; i++)
		{
			if (_cells[i].Confluence == 0) continue;

			var influx = _cells[i].NeighborIds
				.Where(c => _cells[c].RiverId > 0 && _heights[c] > _heights[i])
				.Select(c => _cells[c].Flux)
				.OrderByDescending(f => f)
				.ToList();

			_cells[i].Confluence = (ushort)influx.Skip(1).Sum(f => f);
		}
	}
}
