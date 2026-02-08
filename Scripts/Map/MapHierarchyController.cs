using System;
using FantasyMapGenerator.Scripts.Rendering;

namespace FantasyMapGenerator.Scripts.Map;

public class MapHierarchyController
{
	private readonly MapView _mapView;
	private readonly MapHierarchyConfig _config;
	private MapContext _current;

	public bool HasContext => _current != null;
	public MapContext Current => _current;
	public bool CanReturnToParent => _current?.Parent != null;

	public MapHierarchyController(MapView mapView, MapHierarchyConfig config)
	{
		_mapView = mapView;
		_config = config ?? new MapHierarchyConfig();
	}

	public void SetRoot(MapLevel level, int? cellCountOverride = null, string seed = null, bool regenerate = true)
	{
		if (_mapView == null)
		{
			return;
		}

		int fallback = _mapView.CellCount > 0 ? _mapView.CellCount : _config.GetCellCount(level, 2000);
		int cellCount = cellCountOverride ?? _config.GetCellCount(level, fallback);
		var root = new MapContext(level, cellCount, seed ?? GenerateSeed(level), null, null, null);
		if (regenerate)
		{
			ApplyContext(root);
			return;
		}

		_current = root;
		_mapView.SetCellCountWithoutGeneration(root.CellCount);
	}

	public void RestoreContext(MapContext context, bool regenerate = false)
	{
		if (_mapView == null || context == null)
		{
			return;
		}

		if (regenerate)
		{
			ApplyContext(context);
			return;
		}

		_current = context;
		_mapView.SetCellCountWithoutGeneration(context.CellCount);
	}

	public void UpdateCurrentCellCount(int cellCount)
	{
		if (_current == null)
		{
			return;
		}

		_current = _current.WithCellCount(cellCount);
	}

	public bool TryEnterChild(int cellId)
	{
		if (_current == null)
		{
			return false;
		}

		var parentMapData = _mapView.GetMapData();
		if (parentMapData?.Cells == null || cellId < 0 || cellId >= parentMapData.Cells.Length)
		{
			return false;
		}

		var childLevel = GetChildLevel(_current.Level);
		if (childLevel == null)
		{
			return false;
		}

		int childCellCount = _config.GetCellCount(childLevel.Value, _current.CellCount);
		string childSeed = DeriveChildSeed(_current.Seed, childLevel.Value, cellId);
		var child = new MapContext(childLevel.Value, childCellCount, childSeed, _current, cellId, null, parentMapData);
		ApplyContext(child);
		return true;
	}

	public bool TryReturnToParent()
	{
		if (_current?.Parent == null)
		{
			return false;
		}

		ApplyContext(_current.Parent);
		return true;
	}

	public void RegenerateCurrentMap()
	{
		if (_current == null)
		{
			return;
		}

		ApplyContext(_current.WithSeed(GenerateSeed(_current.Level)));
	}

	private void ApplyContext(MapContext context)
	{
		if (_mapView == null || context == null)
		{
			return;
		}

		_current = context;
		_mapView.GenerateMapForContext(context);
	}

	private static MapLevel? GetChildLevel(MapLevel level)
	{
		return level switch
		{
			MapLevel.World => MapLevel.Province,
			MapLevel.Country => MapLevel.City,
			_ => null
		};
	}

	private static string DeriveChildSeed(string parentSeed, MapLevel level, int cellId)
	{
		if (string.IsNullOrWhiteSpace(parentSeed))
		{
			return GenerateSeed(level);
		}

		return $"{parentSeed}:{level}:{cellId}";
	}

	private static string GenerateSeed(MapLevel level)
	{
		long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		int random = Random.Shared.Next(1000, 9999);
		return $"{level}_{timestamp}_{random}";
	}
}
