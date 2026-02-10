using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Genesis;

public static class PlanetTerrainGenerator
{
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

	private readonly struct StressCell
	{
		public StressCell(
			bool isBorder,
			float direct,
			float shear,
			char type,
			int id0,
			int id1,
			int neighborX,
			int neighborY)
		{
			IsBorder = isBorder;
			Direct = direct;
			Shear = shear;
			Type = type;
			Id0 = id0;
			Id1 = id1;
			NeighborX = neighborX;
			NeighborY = neighborY;
		}

		public bool IsBorder { get; }
		public float Direct { get; }
		public float Shear { get; }
		public char Type { get; }
		public int Id0 { get; }
		public int Id1 { get; }
		public int NeighborX { get; }
		public int NeighborY { get; }
	}

	private readonly struct EdgePoint
	{
		public EdgePoint(int x, int y, int id, int neighborId, char type, bool isOceanic)
		{
			X = x;
			Y = y;
			Id = id;
			NeighborId = neighborId;
			Type = type;
			IsOceanic = isOceanic;
		}

		public int X { get; }
		public int Y { get; }
		public int Id { get; }
		public int NeighborId { get; }
		public char Type { get; }
		public bool IsOceanic { get; }
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
		ulong totalStartMs = Time.GetTicksMsec();
		ulong stageStartMs = totalStartMs;

		if (planetData == null)
		{
			return null;
		}

		int targetWidth = Mathf.Max(64, width);
		int targetHeight = Mathf.Max(32, height);
		int pixelCount = targetWidth * targetHeight;

		int plateCount = Mathf.Clamp(profile.TectonicPlateCount, 2, 128);
		float plateOceanRatio = 0.5f;

		var random = new Random(seed);
		Vector2I[] plateCoords = GeneratePlateCoordinates(plateCount, targetWidth, targetHeight, random);
		int[] plateIdMap = SetPlateIds(plateCoords, targetWidth, targetHeight);
		PlateAttribute[] plateAttributes = SetPlateAttributes(plateOceanRatio, plateCount, random);
		StressCell[] stressMap = SetPlateBoundaryStress(
			plateIdMap,
			plateAttributes,
			plateCoords,
			targetWidth,
			targetHeight);
		List<NeighborForce> neighborForces = FindPlateNeighbors(stressMap, targetWidth, targetHeight);
		LogStageTiming("Heightmap/Plates+Stress", stageStartMs);
		stageStartMs = Time.GetTicksMsec();

		float[] ringCos = BuildRingCos(targetWidth);
		float[] ringSin = BuildRingSin(targetWidth);
		float[] latitudes = BuildLatitudes(targetHeight);

		FastNoiseLite baseNoise = CreateNoise(seed + 17);
		float[] elevation = CreatePerlinElevation(baseNoise, targetWidth, targetHeight, ringCos, ringSin, latitudes);
		LogStageTiming("Heightmap/BaseElevation", stageStartMs);
		stageStartMs = Time.GetTicksMsec();

		float tectonicSeaLevel = Mathf.Clamp(planetData.OceanCoverage, 0f, 1f);
		FastNoiseLite detailNoise = CreateNoise(seed + 113);
		float[] modified = ModifyElevation3(
			elevation,
			stressMap,
			plateAttributes,
			neighborForces,
			detailNoise,
			targetWidth,
			targetHeight,
			tectonicSeaLevel,
			random,
			ringCos,
			ringSin,
			latitudes);
		LogStageTiming("Heightmap/ModifyElevation3", stageStartMs);
		stageStartMs = Time.GetTicksMsec();

		int smoothingPasses = Mathf.Clamp(profile.ErosionIterations, 0, 16);
		for (int i = 0; i < smoothingPasses; i++)
		{
			modified = AverageElevation(modified, targetWidth, targetHeight, tectonicSeaLevel);
		}
		LogStageTiming("Heightmap/Smoothing", stageStartMs);
		stageStartMs = Time.GetTicksMsec();

		Normalize(modified);
		LogStageTiming("Heightmap/Stitch+Normalize", stageStartMs);
		LogStageTiming("Heightmap/TOTAL", totalStartMs);
		if (modified.Length != pixelCount)
		{
			return new float[pixelCount];
		}

		return modified;
	}

	public static PlanetSurfaceData GenerateSurfaceData(PlanetData planetData, PlanetGenerationProfile profile, int width, int height, int seed)
	{
		ulong totalStartMs = Time.GetTicksMsec();
		ulong stageStartMs = totalStartMs;

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
		LogStageTiming("Surface/ElevationReady", stageStartMs);
		stageStartMs = Time.GetTicksMsec();

		float seaLevel = Mathf.Clamp(planetData.OceanCoverage, 0f, 1f);

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

		LogStageTiming("Surface/Temperature", stageStartMs);
		stageStartMs = Time.GetTicksMsec();

		float[] moisture = GenerateBaseMoisture(elevation, temperature, targetWidth, targetHeight, seaLevel);
		Vector2[] wind = GenerateBaseWind(profile.WindCellCount, targetWidth, targetHeight, random);
		LogStageTiming("Surface/BaseMoisture+Wind", stageStartMs);
		stageStartMs = Time.GetTicksMsec();

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

		LogStageTiming("Surface/DistributeMoisture", stageStartMs);
		stageStartMs = Time.GetTicksMsec();

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
		LogStageTiming("Surface/Biomes+Rivers", stageStartMs);
		stageStartMs = Time.GetTicksMsec();

		LogStageTiming("Surface/StitchLayers", stageStartMs);
		LogStageTiming("Surface/TOTAL", totalStartMs);

		return new PlanetSurfaceData(
			elevation,
			temperature,
			distributedMoisture,
			wind,
			biomes,
			riverLayer,
			seaLevel);
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
				? 0.55f * (float)random.NextDouble()
				: 0.55f * (float)random.NextDouble() + 0.45f;

			attributes[i] = new PlateAttribute(vectorX, vectorY, isOceanic, baseElevation);
		}

		return attributes;
	}

	private static StressCell[] SetPlateBoundaryStress(
		int[] plateIdMap,
		PlateAttribute[] plateAttributes,
		Vector2I[] plateCoords,
		int width,
		int height)
	{
		var stressArray = new StressCell[width * height];
		var orderedPlates = BuildOrderedPlateCoords(plateIdMap, plateCoords, width, height);

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				int currentId = plateIdMap[index];
				bool isBorder = false;
				int neighborX = -1;
				int neighborY = -1;
				int neighborId = -1;
				for (int i = 0; i < NeighborOffsetX.Length && !isBorder; i++)
				{
					int sampleX = WrapX(x + NeighborOffsetX[i], width);
					int sampleY = ClampY(y + NeighborOffsetY[i], height);
					int sampleId = plateIdMap[sampleY * width + sampleX];
					if (sampleId != currentId)
					{
						isBorder = true;
						neighborX = sampleX;
						neighborY = sampleY;
						neighborId = sampleId;
					}
				}

				if (!isBorder)
				{
					stressArray[index] = new StressCell(false, 0f, 0f, 'n', currentId, -1, -1, -1);
					continue;
				}

				PlateAttribute current = plateAttributes[currentId];
				PlateAttribute adjacent = plateAttributes[neighborId];

				Vector2I currentCoord = orderedPlates[currentId];
				Vector2I neighborCoord = orderedPlates[neighborId];
				float slopeY = neighborCoord.Y - currentCoord.Y;
				float slopeX = neighborCoord.X - currentCoord.X;

				float relMotionX = current.VectorX - adjacent.VectorX;
				float relMotionY = current.VectorY - adjacent.VectorY;

				float plateMagnitude = Mathf.Sqrt(slopeX * slopeX + slopeY * slopeY);
				if (plateMagnitude < 0.0001f)
				{
					plateMagnitude = 0.0001f;
				}

				float parallel = (relMotionX * slopeX + relMotionY * slopeY) / plateMagnitude;
				float parallelProjectionX = parallel * (slopeX / plateMagnitude);
				float parallelProjectionY = parallel * (slopeY / plateMagnitude);
				float perpProjectionX = relMotionX - parallelProjectionX;
				float perpProjectionY = relMotionY - parallelProjectionY;
				float perp = Mathf.Sqrt(perpProjectionX * perpProjectionX + perpProjectionY * perpProjectionY);

				char type;
				if (perp > Mathf.Abs(parallel))
				{
					type = 't';
				}
				else if (parallel > 0f)
				{
					type = 'c';
				}
				else
				{
					type = 'd';
				}

				stressArray[index] = new StressCell(
					true,
					parallel,
					perp,
					type,
					currentId,
					neighborId,
					neighborX,
					neighborY);
			}
		}

		return stressArray;
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

	private static Vector2I[] BuildOrderedPlateCoords(int[] plateIdMap, Vector2I[] plateCoords, int width, int height)
	{
		var ordered = new Vector2I[plateCoords.Length];
		var used = new bool[plateCoords.Length];

		for (int i = 0; i < plateCoords.Length; i++)
		{
			int px = Mathf.Clamp(plateCoords[i].X, 0, width - 1);
			int py = Mathf.Clamp(plateCoords[i].Y, 0, height - 1);
			int id = plateIdMap[py * width + px];
			if (id >= 0 && id < ordered.Length && !used[id])
			{
				ordered[id] = new Vector2I(px, py);
				used[id] = true;
			}
		}

		for (int id = 0; id < ordered.Length; id++)
		{
			if (used[id])
			{
				continue;
			}

			bool found = false;
			for (int y = 0; y < height && !found; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (plateIdMap[y * width + x] == id)
					{
						ordered[id] = new Vector2I(x, y);
						used[id] = true;
						found = true;
						break;
					}
				}
			}

			if (!found)
			{
				ordered[id] = Vector2I.Zero;
			}
		}

		return ordered;
	}

	private static List<NeighborForce> FindPlateNeighbors(StressCell[] stressMap, int width, int height)
	{
		var neighbors = new List<NeighborForce>();
		var seen = new HashSet<long>();

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				StressCell cell = stressMap[y * width + x];
				if (!cell.IsBorder || cell.Id1 < 0)
				{
					continue;
				}

				long key = ((long)cell.Id0 << 32) | (uint)cell.Id1;
				if (!seen.Add(key))
				{
					continue;
				}

				neighbors.Add(new NeighborForce(cell.Id0, cell.Id1, cell.Direct, cell.Shear, cell.Type));
			}
		}

		neighbors.Sort((a, b) => a.Id != b.Id ? a.Id.CompareTo(b.Id) : a.Neighbor.CompareTo(b.Neighbor));
		return neighbors;
	}

	private static float[] ModifyElevation3(
		float[] elevation,
		StressCell[] stressMap,
		PlateAttribute[] plateAttributes,
		List<NeighborForce> neighborForces,
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

		var edgeOnly = new List<EdgePoint>();
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				StressCell cell = stressMap[y * width + x];
				if (!cell.IsBorder || cell.Id1 < 0)
				{
					continue;
				}

				edgeOnly.Add(new EdgePoint(x, y, cell.Id0, cell.Id1, cell.Type, plateAttributes[cell.Id0].IsOceanic));
			}
		}

		edgeOnly.Sort((a, b) =>
		{
			int cmp = a.Id.CompareTo(b.Id);
			if (cmp != 0)
			{
				return cmp;
			}

			return a.NeighborId.CompareTo(b.NeighborId);
		});

		var edgeById = new Dictionary<int, List<EdgePoint>>();
		for (int i = 0; i < edgeOnly.Count; i++)
		{
			if (!edgeById.TryGetValue(edgeOnly[i].Id, out List<EdgePoint> points))
			{
				points = new List<EdgePoint>();
				edgeById[edgeOnly[i].Id] = points;
			}

			points.Add(edgeOnly[i]);
		}

		var neighborById = new Dictionary<int, List<NeighborForce>>();
		for (int i = 0; i < neighborForces.Count; i++)
		{
			NeighborForce force = neighborForces[i];
			if (!neighborById.TryGetValue(force.Id, out List<NeighborForce> forceList))
			{
				forceList = new List<NeighborForce>();
				neighborById[force.Id] = forceList;
			}

			forceList.Add(force);
		}

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				StressCell currentStress = stressMap[index];
				int currentId = currentStress.Id0;
				if (currentId < 0 || currentId >= plateAttributes.Length)
				{
					currentId = 0;
				}

				if (!neighborById.TryGetValue(currentId, out List<NeighborForce> neighborList))
				{
					neighborList = new List<NeighborForce>();
				}

				var pressureList = new List<(float direct, float shear, float distance, char type)>();
				for (int a = 0; a < neighborList.Count; a++)
				{
					float bestDistance = float.PositiveInfinity;
					if (!edgeById.TryGetValue(neighborList[a].Neighbor, out List<EdgePoint> candidateEdges))
					{
						continue;
					}

					for (int n = 0; n < candidateEdges.Count; n++)
					{
						float dx = Mathf.Abs(candidateEdges[n].X - x);
						if (dx > width * 0.5f)
						{
							dx = width - dx;
						}

						float dy = candidateEdges[n].Y - y;
						float distance = Mathf.Sqrt(dx * dx + dy * dy);
						if (distance < bestDistance)
						{
							bestDistance = distance;
						}
					}

					if (float.IsFinite(bestDistance))
					{
						pressureList.Add((neighborList[a].DirectForce, neighborList[a].ShearForce, bestDistance, neighborList[a].BoundaryType));
					}
				}

				float totalPressure = 0f;
				float bestDistanceForBase = float.PositiveInfinity;
				char bestType = 't';
				for (int i = 0; i < pressureList.Count; i++)
				{
					float distanceFactor = pressureList[i].type != 't'
						? 0.4f / (0.02f * pressureList[i].distance * pressureList[i].distance + 1f)
						: 0.2f / (0.002f * pressureList[i].distance * pressureList[i].distance + 1f);

					totalPressure += pressureList[i].direct * distanceFactor;
					if (pressureList[i].distance < bestDistanceForBase)
					{
						bestDistanceForBase = pressureList[i].distance;
						bestType = pressureList[i].type;
					}
				}

				PlateAttribute plate = plateAttributes[currentId];
				float modifiedBaseElevation = plate.BaseElevation
					+ (1f / (0.01f * bestDistanceForBase * bestDistanceForBase + 1f)) * (1f - plate.BaseElevation);

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
				if ((currentElevation + totalPressure * 0.7f * value) * plate.BaseElevation >= seaLevel)
				{
					modifiedElevation[index] = (currentElevation + totalPressure * value) * modifiedBaseElevation * gradientFactor;
					modifiedElevation[index] = modifiedElevation[index] + 0.15f * (1f - modifiedElevation[index]) - 0.12f;
				}
				else
				{
					modifiedElevation[index] = (currentElevation + totalPressure * 0.7f * value) * (plate.BaseElevation * modifiedBaseElevation);
				}

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
						int wrapX = WrapX(x + p, width);
						int wrapY = ClampY(y + q, height);
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
		float rawHeatFactor = Mathf.Clamp((1000f - profile.HeatFactor) / 1000f, 0.001f, 0.999f);
		float heatFactor = Mathf.Pow(rawHeatFactor, 1f / 3f);

		for (int y = 0; y < height; y++)
		{
			float gradientPosition = height > 1 ? y / (float)(height - 1) : 0f;
			float gradient;

			if (gradientPosition <= 0.5f)
			{
				float topT = gradientPosition / 0.5f;
				if (topT <= heatFactor)
				{
					gradient = topT / heatFactor;
				}
				else
				{
					gradient = 1f;
				}
			}
			else
			{
				float bottomT = (gradientPosition - 0.5f) / 0.5f;
				float whiteUntil = 1f - heatFactor;
				if (bottomT <= whiteUntil)
				{
					gradient = 1f;
				}
				else
				{
					gradient = 1f - ((bottomT - whiteUntil) / heatFactor);
				}
			}

			gradient = Mathf.Clamp(gradient, 0f, 1f);

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

				baseTemperatures[index] = temperature;
			}
		}

		return baseTemperatures;
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
		int originCount = Mathf.Max(1, windCellCount);
		float maxReach = Mathf.Sqrt(width * width + height * height) * 0.25f;

		for (int i = 0; i < originCount; i++)
		{
			int originX = random.Next(0, Mathf.Max(1, width - 1));
			int originY = random.Next(0, Mathf.Max(1, height - 1));
			float intensity = random.Next(1, 50);
			int reach = Mathf.Max(1, Mathf.FloorToInt((float)random.NextDouble() * maxReach));
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
					float tempIntensity = random.Next(-25, 25);
					float fallback = 0.15f * tempIntensity;
					wind[index] = new Vector2(fallback, fallback);
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
					distributed[index] += 0.15f * value;
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
				float moistureRemaining = baseMoisture[index] * 25f;
				int guardSteps = 0;
				int guardLimit = Mathf.Max(32, width + height);
				float unitX = windX / windSpeed;
				float unitY = windY / windSpeed;

				int traceX = x + Mathf.RoundToInt(unitX);
				int traceY = y + Mathf.RoundToInt(unitY);

				while (moistureRemaining > 1f && guardSteps < guardLimit)
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

					float deposited = moistureRemaining * elSlope;
					distributed[traceIndex] += deposited;
					if (elevation[traceIndex] < seaLevel)
					{
						distributed[traceIndex] = 0.0001f;
					}

					float moistureLoss = 0.1f * Mathf.Max(0.0001f, distributed[traceIndex]);
					moistureRemaining -= moistureLoss;

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
					guardSteps++;
				}
			}
		}

		distributed = AverageArray(distributed, elevation, width, height, 12, elevationOnly: true, seaLevel);
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				if (elevation[index] < seaLevel)
				{
					distributed[index] = 0f;
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

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				int index = y * width + x;
				float elevationValue = elevation[index];
				float temperatureValue = temperature[index];
				float moistureValue = moisture[index];

				if (temperatureValue < 0.03f && elevationValue < 0.65f)
				{
					biomes[index] = PlanetBiomeType.Ice;
					continue;
				}

				if (generateRivers)
				{
					if (elevationValue > 0.55f
						&& moistureValue > 0.15f
						&& random.NextDouble() < 0.0035f)
					{
						riverLayer[index] = 0.1f + 0.9f * (float)random.NextDouble();
						DefineRivers3(riverLayer, elevation, x, y, width, height, seaLevel, random);
					}
				}

				if (elevationValue < 0.5714f * seaLevel)
				{
					biomes[index] = PlanetBiomeType.Ocean;
					continue;
				}

				if (elevationValue < seaLevel)
				{
					biomes[index] = PlanetBiomeType.ShallowOcean;
					continue;
				}

				if (elevationValue < seaLevel + 0.027f)
				{
					biomes[index] = PlanetBiomeType.Coastland;
					continue;
				}

				if (elevationValue >= 0.68f)
				{
					biomes[index] = temperatureValue > 0.2f
						? PlanetBiomeType.RockyMountain
						: PlanetBiomeType.SnowyMountain;
					continue;
				}

				if (temperatureValue > 0.6f)
				{
					if (moistureValue < 0.15f)
					{
						biomes[index] = PlanetBiomeType.TropicalDesert;
						continue;
					}
					if (moistureValue < seaLevel)
					{
						biomes[index] = PlanetBiomeType.Savannah;
						continue;
					}
					if (moistureValue < 0.5f)
					{
						biomes[index] = PlanetBiomeType.Shrubland;
						continue;
					}
					if (moistureValue < 0.75f)
					{
						biomes[index] = PlanetBiomeType.TropicalSeasonalForest;
						continue;
					}
					biomes[index] = PlanetBiomeType.TropicalRainForest;
					continue;
				}

				if (temperatureValue > 0.25f)
				{
					if (moistureValue < 0.15f)
					{
						biomes[index] = PlanetBiomeType.TemperateDesert;
						continue;
					}
					if (moistureValue < 0.2f)
					{
						biomes[index] = PlanetBiomeType.Steppe;
						continue;
					}
					if (moistureValue < 0.4f)
					{
						biomes[index] = PlanetBiomeType.Grassland;
						continue;
					}
					if (moistureValue < 0.5f)
					{
						biomes[index] = PlanetBiomeType.Chaparral;
						continue;
					}
					if (moistureValue < 0.85f)
					{
						biomes[index] = PlanetBiomeType.TemperateSeasonalForest;
						continue;
					}
					biomes[index] = PlanetBiomeType.TemperateRainForest;
					continue;
				}

				if (temperatureValue > 0.05f)
				{
					if (moistureValue < 0.2f)
					{
						biomes[index] = PlanetBiomeType.Tundra;
						continue;
					}
					if (moistureValue < 0.55f)
					{
						biomes[index] = PlanetBiomeType.Taiga;
						continue;
					}
					biomes[index] = PlanetBiomeType.BorealForest;
					continue;
				}

				biomes[index] = moistureValue < 0.1f ? PlanetBiomeType.Tundra : PlanetBiomeType.Ice;
			}
		}

		return biomes;
	}



	private static void DefineRivers3(float[] riverLayer, float[] elevation, int startX, int startY, int width, int height, float seaLevel, Random random)
	{
		int currentX = startX;
		int currentY = startY;
		float currentElevation = elevation[currentY * width + currentX];
		int guardSteps = 0;
		int guardLimit = Mathf.Max(32, width + height);

		while (currentElevation >= seaLevel && guardSteps < guardLimit)
		{
			float smallestElevation = currentElevation;
			float nextSmallestElevation = float.PositiveInfinity;
			int lowestX = currentX;
			int lowestY = currentY;
			bool foundLower = false;

			for (int p = -1; p <= 1; p++)
			{
				for (int q = -1; q <= 1; q++)
				{
					if (p == 0 && q == 0)
					{
						continue;
					}

					int wrapY = currentY + q;
					if (wrapY >= height || wrapY < 0)
					{
						continue;
					}

					int wrapX = WrapX(currentX + p, width);
					int neighborIndex = wrapY * width + wrapX;
					if (riverLayer[neighborIndex] > 0f)
					{
						continue;
					}

					float candidateElevation = elevation[neighborIndex];
					if (candidateElevation < smallestElevation)
					{
						smallestElevation = candidateElevation;
						lowestX = wrapX;
						lowestY = wrapY;
						foundLower = true;
					}
					else if (candidateElevation < nextSmallestElevation)
					{
						nextSmallestElevation = candidateElevation;
					}
				}
			}

			if (!foundLower)
			{
				break;
			}

			int sourceIndex = currentY * width + currentX;
			int nextIndex = lowestY * width + lowestX;
			riverLayer[nextIndex] += riverLayer[sourceIndex] * 0.5f;
			riverLayer[sourceIndex] *= 0.5f;

			currentX = lowestX;
			currentY = lowestY;
			if (elevation[nextIndex] < seaLevel)
			{
				break;
			}

			currentElevation -= 0.000001f;
			guardSteps++;
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

	private static void LogStageTiming(string stage, ulong startMs)
	{
		if (!OS.IsDebugBuild())
		{
			return;
		}

		ulong elapsedMs = Time.GetTicksMsec() - startMs;
		GD.Print($"[PlanetGenTiming] {stage}: {elapsedMs}ms");
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
