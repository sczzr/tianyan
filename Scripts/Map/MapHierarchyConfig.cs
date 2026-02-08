namespace FantasyMapGenerator.Scripts.Map;

public class MapHierarchyConfig
{
	public int WorldCellCount { get; set; } = 15000;
	public int CountryCellCount { get; set; } = 3000;
	public int ProvinceCellCount { get; set; } = 2000;
	public int CityCellCount { get; set; } = 2000;

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
