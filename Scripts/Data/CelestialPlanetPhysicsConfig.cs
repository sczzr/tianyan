using Godot;

namespace FantasyMapGenerator.Scripts.Data;

[GlobalClass]
public partial class CelestialPlanetPhysicsConfig : Resource
{
	[Export] public string BodyId { get; set; } = string.Empty;
	[Export] public string Name { get; set; } = "后土";
	[Export] public PlanetElement Element { get; set; } = PlanetElement.Terra;
	[Export] public bool Visible { get; set; } = true;

	[Export(PropertyHint.Range, "0.1,20.0,0.01")]
	public float RadiusEarth { get; set; } = 1.0f;

	[Export(PropertyHint.Range, "0.01,500.0,0.01")]
	public float MassEarth { get; set; } = 1.0f;

	[Export(PropertyHint.Range, "0.03,80.0,0.001")]
	public float OrbitDistanceAu { get; set; } = 1.0f;

	[Export(PropertyHint.Range, "0.0,0.9,0.001")]
	public float OrbitEccentricity { get; set; } = 0.0167f;

	[Export(PropertyHint.Range, "-60.0,60.0,0.1")]
	public float OrbitInclinationDeg { get; set; } = 0.0f;

	[Export(PropertyHint.Range, "2.0,5000.0,0.1")]
	public float RotationPeriodHours { get; set; } = 23.934f;

	[Export(PropertyHint.Range, "0.2,500000.0,0.1")]
	public float RevolutionPeriodDays { get; set; } = 365.256f;

	[Export] public bool AutoResolveRevolutionPeriod { get; set; } = true;

	public CelestialPlanetPhysicsConfig DuplicateConfig()
	{
		return new CelestialPlanetPhysicsConfig
		{
			BodyId = BodyId,
			Name = Name,
			Element = Element,
			Visible = Visible,
			RadiusEarth = RadiusEarth,
			MassEarth = MassEarth,
			OrbitDistanceAu = OrbitDistanceAu,
			OrbitEccentricity = OrbitEccentricity,
			OrbitInclinationDeg = OrbitInclinationDeg,
			RotationPeriodHours = RotationPeriodHours,
			RevolutionPeriodDays = RevolutionPeriodDays,
			AutoResolveRevolutionPeriod = AutoResolveRevolutionPeriod
		};
	}
}
