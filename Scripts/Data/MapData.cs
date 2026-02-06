using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace FantasyMapGenerator.Scripts.Data;

public class Cell
{
	public int Id;
	public Vector2 Position;
	public List<int> NeighborIds = new();
	public List<Vector2> Vertices = new();
	public float Height;
	public bool IsLand;
	public Color RenderColor;
	public Vector2 Centroid;

	// 地形特征
	public int FeatureId;
	public sbyte DistanceField;
	public bool IsBorder;

	// 水文系统
	public ushort RiverId;
	public ushort Flux;
	public ushort Confluence;
	public int Haven = -1;
	public byte Harbor;

	// 气候与生态
	public byte BiomeId;
	public sbyte Temperature;
	public byte Precipitation;

	// 网格引用
	public int GridCellId;
}

public class Triangle
{
	public int V0, V1, V2;
	public Vector2 Circumcenter;
	public float Circumradius;

	public Triangle(int v0, int v1, int v2)
	{
		V0 = v0;
		V1 = v1;
		V2 = v2;
	}
}

public class MapData
{
	public Vector2[] Points;
	public Cell[] Cells;
	public Triangle[] Triangles;
	public Vector2[] Vertices;
	public float[] Heightmap;
	public Vector2 MapSize;
	public int Seed;

	// 地貌特征和河流
	public Feature[] Features;
	public River[] Rivers;
}
