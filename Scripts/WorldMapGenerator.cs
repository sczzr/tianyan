using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop
{
    /// <summary>
    /// 生物群系类型 - 对应不同的地形和气候
    /// </summary>
    public enum BiomeType
    {
        AridShrubland = 0,
        BorealForest = 1,
        ColdBog = 2,
        Desert = 3,
        ExtremeDesert = 4,
        IceSheet = 5,
        IceSheetOcean = 6,
        Ocean = 7,
        TemperateForest = 8,
        TemperateSwamp = 9,
        TropicalRainforest = 10,
        TropicalSwamp = 11,
        Tundra = 12
    }

    /// <summary>
    /// 生物群系数据
    /// </summary>
    public struct BiomeData
    {
        public BiomeType Type;
        public string Name;
        public string TexturePath;
        public float MinTemperature;
        public float MaxTemperature;
        public float MinRainfall;
        public float MaxRainfall;
        public float MinElevation;
        public Color MapColor;

        public BiomeData(BiomeType type, string name, string texturePath,
            float minTemp, float maxTemp, float minRain, float maxRain,
            float minElev, Color mapColor)
        {
            Type = type;
            Name = name;
            TexturePath = texturePath;
            MinTemperature = minTemp;
            MaxTemperature = maxTemp;
            MinRainfall = minRain;
            MaxRainfall = maxRain;
            MinElevation = minElev;
            MapColor = mapColor;
        }
    }

    /// <summary>
    /// 地图瓦片数据
    /// </summary>
    public struct MapTile
    {
        public BiomeType Biome;
        public float Elevation;
        public float Temperature;
        public float Rainfall;
        public float Fertility;

        public MapTile(BiomeType biome, float elevation, float temperature, float rainfall, float fertility)
        {
            Biome = biome;
            Elevation = elevation;
            Temperature = temperature;
            Rainfall = rainfall;
            Fertility = fertility;
        }
    }

    /// <summary>
    /// 世界地图生成器 - 使用 FastNoiseLite 生成类 RimWorld 风格的地形
    /// </summary>
    public partial class WorldMapGenerator : RefCounted
    {
        // 噪声生成器
        private FastNoiseLite _elevationNoise;
        private FastNoiseLite _temperatureNoise;
        private FastNoiseLite _rainfallNoise;
        private FastNoiseLite _fertilityNoise;
        private FastNoiseLite _variationNoise;

        // 地图参数
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }
        public int Seed { get; private set; }
        public float WaterLevel { get; set; } = 0.35f;
        public float TemperatureOffset { get; set; } = 0.0f;

        // 生成的地图数据
        public MapTile[,] MapTiles { get; private set; }

        // 生物群系定义
        public static readonly Dictionary<BiomeType, BiomeData> Biomes = new()
        {
            { BiomeType.Ocean, new BiomeData(BiomeType.Ocean, "海洋",
                "res://Assets/Textures/World/biomes/Ocean.png",
                0.0f, 1.0f, 0.0f, 1.0f, 0.0f, new Color(0.1f, 0.3f, 0.6f)) },

            { BiomeType.IceSheetOcean, new BiomeData(BiomeType.IceSheetOcean, "冰架海洋",
                "res://Assets/Textures/World/biomes/IceSheetOcean.png",
                0.0f, 0.3f, 0.0f, 1.0f, 0.0f, new Color(0.7f, 0.85f, 0.95f)) },

            { BiomeType.IceSheet, new BiomeData(BiomeType.IceSheet, "冰架",
                "res://Assets/Textures/World/biomes/IceSheet.png",
                0.0f, 0.25f, 0.0f, 1.0f, 0.35f, new Color(0.9f, 0.95f, 1.0f)) },

            { BiomeType.Tundra, new BiomeData(BiomeType.Tundra, "苔原",
                "res://Assets/Textures/World/biomes/Tundra.png",
                0.0f, 0.35f, 0.0f, 0.6f, 0.35f, new Color(0.6f, 0.7f, 0.75f)) },

            { BiomeType.ColdBog, new BiomeData(BiomeType.ColdBog, "寒冷沼泽",
                "res://Assets/Textures/World/biomes/ColdBog.png",
                0.0f, 0.4f, 0.6f, 1.0f, 0.35f, new Color(0.5f, 0.6f, 0.65f)) },

            { BiomeType.BorealForest, new BiomeData(BiomeType.BorealForest, "北方针叶林",
                "res://Assets/Textures/World/biomes/BorealForest.png",
                0.1f, 0.45f, 0.3f, 0.8f, 0.35f, new Color(0.2f, 0.4f, 0.3f)) },

            { BiomeType.TemperateForest, new BiomeData(BiomeType.TemperateForest, "温带森林",
                "res://Assets/Textures/World/biomes/TemperateForest.png",
                0.35f, 0.65f, 0.3f, 0.8f, 0.35f, new Color(0.2f, 0.6f, 0.2f)) },

            { BiomeType.TemperateSwamp, new BiomeData(BiomeType.TemperateSwamp, "温带沼泽",
                "res://Assets/Textures/World/biomes/TemperateSwamp.png",
                0.4f, 0.7f, 0.7f, 1.0f, 0.35f, new Color(0.3f, 0.5f, 0.3f)) },

            { BiomeType.AridShrubland, new BiomeData(BiomeType.AridShrubland, "干旱灌木地",
                "res://Assets/Textures/World/biomes/AridShrubland.png",
                0.5f, 0.85f, 0.0f, 0.4f, 0.35f, new Color(0.7f, 0.6f, 0.3f)) },

            { BiomeType.Desert, new BiomeData(BiomeType.Desert, "沙漠",
                "res://Assets/Textures/World/biomes/Desert.png",
                0.6f, 0.95f, 0.0f, 0.3f, 0.35f, new Color(0.9f, 0.8f, 0.5f)) },

            { BiomeType.ExtremeDesert, new BiomeData(BiomeType.ExtremeDesert, "极端沙漠",
                "res://Assets/Textures/World/biomes/ExtremeDesert.png",
                0.7f, 1.0f, 0.0f, 0.2f, 0.35f, new Color(1.0f, 0.9f, 0.6f)) },

            { BiomeType.TropicalRainforest, new BiomeData(BiomeType.TropicalRainforest, "热带雨林",
                "res://Assets/Textures/World/biomes/TropicalRainforest.png",
                0.7f, 1.0f, 0.6f, 1.0f, 0.35f, new Color(0.1f, 0.5f, 0.1f)) },

            { BiomeType.TropicalSwamp, new BiomeData(BiomeType.TropicalSwamp, "热带沼泽",
                "res://Assets/Textures/World/biomes/TropicalSwamp.png",
                0.75f, 1.0f, 0.8f, 1.0f, 0.35f, new Color(0.2f, 0.4f, 0.2f)) }
        };

        public WorldMapGenerator(int width = 256, int height = 256, int seed = -1)
        {
            MapWidth = width;
            MapHeight = height;
            Seed = seed == -1 ? (int)Time.GetUnixTimeFromSystem() : seed;

            InitializeNoiseGenerators();
        }

        /// <summary>
        /// 初始化噪声生成器
        /// </summary>
        private void InitializeNoiseGenerators()
        {
            // 海拔噪声 - 使用分形布朗运动创建自然地形
            _elevationNoise = new FastNoiseLite();
            _elevationNoise.Seed = Seed;
            _elevationNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _elevationNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _elevationNoise.FractalOctaves = 6;
            _elevationNoise.FractalLacunarity = 2.0f;
            _elevationNoise.FractalGain = 0.5f;
            _elevationNoise.Frequency = 0.008f;

            // 温度噪声 - 随纬度变化 + 局部变化
            _temperatureNoise = new FastNoiseLite();
            _temperatureNoise.Seed = Seed + 1;
            _temperatureNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _temperatureNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _temperatureNoise.FractalOctaves = 4;
            _temperatureNoise.Frequency = 0.005f;

            // 降雨量噪声
            _rainfallNoise = new FastNoiseLite();
            _rainfallNoise.Seed = Seed + 2;
            _rainfallNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _rainfallNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _rainfallNoise.FractalOctaves = 5;
            _rainfallNoise.Frequency = 0.006f;

            // 肥沃度噪声
            _fertilityNoise = new FastNoiseLite();
            _fertilityNoise.Seed = Seed + 3;
            _fertilityNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _fertilityNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _fertilityNoise.FractalOctaves = 3;
            _fertilityNoise.Frequency = 0.01f;

            // 变化噪声 - 用于添加微观变化
            _variationNoise = new FastNoiseLite();
            _variationNoise.Seed = Seed + 4;
            _variationNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular;
            _variationNoise.Frequency = 0.02f;
        }

        /// <summary>
        /// 生成完整的地图
        /// </summary>
        public void GenerateMap()
        {
            MapTiles = new MapTile[MapWidth, MapHeight];

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    MapTiles[x, y] = GenerateTile(x, y);
                }
            }

            GD.Print($"地图生成完成: {MapWidth}x{MapHeight}, 种子: {Seed}");
        }

        /// <summary>
        /// 生成单个瓦片的数据
        /// </summary>
        private MapTile GenerateTile(int x, int y)
        {
            // 获取基础噪声值
            float elevationNoise = _elevationNoise.GetNoise2D(x, y) * 0.5f + 0.5f;
            float tempNoise = _temperatureNoise.GetNoise2D(x, y) * 0.5f + 0.5f;
            float rainNoise = _rainfallNoise.GetNoise2D(x, y) * 0.5f + 0.5f;
            float fertilityNoise = _fertilityNoise.GetNoise2D(x, y) * 0.5f + 0.5f;
            float variation = _variationNoise.GetNoise2D(x, y) * 0.5f + 0.5f;

            // 应用变化
            elevationNoise = ApplyVariation(elevationNoise, variation, 0.1f);

            // 计算纬度对温度的影响 (y轴代表南北)
            float latitudeEffect = Mathf.Abs((float)y / MapHeight - 0.5f) * 2.0f;
            float baseTemperature = 1.0f - latitudeEffect * 0.8f;
            float temperature = Mathf.Clamp(baseTemperature + (tempNoise - 0.5f) * 0.4f, 0.0f, 1.0f);

            // 降雨量受海洋距离和纬度的影响
            float rainfall = rainNoise;
            if (elevationNoise < WaterLevel)
            {
                rainfall = Mathf.Max(rainfall, 0.6f);
            }

            // 确定生物群系
            BiomeType biome = DetermineBiome(elevationNoise, temperature, rainfall);

            // 计算肥沃度
            float fertility = CalculateFertility(biome, fertilityNoise, elevationNoise, rainfall);

            return new MapTile(biome, elevationNoise, temperature, rainfall, fertility);
        }

        private float ApplyVariation(float baseValue, float variation, float strength)
        {
            return Mathf.Clamp(baseValue + (variation - 0.5f) * strength, 0.0f, 1.0f);
        }

        private BiomeType DetermineBiome(float elevation, float temperature, float rainfall)
        {
            // 水域
            if (elevation < WaterLevel)
            {
                if (temperature < 0.15f)
                    return BiomeType.IceSheetOcean;
                return BiomeType.Ocean;
            }

            // 冰架
            if (elevation > WaterLevel + 0.05f && temperature < 0.1f)
                return BiomeType.IceSheet;

            // 苔原
            if (temperature < 0.25f && rainfall < 0.5f)
                return BiomeType.Tundra;

            // 寒冷沼泽
            if (temperature < 0.3f && rainfall > 0.6f)
                return BiomeType.ColdBog;

            // 北方针叶林
            if (temperature < 0.4f && rainfall > 0.3f)
                return BiomeType.BorealForest;

            // 温带森林
            if (temperature >= 0.35f && temperature < 0.65f && rainfall > 0.35f && rainfall < 0.75f)
                return BiomeType.TemperateForest;

            // 温带沼泽
            if (temperature >= 0.35f && temperature < 0.7f && rainfall >= 0.7f)
                return BiomeType.TemperateSwamp;

            // 干旱灌木地
            if (temperature >= 0.4f && temperature < 0.75f && rainfall < 0.35f)
                return BiomeType.AridShrubland;

            // 沙漠
            if (temperature >= 0.5f && temperature < 0.8f && rainfall < 0.2f)
                return BiomeType.Desert;

            // 极端沙漠
            if (temperature >= 0.7f && rainfall < 0.15f)
                return BiomeType.ExtremeDesert;

            // 热带雨林
            if (temperature >= 0.65f && rainfall >= 0.6f)
                return BiomeType.TropicalRainforest;

            // 热带沼泽
            if (temperature >= 0.7f && rainfall >= 0.75f)
                return BiomeType.TropicalSwamp;

            return BiomeType.TemperateForest;
        }

        private float CalculateFertility(BiomeType biome, float noise, float elevation, float rainfall)
        {
            float baseFertility = noise;

            baseFertility *= biome switch
            {
                BiomeType.TropicalRainforest => 1.2f,
                BiomeType.TemperateForest => 1.0f,
                BiomeType.BorealForest => 0.7f,
                BiomeType.TemperateSwamp => 0.9f,
                BiomeType.TropicalSwamp => 0.85f,
                BiomeType.ColdBog => 0.5f,
                BiomeType.AridShrubland => 0.4f,
                BiomeType.Desert or BiomeType.ExtremeDesert => 0.1f,
                BiomeType.Tundra => 0.2f,
                BiomeType.IceSheet => 0.0f,
                _ => 0.5f
            };

            if (elevation < WaterLevel)
                baseFertility = 0.0f;
            else
                baseFertility *= Mathf.Clamp(rainfall + 0.3f, 0.3f, 1.2f);

            return Mathf.Clamp(baseFertility, 0.0f, 1.0f);
        }

        public void Regenerate(int newSeed = -1)
        {
            if (newSeed != -1)
            {
                Seed = newSeed;
            }
            else
            {
                Seed = (int)Time.GetUnixTimeFromSystem();
            }

            InitializeNoiseGenerators();
            GenerateMap();
        }

        public MapTile GetTile(int x, int y)
        {
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
                return new MapTile(BiomeType.Ocean, 0, 0, 0, 0);
            return MapTiles[x, y];
        }
    }
}
