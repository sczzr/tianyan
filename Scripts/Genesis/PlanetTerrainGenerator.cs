using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Genesis;

public static class PlanetTerrainGenerator
{
	private const float MinSeaLevel = 0.24f;
	private const float MaxSeaLevel = 0.62f;
	private const float ReferenceTectonicSeaLevel = 0.5f;
	private const int MinEffectivePlateCount = 64;
	private static readonly int[] NeighborOffsetX = { -1, 0, 1, -1, 1, -1, 0, 1 };
	private static readonly int[] NeighborOffsetY = { -1, -1, -1, 0, 0, 1, 1, 1 };

	private readonly struct PlateAttribute
	{
		public PlateAttribute(float vectorX, float vectorY, bool isOceanic, float baseElevation)
		{
			VectorX = vectorX;
			VectorY = vectorY;
			IsOceanic = isOceanic;
			BaseElevation = baseElevation;
		}

		public float VectorX { get; }
		public float VectorY { get; }
		public bool IsOceanic { get; }
		public float BaseElevation { get; }
	}

	private readonly struct NeighborForce
	{
		public NeighborForce(int id, int neighbor, float directForce, float shearForce, char boundaryType)
		{
			Id = id;
			Neighbor = neighbor;
			DirectForce = directForce;
			ShearForce = shearForce;
			BoundaryType = boundaryType;
		}

		public int Id { get; }
		public int Neighbor { get; }
		public float DirectForce { get; }
		public float ShearForce { get; }
		public char BoundaryType { get; }
	}

	public enum PlanetBiomeType
	{
		Ocean,
		ShallowOcean,
		Coastland,
		TropicalRainForest,
		TropicalSeasonalForest,
		Shrubland,
		Savannah,
		TropicalDesert,
		TemperateRainForest,
		TemperateSeasonalForest,
		Chaparral,
		Grassland,
		Steppe,
		TemperateDesert,
		BorealForest,
		Taiga,
		Tundra,
		Ice,
		RockyMountain,
		SnowyMountain
	}


	private readonly struct BiomeAnchor
	{
		public BiomeAnchor(PlanetBiomeType biome, float temperature, float moisture, float reliefPreference)
		{
			Biome = biome;
			Temperature = temperature;
			Moisture = moisture;
			ReliefPreference = reliefPreference;
		}

		public PlanetBiomeType Biome { get; }
		public float Temperature { get; }
		public float Moisture { get; }
		public float ReliefPreference { get; }
	}

	private static readonly BiomeAnchor[] LandBiomeAnchors =
	{
		new BiomeAnchor(PlanetBiomeType.TropicalDesert, 0.9f, 0.08f, 0.25f),
		new BiomeAnchor(PlanetBiomeType.Savannah, 0.83f, 0.2f, 0.22f),
		new BiomeAnchor(PlanetBiomeType.Shrubland, 0.74f, 0.3f, 0.24f),
		new BiomeAnchor(PlanetBiomeType.TropicalSeasonalForest, 0.82f, 0.5f, 0.2f),
		new BiomeAnchor(PlanetBiomeType.TropicalRainForest, 0.88f, 0.76f, 0.18f),
		new BiomeAnchor(PlanetBiomeType.TemperateDesert, 0.56f, 0.1f, 0.26f),
		new BiomeAnchor(PlanetBiomeType.Steppe, 0.48f, 0.18f, 0.22f),
		new BiomeAnchor(PlanetBiomeType.Grassland, 0.5f, 0.3f, 0.2f),
		new BiomeAnchor(PlanetBiomeType.Chaparral, 0.52f, 0.42f, 0.22f),
		new BiomeAnchor(PlanetBiomeType.TemperateSeasonalForest, 0.5f, 0.56f, 0.2f),
		new BiomeAnchor(PlanetBiomeType.TemperateRainForest, 0.47f, 0.74f, 0.18f),
		new BiomeAnchor(PlanetBiomeType.Tundra, 0.15f, 0.2f, 0.22f),
		new BiomeAnchor(PlanetBiomeType.Taiga, 0.24f, 0.36f, 0.2f),
		new BiomeAnchor(PlanetBiomeType.BorealForest, 0.28f, 0.56f, 0.18f)
	};

	public readonly struct PlanetSurfaceData
	{
		public PlanetSurfaceData(
			float[] elevation,
			float[] temperature,
			float[] moisture,
			Vector2[] wind,
			PlanetBiomeType[] biomes,
			float[] riverLayer,
			float seaLevel)
		{
			Elevation = elevation;
			Temperature = temperature;
			Moisture = moisture;
			Wind = wind;
			Biomes = biomes;
			RiverLayer = riverLayer;
			SeaLevel = seaLevel;
		}

		public float[] Elevation { get; }
		public float[] Temperature { get; }
		public float[] Moisture { get; }
		public Vector2[] Wind { get; }
		public PlanetBiomeType[] Biomes { get; }
		public float[] RiverLayer { get; }
		public float SeaLevel { get; }
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

		int plateCount = Mathf.Clamp(Mathf.Max(profile.TectonicPlateCount, MinEffectivePlateCount), 2, 128);
		float plateOceanRatio = 0.5f;
		float mountainScale = Mathf.Lerp(0.72f, 1.42f, Mathf.Clamp(planetData.MountainIntensity, 0f, 1f));

		var random = new Random(seed);
		Vector2I[] plateCoords = GeneratePlateCoordinates(plateCount, targetWidth, targetHeight, random);
		int[] plateIdMap = SetPlateIds(plateCoords, targetWidth, targetHeight);
		PlateAttribute[] plateAttributes = SetPlateAttributes(plateOceanRatio, plateCount, random);
		Dictionary<int, List<NeighborForce>> neighborMap = SetPlateBoundaryStress(
			plateIdMap,
			plateAttributes,
			plateCoords,
			targetWidth,
			targetHeight,
			mountainScale);

		float[] ringCos = BuildRingCos(targetWidth);
		float[] ringSin = BuildRingSin(targetWidth);
		float[] latitudes = BuildLatitudes(targetHeight);

		FastNoiseLite baseNoise = CreateNoise(seed + 17);
		FastNoiseLite continuityNoise = CreateNoise(seed + 31);
		float[] elevation = CreatePerlinElevation(baseNoise, targetWidth, targetHeight, ringCos, ringSin, latitudes);
		for (int y = 0; y < targetHeight; y++)
		{
			for (int x = 0; x < targetWidth; x++)
			{
				int index = y * targetWidth + x;
				float nx = ringCos[x];
				float ny = latitudes[y];
				float nz = ringSin[x];
				float continuity = Noise01(continuityNoise, 0.9f * nx, 0.9f * ny, 0.9f * nz);
				elevation[index] = Mathf.Lerp(elevation[index], continuity, 0.55f);
			}
		}

		FastNoiseLite detailNoise = CreateNoise(seed + 113);
		float[] modified = ModifyElevation3(
			elevation,
			plateIdMap,
			plateAttributes,
			plateCoords,
			neighborMap,
			detailNoise,
			targetWidth,
			targetHeight,
			ReferenceTectonicSeaLevel,
			random,
			ringCos,
			ringSin,
			latitudes);

		int smoothingPasses = Mathf.Clamp(1 + profile.ErosionIterations / 4, 1, 4);
		for (int i = 0; i < smoothingPasses; i++)
		{
			modified = AverageElevation(modified, targetWidth, targetHeight, ReferenceTectonicSeaLevel);
		}

		modified = AverageArray(modified, modified, targetWidth, targetHeight, 1, elevationOnly: false, ReferenceTectonicSeaLevel);
		StitchWrapColumns(modified, targetWidth, targetHeight, 2);

		Normalize(modified);
		if (modified.Length != pixelCount)
		{
			return new float[pixelCount];
		}

		return modified;
	}

	public static PlanetSurfaceData GenerateSurfaceData(PlanetData planetData, PlanetGenerationProfile profile, int width, int height, int seed)
	{
		if (planetData == null)
		{
			return default;
		}

		int targetWidth = Mathf.Max(64, width);
		int targetHeight = Mathf.Max(32, height);
		int pixelCount = targetWidth * targetHeight;

		float[] elevation = GenerateHeightmap(planetData, profile, targetWidth, targetHeight, seed);
		if (elevation == null || elevation.Length != pixelCount)
		{
			return default;
		}
		StitchWrapColumns(elevation, targetWidth, targetHeight, 2);

		float targetOceanCoverage = Mathf.Clamp(planetData.OceanCoverage, 0f, 1f);
		float seaLevel = ResolveSeaLevelFromElevation(elevation, targetOceanCoverage);

		float[] ringCos = BuildRingCos(targetWidth);
		float[] ringSin = BuildRingSin(targetWidth);
		float[] latitudes = BuildLatitudes(targetHeight);

		var random = new Random(seed ^ 0x5EED5EED);
		FastNoiseLite temperatureNoise = CreateNoise(seed + 1403);
		FastNoiseLite moistureNoise = CreateNoise(seed + 1451);

		float[] temperature = GenerateBaseTemperature(
			temperatureNoise,
			elevation,
			profile,
			targetWidth,
			targetHeight,
			ringCos,
			ringSin,
			latitudes,
			doElevationAdjust: true);

		Normalize(temperature);
		StitchWrapColumns(temperature, targetWidth, targetHeight, 2);

		float[] moisture = GenerateBaseMoisture(elevation, temperature, targetWidth, targetHeight, seaLevel);
		Vector2[] wind = GenerateBaseWind(profile.WindCellCount, targetWidth, targetHeight, random);
		StitchWrapColumns(wind, targetWidth, targetHeight, 2);

		float[] distributedMoisture = DistributeMoisture3(
			moisture,
			elevation,
			temperature,
			wind,
			moistureNoise,
			targetWidth,
			targetHeight,
			seaLevel,
			ringCos,
			ringSin,
			latitudes);

		Normalize(distributedMoisture);
		StitchWrapColumns(distributedMoisture, targetWidth, targetHeight, 2);

		float[] riverLayer = new float[pixelCount];
		PlanetBiomeType[] biomes = GenerateBiomes(
			elevation,
			distributedMoisture,
			temperature,
			riverLayer,
			targetWidth,
			targetHeight,
			seaLevel,
			random,
			generateRivers: true);

		StitchWrapColumns(riverLayer, targetWidth, targetHeight, 2);
		StitchWrapColumns(biomes, targetWidth, targetHeight, 1);

		return new PlanetSurfaceData(
			elevation,
			temperature,
			distributedMoisture,
			wind,
			biomes,
			riverLayer,
			seaLevel);
	}

	private static float ResolveSeaLevelFromElevation(float[] elevation, float targetOceanCoverage)
	{
		if (elevation == null || elevation.Length == 0)
		{
			return ReferenceTectonicSeaLevel;
		}

		targetOceanCoverage = Mathf.Clamp(targetOceanCoverage, 0f, 1f);
		float[] sorted = (float[])elevation.Clone();
		Array.Sort(sorted);

		int quantileIndex = Mathf.Clamp(
			Mathf.RoundToInt((sorted.Length - 1) * targetOceanCoverage),
			0,
			sorted.Length - 1);

		float resolved = sorted[quantileIndex];
		float lowerBound = Mathf.Lerp(0f, MinSeaLevel, 0.4f);
		float upperBound = Mathf.Lerp(MaxSeaLevel, 1f, 0.3f);
		return Mathf.Clamp(resolved, lowerBound, upperBound);
	}

	private static FastNoiseLite CreateNoise(int seed)
	{
		return new FastNoiseLite
		{
			Seed = seed,
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Frequency = 1f,
			FractalType = FastNoiseLite.FractalTypeEnum.None
		};
	}

	private static Vector2I[] GeneratePlateCoordinates(int plateCount, int width, int height, Random random)
	{
		var plateCoords = new Vector2I[plateCount];
		for (int i = 0; i < plateCount; i++)
		{
			plateCoords[i] = new Vector2I(
				random.Next(0, width),
				random.Next(0, height));
		}

		return plateCoords;
	}

	private static int[] SetPlateIds(Vector2I[] plateCoords, int width, int height)
	{
		int[] plateIdMap = new int[width * height];
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int bestPlate = 0;
				int bestDistanceSquared = int.MaxValue;
				for (int plate = 0; plate < plateCoords.Length; plate++)
				{
					int dx = Mathf.Abs(plateCoords[plate].X - x);
					if (dx > width / 2)
					{
						dx = width - dx;
					}

					int dy = plateCoords[plate].Y - y;
					int distanceSquared = dx * dx + dy * dy;
					if (distanceSquared < bestDistanceSquared)
					{
						bestDistanceSquared = distanceSquared;
						bestPlate = plate;
					}
				}

				plateIdMap[y * width + x] = bestPlate;
			}
		}

		return plateIdMap;
	}

	private static PlateAttribute[] SetPlateAttributes(float percentOcean, int plateCount, Random random)
	{
		var attributes = new PlateAttribute[plateCount];
		for (int i = 0; i < plateCount; i++)
		{
			float vectorX = (float)(random.NextDouble() * 2.0 - 1.0);
			float vectorY = (float)(random.NextDouble() * 2.0 - 1.0);
			bool isOceanic = random.NextDouble() < percentOcean;
			float baseElevation = isOceanic
				? 0.34f + 0.16f * (float)random.NextDouble()
				: 0.50f + 0.18f * (float)random.NextDouble();

			attributes[i] = new PlateAttribute(vectorX, vectorY, isOceanic, baseElevation);
		}

		return attributes;
	}

	private static Dictionary<int, List<NeighborForce>> SetPlateBoundaryStress(
		int[] plateIdMap,
		PlateAttribute[] plateAttributes,
		Vector2I[] plateCoords,
		int width,
		int height,
		float mountainScale)
	{
		var neighborMap = new Dictionary<int, List<NeighborForce>>();
		var seenPair = new HashSet<long>();

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int currentId = plateIdMap[y * width + x];
				for (int i = 0; i < NeighborOffsetX.Length; i++)
				{
					int neighborX = x + NeighborOffsetX[i];
					if (neighborX < 0)
					{
						neighborX = width - 1;
					}
					else if (neighborX >= width)
					{
						neighborX = 0;
					}

					int neighborY = y + NeighborOffsetY[i];
					if (neighborY < 0 || neighborY >= height)
					{
						neighborY = y;
					}

					int neighborId = plateIdMap[neighborY * width + neighborX];
					if (neighborId == currentId)
					{
						continue;
					}

					PlateAttribute current = plateAttributes[currentId];
					PlateAttribute adjacent = plateAttributes[neighborId];

					float slopeX = plateCoords[neighborId].X - plateCoords[currentId].X;
					if (Mathf.Abs(slopeX) > width * 0.5f)
					{
						slopeX = slopeX > 0 ? slopeX - width : slopeX + width;
					}
					float slopeY = plateCoords[neighborId].Y - plateCoords[currentId].Y;

					float relMotionX = current.VectorX - adjacent.VectorX;
					float relMotionY = current.VectorY - adjacent.VectorY;

					float slopeMagnitude = Mathf.Sqrt(slopeX * slopeX + slopeY * slopeY);
					if (slopeMagnitude < 0.0001f)
					{
						slopeMagnitude = 0.0001f;
					}

					float parallelComponent = (relMotionX * slopeX + relMotionY * slopeY) / slopeMagnitude;
					float parallelProjectionX = parallelComponent * (slopeX / slopeMagnitude);
					float parallelProjectionY = parallelComponent * (slopeY / slopeMagnitude);
					float perpProjectionX = relMotionX - parallelProjectionX;
					float perpProjectionY = relMotionY - parallelProjectionY;
					float perpComponent = Mathf.Sqrt(perpProjectionX * perpProjectionX + perpProjectionY * perpProjectionY);

					char boundaryType;
					if (perpComponent > Mathf.Abs(parallelComponent))
					{
						boundaryType = 't';
					}
					else if (parallelComponent > 0f)
					{
						boundaryType = 'c';
					}
					else
					{
						boundaryType = 'd';
					}

					long pairKey = ((long)currentId << 32) | (uint)neighborId;
					if (!seenPair.Add(pairKey))
					{
						break;
					}

					if (!neighborMap.TryGetValue(currentId, out List<NeighborForce> forceList))
					{
						forceList = new List<NeighborForce>();
						neighborMap[currentId] = forceList;
					}

					forceList.Add(new NeighborForce(
						currentId,
						neighborId,
						parallelComponent * mountainScale,
						perpComponent,
						boundaryType));

					break;
				}
			}
		}

		foreach (KeyValuePair<int, List<NeighborForce>> kvp in neighborMap)
		{
			kvp.Value.Sort((a, b) => a.Neighbor.CompareTo(b.Neighbor));
		}

		return neighborMap;
	}

	private static float[] CreatePerlinElevation(FastNoiseLite noise, int width, int height, float[] ringCos, float[] ringSin, float[] latitudes)
	{
		float[] elevation = new float[width * height];
		for (int y = 0; y < height; y++)
		{
			float ny = latitudes[y];
			for (int x = 0; x < width; x++)
			{
				float nx = ringCos[x];
				float nz = ringSin[x];

				float value = Noise01(noise, nx, ny, nz)
					+ 0.5f * Noise01(noise, 2f * nx, 2f * ny, 2f * nz)
					+ 0.25f * Noise01(noise, 4f * nx, 4f * ny, 4f * nz)
					+ 0.125f * Noise01(noise, 8f * nx, 8f * ny, 8f * nz)
					+ 0.0625f * Noise01(noise, 16f * nx, 16f * ny, 16f * nz);

				value /= 1.28f;
				value = Mathf.Pow(Mathf.Max(0f, value), 2f);
				elevation[y * width + x] = value;
			}
		}

		return elevation;
	}

	private static float[] ModifyElevation3(
		float[] elevation,
		int[] plateIdMap,
		PlateAttribute[] plateAttributes,
		Vector2I[] plateCoords,
		Dictionary<int, List<NeighborForce>> neighborMap,
		FastNoiseLite detailNoise,
		int width,
		int height,
		float seaLevel,
		Random random,
		float[] ringCos,
		float[] ringSin,
		float[] latitudes)
	{
		float[] modifiedElevation = new float[elevation.Length];
		float gradientInitValue = (float)random.NextDouble();
		float gradientCoefficient = (float)random.NextDouble() * 0.1f + 0.1f;

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				int plateId = plateIdMap[index];
				PlateAttribute plate = plateAttributes[plateId];

				float totalPressure = 0f;
				float bestDistance = float.PositiveInfinity;
				char bestType = 't';

				if (neighborMap.TryGetValue(plateId, out List<NeighborForce> neighborList))
				{
					for (int i = 0; i < neighborList.Count; i++)
					{
						NeighborForce neighbor = neighborList[i];
						Vector2I neighborCoord = plateCoords[neighbor.Neighbor];

						float dx = Mathf.Abs(neighborCoord.X - x);
						if (dx > width * 0.5f)
						{
							dx = width - dx;
						}
						float dy = neighborCoord.Y - y;
						float distance = Mathf.Sqrt(dx * dx + dy * dy);

						if (distance < bestDistance)
						{
							bestDistance = distance;
							bestType = neighbor.BoundaryType;
						}

						float distanceFactor = neighbor.BoundaryType != 't'
							? 0.4f / (0.02f * distance * distance + 1f)
							: 0.2f / (0.002f * distance * distance + 1f);

						totalPressure += neighbor.DirectForce * distanceFactor;
					}
				}

				if (!float.IsFinite(bestDistance))
				{
					bestDistance = width * 0.5f;
				}

				totalPressure = Mathf.Clamp(totalPressure, -0.22f, 0.22f);
				float modifiedBaseElevationTarget = plate.BaseElevation +
					(1f / (0.01f * bestDistance * bestDistance + 1f)) * (1f - plate.BaseElevation);
				float modifiedBaseElevation = Mathf.Lerp(1f, modifiedBaseElevationTarget, 0.22f);
				float localPlateLevel = Mathf.Lerp(0.55f, plate.BaseElevation, 0.35f);

				float nx = ringCos[x];
				float ny = latitudes[y];
				float nz = ringSin[x];
				float value = 0.125f * Noise01(detailNoise, 8f * nx, 8f * ny, 8f * nz)
					+ 0.0625f * Noise01(detailNoise, 16f * nx, 16f * ny, 16f * nz)
					+ 0.03125f * Noise01(detailNoise, 32f * nx, 32f * ny, 32f * nz);

				value *= 7f;
				value *= 1f / (1f + Mathf.Pow(100f, -5f * (value - 0.8f)));

				float gradientFactor = (y / (gradientCoefficient * height)) + gradientInitValue;
				if (y >= height * (gradientCoefficient * gradientInitValue + (1f - gradientCoefficient)))
				{
					gradientFactor = -1f * (1f / (gradientCoefficient * height)) * (y - ((1f - gradientCoefficient) * height)) + 1f + gradientInitValue;
				}

				if (gradientFactor > 1f)
				{
					gradientFactor = 1f;
				}

				float currentElevation = elevation[index];
				float tectonicElevation;
				if ((currentElevation + totalPressure * 0.7f * value) * localPlateLevel >= seaLevel)
				{
					float raised = (currentElevation + totalPressure * value) * modifiedBaseElevation * gradientFactor;
					tectonicElevation = raised + (0.15f * (1f - raised)) - 0.12f;
				}
				else
				{
					tectonicElevation = (currentElevation + totalPressure * 0.7f * value) * (localPlateLevel * modifiedBaseElevation);
				}

				float blendedElevation = Mathf.Lerp(currentElevation, tectonicElevation, 0.34f);
				float microDetail = (Noise01(detailNoise, 3.2f * nx, 3.2f * ny, 3.2f * nz) - 0.5f) * 0.045f;
				modifiedElevation[index] = blendedElevation + microDetail;

				if (bestType == 'd' && plate.IsOceanic)
				{
					modifiedElevation[index] *= 0.985f;
				}
			}
		}

		return modifiedElevation;
	}

	private static float[] AverageElevation(float[] elevation, int width, int height, float seaLevel)
	{
		float[] averaged = new float[elevation.Length];
		const int radius = 3;

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				if (elevation[index] <= seaLevel)
				{
					averaged[index] = elevation[index];
					continue;
				}

				float sum = 0f;
				int count = 1;
				for (int q = -radius; q <= radius; q++)
				{
					for (int p = -radius; p <= radius; p++)
					{
						int wrapX = x + p;
						if (wrapX < 0)
						{
							wrapX += width;
						}
						if (wrapX >= width)
						{
							wrapX %= width;
						}

						int wrapY = y + q;
						if (wrapY < 0)
						{
							wrapY = 0;
						}
						if (wrapY >= height)
						{
							wrapY = height - 1;
						}

						float value = elevation[wrapY * width + wrapX];
						if (value > seaLevel)
						{
							sum += value;
							count++;
						}
					}
				}

				averaged[index] = sum / count;
			}
		}

		return averaged;
	}

	private static float[] BuildRingCos(int width)
	{
		float[] ringCos = new float[width];
		for (int x = 0; x < width; x++)
		{
			ringCos[x] = Mathf.Cos((x * Mathf.Tau) / width);
		}

		return ringCos;
	}

	private static float[] BuildRingSin(int width)
	{
		float[] ringSin = new float[width];
		for (int x = 0; x < width; x++)
		{
			ringSin[x] = Mathf.Sin((x * Mathf.Tau) / width);
		}

		return ringSin;
	}

	private static float[] BuildLatitudes(int height)
	{
		float[] latitudes = new float[height];
		for (int y = 0; y < height; y++)
		{
			latitudes[y] = 4f * y / height;
		}

		return latitudes;
	}

	private static float[] GenerateBaseTemperature(
		FastNoiseLite noise,
		float[] elevation,
		PlanetGenerationProfile profile,
		int width,
		int height,
		float[] ringCos,
		float[] ringSin,
		float[] latitudes,
		bool doElevationAdjust)
	{
		float[] baseTemperatures = new float[width * height];
		float heatSource = Mathf.Clamp((1000f - profile.HeatFactor) / 999f, 0f, 1f);
		float heatFactor = Mathf.Pow(heatSource, 1f / 3f);

		for (int y = 0; y < height; y++)
		{
			float gradient = ResolveLatitudinalGradient(y, height, heatFactor);
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				float nx = ringCos[x];
				float ny = latitudes[y];
				float nz = ringSin[x];

				float value = Noise01(noise, nx, ny, nz)
					+ 0.5f * Noise01(noise, 2f * nx, 2f * ny, 2f * nz)
					+ 0.25f * Noise01(noise, 4f * nx, 4f * ny, 4f * nz)
					+ 0.125f * Noise01(noise, 8f * nx, 8f * ny, 8f * nz)
					+ 0.0625f * Noise01(noise, 16f * nx, 16f * ny, 16f * nz);

				value /= 1.28f;
				value = Mathf.Pow(Mathf.Max(0f, value), 2f);
				float temperature = 1.15f * value * gradient;

				if (doElevationAdjust)
				{
					float h = elevation[index];
					if (h > 0.9f)
					{
						temperature -= 0.4f * h;
					}
					else if (h > 0.8f)
					{
						temperature -= 0.2f * h;
					}
					else if (h > 0.7f)
					{
						temperature -= 0.12f * h;
					}
					else if (h > 0.6f)
					{
						temperature -= 0.08f * h;
					}
					else if (h > 0.5f)
					{
						temperature -= 0.05f * h;
					}
					else if (h > 0.4f)
					{
						temperature -= 0.02f * h;
					}
				}

				baseTemperatures[index] = Mathf.Clamp(temperature, 0f, 1f);
			}
		}

		return baseTemperatures;
	}

	private static float ResolveLatitudinalGradient(int y, int height, float heatFactor)
	{
		float latitude01 = height > 1 ? y / (float)(height - 1) : 0.5f;
		float latitudeAbs = Mathf.Abs(latitude01 * 2f - 1f);
		float exponent = Mathf.Lerp(2.2f, 0.9f, heatFactor);
		float polarFloor = Mathf.Lerp(0.02f, 0.24f, heatFactor);
		float equatorHeat = Mathf.Pow(Mathf.Clamp(1f - latitudeAbs, 0f, 1f), exponent);
		return Mathf.Clamp(polarFloor + (1f - polarFloor) * equatorHeat, 0f, 1f);
	}

	private static float[] GenerateBaseMoisture(float[] elevation, float[] temperature, int width, int height, float seaLevel)
	{
		float[] moisture = new float[width * height];
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				moisture[index] = elevation[index] < seaLevel ? temperature[index] : 0f;
			}
		}

		return moisture;
	}

	private static Vector2[] GenerateBaseWind(int windCellCount, int width, int height, Random random)
	{
		int pixelCount = width * height;
		var wind = new Vector2[pixelCount];
		var windCount = new int[pixelCount];
		int originCount = Mathf.Clamp(windCellCount, 1, 64);
		float maxReach = Mathf.Sqrt(width * width + height * height) * 0.25f;

		for (int i = 0; i < originCount; i++)
		{
			int originX = random.Next(0, width);
			int originY = random.Next(0, height);
			float intensity = random.Next(1, 50);
			int reach = Mathf.Max(1, Mathf.RoundToInt((float)random.NextDouble() * maxReach));
			bool clockwise = random.Next(0, 2) == 1;

			for (int r = 1; r <= reach; r++)
			{
				for (int p = -r; p <= r; p++)
				{
					for (int q = -r; q <= r; q++)
					{
						if (Mathf.Abs(p) != r && Mathf.Abs(q) != r)
						{
							continue;
						}

						int wrappedX = WrapX(originX + p, width);
						int wrappedY = ClampY(originY + q, height);
						int index = wrappedY * width + wrappedX;

						Vector2 contribution = clockwise
							? new Vector2(intensity * (-q) / r, intensity * p / r)
							: new Vector2(intensity * q / r, intensity * (-p) / r);

						wind[index] += contribution;
						windCount[index]++;
					}
				}
			}
		}

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				if (windCount[index] <= 0)
				{
					float randomDrift = (float)random.Next(-25, 25) * 0.15f;
					wind[index] = new Vector2(randomDrift, randomDrift);
				}
				else
				{
					wind[index] /= windCount[index];
				}
			}
		}

		return wind;
	}

	private static float[] DistributeMoisture3(
		float[] baseMoisture,
		float[] elevation,
		float[] temperature,
		Vector2[] wind,
		FastNoiseLite moistureNoise,
		int width,
		int height,
		float seaLevel,
		float[] ringCos,
		float[] ringSin,
		float[] latitudes)
	{
		float[] distributed = new float[width * height];
		int stepLimit = Mathf.Clamp((width + height) / 30, 14, 80);

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				float nx = ringCos[x];
				float ny = latitudes[y];
				float nz = ringSin[x];

				float value = Noise01(moistureNoise, nx, ny, nz)
					+ 0.5f * Noise01(moistureNoise, 2f * nx, 2f * ny, 2f * nz)
					+ 0.25f * Noise01(moistureNoise, 4f * nx, 4f * ny, 4f * nz)
					+ 0.125f * Noise01(moistureNoise, 8f * nx, 8f * ny, 8f * nz)
					+ 0.0625f * Noise01(moistureNoise, 16f * nx, 16f * ny, 16f * nz);

				value /= 1.28f;
				value = Mathf.Pow(Mathf.Max(0f, value), 2f);

				if (elevation[index] >= seaLevel)
				{
					distributed[index] += 0.34f * value;
				}

				if (elevation[index] >= seaLevel)
				{
					continue;
				}


				Vector2 currentWind = wind[index];
				float windSpeed = currentWind.Length();
				if (windSpeed <= 0.0001f)
				{
					continue;
				}

				float windX = currentWind.X;
				float windY = currentWind.Y;
				float moistureRemaining = baseMoisture[index] * 8.8f;
				float unitX = windX / windSpeed;
				float unitY = windY / windSpeed;

				int traceX = x + Mathf.RoundToInt(unitX);
				int traceY = y + Mathf.RoundToInt(unitY);

				int steps = 0;
				while (moistureRemaining > 1f && steps < stepLimit)
				{
					traceX = WrapX(traceX, width);
					if (traceY >= height || traceY < 0)
					{
						break;
					}

					int traceIndex = traceY * width + traceX;
					float elSlope = 0.5f * elevation[traceIndex] * elevation[traceIndex]
						- Mathf.Sqrt((temperature[traceIndex] + 0.6f) / 1.9f)
						- 0.005f * windSpeed
						+ 1.25f;

					distributed[traceIndex] += moistureRemaining * elSlope;
					if (elevation[traceIndex] < seaLevel)
					{
						distributed[traceIndex] = 0.0001f;
					}

					moistureRemaining -= 0.1f * Mathf.Max(0.01f, distributed[traceIndex]);

					traceX += Mathf.RoundToInt(unitX);
					traceY += Mathf.RoundToInt(unitY);
					traceX = WrapX(traceX, width);
					if (traceY >= height || traceY < 0)
					{
						break;
					}

					int nextIndex = traceY * width + traceX;
					float resultX = wind[nextIndex].X + windX;
					float resultY = wind[nextIndex].Y + windY;
					float resultMag = Mathf.Sqrt(resultX * resultX + resultY * resultY);
					if (resultMag <= 0.0001f)
					{
						break;
					}

					unitX = resultX / resultMag;
					unitY = resultY / resultMag;
					steps++;
				}
			}
		}

		distributed = AverageArray(distributed, elevation, width, height, 5, elevationOnly: true, seaLevel);
		distributed = AverageArray(distributed, elevation, width, height, 2, elevationOnly: false, seaLevel);
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				if (elevation[index] < seaLevel)
				{
					distributed[index] = 0f;
				}
				else
				{
					float thermalMoisture = Mathf.Clamp(temperature[index], 0f, 1f);
					distributed[index] = Mathf.Clamp(distributed[index] + 0.05f + thermalMoisture * 0.07f, 0f, 1f);
				}
			}
		}

		return distributed;
	}

	private static float[] AverageArray(
		float[] input,
		float[] elevation,
		int width,
		int height,
		int radius,
		bool elevationOnly,
		float seaLevel)
	{
		float[] averaged = new float[input.Length];
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				float sum = 0f;
				int count = 1;

				for (int q = -radius; q <= radius; q++)
				{
					for (int p = -radius; p <= radius; p++)
					{
						int wrappedX = WrapX(x + p, width);
						int wrappedY = ClampY(y + q, height);
						int sampleIndex = wrappedY * width + wrappedX;

						if (elevationOnly)
						{
							if (elevation[sampleIndex] > seaLevel)
							{
								sum += input[sampleIndex];
								count++;
							}
						}
						else
						{
							sum += input[sampleIndex];
							count++;
						}
					}
				}

				averaged[y * width + x] = sum / Mathf.Max(1, count);
			}
		}

		return averaged;
	}

	private static PlanetBiomeType[] GenerateBiomes(
		float[] elevation,
		float[] moisture,
		float[] temperature,
		float[] riverLayer,
		int width,
		int height,
		float seaLevel,
		Random random,
		bool generateRivers)
	{
		var biomes = new PlanetBiomeType[width * height];
		float[] smoothedTemperature = AverageArray(temperature, elevation, width, height, 1, elevationOnly: false, seaLevel);
		float[] smoothedMoisture = AverageArray(moisture, elevation, width, height, 2, elevationOnly: true, seaLevel);

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				float latitude01 = height > 1 ? y / (float)(height - 1) : 0.5f;
				float latitudeAbs = Mathf.Abs(latitude01 * 2f - 1f);

				float elevationValue = elevation[index];
				float temperatureValue = Mathf.Clamp(smoothedTemperature[index], 0f, 1f);
				float moistureValue = Mathf.Clamp(smoothedMoisture[index], 0f, 1f);

				if (generateRivers)
				{
					float reliefForRiver = Mathf.Clamp((elevationValue - seaLevel) / Mathf.Max(0.0001f, 1f - seaLevel), 0f, 1f);
					float riverSeedChance = 0.0012f + reliefForRiver * 0.0014f;
					if (elevationValue > seaLevel + 0.06f
						&& moistureValue > 0.2f
						&& temperatureValue > 0.08f
						&& random.NextDouble() < riverSeedChance)
					{
						float seedFlow = 0.22f + 0.58f * reliefForRiver + 0.2f * moistureValue;
						riverLayer[index] = Mathf.Max(riverLayer[index], Mathf.Clamp(seedFlow, 0.15f, 0.95f));
						DefineRivers3(riverLayer, elevation, x, y, width, height, seaLevel, random);
					}
				}

				if (elevationValue < 0.565f * seaLevel)
				{
					biomes[index] = PlanetBiomeType.Ocean;
					continue;
				}

				if (elevationValue < seaLevel)
				{
					biomes[index] = PlanetBiomeType.ShallowOcean;
					continue;
				}

				if (elevationValue < seaLevel + 0.024f)
				{
					biomes[index] = PlanetBiomeType.Coastland;
					continue;
				}

				float relief = Mathf.Clamp((elevationValue - seaLevel) / Mathf.Max(0.0001f, 1f - seaLevel), 0f, 1f);
				float adjustedTemperature = Mathf.Clamp(
					temperatureValue
					- relief * 0.22f
					- Mathf.Max(0f, latitudeAbs - 0.72f) * 0.22f,
					0f,
					1f);
				float adjustedMoisture = Mathf.Clamp(
					moistureValue
					+ (1f - relief) * 0.05f
					- relief * 0.06f,
					0f,
					1f);

				float polarIceScore = Mathf.Clamp(
					Mathf.Pow(latitudeAbs, 1.8f) * 0.92f
					+ (0.38f - adjustedTemperature) * 1.25f
					+ (0.18f - relief) * 0.18f,
					0f,
					1f);
				if (polarIceScore > 0.58f && relief < 0.82f)
				{
					biomes[index] = PlanetBiomeType.Ice;
					continue;
				}

				if (relief > 0.69f)
				{
					float snowLineTemperature = Mathf.Lerp(0.18f, 0.34f, 1f - latitudeAbs);
					biomes[index] = adjustedTemperature < snowLineTemperature || polarIceScore > 0.46f
						? PlanetBiomeType.SnowyMountain
						: PlanetBiomeType.RockyMountain;
					continue;
				}

				biomes[index] = SelectClimateBiome(adjustedTemperature, adjustedMoisture, relief, latitudeAbs);
			}
		}

		biomes = SmoothLandBiomeEdges(biomes, elevation, width, height, seaLevel, 3);
		return biomes;
	}

	private static PlanetBiomeType SelectClimateBiome(float temperature, float moisture, float relief, float latitudeAbs)
	{
		PlanetBiomeType selected = PlanetBiomeType.Grassland;
		float bestScore = float.PositiveInfinity;

		for (int i = 0; i < LandBiomeAnchors.Length; i++)
		{
			BiomeAnchor anchor = LandBiomeAnchors[i];
			float dt = temperature - anchor.Temperature;
			float dm = moisture - anchor.Moisture;
			float dr = relief - anchor.ReliefPreference;
			float score = dt * dt * 1.6f + dm * dm * 1.25f + dr * dr * 0.16f;
			score += ComputeBiomePenalty(anchor.Biome, temperature, moisture, latitudeAbs);

			if (score < bestScore)
			{
				bestScore = score;
				selected = anchor.Biome;
			}
		}

		return selected;
	}

	private static float ComputeBiomePenalty(PlanetBiomeType biome, float temperature, float moisture, float latitudeAbs)
	{
		float penalty = 0f;
		switch (biome)
		{
			case PlanetBiomeType.TropicalDesert:
			case PlanetBiomeType.Savannah:
			case PlanetBiomeType.Shrubland:
			case PlanetBiomeType.TropicalSeasonalForest:
			case PlanetBiomeType.TropicalRainForest:
				penalty += Mathf.Clamp(0.56f - temperature, 0f, 1f) * 1.4f;
				penalty += Mathf.Clamp(latitudeAbs - 0.62f, 0f, 1f) * 1.0f;
				break;

			case PlanetBiomeType.TemperateDesert:
			case PlanetBiomeType.Steppe:
			case PlanetBiomeType.Grassland:
			case PlanetBiomeType.Chaparral:
			case PlanetBiomeType.TemperateSeasonalForest:
			case PlanetBiomeType.TemperateRainForest:
				penalty += Mathf.Clamp(Mathf.Abs(temperature - 0.48f) - 0.28f, 0f, 1f) * 0.95f;
				break;

			case PlanetBiomeType.BorealForest:
			case PlanetBiomeType.Taiga:
			case PlanetBiomeType.Tundra:
				penalty += Mathf.Clamp(temperature - 0.44f, 0f, 1f) * 1.4f;
				penalty += Mathf.Clamp(0.25f - latitudeAbs, 0f, 1f) * 0.85f;
				break;
		}

		if (biome == PlanetBiomeType.TropicalDesert || biome == PlanetBiomeType.TemperateDesert)
		{
			penalty += Mathf.Clamp(moisture - 0.38f, 0f, 1f) * 1.2f;
		}
		else if (biome == PlanetBiomeType.TropicalRainForest || biome == PlanetBiomeType.TemperateRainForest)
		{
			penalty += Mathf.Clamp(0.44f - moisture, 0f, 1f) * 1.3f;
		}

		return penalty;
	}

	private static PlanetBiomeType[] SmoothLandBiomeEdges(
		PlanetBiomeType[] input,
		float[] elevation,
		int width,
		int height,
		float seaLevel,
		int passes)
	{
		PlanetBiomeType[] source = input;
		for (int pass = 0; pass < passes; pass++)
		{
			PlanetBiomeType[] smoothed = (PlanetBiomeType[])source.Clone();
			int[] counts = new int[Enum.GetValues<PlanetBiomeType>().Length];

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int index = y * width + x;
					if (elevation[index] < seaLevel)
					{
						continue;
					}

					PlanetBiomeType current = source[index];
					if (!IsBlendableLandBiome(current))
					{
						continue;
					}

					Array.Clear(counts, 0, counts.Length);
					int oceanNeighbors = 0;
					counts[(int)current] += 2;

					for (int i = 0; i < NeighborOffsetX.Length; i++)
					{
						int sampleX = WrapX(x + NeighborOffsetX[i], width);
						int sampleY = y + NeighborOffsetY[i];
						if (sampleY < 0 || sampleY >= height)
						{
							continue;
						}

						int sampleIndex = sampleY * width + sampleX;
						if (elevation[sampleIndex] < seaLevel)
						{
							oceanNeighbors++;
							continue;
						}

						PlanetBiomeType neighbor = source[sampleIndex];
						if (neighbor == PlanetBiomeType.Ocean || neighbor == PlanetBiomeType.ShallowOcean)
						{
							oceanNeighbors++;
							continue;
						}

						if (!IsBlendableLandBiome(neighbor))
						{
							continue;
						}

						counts[(int)neighbor]++;
					}

					if (oceanNeighbors >= 4)
					{
						smoothed[index] = PlanetBiomeType.Coastland;
						continue;
					}

					PlanetBiomeType dominant = current;
					int dominantCount = counts[(int)current];
					for (int i = 0; i < counts.Length; i++)
					{
						if (counts[i] > dominantCount)
						{
							dominantCount = counts[i];
							dominant = (PlanetBiomeType)i;
						}
					}

					if (dominant != current && dominantCount >= 4)
					{
						smoothed[index] = dominant;
					}
				}
			}

			source = smoothed;
		}

		return source;
	}

	private static bool IsBlendableLandBiome(PlanetBiomeType biome)
	{
		return biome != PlanetBiomeType.Ocean
			&& biome != PlanetBiomeType.ShallowOcean
			&& biome != PlanetBiomeType.Coastland
			&& biome != PlanetBiomeType.RockyMountain
			&& biome != PlanetBiomeType.SnowyMountain
			&& biome != PlanetBiomeType.Ice;
	}

	private static void DefineRivers3(float[] riverLayer, float[] elevation, int startX, int startY, int width, int height, float seaLevel, Random random)
	{
		int currentX = startX;
		int currentY = startY;
		int startIndex = currentY * width + currentX;
		float flow = Mathf.Clamp(riverLayer[startIndex], 0.08f, 1f);
		if (flow <= 0f)
		{
			return;
		}

		int previousDx = 0;
		int previousDy = 0;
		int steps = 0;
		int stepLimit = width + height;

		while (steps < stepLimit)
		{
			int currentIndex = currentY * width + currentX;
			float currentElevation = elevation[currentIndex];
			if (currentElevation < seaLevel)
			{
				break;
			}

			float bestScore = float.NegativeInfinity;
			int bestX = currentX;
			int bestY = currentY;
			int bestDx = 0;
			int bestDy = 0;
			bool found = false;

			for (int i = 0; i < NeighborOffsetX.Length; i++)
			{
				int dx = NeighborOffsetX[i];
				int dy = NeighborOffsetY[i];
				int candidateY = currentY + dy;
				if (candidateY < 0 || candidateY >= height)
				{
					continue;
				}

				int candidateX = WrapX(currentX + dx, width);
				int candidateIndex = candidateY * width + candidateX;
				float drop = currentElevation - elevation[candidateIndex];
				if (drop < -0.0015f)
				{
					continue;
				}

				float alignment = 0f;
				if (previousDx != 0 || previousDy != 0)
				{
					float denom = Mathf.Sqrt((previousDx * previousDx + previousDy * previousDy) * (dx * dx + dy * dy));
					if (denom > 0.0001f)
					{
						alignment = (previousDx * dx + previousDy * dy) / denom;
					}
				}

				float mergeBonus = riverLayer[candidateIndex] > 0f ? 0.14f : 0f;
				float meander = ((float)random.NextDouble() - 0.5f) * 0.18f;
				float diagonalPenalty = dx != 0 && dy != 0 ? 0.015f : 0f;
				float score = drop * 7.8f + alignment * 0.22f + mergeBonus + meander - diagonalPenalty;

				if (score > bestScore)
				{
					bestScore = score;
					bestX = candidateX;
					bestY = candidateY;
					bestDx = dx;
					bestDy = dy;
					found = true;
				}
			}

			if (!found || (bestX == currentX && bestY == currentY))
			{
				break;
			}

			int nextIndex = bestY * width + bestX;
			float carriedFlow = Mathf.Clamp(flow * 0.62f, 0.02f, 0.95f);
			riverLayer[currentIndex] = Mathf.Max(riverLayer[currentIndex], flow);
			riverLayer[nextIndex] = Mathf.Clamp(Mathf.Max(riverLayer[nextIndex], carriedFlow), 0f, 1.35f);

			flow = Mathf.Clamp(Mathf.Lerp(flow, riverLayer[nextIndex], 0.28f) * 0.992f, 0f, 1.2f);
			currentX = bestX;
			currentY = bestY;
			previousDx = bestDx;
			previousDy = bestDy;

			if (flow < 0.015f)
			{
				break;
			}

			steps++;
		}
	}

	private static int WrapX(int x, int width)
	{
		if (width <= 0)
		{
			return 0;
		}

		return ((x % width) + width) % width;
	}

	private static int ClampY(int y, int height)
	{
		if (y < 0)
		{
			return 0;
		}

		if (y >= height)
		{
			return height - 1;
		}

		return y;
	}

	private static void StitchWrapColumns(float[] values, int width, int height, int columns)
	{
		if (values == null || values.Length != width * height || width <= 1 || columns <= 0)
		{
			return;
		}

		int blendColumns = Mathf.Min(columns, width / 2);
		for (int y = 0; y < height; y++)
		{
			for (int c = 0; c < blendColumns; c++)
			{
				int leftX = c;
				int rightX = width - 1 - c;
				int leftIndex = y * width + leftX;
				int rightIndex = y * width + rightX;
				float blended = (values[leftIndex] + values[rightIndex]) * 0.5f;
				values[leftIndex] = blended;
				values[rightIndex] = blended;
			}
		}
	}

	private static void StitchWrapColumns(Vector2[] values, int width, int height, int columns)
	{
		if (values == null || values.Length != width * height || width <= 1 || columns <= 0)
		{
			return;
		}

		int blendColumns = Mathf.Min(columns, width / 2);
		for (int y = 0; y < height; y++)
		{
			for (int c = 0; c < blendColumns; c++)
			{
				int leftIndex = y * width + c;
				int rightIndex = y * width + (width - 1 - c);
				Vector2 blended = (values[leftIndex] + values[rightIndex]) * 0.5f;
				values[leftIndex] = blended;
				values[rightIndex] = blended;
			}
		}
	}

	private static void StitchWrapColumns(PlanetBiomeType[] values, int width, int height, int columns)
	{
		if (values == null || values.Length != width * height || width <= 1 || columns <= 0)
		{
			return;
		}

		int blendColumns = Mathf.Min(columns, width / 2);
		for (int y = 0; y < height; y++)
		{
			for (int c = 0; c < blendColumns; c++)
			{
				int leftIndex = y * width + c;
				int rightIndex = y * width + (width - 1 - c);
				values[rightIndex] = values[leftIndex];
			}
		}
	}

	private static float Noise01(FastNoiseLite noise, float x, float y, float z)
	{
		return (noise.GetNoise3D(x, y, z) + 1f) * 0.5f;
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
			if (values[i] < min)
			{
				min = values[i];
			}

			if (values[i] > max)
			{
				max = values[i];
			}
		}

		float range = max - min;
		if (range < 0.000001f)
		{
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = 0f;
			}
			return;
		}

		for (int i = 0; i < values.Length; i++)
		{
			values[i] = (values[i] - min) / range;
		}
	}
}
