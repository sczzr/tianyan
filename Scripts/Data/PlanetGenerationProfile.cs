using Godot;

namespace FantasyMapGenerator.Scripts.Data;

public readonly struct PlanetGenerationProfile
{
	public int TectonicPlateCount { get; }
	public int WindCellCount { get; }
	public int ErosionIterations { get; }
	public float ErosionStrength { get; }
	public float HeatFactor { get; }
	public float ContinentalFrequency { get; }
	public float ReliefStrength { get; }
	public float MoistureTransport { get; }
	public float PlateBoundarySharpness { get; }

	public static PlanetGenerationProfile Default => new(
		tectonicPlateCount: 24,
		windCellCount: 8,
		erosionIterations: 4,
		erosionStrength: 0.16f,
		heatFactor: 560f,
		continentalFrequency: 0.62f,
		reliefStrength: 0.38f,
		moistureTransport: 0.58f,
		plateBoundarySharpness: 6.2f);

	public PlanetGenerationProfile(
		int tectonicPlateCount,
		int windCellCount,
		int erosionIterations,
		float erosionStrength,
		float heatFactor,
		float continentalFrequency,
		float reliefStrength,
		float moistureTransport,
		float plateBoundarySharpness)
	{
		TectonicPlateCount = Mathf.Clamp(tectonicPlateCount, 1, 64);
		WindCellCount = Mathf.Clamp(windCellCount, 1, 24);
		ErosionIterations = Mathf.Clamp(erosionIterations, 0, 16);
		ErosionStrength = Mathf.Clamp(erosionStrength, 0f, 1f);
		HeatFactor = Mathf.Clamp(heatFactor, 1f, 1000f);
		ContinentalFrequency = Mathf.Clamp(continentalFrequency, 0.08f, 2.5f);
		ReliefStrength = Mathf.Clamp(reliefStrength, 0f, 1f);
		MoistureTransport = Mathf.Clamp(moistureTransport, 0f, 1f);
		PlateBoundarySharpness = Mathf.Clamp(plateBoundarySharpness, 1f, 24f);
	}

	public static PlanetGenerationProfile FromPlanet(PlanetData planetData, int lawAlignment = 50)
	{
		if (planetData == null)
		{
			return Default;
		}

		float ocean = Mathf.Clamp(planetData.OceanCoverage, 0f, 1f);
		float temperature = Mathf.Clamp(planetData.Temperature, 0f, 1f);
		float atmosphere = Mathf.Clamp(planetData.AtmosphereDensity, 0f, 1f);
		float mountain = Mathf.Clamp(planetData.MountainIntensity, 0f, 1f);
		float law = Mathf.Clamp(lawAlignment / 100f, 0f, 1f);

		float plateSignal = mountain * 0.72f + (1f - ocean) * 0.28f;
		int plateCount = Mathf.RoundToInt(Mathf.Lerp(8f, 38f, plateSignal));

		float windSignal = atmosphere * 0.78f + (1f - Mathf.Abs(temperature - 0.5f) * 2f) * 0.22f;
		int windCellCount = Mathf.RoundToInt(Mathf.Lerp(4f, 18f, windSignal));

		float erosionSignal = atmosphere * 0.46f + ocean * 0.24f + mountain * 0.3f;
		int erosionIterations = Mathf.RoundToInt(Mathf.Lerp(2f, 10f, erosionSignal));
		float erosionStrength = Mathf.Lerp(0.1f, 0.46f, erosionSignal);

		float heatFactor = Mathf.Lerp(920f, 140f, temperature);
		heatFactor = Mathf.Lerp(heatFactor, Mathf.Lerp(780f, 220f, temperature), law * 0.35f);

		float continentalFrequency = Mathf.Lerp(0.42f, 0.84f, 1f - ocean * 0.72f);
		continentalFrequency = Mathf.Lerp(continentalFrequency, continentalFrequency * 1.14f, law * 0.18f);

		float reliefStrength = Mathf.Lerp(0.22f, 0.86f, mountain);
		float moistureTransport = Mathf.Lerp(0.3f, 0.92f, atmosphere);
		float boundarySharpness = Mathf.Lerp(4.8f, 11.6f, mountain * 0.82f + law * 0.18f);

		return new PlanetGenerationProfile(
			plateCount,
			windCellCount,
			erosionIterations,
			erosionStrength,
			heatFactor,
			continentalFrequency,
			reliefStrength,
			moistureTransport,
			boundarySharpness);
	}
}
