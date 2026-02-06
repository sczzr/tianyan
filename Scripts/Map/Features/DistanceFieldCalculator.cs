using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Map.Features;

/// <summary>
/// 距离场计算器，计算每个Cell到海岸线的距离
/// </summary>
public class DistanceFieldCalculator
{
	private readonly Cell[] _cells;
	private readonly float _waterLevel;

	// 距离场常量
	private const sbyte DeeperLand = 3;
	private const sbyte Landlocked = 2;
	private const sbyte LandCoast = 1;
	private const sbyte Unmarked = 0;
	private const sbyte WaterCoast = -1;
	private const sbyte DeepWater = -2;
	private const sbyte MaxLandDistance = 127;
	private const sbyte MaxWaterDistance = -10;

	public DistanceFieldCalculator(Cell[] cells, float waterLevel = 0.35f)
	{
		_cells = cells;
		_waterLevel = waterLevel;
	}

	/// <summary>
	/// 计算完整的距离场
	/// </summary>
	public void Calculate()
	{
		// 首先确保海岸线已标记（由FeatureDetector完成）
		// 然后从海岸向内陆和深海扩散

		// 扩散陆地距离
		MarkupDistance(LandCoast, 1, MaxLandDistance, true);

		// 扩散海洋距离
		MarkupDistance(WaterCoast, -1, MaxWaterDistance, false);
	}

	/// <summary>
	/// 从起始距离开始扩散标记
	/// </summary>
	private void MarkupDistance(sbyte startDistance, int increment, sbyte limit, bool isLand)
	{
		int marked = int.MaxValue;

		for (sbyte distance = (sbyte)(startDistance + increment);
		     marked > 0 && distance != limit;
		     distance = (sbyte)(distance + increment))
		{
			marked = 0;
			sbyte prevDistance = (sbyte)(distance - increment);

			for (int cellId = 0; cellId < _cells.Length; cellId++)
			{
				if (_cells[cellId].DistanceField != prevDistance) continue;

				foreach (int neighborId in _cells[cellId].NeighborIds)
				{
					// 只标记同类型（陆地或水）且未标记的Cell
					bool neighborIsLand = _cells[neighborId].Height > _waterLevel;
					if (neighborIsLand != isLand) continue;
					if (_cells[neighborId].DistanceField != Unmarked) continue;

					_cells[neighborId].DistanceField = distance;
					marked++;
				}
			}
		}
	}

	/// <summary>
	/// 获取Cell的地形类型描述
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string GetTerrainDescription(sbyte distanceField)
	{
		return distanceField switch
		{
			<= -10 => "Deep Ocean",
			-9 or -8 or -7 or -6 or -5 => "Open Ocean",
			-4 or -3 => "Offshore",
			-2 => "Coastal Water",
			-1 => "Water Coast",
			0 => "Unmarked",
			1 => "Land Coast",
			2 => "Coastal Land",
			3 => "Inland",
			>= 4 => "Deep Inland",
		};
	}

	/// <summary>
	/// 判断Cell是否为海岸线
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsCoast(sbyte distanceField)
	{
		return distanceField == LandCoast || distanceField == WaterCoast;
	}

	/// <summary>
	/// 判断Cell是否为深水
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsDeepWater(sbyte distanceField)
	{
		return distanceField <= DeepWater;
	}

	/// <summary>
	/// 判断Cell是否为内陆
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInland(sbyte distanceField)
	{
		return distanceField >= DeeperLand;
	}
}
