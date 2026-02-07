using System.Collections.Generic;
using Godot;

namespace FantasyMapGenerator.Scripts.Data;

public class Country
{
	public int Id;
	public string Name;
	public Color Color;
	public int CapitalCellId;
	public Vector2 Center;
	public List<int> CellIds = new();
}
