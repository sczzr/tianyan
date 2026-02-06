using System;
using Godot;

using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Types;
using TianYanShop.MapGeneration.Data.Entities;

namespace TianYanShop.MapGeneration.Generation
{
    public class BiomeGenerator
    {
        private RandomManager _random;

        public BiomeGenerator(RandomManager random)
        {
            _random = random;
        }

        public void Generate(VoronoiGraph graph, MapSettings settings)
        {
            for (int i = 0; i < graph.CellsCount; i++)
            {
                graph.Biomes[i] = (ushort)DetermineBiome(graph, i);
            }
        }

        private BiomeType DetermineBiome(VoronoiGraph graph, int cell)
        {
            float height = graph.Heights[cell] / 100f;
            float temperature = CalculateTemperature(graph, cell);
            float moisture = CalculateMoisture(graph, cell);

            if (height < 0.32f)
                return BiomeType.Ocean;

            if (temperature < 0.1f)
            {
                if (moisture < 0.2f) return BiomeType.Tundra;
                if (moisture < 0.4f) return BiomeType.Snow;
                return BiomeType.Tundra;
            }

            if (temperature < 0.3f)
            {
                if (moisture < 0.2f) return BiomeType.Tundra;
                if (moisture < 0.4f) return BiomeType.Snow;
                return BiomeType.Tundra;
            }

            if (temperature < 0.6f)
            {
                if (moisture < 0.2f) return BiomeType.TemperateDesert;
                if (moisture < 0.4f) return BiomeType.TemperateSeasonalForest;
                if (moisture < 0.6f) return BiomeType.TemperateSeasonalForest;
                return BiomeType.BorealForest;
            }

            if (temperature < 0.8f)
            {
                if (moisture < 0.2f) return BiomeType.TropicalDesert;
                if (moisture < 0.4f) return BiomeType.TropicalSeasonalForest;
                if (moisture < 0.6f) return BiomeType.TropicalSeasonalForest;
                return BiomeType.TropicalRainforest;
            }

            if (moisture < 0.2f) return BiomeType.TropicalDesert;
            if (moisture < 0.4f) return BiomeType.TropicalSeasonalForest;
            return BiomeType.TropicalRainforest;
        }

        private float CalculateTemperature(VoronoiGraph graph, int cell)
        {
            float baseTemp = 0.5f;
            float heightFactor = (graph.Heights[cell] / 100f) * 0.3f;
            return global::System.Math.Clamp(baseTemp - heightFactor, 0f, 1f);
        }

        private float CalculateMoisture(VoronoiGraph graph, int cell)
        {
            return _random.NextRange(0.1f, 0.9f);
        }
    }
}
