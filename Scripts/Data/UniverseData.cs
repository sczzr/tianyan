using System;

namespace FantasyMapGenerator.Scripts.Data;

public class UniverseData
{
	public string UniverseId { get; set; } = Guid.NewGuid().ToString("N");
	public string Name { get; set; } = "太虚幻境";
	public int LawAlignment { get; set; } = 50;
	public string AestheticTheme { get; set; } = "Steampunk";
	public int CivilizationDensity { get; set; } = 50;
	public float TimeFlowRate { get; set; } = 1.0f;
	public HierarchyConfigData HierarchyConfig { get; set; } = HierarchyConfigData.CreateFromArchetype(HierarchyArchetype.Standard);
	public PlanetData CurrentPlanet { get; set; } = new();
	public float[] PlanetTerrainHeightmap { get; set; }
	public int PlanetTerrainWidth { get; set; }
	public int PlanetTerrainHeight { get; set; }
}
