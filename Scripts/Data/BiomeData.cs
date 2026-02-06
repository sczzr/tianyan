using System;
using Godot;

namespace FantasyMapGenerator.Scripts.Data;

public enum BiomeType
{
	Marine = 0,
	HotDesert = 1,
	ColdDesert = 2,
	Savanna = 3,
	Grassland = 4,
	TropicalSeasonalForest = 5,
	TemperateDeciduousForest = 6,
	TropicalRainforest = 7,
	TemperateRainforest = 8,
	Taiga = 9,
	Tundra = 10,
	Glacier = 11,
	Wetland = 12
}

public static class BiomeData
{
	public static readonly string[] Names =
	{
		"Marine",
		"Hot desert",
		"Cold desert",
		"Savanna",
		"Grassland",
		"Tropical seasonal forest",
		"Temperate deciduous forest",
		"Tropical rainforest",
		"Temperate rainforest",
		"Taiga",
		"Tundra",
		"Glacier",
		"Wetland"
	};

	public static readonly Color[] Colors =
	{
		new Color("466eab"),  // Marine
		new Color("fbe79f"),  // Hot desert
		new Color("b5b887"),  // Cold desert
		new Color("d2d082"),  // Savanna
		new Color("c8d68f"),  // Grassland
		new Color("b6d95d"),  // Tropical seasonal forest
		new Color("29bc56"),  // Temperate deciduous forest
		new Color("7dcb35"),  // Tropical rainforest
		new Color("409c43"),  // Temperate rainforest
		new Color("4b6b32"),  // Taiga
		new Color("96784b"),  // Tundra
		new Color("d5e7eb"),  // Glacier
		new Color("0b9131")   // Wetland
	};

	public static readonly int[] Habitability =
	{
		0, 4, 10, 22, 30, 50, 100, 80, 90, 12, 4, 0, 12
	};

	public static readonly int[] MovementCost =
	{
		10, 200, 150, 60, 50, 70, 70, 80, 90, 200, 1000, 5000, 150
	};

	// BiomesMatrix[moistureBand][temperatureBand] -> BiomeType
	// moistureBand: 0-4 (dry to wet)
	// temperatureBand: 0-25 (hot to cold, where 0 = >19°C, 25 = <-4°C)
	public static readonly byte[,] Matrix = new byte[5, 26]
	{
		// Dry (moisture band 0)
		{ 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 10 },
		// Somewhat dry (moisture band 1)
		{ 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 9, 9, 9, 9, 10, 10, 10 },
		// Moderate (moisture band 2)
		{ 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 9, 9, 9, 9, 9, 10, 10, 10 },
		// Somewhat wet (moisture band 3)
		{ 5, 6, 6, 6, 6, 6, 6, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 10, 10, 10 },
		// Wet (moisture band 4)
		{ 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 9, 10, 10 }
	};

	public static BiomeType GetBiome(int moistureBand, int temperatureBand)
	{
		moistureBand = Math.Clamp(moistureBand, 0, 4);
		temperatureBand = Math.Clamp(temperatureBand, 0, 25);
		return (BiomeType)Matrix[moistureBand, temperatureBand];
	}

	public static Color GetColor(BiomeType biome)
	{
		return Colors[(int)biome];
	}

	public static Color GetColor(int biomeId)
	{
		if (biomeId < 0 || biomeId >= Colors.Length)
			return Colors[0];
		return Colors[biomeId];
	}
}
