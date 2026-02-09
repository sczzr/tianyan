using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Genesis;

public static class CelestialPhysicsSolver
{
	private const double GravitationalConstant = 6.67430e-11;
	private const double EarthMassKg = 5.97219e24;
	private const double EarthRadiusM = 6_371_000.0;
	private const double SunMassKg = 1.98847e30;
	private const double AstronomicalUnitM = 149_597_870_700.0;
	private const double EarthGravityMs2 = 9.80665;
	private const double SecondsPerDay = 86400.0;
	private const double SecondsPerHour = 3600.0;

	private const double MinRelativeDensity = 0.08;
	private const double MaxRelativeDensity = 12.0;

	public sealed class ResolvedPlanet
	{
		public string Name { get; init; } = string.Empty;
		public PlanetElement Element { get; init; } = PlanetElement.Terra;
		public bool Visible { get; init; } = true;
		public float RadiusEarth { get; init; } = 1.0f;
		public float MassEarth { get; init; } = 1.0f;
		public float OrbitDistanceAu { get; init; } = 1.0f;
		public float OrbitEccentricity { get; init; } = 0.0f;
		public float OrbitInclinationDeg { get; init; } = 0.0f;
		public float RotationPeriodHours { get; init; } = 24.0f;
		public float RevolutionPeriodDays { get; init; } = 365.0f;
		public float SurfaceGravityEarth { get; init; } = 1.0f;
		public float SurfaceGravityMs2 { get; init; } = 9.80665f;
		public float OrbitAngularSpeedRadPerSec { get; init; }
		public float SpinAngularSpeedRadPerSec { get; init; }
	}

	public sealed class ResolvedSatellite
	{
		public string Name { get; init; } = string.Empty;
		public bool Visible { get; init; } = true;
		public float RadiusEarth { get; init; } = 0.2724f;
		public float MassEarth { get; init; } = 0.0123f;
		public float OrbitDistancePlanetRadii { get; init; } = 60.3f;
		public float OrbitEccentricity { get; init; }
		public float OrbitInclinationDeg { get; init; }
		public float RotationPeriodHours { get; init; } = 655.7f;
		public float RevolutionPeriodDays { get; init; } = 27.32f;
		public float SurfaceGravityEarth { get; init; } = 0.165f;
		public float SurfaceGravityMs2 { get; init; } = 1.62f;
		public float OrbitAngularSpeedRadPerSec { get; init; }
		public float SpinAngularSpeedRadPerSec { get; init; }
	}

	public sealed class ResolvedSystem
	{
		public float StarMassSolar { get; init; } = 1.0f;
		public ResolvedPlanet PrimaryPlanet { get; init; } = new();
		public List<ResolvedPlanet> AdditionalPlanets { get; init; } = new();
		public List<ResolvedSatellite> Satellites { get; init; } = new();
		public List<string> Diagnostics { get; init; } = new();
	}

	public static ResolvedSystem Resolve(CelestialSystemPhysicsConfig source)
	{
		CelestialSystemPhysicsConfig safeConfig = source?.DuplicateConfig() ?? CelestialSystemPhysicsConfig.CreateDefault();
		float starMassSolar = Mathf.Clamp(safeConfig.StarMassSolar, 0.08f, 80.0f);
		var diagnostics = new List<string>();

		ResolvedPlanet primary = ResolvePlanet(
			safeConfig.PrimaryPlanet ?? new CelestialPlanetPhysicsConfig(),
			starMassSolar,
			diagnostics,
			"Primary");

		var additionalPlanets = new List<ResolvedPlanet>();
		if (safeConfig.AdditionalPlanets != null)
		{
			for (int i = 0; i < safeConfig.AdditionalPlanets.Count; i++)
			{
				CelestialPlanetPhysicsConfig planet = safeConfig.AdditionalPlanets[i];
				if (planet == null)
				{
					continue;
				}

				additionalPlanets.Add(ResolvePlanet(planet, starMassSolar, diagnostics, $"Planet[{i}]") );
			}
		}

		var satellites = new List<ResolvedSatellite>();
		if (safeConfig.Satellites != null)
		{
			for (int i = 0; i < safeConfig.Satellites.Count; i++)
			{
				CelestialSatellitePhysicsConfig satellite = safeConfig.Satellites[i];
				if (satellite == null)
				{
					continue;
				}

				satellites.Add(ResolveSatellite(satellite, primary, diagnostics, $"Satellite[{i}]") );
			}
		}

		return new ResolvedSystem
		{
			StarMassSolar = starMassSolar,
			PrimaryPlanet = primary,
			AdditionalPlanets = additionalPlanets,
			Satellites = satellites,
			Diagnostics = diagnostics
		};
	}

	private static ResolvedPlanet ResolvePlanet(
		CelestialPlanetPhysicsConfig source,
		float starMassSolar,
		List<string> diagnostics,
		string bodyLabel)
	{
		float radiusEarth = Mathf.Clamp(source.RadiusEarth, 0.1f, 20.0f);
		float massEarthRaw = Mathf.Clamp(source.MassEarth, 0.01f, 500.0f);
		float minMassByDensity = radiusEarth * radiusEarth * radiusEarth * (float)MinRelativeDensity;
		float maxMassByDensity = radiusEarth * radiusEarth * radiusEarth * (float)MaxRelativeDensity;
		float massEarth = Mathf.Clamp(massEarthRaw, minMassByDensity, maxMassByDensity);

		if (!Mathf.IsEqualApprox(massEarthRaw, massEarth))
		{
			diagnostics.Add($"{bodyLabel}: mass clamped by density range.");
		}

		float orbitEccentricity = Mathf.Clamp(source.OrbitEccentricity, 0f, 0.9f);
		float rotationPeriodHours = Mathf.Clamp(source.RotationPeriodHours, 2f, 5000f);
		float orbitDistanceAu = Mathf.Clamp(source.OrbitDistanceAu, 0.03f, 80f);
		float revolutionPeriodDays = Mathf.Clamp(source.RevolutionPeriodDays, 0.2f, 500000f);

		if (source.AutoResolveRevolutionPeriod)
		{
			revolutionPeriodDays = ComputePlanetRevolutionDays(orbitDistanceAu, starMassSolar, massEarth);
		}
		else
		{
			orbitDistanceAu = ComputePlanetOrbitDistanceAu(revolutionPeriodDays, starMassSolar, massEarth);
		}

		float gravityEarth = Mathf.Max(0.01f, massEarth / Mathf.Max(0.0001f, radiusEarth * radiusEarth));
		float gravityMs2 = (float)(gravityEarth * EarthGravityMs2);

		float orbitAngularSpeed = ComputeAngularSpeed(revolutionPeriodDays * (float)SecondsPerDay);
		float spinAngularSpeed = ComputeAngularSpeed(rotationPeriodHours * (float)SecondsPerHour);

		return new ResolvedPlanet
		{
			Name = string.IsNullOrWhiteSpace(source.Name) ? bodyLabel : source.Name,
			Element = source.Element,
			Visible = source.Visible,
			RadiusEarth = radiusEarth,
			MassEarth = massEarth,
			OrbitDistanceAu = orbitDistanceAu,
			OrbitEccentricity = orbitEccentricity,
			OrbitInclinationDeg = Mathf.Clamp(source.OrbitInclinationDeg, -60f, 60f),
			RotationPeriodHours = rotationPeriodHours,
			RevolutionPeriodDays = revolutionPeriodDays,
			SurfaceGravityEarth = gravityEarth,
			SurfaceGravityMs2 = gravityMs2,
			OrbitAngularSpeedRadPerSec = orbitAngularSpeed,
			SpinAngularSpeedRadPerSec = spinAngularSpeed
		};
	}

	private static ResolvedSatellite ResolveSatellite(
		CelestialSatellitePhysicsConfig source,
		ResolvedPlanet primary,
		List<string> diagnostics,
		string bodyLabel)
	{
		float radiusEarth = Mathf.Clamp(source.RadiusEarth, 0.01f, 5.0f);
		float massEarthRaw = Mathf.Clamp(source.MassEarth, 0.0001f, 30.0f);
		float minMassByDensity = radiusEarth * radiusEarth * radiusEarth * (float)MinRelativeDensity;
		float maxMassByDensity = radiusEarth * radiusEarth * radiusEarth * (float)MaxRelativeDensity;
		float massEarth = Mathf.Clamp(massEarthRaw, minMassByDensity, maxMassByDensity);

		if (!Mathf.IsEqualApprox(massEarthRaw, massEarth))
		{
			diagnostics.Add($"{bodyLabel}: mass clamped by density range.");
		}

		float orbitDistancePlanetRadii = Mathf.Clamp(source.OrbitDistancePlanetRadii, 2.0f, 500f);
		float rotationPeriodHours = Mathf.Clamp(source.RotationPeriodHours, 1.0f, 5000f);
		float revolutionPeriodDays = Mathf.Clamp(source.RevolutionPeriodDays, 0.1f, 100000f);

		if (source.AutoResolveRevolutionPeriod)
		{
			revolutionPeriodDays = ComputeSatelliteRevolutionDays(orbitDistancePlanetRadii, primary.RadiusEarth, primary.MassEarth, massEarth);
		}
		else
		{
			orbitDistancePlanetRadii = ComputeSatelliteOrbitDistancePlanetRadii(revolutionPeriodDays, primary.RadiusEarth, primary.MassEarth, massEarth);
		}

		float gravityEarth = Mathf.Max(0.001f, massEarth / Mathf.Max(0.0001f, radiusEarth * radiusEarth));
		float gravityMs2 = (float)(gravityEarth * EarthGravityMs2);
		float orbitAngularSpeed = ComputeAngularSpeed(revolutionPeriodDays * (float)SecondsPerDay);
		float spinAngularSpeed = ComputeAngularSpeed(rotationPeriodHours * (float)SecondsPerHour);

		return new ResolvedSatellite
		{
			Name = string.IsNullOrWhiteSpace(source.Name) ? bodyLabel : source.Name,
			Visible = source.Visible,
			RadiusEarth = radiusEarth,
			MassEarth = massEarth,
			OrbitDistancePlanetRadii = orbitDistancePlanetRadii,
			OrbitEccentricity = Mathf.Clamp(source.OrbitEccentricity, 0f, 0.9f),
			OrbitInclinationDeg = Mathf.Clamp(source.OrbitInclinationDeg, -80f, 80f),
			RotationPeriodHours = rotationPeriodHours,
			RevolutionPeriodDays = revolutionPeriodDays,
			SurfaceGravityEarth = gravityEarth,
			SurfaceGravityMs2 = gravityMs2,
			OrbitAngularSpeedRadPerSec = orbitAngularSpeed,
			SpinAngularSpeedRadPerSec = spinAngularSpeed
		};
	}

	private static float ComputePlanetRevolutionDays(float orbitDistanceAu, float starMassSolar, float planetMassEarth)
	{
		double axisM = orbitDistanceAu * AstronomicalUnitM;
		double massKg = starMassSolar * SunMassKg + planetMassEarth * EarthMassKg;
		double periodSec = Math.Tau * Math.Sqrt((axisM * axisM * axisM) / (GravitationalConstant * massKg));
		return (float)(periodSec / SecondsPerDay);
	}

	private static float ComputePlanetOrbitDistanceAu(float revolutionDays, float starMassSolar, float planetMassEarth)
	{
		double periodSec = Math.Max(1.0, revolutionDays * SecondsPerDay);
		double massKg = starMassSolar * SunMassKg + planetMassEarth * EarthMassKg;
		double axisM = Math.Pow((periodSec / Math.Tau) * (periodSec / Math.Tau) * GravitationalConstant * massKg, 1.0 / 3.0);
		return (float)Math.Clamp(axisM / AstronomicalUnitM, 0.03, 80.0);
	}

	private static float ComputeSatelliteRevolutionDays(float orbitDistancePlanetRadii, float primaryRadiusEarth, float primaryMassEarth, float satelliteMassEarth)
	{
		double primaryRadiusM = primaryRadiusEarth * EarthRadiusM;
		double axisM = orbitDistancePlanetRadii * primaryRadiusM;
		double massKg = (primaryMassEarth + satelliteMassEarth) * EarthMassKg;
		double periodSec = Math.Tau * Math.Sqrt((axisM * axisM * axisM) / (GravitationalConstant * massKg));
		return (float)(periodSec / SecondsPerDay);
	}

	private static float ComputeSatelliteOrbitDistancePlanetRadii(float revolutionDays, float primaryRadiusEarth, float primaryMassEarth, float satelliteMassEarth)
	{
		double periodSec = Math.Max(1.0, revolutionDays * SecondsPerDay);
		double massKg = (primaryMassEarth + satelliteMassEarth) * EarthMassKg;
		double axisM = Math.Pow((periodSec / Math.Tau) * (periodSec / Math.Tau) * GravitationalConstant * massKg, 1.0 / 3.0);
		double primaryRadiusM = Math.Max(1.0, primaryRadiusEarth * EarthRadiusM);
		return (float)Math.Clamp(axisM / primaryRadiusM, 2.0, 500.0);
	}

	private static float ComputeAngularSpeed(float periodSeconds)
	{
		if (periodSeconds <= 0.0001f)
		{
			return 0f;
		}

		return (float)(Math.Tau / periodSeconds);
	}
}
