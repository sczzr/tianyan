using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Godot;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Map;

namespace FantasyMapGenerator.Scripts.Rendering;

public partial class MapView
{
	private MapLevel _activeContextLevel = MapLevel.World;
	private readonly Dictionary<int, Texture2D> _highPrecisionWorldTextureByLod = new();
	private readonly Dictionary<int, int> _highPrecisionWorldSignatureByLod = new();
	private readonly Dictionary<int, Texture2D> _highPrecisionRiverTextureByLod = new();
	private readonly Dictionary<int, int> _highPrecisionRiverSignatureByLod = new();
	private int _highPrecisionWorldSignature = int.MinValue;
	private int _highPrecisionRiverSignature = int.MinValue;
	private int _highPrecisionWorldLod = 1;
	private int _highPrecisionRiverLod = 1;
	private ulong _lastHighPrecisionWorldBuildMsec = 0;
	private ulong _lastHighPrecisionRiverBuildMsec = 0;
	private const ulong HighPrecisionBuildCooldownMsec = 180;
	private bool _preferHybridHighPrecision = true;
	private bool IsHighPrecisionWorldModeEnabled()
	{
		return _mapGenerator?.Data?.Heightmap != null;
	}

	private void InvalidateHighPrecisionWorldVectorCache()
	{
		_highPrecisionWorldSignature = int.MinValue;
		_highPrecisionRiverSignature = int.MinValue;
		_highPrecisionWorldLod = 1;
		_highPrecisionRiverLod = 1;
		_highPrecisionWorldTextureByLod.Clear();
		_highPrecisionWorldSignatureByLod.Clear();
		_highPrecisionRiverTextureByLod.Clear();
		_highPrecisionRiverSignatureByLod.Clear();
		_lastHighPrecisionWorldBuildMsec = 0;
		_lastHighPrecisionRiverBuildMsec = 0;
	}

	private int ResolveHighPrecisionLodScale()
	{
		if (!_preferHybridHighPrecision)
		{
			return 1;
		}

		float scale = Mathf.Max(0.001f, ViewScale);
		if (scale >= 3.2f)
		{
			return 4;
		}

		if (scale >= 1.8f)
		{
			return 2;
		}

		return 1;
	}

	private static int ComputeLodDimension(int baseDimension, int lod)
	{
		int safeBase = Mathf.Max(1, baseDimension);
		int safeLod = Mathf.Max(1, lod);
		long scaled = (long)safeBase * safeLod;
		int clamped = (int)Math.Min(scaled, 4096L);
		return Mathf.Max(safeBase, clamped);
	}

	private bool TryGetFallbackWorldTexture(int signature, out Texture2D texture)
	{
		foreach (var entry in _highPrecisionWorldSignatureByLod)
		{
			if (entry.Value != signature)
			{
				continue;
			}

			if (_highPrecisionWorldTextureByLod.TryGetValue(entry.Key, out var cached) && cached != null)
			{
				texture = cached;
				return true;
			}
		}

		texture = null;
		return false;
	}

	private bool TryGetFallbackRiverTexture(int signature, out Texture2D texture)
	{
		foreach (var entry in _highPrecisionRiverSignatureByLod)
		{
			if (entry.Value != signature)
			{
				continue;
			}

			if (_highPrecisionRiverTextureByLod.TryGetValue(entry.Key, out var cached) && cached != null)
			{
				texture = cached;
				return true;
			}
		}

		texture = null;
		return false;
	}

	private void DrawHighPrecisionWorldHeightmap(Rect2 baseRect)
	{
		var data = _mapGenerator?.Data;
		if (data?.Heightmap == null)
		{
			return;
		}

		int width = Mathf.Max(1, Mathf.RoundToInt(data.MapSize.X));
		int height = Mathf.Max(1, Mathf.RoundToInt(data.MapSize.Y));
		if (data.Heightmap.Length < width * height)
		{
			return;
		}

		int lod = ResolveHighPrecisionLodScale();
		int signature = BuildHighPrecisionWorldSignature(data.Heightmap, width, height, data.Seed);
		Texture2D texture = null;

		if (_highPrecisionWorldTextureByLod.TryGetValue(lod, out var cached)
			&& _highPrecisionWorldSignatureByLod.TryGetValue(lod, out var cachedSignature)
			&& cachedSignature == signature
			&& cached != null)
		{
			texture = cached;
		}
		else
		{
			ulong now = Time.GetTicksMsec();
			bool canRebuild = now - _lastHighPrecisionWorldBuildMsec >= HighPrecisionBuildCooldownMsec;
			if (canRebuild)
			{
				texture = BuildHighPrecisionWorldTexture(data.Heightmap, width, height, lod);
				if (texture != null)
				{
					_highPrecisionWorldTextureByLod[lod] = texture;
					_highPrecisionWorldSignatureByLod[lod] = signature;
					_lastHighPrecisionWorldBuildMsec = now;
				}
			}

			if (texture == null)
			{
				TryGetFallbackWorldTexture(signature, out texture);
			}
		}

		_highPrecisionWorldSignature = signature;
		_highPrecisionWorldLod = lod;

		if (texture != null)
		{
			DrawTextureRect(texture, baseRect, false);
		}
	}

	private Texture2D BuildHighPrecisionWorldTexture(float[] heightmap, int width, int height, int lod)
	{
		int renderWidth = ComputeLodDimension(width, lod);
		int renderHeight = ComputeLodDimension(height, lod);
		if (renderWidth <= 0 || renderHeight <= 0)
		{
			return null;
		}

		var image = Image.CreateEmpty(renderWidth, renderHeight, false, Image.Format.Rgba8);
		float waterLevel = Mathf.Clamp(_generationWaterLevel, 0.01f, 0.99f);
		const float coastBlendBand = 0.02f;
		const float shorelineBand = 0.011f;
		bool useCivilizationReliefShading = _visualStyleMode == MapVisualStyleSelection.Relief || _useGenesisPlanetTint;
		float sampleStepX = width > 1 ? (width - 1f) / Mathf.Max(1f, renderWidth - 1f) : 1f;
		float sampleStepY = height > 1 ? (height - 1f) / Mathf.Max(1f, renderHeight - 1f) : 1f;

		for (int y = 0; y < renderHeight; y++)
		{
			float srcY = renderHeight > 1 ? y * (height - 1f) / (renderHeight - 1f) : 0f;
			float latitude01 = renderHeight > 1 ? y / (float)(renderHeight - 1) : 0.5f;
			for (int x = 0; x < renderWidth; x++)
			{
				float srcX = renderWidth > 1 ? x * (width - 1f) / (renderWidth - 1f) : 0f;
				float mapHeight = Mathf.Clamp(SampleHeightmapBilinear(heightmap, width, height, new Vector2(srcX, srcY)), 0f, 1f);
				Color pixelColor;

				if (useCivilizationReliefShading)
				{
					float left = Mathf.Clamp(SampleHeightmapBilinear(heightmap, width, height, new Vector2(srcX - sampleStepX, srcY)), 0f, 1f);
					float right = Mathf.Clamp(SampleHeightmapBilinear(heightmap, width, height, new Vector2(srcX + sampleStepX, srcY)), 0f, 1f);
					float down = Mathf.Clamp(SampleHeightmapBilinear(heightmap, width, height, new Vector2(srcX, srcY - sampleStepY)), 0f, 1f);
					float up = Mathf.Clamp(SampleHeightmapBilinear(heightmap, width, height, new Vector2(srcX, srcY + sampleStepY)), 0f, 1f);

					Color landColor = ResolveCivilizationLandColor(mapHeight, waterLevel, latitude01, srcX, srcY, width, height);
					Color waterColor = ResolveCivilizationWaterColor(mapHeight, waterLevel);
					float coastMix = Mathf.SmoothStep(waterLevel - coastBlendBand, waterLevel + coastBlendBand, mapHeight);
					pixelColor = waterColor.Lerp(landColor, coastMix);

					Vector3 normal = new Vector3(
						-(right - left) * 3.6f,
						-(up - down) * 3.6f,
						1f).Normalized();
					Vector3 lightDirection = new Vector3(-0.58f, -0.33f, 0.74f).Normalized();
					float diffuse = Mathf.Clamp(normal.Dot(lightDirection), -1f, 1f);
					float shading = mapHeight > waterLevel
						? 0.72f + diffuse * 0.34f
						: 0.9f + diffuse * 0.08f;
					pixelColor = ApplyReliefLighting(pixelColor, shading);

					if (mapHeight > waterLevel)
					{
						float curvature = mapHeight * 4f - (left + right + up + down);
						float ridgeAmount = Mathf.Clamp(curvature * 4.2f, -1f, 1f);
						if (ridgeAmount > 0f)
						{
							pixelColor = pixelColor.Lightened(ridgeAmount * 0.08f);
						}
						else if (ridgeAmount < 0f)
						{
							pixelColor = pixelColor.Darkened(-ridgeAmount * 0.06f);
						}

						float elevation01 = Mathf.Clamp(Mathf.InverseLerp(waterLevel, 1f, mapHeight), 0f, 1f);
						float polarFactor = Mathf.Abs(latitude01 * 2f - 1f);
						float snowFromLatitude = Mathf.SmoothStep(0.9f, 0.985f, polarFactor);
						float snowFromElevation = Mathf.SmoothStep(0.94f, 1f, elevation01);
						float snowAmount = Mathf.Clamp(
							Mathf.Max(snowFromLatitude * Mathf.SmoothStep(0.22f, 1f, elevation01), snowFromElevation),
							0f,
							1f);
						if (snowAmount > 0f)
						{
							pixelColor = pixelColor.Lerp(new Color(0.95f, 0.97f, 0.99f, 1f), snowAmount * 0.28f);
						}
					}
				}
				else
				{
					Color landColor = GetTerrainColorFromHeightSample(mapHeight, true);
					Color waterColor = GetWaterColor(mapHeight);
					float coastMix = Mathf.SmoothStep(waterLevel - coastBlendBand, waterLevel + coastBlendBand, mapHeight);
					pixelColor = waterColor.Lerp(landColor, coastMix);
				}

				float shoreDistance = Mathf.Abs(mapHeight - waterLevel);
				float shoreAlpha = 1f - Mathf.Clamp(shoreDistance / shorelineBand, 0f, 1f);
				int sx = Mathf.Clamp(Mathf.RoundToInt(srcX), 0, width - 1);
				int sy = Mathf.Clamp(Mathf.RoundToInt(srcY), 0, height - 1);
				float shoreEdge = CalculateShoreEdgeFactor(heightmap, width, height, sx, sy, waterLevel, shorelineBand);
				shoreAlpha = Mathf.Max(shoreAlpha, shoreEdge * 0.82f);
				if (shoreAlpha > 0f)
				{
					Color shoreColor = _visualStyleMode == MapVisualStyleSelection.Parchment
						? new Color(0.95f, 0.88f, 0.7f, 1f)
						: new Color(0.86f, 0.84f, 0.78f, 1f);
					pixelColor = pixelColor.Lerp(shoreColor, shoreAlpha * 0.32f);
				}

				image.SetPixel(x, y, pixelColor);
			}
		}

		return ImageTexture.CreateFromImage(image);
	}

	private Color ResolveCivilizationLandColor(float mapHeight, float waterLevel, float latitude01)
	{
		return ResolveCivilizationLandColor(mapHeight, waterLevel, latitude01, 0.5f, latitude01, 1, 1);
	}

	private Color ResolveCivilizationLandColor(float mapHeight, float waterLevel, float latitude01, float sampleX, float sampleY, int width, int height)
	{
		float elevation01 = Mathf.Clamp(Mathf.InverseLerp(waterLevel, 1f, mapHeight), 0f, 1f);
		if (elevation01 <= 0f)
		{
			return new Color(0.58f, 0.66f, 0.49f, 0.96f);
		}

		float normalizedX = width > 1 ? Mathf.Clamp(sampleX / (width - 1f), 0f, 1f) : 0.5f;
		float normalizedY = height > 1 ? Mathf.Clamp(sampleY / (height - 1f), 0f, 1f) : latitude01;

		float tectonicScale = Mathf.Clamp((_genesisTerrainProfile.TectonicPlateCount - 1f) / 63f, 0f, 1f);
		float windScale = Mathf.Clamp((_genesisTerrainProfile.WindCellCount - 1f) / 23f, 0f, 1f);
		float erosionScale = Mathf.Clamp(_genesisTerrainProfile.ErosionStrength * (_genesisTerrainProfile.ErosionIterations / 16f), 0f, 1f);
		float heatNormalized = Mathf.Clamp((1000f - _genesisTerrainProfile.HeatFactor) / 999f, 0f, 1f);

		int macroCellsX = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(5f, 22f, tectonicScale)), 4, 30);
		int macroCellsY = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(3f, 12f, tectonicScale)), 3, 18);
		int biomeCellsX = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(7f, 30f, windScale * 0.75f + tectonicScale * 0.25f)), 5, 40);
		int biomeCellsY = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(4f, 14f, windScale)), 3, 20);

		int macroX = Mathf.Clamp(Mathf.FloorToInt(normalizedX * macroCellsX), 0, macroCellsX - 1);
		int macroY = Mathf.Clamp(Mathf.FloorToInt(normalizedY * macroCellsY), 0, macroCellsY - 1);
		int biomeX = Mathf.Clamp(Mathf.FloorToInt(normalizedX * biomeCellsX), 0, biomeCellsX - 1);
		int biomeY = Mathf.Clamp(Mathf.FloorToInt(normalizedY * biomeCellsY), 0, biomeCellsY - 1);

		float macroNoise = HashSigned01(macroX * 92821 + macroY * 68917 + 941, 17);
		float mesoNoise = HashSigned01(biomeX * 31337 + biomeY * 19997 + 577, 31);
		int sx = Mathf.RoundToInt(normalizedX * 4096f);
		int sy = Mathf.RoundToInt(normalizedY * 4096f);
		float microNoise = HashSigned01(sx * 12011 + sy * 7001 + 163, 13) * 0.08f;

		float absLat = Mathf.Abs(latitude01 * 2f - 1f);
		float equatorFactor = 1f - absLat;
		float subtropicBand = Mathf.SmoothStep(0.18f, 0.42f, equatorFactor) * (1f - Mathf.SmoothStep(0.58f, 0.84f, equatorFactor));
		float coastProximity = 1f - Mathf.Clamp(Mathf.Abs(mapHeight - waterLevel) / 0.085f, 0f, 1f);

		float forestPreference = Mathf.Clamp(
			_genesisAtmosphereInfluence * 0.52f
			+ _genesisTerrainProfile.MoistureTransport * 0.28f
			+ windScale * 0.34f
			- _genesisDesertInfluence * 0.26f,
			0f,
			1f);
		float grassPreference = Mathf.Clamp(
			_genesisDesertInfluence * 0.58f
			+ (1f - forestPreference) * 0.36f
			+ erosionScale * 0.16f,
			0f,
			1f);

		float moisture01 = 0.44f
			+ macroNoise * 0.24f
			+ mesoNoise * 0.16f
			+ microNoise
			+ coastProximity * 0.14f
			+ windScale * 0.08f;
		moisture01 -= subtropicBand * (0.16f + _genesisDesertInfluence * 0.14f);
		moisture01 -= elevation01 * 0.12f;
		moisture01 += forestPreference * 0.16f - grassPreference * 0.1f;
		moisture01 = Mathf.Clamp(moisture01, 0f, 1f);

		float coldness01 = Mathf.Clamp(
			absLat * Mathf.Lerp(0.72f, 0.94f, _genesisPolarInfluence)
			+ elevation01 * Mathf.Lerp(0.48f, 0.74f, _genesisMountainInfluence)
			+ (0.5f - heatNormalized) * 0.14f,
			0f,
			1f);
		int temperatureBand = Mathf.Clamp(Mathf.RoundToInt(coldness01 * 25f), 0, 25);
		int moistureBand = Mathf.Clamp(Mathf.RoundToInt(moisture01 * 4f), 0, 4);

		BiomeType biome = BiomeData.GetBiome(moistureBand, temperatureBand);
		if (biome == BiomeType.Grassland && forestPreference > 0.72f && moisture01 > 0.64f)
		{
			biome = BiomeType.TemperateDeciduousForest;
		}
		else if ((biome == BiomeType.TemperateRainforest || biome == BiomeType.TropicalRainforest || biome == BiomeType.TemperateDeciduousForest)
			&& grassPreference > 0.68f && moisture01 < 0.56f)
		{
			biome = BiomeType.Grassland;
		}
		else if (biome == BiomeType.Savanna && grassPreference > 0.66f)
		{
			biome = BiomeType.Grassland;
		}

		if (elevation01 > Mathf.Lerp(0.91f, 0.82f, _genesisMountainInfluence))
		{
			biome = temperatureBand >= 18 ? BiomeType.Glacier : BiomeType.Tundra;
		}
		else if (temperatureBand >= Mathf.RoundToInt(Mathf.Lerp(21f, 16f, _genesisPolarInfluence)) && elevation01 > 0.72f)
		{
			biome = BiomeType.Tundra;
		}

		Color biomeColor = BiomeData.GetColor((int)biome);
		Color color = new Color(biomeColor.R, biomeColor.G, biomeColor.B, 0.97f);
		float saturation = Mathf.Lerp(0.5f, 0.64f, forestPreference * 0.65f + grassPreference * 0.35f);
		color = AdjustColorSaturation(color, saturation);

		float rockyMix = Mathf.SmoothStep(0.72f, 0.95f, elevation01);
		color = color.Lerp(new Color(0.62f, 0.62f, 0.6f, 1f), rockyMix * 0.5f);

		return color;
	}

	private Color ResolveCivilizationWaterColor(float mapHeight, float waterLevel)
	{
		float depth01 = Mathf.Clamp(Mathf.InverseLerp(waterLevel, 0f, mapHeight), 0f, 1f);
		float tectonicScale = Mathf.Clamp((_genesisTerrainProfile.TectonicPlateCount - 1f) / 63f, 0f, 1f);
		float erosionScale = Mathf.Clamp(_genesisTerrainProfile.ErosionStrength * (_genesisTerrainProfile.ErosionIterations / 16f), 0f, 1f);
		int depthBands = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(3f, 7f, tectonicScale * 0.65f + erosionScale * 0.35f)), 3, 8);
		float depthBand = Mathf.Floor(depth01 * depthBands) / depthBands;

		Color shallow = new Color(0.35f, 0.62f, 0.76f, 1f);
		Color mid = new Color(0.19f, 0.44f, 0.63f, 1f);
		Color deep = new Color(0.07f, 0.2f, 0.37f, 1f);

		Color color;
		if (depthBand < 0.34f)
		{
			color = shallow;
		}
		else if (depthBand < 0.68f)
		{
			color = mid;
		}
		else
		{
			color = deep;
		}

		float coastalGlow = 1f - Mathf.Clamp(depthBand / 0.26f, 0f, 1f);
		color = color.Lerp(shallow.Lightened(0.08f), coastalGlow * 0.28f);
		color = AdjustColorSaturation(color, 0.58f);
		return new Color(color.R, color.G, color.B, 0.92f);
	}

	private static Color AdjustColorSaturation(Color color, float saturation)
	{
		float clampedSaturation = Mathf.Clamp(saturation, 0f, 1.2f);
		float luma = color.R * 0.299f + color.G * 0.587f + color.B * 0.114f;
		return new Color(
			Mathf.Lerp(luma, color.R, clampedSaturation),
			Mathf.Lerp(luma, color.G, clampedSaturation),
			Mathf.Lerp(luma, color.B, clampedSaturation),
			color.A);
	}

	private static Color ApplyReliefLighting(Color color, float lightingFactor)
	{
		float clamped = Mathf.Clamp(lightingFactor, 0.45f, 1.22f);
		if (clamped >= 1f)
		{
			return color.Lerp(new Color(1f, 1f, 1f, color.A), (clamped - 1f) * 0.55f);
		}

		return color.Darkened((1f - clamped) * 0.88f);
	}

	private void DrawHighPrecisionWorldRivers(Rect2 baseRect)
	{
		var data = _mapGenerator?.Data;
		if (data?.Rivers == null || data.Rivers.Length == 0)
		{
			return;
		}

		int width = Mathf.Max(1, Mathf.RoundToInt(data.MapSize.X));
		int height = Mathf.Max(1, Mathf.RoundToInt(data.MapSize.Y));
		if (width <= 1 || height <= 1)
		{
			return;
		}

		int lod = ResolveHighPrecisionLodScale();
		int signature = BuildHighPrecisionRiverSignature(data.Rivers, data.Cells, width, height, data.Seed);
		Texture2D texture = null;

		if (_highPrecisionRiverTextureByLod.TryGetValue(lod, out var cached)
			&& _highPrecisionRiverSignatureByLod.TryGetValue(lod, out var cachedSignature)
			&& cachedSignature == signature
			&& cached != null)
		{
			texture = cached;
		}
		else
		{
			ulong now = Time.GetTicksMsec();
			bool canRebuild = now - _lastHighPrecisionRiverBuildMsec >= HighPrecisionBuildCooldownMsec;
			if (canRebuild)
			{
				texture = BuildHighPrecisionRiverTexture(data, width, height, lod);
				if (texture != null)
				{
					_highPrecisionRiverTextureByLod[lod] = texture;
					_highPrecisionRiverSignatureByLod[lod] = signature;
					_lastHighPrecisionRiverBuildMsec = now;
				}
			}

			if (texture == null)
			{
				TryGetFallbackRiverTexture(signature, out texture);
			}
		}

		_highPrecisionRiverSignature = signature;
		_highPrecisionRiverLod = lod;

		if (texture != null)
		{
			DrawTextureRect(texture, baseRect, false);
		}
	}

	private Texture2D BuildHighPrecisionRiverTexture(MapData data, int width, int height, int lod)
	{
		int renderWidth = ComputeLodDimension(width, lod);
		int renderHeight = ComputeLodDimension(height, lod);
		if (renderWidth <= 1 || renderHeight <= 1)
		{
			return null;
		}

		var image = Image.CreateEmpty(renderWidth, renderHeight, false, Image.Format.Rgba8);
		float waterLevel = Mathf.Clamp(_generationWaterLevel, 0.01f, 0.99f);
		Color headColor = _dynamicRiverColor.Lightened(0.12f);
		Color mouthColor = _dynamicRiverColor.Darkened(0.08f).Lerp(_dynamicOceanColor, 0.22f);

		float scaleX = width > 1 ? (renderWidth - 1f) / (width - 1f) : 1f;
		float scaleY = height > 1 ? (renderHeight - 1f) / (height - 1f) : 1f;
		float pixelScale = (scaleX + scaleY) * 0.5f;

		foreach (var river in data.Rivers)
		{
			if (river?.MeanderedPoints == null || river.MeanderedPoints.Count < 2)
			{
				continue;
			}

			var riverTypeStyle = GetRiverTypeStyle(river.Type);
			int segmentCount = river.MeanderedPoints.Count - 1;

			for (int i = 1; i < river.MeanderedPoints.Count; i++)
			{
				var prevPoint = river.MeanderedPoints[i - 1];
				var currentPoint = river.MeanderedPoints[i];
				var fromMap = new Vector2(prevPoint.X, prevPoint.Y);
				var toMap = new Vector2(currentPoint.X, currentPoint.Y);
				var from = new Vector2(fromMap.X * scaleX, fromMap.Y * scaleY);
				var to = new Vector2(toMap.X * scaleX, toMap.Y * scaleY);
				float progress = segmentCount > 0 ? i / (float)segmentCount : 1f;

				float prevHeight = SampleHeightmapBilinear(data.Heightmap, width, height, fromMap);
				float currentHeight = SampleHeightmapBilinear(data.Heightmap, width, height, toMap);
				bool prevWater = prevHeight <= waterLevel;
				bool currentWater = currentHeight <= waterLevel;

				if (prevWater && currentWater && progress < 0.96f)
				{
					continue;
				}

				float flux = float.IsNaN(currentPoint.Z) ? 0f : Mathf.Max(0f, currentPoint.Z);
				float baseWidth = Mathf.Clamp(Mathf.Sqrt(flux) / 10f, 0.72f, 5.2f);
				float downstreamScale = Mathf.Lerp(0.86f, 1.34f, Mathf.Pow(progress, 0.82f));
				float estuaryT = Mathf.Clamp((progress - 0.82f) / 0.18f, 0f, 1f);
				float estuaryScale = 1f + Mathf.Pow(estuaryT, 1.35f) * 1.85f;
				bool crossingMouth = !prevWater && currentWater;
				if (crossingMouth)
				{
					estuaryScale *= 1.25f;
				}

				float widthPixels = Mathf.Clamp(baseWidth * downstreamScale * estuaryScale * riverTypeStyle.WidthScale * pixelScale, 0.8f, 20f);

				Color segmentColor = headColor.Lerp(mouthColor, Mathf.Pow(progress, 1.08f));
				segmentColor = segmentColor.Lerp(riverTypeStyle.Tint, riverTypeStyle.TintMix);
				if (crossingMouth)
				{
					segmentColor = segmentColor.Lerp(_dynamicOceanColor, 0.28f);
				}

				float segmentAlpha = Mathf.Lerp(0.58f, 0.92f, Mathf.Clamp(widthPixels / 6.6f, 0f, 1f)) * riverTypeStyle.AlphaScale;
				segmentColor = new Color(segmentColor.R, segmentColor.G, segmentColor.B, segmentAlpha);
				float jitterStrength = Mathf.Clamp(widthPixels * riverTypeStyle.NoiseScale, 0.08f, 2.25f);
				int jitterSeed = river.Id * 104729 + i * 739;
				RasterizeRiverSegment(
					image,
					from,
					to,
					widthPixels,
					segmentColor,
					jitterSeed,
					jitterStrength,
					renderWidth,
					renderHeight);
			}
		}

		if (data.Cells != null && data.Cells.Length > 0)
		{
			Color glowColor = _dynamicRiverColor.Lightened(0.36f);
			for (int i = 0; i < data.Cells.Length; i++)
			{
				var cell = data.Cells[i];
				if (cell.RiverId <= 0 || cell.Confluence <= 0 || cell.Height <= waterLevel)
				{
					continue;
				}

				float confluenceStrength = Mathf.Clamp(cell.Confluence / 180f, 0f, 1f);
				float radius = Mathf.Lerp(1.2f, 4.8f, Mathf.Pow(confluenceStrength, 0.7f)) * pixelScale;
				float alpha = Mathf.Lerp(0.22f, 0.72f, confluenceStrength);
				var center = new Vector2(cell.Position.X * scaleX, cell.Position.Y * scaleY);

				DrawSoftCircle(image, center, radius, new Color(glowColor.R, glowColor.G, glowColor.B, alpha), renderWidth, renderHeight);
				DrawSoftCircle(image, center, radius * 0.46f, new Color(0.9f, 0.96f, 1f, alpha * 0.72f), renderWidth, renderHeight);
			}
		}

		return ImageTexture.CreateFromImage(image);
	}

	private int BuildHighPrecisionWorldSignature(float[] heightmap, int width, int height, int seed)
	{
		unchecked
		{
			int hash = 17;
			hash = hash * 31 + RuntimeHelpers.GetHashCode(heightmap);
			hash = hash * 31 + width;
			hash = hash * 31 + height;
			hash = hash * 31 + seed;
			hash = hash * 31 + Mathf.RoundToInt(_generationWaterLevel * 1000f);
			hash = hash * 31 + (int)_visualStyleMode;
			hash = hash * 31 + (int)_terrainStyleMode;
			hash = hash * 31 + (_useGenesisPlanetTint ? 1 : 0);
			hash = hash * 31 + Mathf.RoundToInt(_dynamicTerrainBlend * 1000f);
			hash = hash * 31 + Mathf.RoundToInt(_genesisMountainInfluence * 1000f);
			hash = hash * 31 + Mathf.RoundToInt(_genesisPolarInfluence * 1000f);
			hash = hash * 31 + Mathf.RoundToInt(_genesisDesertInfluence * 1000f);
			hash = hash * 31 + Mathf.RoundToInt(_genesisAtmosphereInfluence * 1000f);
			hash = hash * 31 + _genesisTerrainProfile.TectonicPlateCount;
			hash = hash * 31 + _genesisTerrainProfile.WindCellCount;
			hash = hash * 31 + _genesisTerrainProfile.ErosionIterations;
			hash = hash * 31 + Mathf.RoundToInt(_genesisTerrainProfile.ErosionStrength * 1000f);
			hash = hash * 31 + Mathf.RoundToInt(_genesisTerrainProfile.HeatFactor);
			hash = hash * 31 + QuantizeColor(_dynamicTerrainLowColor);
			hash = hash * 31 + QuantizeColor(_dynamicTerrainHighColor);
			hash = hash * 31 + QuantizeColor(_dynamicOceanColor);
			return hash;
		}
	}

	private int BuildHighPrecisionRiverSignature(River[] rivers, Cell[] cells, int width, int height, int seed)
	{
		unchecked
		{
			int hash = 23;
			hash = hash * 31 + RuntimeHelpers.GetHashCode(rivers);
			hash = hash * 31 + RuntimeHelpers.GetHashCode(cells);
			hash = hash * 31 + width;
			hash = hash * 31 + height;
			hash = hash * 31 + seed;
			hash = hash * 31 + Mathf.RoundToInt(_generationWaterLevel * 1000f);
			hash = hash * 31 + QuantizeColor(_dynamicRiverColor);
			hash = hash * 31 + QuantizeColor(_dynamicOceanColor);
			for (int i = 0; i < rivers.Length; i++)
			{
				var river = rivers[i];
				if (river == null)
				{
					continue;
				}

				hash = hash * 31 + (int)river.Type;
				hash = hash * 31 + Mathf.RoundToInt(river.Discharge);
				hash = hash * 31 + Mathf.RoundToInt(river.Length);
			}

			if (cells != null)
			{
				int step = Mathf.Max(1, cells.Length / 64);
				for (int i = 0; i < cells.Length; i += step)
				{
					hash = hash * 31 + cells[i].Confluence;
					hash = hash * 31 + cells[i].RiverId;
				}
			}
			return hash;
		}
	}

	private static float CalculateShoreEdgeFactor(
		float[] heightmap,
		int width,
		int height,
		int x,
		int y,
		float waterLevel,
		float shorelineBand)
	{
		if (width <= 2 || height <= 2)
		{
			return 0f;
		}

		float center = heightmap[y * width + x];
		bool centerLand = center > waterLevel;
		int transitions = 0;
		float minDistance = Mathf.Abs(center - waterLevel);

		void Visit(int sampleX, int sampleY)
		{
			if (sampleX < 0 || sampleX >= width || sampleY < 0 || sampleY >= height)
			{
				return;
			}

			float neighbor = heightmap[sampleY * width + sampleX];
			if ((neighbor > waterLevel) != centerLand)
			{
				transitions++;
			}

			minDistance = Mathf.Min(minDistance, Mathf.Abs(neighbor - waterLevel));
		}

		Visit(x - 1, y);
		Visit(x + 1, y);
		Visit(x, y - 1);
		Visit(x, y + 1);

		if (transitions == 0)
		{
			return 0f;
		}

		float transitionFactor = transitions / 4f;
		float proximity = 1f - Mathf.Clamp(minDistance / Mathf.Max(0.0001f, shorelineBand * 1.8f), 0f, 1f);
		return transitionFactor * proximity;
	}

	private static float SampleHeightmapBilinear(float[] heightmap, int width, int height, Vector2 mapPosition)
	{
		if (heightmap == null || width <= 0 || height <= 0)
		{
			return 0f;
		}

		float x = Mathf.Clamp(mapPosition.X, 0f, width - 1f);
		float y = Mathf.Clamp(mapPosition.Y, 0f, height - 1f);
		int x0 = Mathf.Clamp((int)Mathf.Floor(x), 0, width - 1);
		int y0 = Mathf.Clamp((int)Mathf.Floor(y), 0, height - 1);
		int x1 = Mathf.Clamp(x0 + 1, 0, width - 1);
		int y1 = Mathf.Clamp(y0 + 1, 0, height - 1);
		float tx = x - x0;
		float ty = y - y0;

		float h00 = heightmap[y0 * width + x0];
		float h10 = heightmap[y0 * width + x1];
		float h01 = heightmap[y1 * width + x0];
		float h11 = heightmap[y1 * width + x1];

		float top = Mathf.Lerp(h00, h10, tx);
		float bottom = Mathf.Lerp(h01, h11, tx);
		return Mathf.Lerp(top, bottom, ty);
	}

	private static (float WidthScale, float AlphaScale, float NoiseScale, Color Tint, float TintMix) GetRiverTypeStyle(RiverType riverType)
	{
		return riverType switch
		{
			RiverType.River => (1.32f, 1.14f, 0.12f, new Color(0.42f, 0.64f, 0.95f, 1f), 0.22f),
			RiverType.Fork => (1.12f, 1.06f, 0.16f, new Color(0.46f, 0.7f, 0.98f, 1f), 0.16f),
			RiverType.Stream => (1.0f, 1.0f, 0.2f, new Color(0.52f, 0.76f, 1f, 1f), 0.1f),
			RiverType.Branch => (0.84f, 0.9f, 0.28f, new Color(0.62f, 0.84f, 1f, 1f), 0.08f),
			RiverType.Brook => (0.74f, 0.82f, 0.34f, new Color(0.68f, 0.9f, 1f, 1f), 0.1f),
			RiverType.Creek => (0.62f, 0.74f, 0.4f, new Color(0.74f, 0.93f, 1f, 1f), 0.12f),
			_ => (1f, 1f, 0.2f, new Color(0.52f, 0.76f, 1f, 1f), 0.1f)
		};
	}

	private static int QuantizeColor(Color color)
	{
		int r = Mathf.Clamp(Mathf.RoundToInt(color.R * 255f), 0, 255);
		int g = Mathf.Clamp(Mathf.RoundToInt(color.G * 255f), 0, 255);
		int b = Mathf.Clamp(Mathf.RoundToInt(color.B * 255f), 0, 255);
		int a = Mathf.Clamp(Mathf.RoundToInt(color.A * 255f), 0, 255);
		return (r << 24) | (g << 16) | (b << 8) | a;
	}

	private static void RasterizeRiverSegment(
		Image image,
		Vector2 from,
		Vector2 to,
		float widthPixels,
		Color color,
		int jitterSeed,
		float jitterStrength,
		int mapWidth,
		int mapHeight)
	{
		float distance = from.DistanceTo(to);
		int steps = Mathf.Max(1, Mathf.CeilToInt(distance * 1.6f));
		float radius = Mathf.Max(0.5f, widthPixels * 0.5f);
		Vector2 direction = distance > 0.0001f ? (to - from) / distance : Vector2.Right;
		Vector2 normal = new Vector2(-direction.Y, direction.X);

		for (int step = 0; step <= steps; step++)
		{
			float t = step / (float)steps;
			float noise = HashSigned01(jitterSeed, step);
			float taper = Mathf.Sin(t * Mathf.Pi);
			float offsetAmount = noise * jitterStrength * taper;
			Vector2 center = from.Lerp(to, t) + normal * offsetAmount;
			DrawSoftCircle(image, center, radius, color, mapWidth, mapHeight);
		}
	}

	private void DrawRiverConfluenceHighlights(Image image, Cell[] cells, float waterLevel, int mapWidth, int mapHeight)
	{
		Color glowColor = _dynamicRiverColor.Lightened(0.36f);
		for (int i = 0; i < cells.Length; i++)
		{
			var cell = cells[i];
			if (cell.RiverId <= 0 || cell.Confluence <= 0 || cell.Height <= waterLevel)
			{
				continue;
			}

			float confluenceStrength = Mathf.Clamp(cell.Confluence / 180f, 0f, 1f);
			float radius = Mathf.Lerp(1.2f, 4.8f, Mathf.Pow(confluenceStrength, 0.7f));
			float alpha = Mathf.Lerp(0.22f, 0.72f, confluenceStrength);
			var center = new Vector2(cell.Position.X, cell.Position.Y);

			DrawSoftCircle(image, center, radius, new Color(glowColor.R, glowColor.G, glowColor.B, alpha), mapWidth, mapHeight);
			DrawSoftCircle(image, center, radius * 0.46f, new Color(0.9f, 0.96f, 1f, alpha * 0.72f), mapWidth, mapHeight);
		}
	}

	private static float HashSigned01(int seed, int step)
	{
		unchecked
		{
			uint h = (uint)(seed * 374761393 + step * 668265263 + 1274126177);
			h ^= h >> 13;
			h *= 1274126177;
			h ^= h >> 16;
			float value01 = (h & 0x00FFFFFF) / 16777215f;
			return value01 * 2f - 1f;
		}
	}

	private static void DrawSoftCircle(Image image, Vector2 center, float radius, Color color, int width, int height)
	{
		int minX = Mathf.Clamp(Mathf.FloorToInt(center.X - radius - 1f), 0, width - 1);
		int maxX = Mathf.Clamp(Mathf.CeilToInt(center.X + radius + 1f), 0, width - 1);
		int minY = Mathf.Clamp(Mathf.FloorToInt(center.Y - radius - 1f), 0, height - 1);
		int maxY = Mathf.Clamp(Mathf.CeilToInt(center.Y + radius + 1f), 0, height - 1);

		for (int y = minY; y <= maxY; y++)
		{
			for (int x = minX; x <= maxX; x++)
			{
				float dx = x - center.X;
				float dy = y - center.Y;
				float dist = Mathf.Sqrt(dx * dx + dy * dy);
				if (dist > radius + 0.9f)
				{
					continue;
				}

				float alpha = Mathf.Clamp(radius + 0.9f - dist, 0f, 1f) * color.A;
				if (alpha <= 0.001f)
				{
					continue;
				}

				Color old = image.GetPixel(x, y);
				float inv = 1f - alpha;
				Color blended = new Color(
					old.R * inv + color.R * alpha,
					old.G * inv + color.G * alpha,
					old.B * inv + color.B * alpha,
					Mathf.Clamp(old.A + alpha, 0f, 1f));
				image.SetPixel(x, y, blended);
			}
		}
	}

	public string ExportWorldVectorSvg(string outputPath = "")
	{
		var data = _mapGenerator?.Data;
		if (data?.Heightmap == null)
		{
			return string.Empty;
		}

		int width = Mathf.Max(2, Mathf.RoundToInt(data.MapSize.X));
		int height = Mathf.Max(2, Mathf.RoundToInt(data.MapSize.Y));
		if (data.Heightmap.Length < width * height)
		{
			return string.Empty;
		}

		if (string.IsNullOrWhiteSpace(outputPath))
		{
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
			outputPath = $"user://exports/world_map_{timestamp}.svg";
		}

		if (!EnsureOutputDirectory(outputPath))
		{
			return string.Empty;
		}

		var svg = new StringBuilder(width * height / 2);
		svg.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
		svg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width - 1} {height - 1}\" shape-rendering=\"geometricPrecision\">");
		svg.AppendLine($"  <rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" fill=\"{ToSvgColor(_dynamicOceanColor)}\" fill-opacity=\"{ToSvgOpacity(_dynamicOceanColor.A)}\" />");

		AppendSvgTerrainContours(svg, data.Heightmap, width, height);
		AppendSvgCoastline(svg, data.Heightmap, width, height);
		AppendSvgRivers(svg, data.Rivers);

		svg.AppendLine("</svg>");

		using var file = FileAccess.Open(outputPath, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			return string.Empty;
		}

		file.StoreString(svg.ToString());
		return outputPath;
	}

	private void AppendSvgTerrainContours(StringBuilder svg, float[] heightmap, int width, int height)
	{
		float waterLevel = Mathf.Clamp(_generationWaterLevel, 0.01f, 0.99f);
		const int contourLevels = 8;

		svg.AppendLine("  <g id=\"terrain-contours\" fill=\"none\" stroke-linecap=\"round\" stroke-linejoin=\"round\">");
		for (int i = 0; i < contourLevels; i++)
		{
			float t = (i + 1f) / (contourLevels + 1f);
			float level = Mathf.Lerp(waterLevel + 0.02f, 0.98f, t);
			Color lineColor = GetTerrainColorFromHeightSample(level, true).Darkened(0.2f);
			float strokeWidth = Mathf.Lerp(0.28f, 0.9f, t);
			AppendSvgMarchingSquaresSegments(svg, heightmap, width, height, level, lineColor, 0.55f, strokeWidth);
		}
		svg.AppendLine("  </g>");
	}

	private void AppendSvgCoastline(StringBuilder svg, float[] heightmap, int width, int height)
	{
		float waterLevel = Mathf.Clamp(_generationWaterLevel, 0.01f, 0.99f);
		Color coastColor = _visualStyleMode == MapVisualStyleSelection.Parchment
			? new Color(0.45f, 0.33f, 0.2f, 0.92f)
			: new Color(0.93f, 0.97f, 1f, 0.92f);

		svg.AppendLine("  <g id=\"coastline\" fill=\"none\" stroke-linecap=\"round\" stroke-linejoin=\"round\">");
		AppendSvgMarchingSquaresSegments(svg, heightmap, width, height, waterLevel, coastColor, 0.95f, 1.35f);
		svg.AppendLine("  </g>");
	}

	private void AppendSvgRivers(StringBuilder svg, River[] rivers)
	{
		if (rivers == null || rivers.Length == 0)
		{
			return;
		}

		svg.AppendLine("  <g id=\"rivers\" fill=\"none\" stroke-linecap=\"round\" stroke-linejoin=\"round\">");
		foreach (var river in rivers)
		{
			if (river?.MeanderedPoints == null || river.MeanderedPoints.Count < 2)
			{
				continue;
			}

			var style = GetRiverTypeStyle(river.Type);
			float baseWidth = Mathf.Clamp(Mathf.Sqrt(Mathf.Max(0f, river.Discharge)) / 8f, 0.45f, 3.8f) * style.WidthScale;
			Color riverColor = _dynamicRiverColor.Lerp(style.Tint, style.TintMix);
			float opacity = Mathf.Clamp(0.82f * style.AlphaScale, 0.25f, 1f);

			var pathData = new StringBuilder();
			for (int i = 0; i < river.MeanderedPoints.Count; i++)
			{
				var p = river.MeanderedPoints[i];
				if (i == 0)
				{
					pathData.Append($"M {ToSvgFloat(p.X)} {ToSvgFloat(p.Y)} ");
				}
				else
				{
					pathData.Append($"L {ToSvgFloat(p.X)} {ToSvgFloat(p.Y)} ");
				}
			}

			svg.AppendLine($"    <path d=\"{pathData}\" stroke=\"{ToSvgColor(riverColor)}\" stroke-opacity=\"{ToSvgOpacity(opacity)}\" stroke-width=\"{ToSvgFloat(baseWidth)}\" />");
		}
		svg.AppendLine("  </g>");
	}

	private static void AppendSvgMarchingSquaresSegments(
		StringBuilder svg,
		float[] heightmap,
		int width,
		int height,
		float level,
		Color strokeColor,
		float opacity,
		float strokeWidth)
	{
		for (int y = 0; y < height - 1; y++)
		{
			for (int x = 0; x < width - 1; x++)
			{
				float h00 = heightmap[y * width + x];
				float h10 = heightmap[y * width + x + 1];
				float h11 = heightmap[(y + 1) * width + x + 1];
				float h01 = heightmap[(y + 1) * width + x];

				int index = 0;
				if (h00 > level) index |= 1;
				if (h10 > level) index |= 2;
				if (h11 > level) index |= 4;
				if (h01 > level) index |= 8;

				if (index == 0 || index == 15)
				{
					continue;
				}

				AppendCaseSegments(svg, x, y, h00, h10, h11, h01, level, index, strokeColor, opacity, strokeWidth);
			}
		}
	}

	private static void AppendCaseSegments(
		StringBuilder svg,
		int x,
		int y,
		float h00,
		float h10,
		float h11,
		float h01,
		float level,
		int index,
		Color strokeColor,
		float opacity,
		float strokeWidth)
	{
		void AppendSegment(int edgeA, int edgeB)
		{
			Vector2 p1 = InterpolateEdgePoint(x, y, h00, h10, h11, h01, level, edgeA);
			Vector2 p2 = InterpolateEdgePoint(x, y, h00, h10, h11, h01, level, edgeB);
			svg.AppendLine($"    <line x1=\"{ToSvgFloat(p1.X)}\" y1=\"{ToSvgFloat(p1.Y)}\" x2=\"{ToSvgFloat(p2.X)}\" y2=\"{ToSvgFloat(p2.Y)}\" stroke=\"{ToSvgColor(strokeColor)}\" stroke-opacity=\"{ToSvgOpacity(opacity)}\" stroke-width=\"{ToSvgFloat(strokeWidth)}\" />");
		}

		switch (index)
		{
			case 1: AppendSegment(3, 0); break;
			case 2: AppendSegment(0, 1); break;
			case 3: AppendSegment(3, 1); break;
			case 4: AppendSegment(1, 2); break;
			case 5: AppendSegment(3, 2); AppendSegment(0, 1); break;
			case 6: AppendSegment(0, 2); break;
			case 7: AppendSegment(3, 2); break;
			case 8: AppendSegment(2, 3); break;
			case 9: AppendSegment(0, 2); break;
			case 10: AppendSegment(0, 3); AppendSegment(1, 2); break;
			case 11: AppendSegment(1, 2); break;
			case 12: AppendSegment(1, 3); break;
			case 13: AppendSegment(0, 1); break;
			case 14: AppendSegment(0, 3); break;
		}
	}

	private static Vector2 InterpolateEdgePoint(
		int x,
		int y,
		float h00,
		float h10,
		float h11,
		float h01,
		float level,
		int edge)
	{
		const float epsilon = 1e-6f;

		float x1;
		float y1;
		float v1;
		float x2;
		float y2;
		float v2;

		switch (edge)
		{
			case 0:
				x1 = x; y1 = y; v1 = h00;
				x2 = x + 1f; y2 = y; v2 = h10;
				break;
			case 1:
				x1 = x + 1f; y1 = y; v1 = h10;
				x2 = x + 1f; y2 = y + 1f; v2 = h11;
				break;
			case 2:
				x1 = x + 1f; y1 = y + 1f; v1 = h11;
				x2 = x; y2 = y + 1f; v2 = h01;
				break;
			case 3:
				x1 = x; y1 = y + 1f; v1 = h01;
				x2 = x; y2 = y; v2 = h00;
				break;
			default:
				x1 = x; y1 = y; v1 = h00;
				x2 = x + 1f; y2 = y; v2 = h10;
				break;
		}

		float t = Mathf.Abs(v2 - v1) < epsilon ? 0.5f : Mathf.Clamp((level - v1) / (v2 - v1), 0f, 1f);
		return new Vector2(
			Mathf.Lerp(x1, x2, t),
			Mathf.Lerp(y1, y2, t));
	}

	private static bool EnsureOutputDirectory(string outputPath)
	{
		if (string.IsNullOrWhiteSpace(outputPath))
		{
			return false;
		}

		int idx = outputPath.LastIndexOf('/');
		if (idx <= 0)
		{
			return true;
		}

		string dirPath = outputPath[..idx];
		if (string.IsNullOrWhiteSpace(dirPath))
		{
			return true;
		}

		string absoluteDir = dirPath.StartsWith("user://", StringComparison.Ordinal)
			? ProjectSettings.GlobalizePath(dirPath)
			: dirPath;

		Error error = DirAccess.MakeDirRecursiveAbsolute(absoluteDir);
		return error == Error.Ok || error == Error.AlreadyExists;
	}

	private static string ToSvgColor(Color color)
	{
		int r = Mathf.Clamp(Mathf.RoundToInt(color.R * 255f), 0, 255);
		int g = Mathf.Clamp(Mathf.RoundToInt(color.G * 255f), 0, 255);
		int b = Mathf.Clamp(Mathf.RoundToInt(color.B * 255f), 0, 255);
		return $"#{r:X2}{g:X2}{b:X2}";
	}

	private static string ToSvgOpacity(float opacity)
	{
		float clamped = Mathf.Clamp(opacity, 0f, 1f);
		return clamped.ToString("0.###", CultureInfo.InvariantCulture);
	}

	private static string ToSvgFloat(float value)
	{
		return value.ToString("0.###", CultureInfo.InvariantCulture);
	}

}
