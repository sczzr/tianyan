using System;
using System.Collections.Generic;

namespace FantasyMapGenerator.Scripts.Data;

public enum FeatureType
{
	Ocean,
	Lake,
	Island
}

public enum FeatureGroup
{
	// 水体分类
	Ocean,
	Sea,
	Gulf,
	// 湖泊分类
	Freshwater,
	Salt,
	Frozen,
	Lava,
	Dry,
	Sinkhole,
	// 陆地分类
	Continent,
	Island,
	Isle,
	LakeIsland
}

public class Feature
{
	public int Id;
	public FeatureType Type;
	public bool IsLand;
	public bool IsBorder;
	public int CellCount;
	public int FirstCell;
	public List<int> Cells = new();
	public List<int> Vertices = new();
	public List<int> Shoreline = new();
	public float Height;
	public FeatureGroup Group;
	public string Name = string.Empty;

	// 水文数据 (主要用于湖泊)
	public float Flux;
	public float Evaporation;
	public float Temperature;
	public List<int> Inlets = new();
	public int Outlet = -1;
	public int OutCell = -1;
	public bool Closed;

	public Feature(int id, FeatureType type, bool isLand, bool isBorder)
	{
		Id = id;
		Type = type;
		IsLand = isLand;
		IsBorder = isBorder;
	}
}
