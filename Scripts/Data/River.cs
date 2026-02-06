using System;
using System.Collections.Generic;
using Godot;

namespace FantasyMapGenerator.Scripts.Data;

public enum RiverType
{
	River,
	Creek,
	Brook,
	Stream,
	Fork,
	Branch
}

public class River
{
	public int Id;
	public int Source;
	public int Mouth;
	public int Parent;
	public int Basin;
	public List<int> Cells = new();
	public float Discharge;
	public float Length;
	public float Width;
	public float SourceWidth;
	public float WidthFactor;
	public string Name = string.Empty;
	public RiverType Type;

	// 渲染用的曲折路径点 (x, y, flux)
	public List<Vector3> MeanderedPoints = new();

	public River(int id)
	{
		Id = id;
		Parent = 0;
		Basin = id;
		WidthFactor = 1.0f;
	}
}
