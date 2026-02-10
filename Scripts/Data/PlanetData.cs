namespace FantasyMapGenerator.Scripts.Data;

public enum PlanetElement
{
	Terra,
	Pyro,
	Cryo,
	Aero
}

public enum PlanetSize
{
	Small,
	Medium,
	Large,
	Colossal
}

public class PlanetData
{
	public string PlanetId { get; set; } = string.Empty;
	public string Name { get; set; } = "后土";
	public PlanetElement Element { get; set; } = PlanetElement.Terra;
	public PlanetSize Size { get; set; } = PlanetSize.Medium;
	public float OceanCoverage { get; set; } = 0.56f;
	public float Temperature { get; set; } = 0.52f;
	public float AtmosphereDensity { get; set; } = 0.62f;
	public float MountainIntensity { get; set; } = 0.55f;
	public float PolarCoverage { get; set; } = 0.55f;
	public float DesertRatio { get; set; } = 0.45f;
}
