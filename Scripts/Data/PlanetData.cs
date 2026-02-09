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
	public float OceanCoverage { get; set; } = 0.35f;
	public float Temperature { get; set; } = 0.5f;
	public float AtmosphereDensity { get; set; } = 0.5f;
}
