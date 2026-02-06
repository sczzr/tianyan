using System;
using System.Collections.Generic;
using Godot;

namespace TianYanShop.MapGeneration.Data
{
    /// <summary>
    /// 生物群系颜色配置
    /// </summary>
    [Serializable]
    public partial class BiomeColorConfig : Resource
    {
        [Export] public Color Ocean { get; set; } = new Color(0.16f, 0.38f, 0.65f);
        [Export] public Color Lake { get; set; } = new Color(0.16f, 0.38f, 0.75f);
        [Export] public Color TropicalDesert { get; set; } = new Color(0.95f, 0.9f, 0.67f);
        [Export] public Color TemperateDesert { get; set; } = new Color(0.93f, 0.85f, 0.67f);
        [Export] public Color ColdDesert { get; set; } = new Color(0.8f, 0.78f, 0.7f);
        [Export] public Color TropicalRainforest { get; set; } = new Color(0.05f, 0.4f, 0.1f);
        [Export] public Color TropicalSeasonalForest { get; set; } = new Color(0.3f, 0.6f, 0.15f);
        [Export] public Color TemperateSeasonalForest { get; set; } = new Color(0.3f, 0.55f, 0.25f);
        [Export] public Color TemperateRainforest { get; set; } = new Color(0.15f, 0.45f, 0.2f);
        [Export] public Color BorealForest { get; set; } = new Color(0.15f, 0.35f, 0.25f);
        [Export] public Color Tundra { get; set; } = new Color(0.55f, 0.65f, 0.55f);
        [Export] public Color Snow { get; set; } = new Color(0.95f, 0.98f, 1f);
        [Export] public Color Mangrove { get; set; } = new Color(0.1f, 0.45f, 0.35f);

        public Color GetBiomeColor(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Ocean => Ocean,
                BiomeType.Lake => Lake,
                BiomeType.TropicalDesert => TropicalDesert,
                BiomeType.TemperateDesert => TemperateDesert,
                BiomeType.ColdDesert => ColdDesert,
                BiomeType.TropicalRainforest => TropicalRainforest,
                BiomeType.TropicalSeasonalForest => TropicalSeasonalForest,
                BiomeType.TemperateSeasonalForest => TemperateSeasonalForest,
                BiomeType.TemperateRainforest => TemperateRainforest,
                BiomeType.BorealForest => BorealForest,
                BiomeType.Tundra => Tundra,
                BiomeType.Snow => Snow,
                BiomeType.Mangrove => Mangrove,
                _ => Ocean
            };
        }
    }

    /// <summary>
    /// 生物群系配置
    /// </summary>
    [Serializable]
    public partial class BiomeConfig : Resource
    {
        [Export] public int CostWaterCrossing { get; set; } = 1000;
        [Export] public int CostLakeCrossing { get; set; } = 100;
        [Export] public int CostRiverCrossing { get; set; } = 100;
        [Export] public int CostMountain { get; set; } = 2200;
        [Export] public int CostHills { get; set; } = 300;
        [Export] public int CostCoastline { get; set; } = 20;

        public int[] BiomeCosts { get; } = new int[13];

        public BiomeConfig()
        {
            InitializeCosts();
        }

        private void InitializeCosts()
        {
            BiomeCosts[(int)BiomeType.Ocean] = 0;
            BiomeCosts[(int)BiomeType.Lake] = 0;
            BiomeCosts[(int)BiomeType.TropicalDesert] = 10;
            BiomeCosts[(int)BiomeType.TemperateDesert] = 10;
            BiomeCosts[(int)BiomeType.ColdDesert] = 15;
            BiomeCosts[(int)BiomeType.TropicalRainforest] = 40;
            BiomeCosts[(int)BiomeType.TropicalSeasonalForest] = 30;
            BiomeCosts[(int)BiomeType.TemperateSeasonalForest] = 20;
            BiomeCosts[(int)BiomeType.TemperateRainforest] = 30;
            BiomeCosts[(int)BiomeType.BorealForest] = 10;
            BiomeCosts[(int)BiomeType.Tundra] = 5;
            BiomeCosts[(int)BiomeType.Snow] = 10;
            BiomeCosts[(int)BiomeType.Mangrove] = 20;
        }

        public int GetBiomeCost(BiomeType biome)
        {
            int id = (int)biome;
            if (id >= 0 && id < BiomeCosts.Length)
                return BiomeCosts[id];
            return 10;
        }

        public int GetBiomeCost(int biomeId)
        {
            if (biomeId >= 0 && biomeId < BiomeCosts.Length)
                return BiomeCosts[biomeId];
            return 10;
        }
    }
}
