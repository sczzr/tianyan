using System;
using System.Linq;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Map.Biomes;

/// <summary>
/// 生物群落分配器，基于温度和湿度分配13种生物群落
/// </summary>
public class BiomeAssigner
{
	private readonly Cell[] _cells;
	private readonly float _waterLevel;
	private const int MinLandHeight = 20; // 高度阈值（0-100范围）

	public BiomeAssigner(Cell[] cells, float waterLevel = 0.35f)
	{
		_cells = cells;
		_waterLevel = waterLevel;
	}

	/// <summary>
	/// 为所有Cell分配生物群落
	/// </summary>
	public void Assign()
	{
		// 首先生成简化的气候数据
		GenerateClimateData();

		// 然后分配生物群落
		for (int cellId = 0; cellId < _cells.Length; cellId++)
		{
			var cell = _cells[cellId];

			// 水体为海洋生物群落
			if (cell.Height < _waterLevel)
			{
				cell.BiomeId = (byte)BiomeType.Marine;
				continue;
			}

			int moisture = CalculateMoisture(cellId);
			int temperature = cell.Temperature;

			cell.BiomeId = (byte)GetBiomeId(moisture, temperature, cell.Height, cell.RiverId > 0);
		}
	}

	/// <summary>
	/// 生成简化的气候数据
	/// </summary>
	private void GenerateClimateData()
	{
		// 计算地图边界
		float minY = float.MaxValue, maxY = float.MinValue;
		float maxHeight = 0;

		foreach (var cell in _cells)
		{
			minY = MathF.Min(minY, cell.Position.Y);
			maxY = MathF.Max(maxY, cell.Position.Y);
			maxHeight = MathF.Max(maxHeight, cell.Height);
		}

		float heightRange = maxY - minY;
		if (heightRange < 0.001f) heightRange = 1.0f;

		foreach (var cell in _cells)
		{
			// 计算纬度因子 (0 = 赤道, 1 = 极地)
			float latitudeFactor = MathF.Abs((cell.Position.Y - minY) / heightRange - 0.5f) * 2;

			// 基础温度: 赤道30°C，极地-10°C
			float baseTemp = 30 - latitudeFactor * 40;

			// 海拔修正: 每100m降低0.6°C (假设高度1.0 = 1000m)
			float heightMeters = cell.Height * 1000;
			float tempCorrection = heightMeters * 0.006f;

			cell.Temperature = (sbyte)Math.Clamp(baseTemp - tempCorrection, -40, 50);

			// 计算降水量
			// 海岸线附近降水多，内陆降水少
			float distanceToCoast = MathF.Abs(cell.DistanceField);
			float basePrecip = 100;

			// 海岸效应
			if (distanceToCoast <= 3)
				basePrecip = 80 - distanceToCoast * 10;
			else
				basePrecip = 50 - MathF.Min(distanceToCoast - 3, 10) * 3;

			// 纬度效应 (热带多雨，极地和沙漠带少雨)
			float latitudeEffect = 1.0f;
			if (latitudeFactor > 0.6f) // 极地
				latitudeEffect = 0.3f;
			else if (latitudeFactor > 0.2f && latitudeFactor < 0.4f) // 副热带高压带
				latitudeEffect = 0.5f;

			cell.Precipitation = (byte)Math.Clamp(basePrecip * latitudeEffect, 0, 100);
		}
	}

	/// <summary>
	/// 计算湿度
	/// </summary>
	private int CalculateMoisture(int cellId)
	{
		var cell = _cells[cellId];
		int moisture = cell.Precipitation;

		// 河流加成
		if (cell.RiverId > 0)
		{
			moisture += Math.Max(cell.Flux / 10, 2);
		}

		// 考虑邻居的湿度
		int neighborCount = 0;
		int neighborMoisture = 0;

		foreach (int neighborId in cell.NeighborIds)
		{
			if (_cells[neighborId].Height >= _waterLevel)
			{
				neighborMoisture += _cells[neighborId].Precipitation;
				neighborCount++;
			}
		}

		if (neighborCount > 0)
		{
			moisture = (moisture + neighborMoisture / neighborCount) / 2;
		}

		return Math.Clamp(4 + moisture, 0, 100);
	}

	/// <summary>
	/// 获取生物群落ID
	/// </summary>
	private BiomeType GetBiomeId(int moisture, int temperature, float height, bool hasRiver)
	{
		int height100 = (int)(height * 100);

		// 水体: 海洋生物群落
		if (height100 < MinLandHeight) return BiomeType.Marine;

		// 极寒: 冰川
		if (temperature < -5) return BiomeType.Glacier;

		// 炎热干燥且无河流: 热沙漠
		if (temperature >= 25 && !hasRiver && moisture < 8) return BiomeType.HotDesert;

		// 湿地检测
		if (IsWetland(moisture, temperature, height100)) return BiomeType.Wetland;

		// 使用生物群落矩阵
		int moistureBand = Math.Min(moisture / 5, 4);  // [0-4]
		int temperatureBand = Math.Min(Math.Max(20 - temperature, 0), 25);  // [0-25]

		return BiomeData.GetBiome(moistureBand, temperatureBand);
	}

	/// <summary>
	/// 判断是否为湿地
	/// </summary>
	private bool IsWetland(int moisture, int temperature, int height100)
	{
		// 温度太低不是湿地
		if (temperature <= -2) return false;

		// 海岸附近的高湿度
		if (moisture > 40 && height100 < 25) return true;

		// 内陆的高湿度
		if (moisture > 24 && height100 > 24 && height100 < 60) return true;

		return false;
	}

	/// <summary>
	/// 根据生物群落分配渲染颜色
	/// </summary>
	public void AssignColors()
	{
		foreach (var cell in _cells)
		{
			cell.RenderColor = BiomeData.GetColor(cell.BiomeId);
		}
	}
}
