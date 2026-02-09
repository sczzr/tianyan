using Godot;

namespace FantasyMapGenerator.Scripts.Data;

[GlobalClass]
public partial class CelestialSatellitePhysicsConfig : Resource
{
	[Export] public string BodyId { get; set; } = string.Empty;
	[Export] public string Name { get; set; } = "广寒";
	[Export] public bool Visible { get; set; } = true;

	[Export(PropertyHint.Range, "0.01,5.0,0.001")]
	public float RadiusEarth { get; set; } = 0.2724f;

	[Export(PropertyHint.Range, "0.0001,30.0,0.0001")]
	public float MassEarth { get; set; } = 0.0123f;

	[Export(PropertyHint.Range, "2.0,500.0,0.1")]
	public float OrbitDistancePlanetRadii { get; set; } = 60.3f;

	[Export(PropertyHint.Range, "0.0,0.9,0.001")]
	public float OrbitEccentricity { get; set; } = 0.0549f;

	[Export(PropertyHint.Range, "-80.0,80.0,0.1")]
	public float OrbitInclinationDeg { get; set; } = 5.14f;

	[Export(PropertyHint.Range, "1.0,5000.0,0.1")]
	public float RotationPeriodHours { get; set; } = 655.7f;

	[Export(PropertyHint.Range, "0.1,100000.0,0.1")]
	public float RevolutionPeriodDays { get; set; } = 27.3217f;

	[Export] public bool AutoResolveRevolutionPeriod { get; set; } = true;

	public CelestialSatellitePhysicsConfig DuplicateConfig()
	{
		return new CelestialSatellitePhysicsConfig
		{
			BodyId = BodyId,
			Name = Name,
			Visible = Visible,
			RadiusEarth = RadiusEarth,
			MassEarth = MassEarth,
			OrbitDistancePlanetRadii = OrbitDistancePlanetRadii,
			OrbitEccentricity = OrbitEccentricity,
			OrbitInclinationDeg = OrbitInclinationDeg,
			RotationPeriodHours = RotationPeriodHours,
			RevolutionPeriodDays = RevolutionPeriodDays,
			AutoResolveRevolutionPeriod = AutoResolveRevolutionPeriod
		};
	}
}
