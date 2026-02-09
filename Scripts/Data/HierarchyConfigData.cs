using FantasyMapGenerator.Scripts.Map;

namespace FantasyMapGenerator.Scripts.Data;

public class HierarchyConfigData
{
	public HierarchyArchetype Archetype { get; set; } = HierarchyArchetype.Standard;
	public int LevelCount { get; set; } = 2;
	public int WorldCellCount { get; set; } = 15000;
	public int CountryCellCount { get; set; } = 3000;
	public int ProvinceCellCount { get; set; } = 2000;
	public int CityCellCount { get; set; } = 2000;

	public static HierarchyConfigData CreateFromArchetype(HierarchyArchetype archetype)
	{
		return archetype switch
		{
			HierarchyArchetype.Simple => new HierarchyConfigData
			{
				Archetype = archetype,
				LevelCount = 1,
				WorldCellCount = 6000,
				CountryCellCount = 2000,
				ProvinceCellCount = 1200,
				CityCellCount = 800
			},
			HierarchyArchetype.Standard => new HierarchyConfigData
			{
				Archetype = archetype,
				LevelCount = 2,
				WorldCellCount = 15000,
				CountryCellCount = 3000,
				ProvinceCellCount = 2000,
				CityCellCount = 2000
			},
			HierarchyArchetype.Complex => new HierarchyConfigData
			{
				Archetype = archetype,
				LevelCount = 6,
				WorldCellCount = 22000,
				CountryCellCount = 6000,
				ProvinceCellCount = 3500,
				CityCellCount = 2200
			},
			HierarchyArchetype.Custom => new HierarchyConfigData
			{
				Archetype = archetype,
				LevelCount = 4,
				WorldCellCount = 18000,
				CountryCellCount = 4500,
				ProvinceCellCount = 2600,
				CityCellCount = 2000
			},
			_ => CreateFromArchetype(HierarchyArchetype.Standard)
		};
	}

	public int GetCellCount(MapLevel level, int fallback)
	{
		return level switch
		{
			MapLevel.World => WorldCellCount,
			MapLevel.Country => CountryCellCount,
			MapLevel.Province => ProvinceCellCount,
			MapLevel.City => CityCellCount,
			_ => fallback
		};
	}
}
