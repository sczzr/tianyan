using System;
using Godot;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Genesis;

public static class PlanetTerrainGenerator
{
	private struct Plate
	{
		public Vector3 Direction;
		public Vector3 Drift;
		public float Uplift;
		public float ContinentalBias;
	}

	public static float[] GenerateHeightmap(PlanetData planetData, PlanetGenerationProfile profile, int width, int height, int seed)
	{
		if (planetData == null)
		{
			return null;
		}

		int targetWidth = Mathf.Max(64, width);
		int targetHeight = Mathf.Max(32, height);
		int pixelCount = targetWidth * targetHeight;

		float oceanCoverage = Mathf.Clamp(planetData.OceanCoverage, 0f, 1f);
		float mountainIntensity = Mathf.Clamp(planetData.MountainIntensity, 0f, 1f);
		float atmosphere = Mathf.Clamp(planetData.AtmosphereDensity, 0f, 1f);
		float temperature = Mathf.Clamp(planetData.Temperature, 0f, 1f);

		float tectonicScale = Mathf.Clamp((profile.TectonicPlateCount - 1f) / 63f, 0f, 1f);
		float windScale = Mathf.Clamp((profile.WindCellCount - 1f) / 23f, 0f, 1f);
		float erosionScale = Mathf.Clamp(profile.ErosionStrength * (profile.ErosionIterations / 16f), 0f, 1f);
		float heatNormalized = Mathf.Clamp((1000f - profile.HeatFactor) / 999f, 0f, 1f);

		var continentalNoise = new FastNoiseLite
		{
			Seed = seed,
			NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
			Frequency = Mathf.Clamp(profile.ContinentalFrequency * Mathf.Lerp(0.52f, 1.18f, tectonicScale), 0.08f, 2.8f),
			FractalOctaves = 3,
			FractalLacunarity = 2f,
			FractalGain = 0.55f
		};

		var tectonicNoise = new FastNoiseLite
		{
			Seed = seed + 97,
			NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
			Frequency = Mathf.Lerp(0.55f, 1.42f, profile.ReliefStrength),
			FractalOctaves = 3,
			FractalGain = 0.58f
		};

		var terrainNoise = new FastNoiseLite
		{
			Seed = seed + 211,
			NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
			Frequency = Mathf.Lerp(1.25f, 4.2f, profile.ReliefStrength),
			FractalOctaves = 3,
			FractalLacunarity = 2.05f,
			FractalGain = 0.49f
		};

		var ridgeNoise = new FastNoiseLite
		{
			Seed = seed + 359,
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Frequency = Mathf.Lerp(3.4f, 9.6f, profile.ReliefStrength),
			FractalOctaves = 4,
			FractalGain = 0.46f
		};

		var windNoise = new FastNoiseLite
		{
			Seed = seed + 503,
			NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth,
			Frequency = Mathf.Lerp(0.8f, 3.8f, windScale),
			FractalOctaves = 2,
			FractalGain = 0.62f
		};

		var chunkNoise = new FastNoiseLite
		{
			Seed = seed + 941,
			NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular,
			Frequency = Mathf.Lerp(0.24f, 1.55f, tectonicScale)
		};

		Plate[] plates = BuildPlates(seed + 701, profile.TectonicPlateCount, oceanCoverage);
		float[] elevation = new float[pixelCount];
		float[] moisture = new float[pixelCount];
		float[] boundaryMap = new float[pixelCount];

		float windBands = Mathf.Max(2f, profile.WindCellCount * 0.62f);
		for (int y = 0; y < targetHeight; y++)
		{
			float v = y / (float)(targetHeight - 1);
			float lat = Mathf.Abs(v - 0.5f) * 2f;
			float heatBand = ComputeHeatBand(lat, profile.HeatFactor, temperature);

			for (int x = 0; x < targetWidth; x++)
			{
				int index = y * targetWidth + x;
				float u = x / (float)(targetWidth - 1);
				Vector3 direction = GetSphericalDirection(u, v);

				GetPlateSignal(direction, plates, profile.PlateBoundarySharpness, out float boundary, out float uplift, out int primaryPlateIndex);
				boundaryMap[index] = boundary;
				float plateContinental = primaryPlateIndex >= 0 ? plates[primaryPlateIndex].ContinentalBias : 0.5f;

				float macro = NoiseToUnitRange(continentalNoise.GetNoise3D(direction.X * 2.0f, direction.Y * 2.0f, direction.Z * 2.0f));
				float tectonic = NoiseToUnitRange(tectonicNoise.GetNoise3D(direction.X * 3.0f, direction.Y * 3.0f, direction.Z * 3.0f));
				float regional = NoiseToUnitRange(terrainNoise.GetNoise3D(direction.X * 7.2f, direction.Y * 7.2f, direction.Z * 7.2f));
				float ridges = Mathf.Abs(ridgeNoise.GetNoise3D(direction.X * 13.8f, direction.Y * 13.8f, direction.Z * 13.8f));
				float chunk = NoiseToUnitRange(chunkNoise.GetNoise3D(direction.X * 2.8f, direction.Y * 2.8f, direction.Z * 2.8f));

				float windNoise01 = NoiseToUnitRange(windNoise.GetNoise3D(direction.X * (2.0f + windScale * 5.6f), direction.Y * (2.0f + windScale * 5.6f), direction.Z * (2.0f + windScale * 5.6f)));
				float zonalWave = 0.5f + 0.5f * Mathf.Sin(v * Mathf.Tau * windBands + u * Mathf.Tau * 0.35f + windNoise01 * 1.8f);
				float windField = Mathf.Clamp(windNoise01 * 0.46f + zonalWave * 0.54f, 0f, 1f);

				float continental = plateContinental * 0.36f + macro * 0.26f + chunk * 0.22f + tectonic * 0.1f + regional * 0.06f;
				continental = Mathf.Clamp(Mathf.SmoothStep(0.14f, 0.9f, continental), 0f, 1f);

				float reliefWeight = Mathf.Lerp(0.05f, 0.24f, mountainIntensity) * profile.ReliefStrength;
				float plateLift = uplift * Mathf.Lerp(0.06f, 0.32f, mountainIntensity) * (0.55f + tectonicScale * 0.65f);
				float boundaryRidge = boundary * Mathf.Lerp(0.03f, 0.26f, mountainIntensity) * (0.45f + tectonicScale * 0.9f);
				float thermalSeaShift = (heatNormalized - 0.5f) * 0.09f;

				float elevationValue = continental
					+ (regional - 0.5f) * reliefWeight
					+ ridges * reliefWeight * 0.85f
					+ plateLift
					+ boundaryRidge
					- lat * Mathf.Lerp(0.01f, 0.1f, 1f - temperature)
					- thermalSeaShift * 0.55f
					+ (chunk - 0.5f) * 0.08f * (1f - tectonicScale * 0.35f);

				elevation[index] = Mathf.Clamp(elevationValue, 0f, 1f);

				float moistureValue = 0.22f
					+ windField * 0.44f
					+ (1f - lat) * 0.18f
					+ atmosphere * 0.18f
					+ oceanCoverage * 0.16f
					+ heatBand * 0.12f
					- Mathf.Abs(heatBand - 0.5f) * 0.12f
					- Mathf.Clamp((elevationValue - 0.55f) * 0.22f, 0f, 0.22f);
				moisture[index] = Mathf.Clamp(moistureValue * (0.55f + profile.MoistureTransport * 0.75f), 0f, 1f);
			}
		}

		if (profile.ErosionIterations > 0 && profile.ErosionStrength > 0.0001f)
		{
			float erosionStrength = profile.ErosionStrength * Mathf.Lerp(0.45f, 1.75f, profile.ReliefStrength);
			for (int i = 0; i < profile.ErosionIterations; i++)
			{
				float t = (i + 1f) / Mathf.Max(1f, profile.ErosionIterations);
				float stepStrength = erosionStrength * Mathf.Lerp(0.65f, 1.25f, t);
				ApplyHydraulicErosion(elevation, moisture, targetWidth, targetHeight, stepStrength);
			}

			int smoothingPasses = Mathf.RoundToInt(profile.ErosionIterations * profile.ErosionStrength * 0.6f);
			for (int i = 0; i < smoothingPasses; i++)
			{
				ApplyNeighborhoodSmoothing(elevation, targetWidth, targetHeight, 0.22f + profile.ErosionStrength * 0.34f);
			}
		}

		float landBias = Mathf.Lerp(0.16f, -0.22f, oceanCoverage);
		float heatBias = Mathf.Lerp(0.04f, -0.04f, heatNormalized);
		for (int i = 0; i < elevation.Length; i++)
		{
			float mountainReinforce = boundaryMap[i] * mountainIntensity * 0.18f;
			float climateCarve = (moisture[i] - 0.5f) * Mathf.Lerp(-0.02f, 0.03f, heatNormalized);
			elevation[i] = Mathf.Clamp(elevation[i] + landBias + heatBias + mountainReinforce + climateCarve, 0f, 1f);
		}

		float chunkPassesF = Mathf.Lerp(3f, 1f, tectonicScale) + Mathf.Lerp(0f, 1f, erosionScale * 0.45f);
		int chunkPasses = Mathf.Clamp(Mathf.RoundToInt(chunkPassesF), 1, 4);
		ApplyLandOceanChunking(elevation, boundaryMap, targetWidth, targetHeight, oceanCoverage, chunkPasses);

		ApplyNeighborhoodSmoothing(elevation, targetWidth, targetHeight, Mathf.Lerp(0.03f, 0.12f, erosionScale));
		Normalize(elevation);
		return elevation;
	}

	private static Plate[] BuildPlates(int seed, int count, float oceanCoverage)
	{
		var random = new Random(seed);
		var plates = new Plate[count];
		float offset = (float)random.NextDouble() * 2f;

		for (int i = 0; i < count; i++)
		{
			float t = (i + offset) / Mathf.Max(1f, count - 1f);
			float z = Mathf.Lerp(1f, -1f, t);
			float radius = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
			float theta = Mathf.Tau * (i * 0.61803398875f + (float)random.NextDouble() * 0.33f);

			Vector3 dir = new(
				radius * Mathf.Cos(theta),
				z,
				radius * Mathf.Sin(theta));
			dir = dir.Normalized();

			Vector3 drift = new(
				(float)random.NextDouble() * 2f - 1f,
				(float)random.NextDouble() * 2f - 1f,
				(float)random.NextDouble() * 2f - 1f);
			drift = drift.Normalized();

			float continentalBias = Mathf.Lerp(-0.35f, 0.85f, (float)random.NextDouble());
			continentalBias += (0.5f - oceanCoverage) * 0.85f;
			continentalBias = Mathf.Clamp(continentalBias, -0.95f, 0.95f);

			plates[i] = new Plate
			{
				Direction = dir,
				Drift = drift,
				Uplift = Mathf.Lerp(0.12f, 1f, (float)random.NextDouble()),
				ContinentalBias = (continentalBias + 1f) * 0.5f
			};
		}

		return plates;
	}

	private static void GetPlateSignal(Vector3 direction, Plate[] plates, float boundarySharpness, out float boundary, out float uplift, out int primaryIndex)
	{
		primaryIndex = -1;
		int secondaryIndex = -1;
		float bestDot = -2f;
		float secondDot = -2f;

		for (int i = 0; i < plates.Length; i++)
		{
			float dot = direction.Dot(plates[i].Direction);
			if (dot > bestDot)
			{
				secondDot = bestDot;
				secondaryIndex = primaryIndex;
				bestDot = dot;
				primaryIndex = i;
			}
			else if (dot > secondDot)
			{
				secondDot = dot;
				secondaryIndex = i;
			}
		}

		if (primaryIndex < 0)
		{
			boundary = 0f;
			uplift = 0f;
			return;
		}

		float interiorGap = Mathf.Max(0f, bestDot - secondDot);
		boundary = Mathf.Clamp(1f - interiorGap * boundarySharpness, 0f, 1f);

		Plate primary = plates[primaryIndex];
		Plate secondary = secondaryIndex >= 0 ? plates[secondaryIndex] : primary;
		float driftConvergence = Mathf.Clamp((primary.Drift - secondary.Drift).Length() * 0.5f, 0f, 1f);
		uplift = Mathf.Clamp(primary.Uplift * 0.45f + secondary.Uplift * 0.25f + boundary * driftConvergence * 0.55f, 0f, 1f);
	}

	private static float ComputeHeatBand(float lat01, float heatFactor, float baseTemperature)
	{
		float heatNormalized = Mathf.Clamp((1000f - heatFactor) / 999f, 0f, 1f);
		float bandExponent = Mathf.Lerp(1.9f, 0.66f, heatNormalized);
		float latFalloff = Mathf.Pow(Mathf.Clamp(lat01, 0f, 1f), bandExponent);
		float equatorBand = 1f - latFalloff;
		return Mathf.Clamp(Mathf.Lerp(equatorBand * 0.72f, equatorBand * 1.28f, baseTemperature), 0f, 1f);
	}

	private static void ApplyLandOceanChunking(float[] elevation, float[] boundaryMap, int width, int height, float oceanCoverage, int passes)
	{
		if (elevation == null || elevation.Length != width * height)
		{
			return;
		}

		float dynamicSeaLevel = Mathf.Clamp(Mathf.Lerp(0.46f, 0.68f, oceanCoverage), 0.2f, 0.84f);
		bool[] landMask = new bool[elevation.Length];
		for (int i = 0; i < elevation.Length; i++)
		{
			landMask[i] = elevation[i] >= dynamicSeaLevel;
		}

		for (int pass = 0; pass < passes; pass++)
		{
			bool[] next = new bool[landMask.Length];
			for (int y = 0; y < height; y++)
			{
				int y0 = Mathf.Max(0, y - 1);
				int y2 = Mathf.Min(height - 1, y + 1);
				for (int x = 0; x < width; x++)
				{
					int x0 = x <= 0 ? width - 1 : x - 1;
					int x2 = x >= width - 1 ? 0 : x + 1;
					int index = y * width + x;

					int landVotes = 0;
					landVotes += landMask[y0 * width + x0] ? 1 : 0;
					landVotes += landMask[y0 * width + x] ? 1 : 0;
					landVotes += landMask[y0 * width + x2] ? 1 : 0;
					landVotes += landMask[y * width + x0] ? 1 : 0;
					landVotes += landMask[index] ? 1 : 0;
					landVotes += landMask[y * width + x2] ? 1 : 0;
					landVotes += landMask[y2 * width + x0] ? 1 : 0;
					landVotes += landMask[y2 * width + x] ? 1 : 0;
					landVotes += landMask[y2 * width + x2] ? 1 : 0;

					next[index] = landVotes >= 5;
				}
			}
			landMask = next;
		}

		for (int i = 0; i < elevation.Length; i++)
		{
			float boundaryBoost = boundaryMap != null && boundaryMap.Length == elevation.Length
				? boundaryMap[i] * 0.02f
				: 0f;
			if (landMask[i])
			{
				elevation[i] = Mathf.Max(elevation[i], dynamicSeaLevel + 0.04f + boundaryBoost);
			}
			else
			{
				elevation[i] = Mathf.Min(elevation[i], dynamicSeaLevel - 0.035f);
			}
		}
	}

	private static void ApplyHydraulicErosion(float[] elevation, float[] moisture, int width, int height, float erosionStrength)
	{
		if (erosionStrength <= 0.0001f)
		{
			return;
		}

		float[] next = new float[elevation.Length];

		for (int y = 0; y < height; y++)
		{
			int y0 = Mathf.Max(0, y - 1);
			int y1 = y;
			int y2 = Mathf.Min(height - 1, y + 1);

			for (int x = 0; x < width; x++)
			{
				int x0 = x <= 0 ? width - 1 : x - 1;
				int x1 = x;
				int x2 = x >= width - 1 ? 0 : x + 1;

				int index = y * width + x;
				float center = elevation[index];
				float avg = (
					elevation[y0 * width + x0] + elevation[y0 * width + x1] + elevation[y0 * width + x2] +
					elevation[y1 * width + x0] + elevation[y1 * width + x1] + elevation[y1 * width + x2] +
					elevation[y2 * width + x0] + elevation[y2 * width + x1] + elevation[y2 * width + x2]) / 9f;

				float slope = center - avg;
				float wetness = Mathf.Clamp(moisture[index], 0f, 1f);
				float erosion = Mathf.Max(0f, slope) * erosionStrength * (0.32f + wetness * 0.68f);
				float deposition = Mathf.Max(0f, -slope) * erosionStrength * (0.18f + wetness * 0.16f);

				next[index] = Mathf.Clamp(center - erosion + deposition, 0f, 1f);
			}
		}

		Array.Copy(next, elevation, elevation.Length);
	}

	private static void ApplyNeighborhoodSmoothing(float[] values, int width, int height, float amount)
	{
		if (values == null || values.Length != width * height)
		{
			return;
		}

		float blend = Mathf.Clamp(amount, 0f, 1f);
		if (blend <= 0.0001f)
		{
			return;
		}

		float[] next = new float[values.Length];
		for (int y = 0; y < height; y++)
		{
			int y0 = Mathf.Max(0, y - 1);
			int y2 = Mathf.Min(height - 1, y + 1);
			for (int x = 0; x < width; x++)
			{
				int x0 = x <= 0 ? width - 1 : x - 1;
				int x2 = x >= width - 1 ? 0 : x + 1;
				int index = y * width + x;

				float mean = (
					values[y0 * width + x0] + values[y0 * width + x] + values[y0 * width + x2] +
					values[y * width + x0] + values[index] + values[y * width + x2] +
					values[y2 * width + x0] + values[y2 * width + x] + values[y2 * width + x2]) / 9f;

				next[index] = Mathf.Lerp(values[index], mean, blend);
			}
		}

		Array.Copy(next, values, values.Length);
	}

	private static Vector3 GetSphericalDirection(float u, float v)
	{
		float longitude = (u - 0.5f) * Mathf.Tau;
		float latitude = (0.5f - v) * Mathf.Pi;
		float cosLat = Mathf.Cos(latitude);
		return new Vector3(
			cosLat * Mathf.Cos(longitude),
			Mathf.Sin(latitude),
			cosLat * Mathf.Sin(longitude));
	}

	private static float NoiseToUnitRange(float noiseValue)
	{
		return (noiseValue + 1f) * 0.5f;
	}

	private static void Normalize(float[] values)
	{
		if (values == null || values.Length == 0)
		{
			return;
		}

		float min = float.MaxValue;
		float max = float.MinValue;
		for (int i = 0; i < values.Length; i++)
		{
			min = Mathf.Min(min, values[i]);
			max = Mathf.Max(max, values[i]);
		}

		float range = max - min;
		if (range < 0.000001f)
		{
			return;
		}

		for (int i = 0; i < values.Length; i++)
		{
			values[i] = (values[i] - min) / range;
		}
	}
}
