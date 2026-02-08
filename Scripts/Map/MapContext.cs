using System;

namespace FantasyMapGenerator.Scripts.Map;

public class MapContext
{
	public MapLevel Level { get; }
	public MapContext Parent { get; }
	public int? ParentCellId { get; }
	public string Seed { get; }
	public int CellCount { get; }
	public string DisplayName { get; }

	public MapContext(
		MapLevel level,
		int cellCount,
		string seed,
		MapContext parent,
		int? parentCellId,
		string displayName)
	{
		Level = level;
		CellCount = Math.Max(1, cellCount);
		Seed = seed ?? string.Empty;
		Parent = parent;
		ParentCellId = parentCellId;
		DisplayName = displayName;
	}

	public MapContext WithSeed(string seed)
	{
		return new MapContext(Level, CellCount, seed, Parent, ParentCellId, DisplayName);
	}

	public MapContext WithCellCount(int cellCount)
	{
		return new MapContext(Level, cellCount, Seed, Parent, ParentCellId, DisplayName);
	}
}
