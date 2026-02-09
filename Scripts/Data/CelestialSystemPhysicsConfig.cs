using Godot;

namespace FantasyMapGenerator.Scripts.Data;

[GlobalClass]
public partial class CelestialSystemPhysicsConfig : Resource
{
	[Export(PropertyHint.Range, "0.08,80.0,0.01")]
	public float StarMassSolar { get; set; } = 1.0f;

	[Export] public CelestialPlanetPhysicsConfig PrimaryPlanet { get; set; } = new CelestialPlanetPhysicsConfig();
	[Export] public Godot.Collections.Array<CelestialPlanetPhysicsConfig> AdditionalPlanets { get; set; } = new();
	[Export] public Godot.Collections.Array<CelestialSatellitePhysicsConfig> Satellites { get; set; } = new();

	public static CelestialSystemPhysicsConfig CreateDefault()
	{
		var config = new CelestialSystemPhysicsConfig
		{
			StarMassSolar = 1.0f,
			PrimaryPlanet = new CelestialPlanetPhysicsConfig
			{
				BodyId = "primary",
				Name = "后土",
				Element = PlanetElement.Terra,
				Visible = true,
				RadiusEarth = 1.0f,
				MassEarth = 1.0f,
				OrbitDistanceAu = 1.0f,
				OrbitEccentricity = 0.0167f,
				OrbitInclinationDeg = 0.0f,
				RotationPeriodHours = 23.934f,
				RevolutionPeriodDays = 365.256f,
				AutoResolveRevolutionPeriod = true
			}
		};

		config.Satellites.Add(new CelestialSatellitePhysicsConfig
		{
			BodyId = "moon_0",
			Name = "广寒",
			Visible = true,
			RadiusEarth = 0.2724f,
			MassEarth = 0.0123f,
			OrbitDistancePlanetRadii = 60.3f,
			OrbitEccentricity = 0.0549f,
			OrbitInclinationDeg = 5.14f,
			RotationPeriodHours = 655.7f,
			RevolutionPeriodDays = 27.3217f,
			AutoResolveRevolutionPeriod = true
		});

		return config;
	}

	public CelestialSystemPhysicsConfig DuplicateConfig()
	{
		var copy = new CelestialSystemPhysicsConfig
		{
			StarMassSolar = StarMassSolar,
			PrimaryPlanet = PrimaryPlanet?.DuplicateConfig() ?? new CelestialPlanetPhysicsConfig()
		};

		if (AdditionalPlanets != null)
		{
			foreach (CelestialPlanetPhysicsConfig planet in AdditionalPlanets)
			{
				if (planet != null)
				{
					copy.AdditionalPlanets.Add(planet.DuplicateConfig());
				}
			}
		}

		if (Satellites != null)
		{
			foreach (CelestialSatellitePhysicsConfig satellite in Satellites)
			{
				if (satellite != null)
				{
					copy.Satellites.Add(satellite.DuplicateConfig());
				}
			}
		}

		return copy;
	}
}
