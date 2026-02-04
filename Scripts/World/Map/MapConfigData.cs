using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop.World.Map
{
    /// <summary>
    /// 种子转换工具 - 支持中文、英文、数字
    /// </summary>
    public static class SeedConverter
    {
        public static int StringToSeed(string text)
        {
            if (string.IsNullOrEmpty(text))
                return -1;

            int hashValue = 0;
            foreach (char c in text)
            {
                int charCode = (int)c;
                hashValue = (hashValue * 31 + charCode) & 0x7FFFFFFF;
            }
            return hashValue;
        }

        public static bool IsValidSeedText(string text)
        {
            return !string.IsNullOrEmpty(text?.Trim());
        }
    }

    /// <summary>
    /// 地图生成参数配置
    /// </summary>
    public class MapGeneratorConfig
    {
        public string ProvinceName { get; set; } = null;
        public int Seed { get; set; } = -1;
        public string SeedText { get; set; } = "";
        public int MapWidth { get; set; } = 256;
        public int MapHeight { get; set; } = 256;
        public float ElevationScale { get; set; } = 1.0f;
        public float TemperatureScale { get; set; } = 1.0f;
        public float RainfallScale { get; set; } = 1.0f;
        public float LakeDensity { get; set; } = 1.0f;
        public float ForestDensity { get; set; } = 1.0f;
        public float DesertDensity { get; set; } = 1.0f;
        public bool UseRandomSeed { get; set; } = true;

        public int GetFinalSeed()
        {
            if (UseRandomSeed || Seed < 0)
            {
                return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            return Seed;
        }
    }
}
