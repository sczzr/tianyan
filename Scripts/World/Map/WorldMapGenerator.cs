using Godot;
using System;
using System.Collections.Generic;

using System.Threading.Tasks;
using TianYanShop.World.Config;

namespace TianYanShop.World.Map
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
        public float Spirit;
        public bool HasSpiritVein;
        public bool HasSpecialRegion;
        public SpecialRegionType SpecialRegionType;
        // 混合参数 - 用于平滑过渡
        public float BlendFactor;
        public BiomeType SecondaryBiome;

        public MapTile(BiomeType biome, float elevation, float temperature, float rainfall, float fertility,
            float spirit = 0.5f, bool hasSpiritVein = false, bool hasSpecialRegion = false,
            SpecialRegionType specialRegionType = SpecialRegionType.None,
            float blendFactor = 0f, BiomeType secondaryBiome = BiomeType.Ocean)
        {
            Biome = biome;
            Elevation = elevation;
            Temperature = temperature;
            Rainfall = rainfall;
            Fertility = fertility;
            Spirit = spirit;
            HasSpiritVein = hasSpiritVein;
            HasSpecialRegion = hasSpecialRegion;
            SpecialRegionType = specialRegionType;
            BlendFactor = blendFactor;
            SecondaryBiome = secondaryBiome;
        }
    }

    /// <summary>
    /// 生物群系候选 - 用于过渡计算
    /// </summary>
    public struct BiomeCandidate
    {
        public BiomeType Type;
        public float Score;

        public BiomeCandidate(BiomeType type, float score)
        {
            Type = type;
            Score = score;
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
        private FastNoiseLite _spiritNoise;

        // 地图参数
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }
        public int Seed { get; private set; }
        public float WaterLevel { get; set; } = 0.35f;

        // 省份配置
        private ChinaProvinceConfig _provinceConfig;
        public string ProvinceName { get; private set; } = "";

        // 自定义地形参数
        public float BaseTemperature { get; set; } = 0.5f;      // 基础温度 (0.0-1.0)
        public float BasePrecipitation { get; set; } = 0.5f;    // 基础降水量 (0.0-1.0)
        public float Continentality { get; set; } = 0.5f;       // 大陆度 (0.0-1.0)，越高海洋影响越小
        public float ElevationVariation { get; set; } = 1.0f;    // 海拔变异度 (0.0-2.0)
        public float BaseSpiritDensity { get; set; } = 0.5f;   // 基础灵气浓郁度 (0.0-1.0)

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

        public WorldMapGenerator(int width = 256, int height = 256, int seed = -1, ChinaProvinceConfig provinceConfig = null)
        {
            MapWidth = width;
            MapHeight = height;
            Seed = seed == -1 ? (int)Time.GetUnixTimeFromSystem() : seed;
            _provinceConfig = provinceConfig;

            InitializeNoiseGenerators();
        }

        /// <summary>
        /// 设置省份配置并重新生成地图
        /// </summary>
        public void SetProvinceConfig(ChinaProvinceConfig config)
        {
            _provinceConfig = config;
            ProvinceName = config?.Name ?? "";
            if (_provinceConfig != null)
            {
                // 应用省份特定的参数设置
                WaterLevel = _provinceConfig.OceanThreshold;
                BaseTemperature = _provinceConfig.BaseTemperature;
                BasePrecipitation = _provinceConfig.BaseRainfall;
                Continentality = 0.5f; // 使用默认值
                ElevationVariation = 1.0f; // 使用默认值
                BaseSpiritDensity = _provinceConfig.SpiritDensity;
            }
            GenerateMap();
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

            // 灵气噪声 - 用于生成灵气分布
            _spiritNoise = new FastNoiseLite();
            _spiritNoise.Seed = Seed + 100;
            _spiritNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _spiritNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _spiritNoise.FractalOctaves = 5;
            _spiritNoise.Frequency = 0.012f;
        }

        /// <summary>
        /// 生成完整的地图
        /// </summary>
        public void GenerateMap()
        {
            MapTiles = new MapTile[MapWidth, MapHeight];
            
            GD.Print($"开始多线程生成地图: {MapWidth}x{MapHeight}...");

            int tileCount = MapWidth * MapHeight;
            
            // 使用多线程并行生成
            Parallel.For(0, tileCount, i =>
            {
                int x = i % MapWidth;
                int y = i / MapWidth;
                MapTiles[x, y] = GenerateTile(x, y);
            });

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

            // 应用海拔变异度
            float elevationVariation = ElevationVariation;
            elevationNoise = ApplyVariation(elevationNoise, variation, 0.1f * elevationVariation);

            // 计算纬度对温度的影响 (y轴代表南北)
            float latitudeEffect = Mathf.Abs((float)y / MapHeight - 0.5f) * 2.0f;
            
            // 使用基础温度参数调整
            float baseTemperature = 1.0f - latitudeEffect * 0.8f;
            // 将 BaseTemperature (0-1) 映射到合理的温度范围
            // BaseTemperature=0.5 时温度适中，0 时偏冷，1 时偏热
            baseTemperature = Mathf.Lerp(baseTemperature, Mathf.Clamp(BaseTemperature + (tempNoise - 0.5f) * 0.3f, 0.0f, 1.0f), 0.5f);
            float temperature = Mathf.Clamp(baseTemperature + (tempNoise - 0.5f) * 0.3f, 0.0f, 1.0f);

            // 降水量计算 - 受海洋距离和大陆度影响
            float rainfall = rainNoise;
            
            // 大陆度影响：远离赤道和海洋的地方更干燥
            float continentalEffect = Continentality * 0.4f;
            rainfall = Mathf.Lerp(rainfall, rainfall * (1.0f - continentalEffect), 0.5f);
            
            // 应用基础降水量参数
            rainfall = Mathf.Lerp(rainfall, BasePrecipitation + (rainNoise - 0.5f) * 0.3f, 0.5f);
            
            // 海洋增加湿度
            if (elevationNoise < WaterLevel)
            {
                rainfall = Mathf.Max(rainfall, 0.6f);
            }

            // 应用省份配置的修改
            if (_provinceConfig != null)
            {
                // 应用省份特定的温度、降雨量和海拔修改
                float modifiedTemp = _provinceConfig.GetModifiedTemperature(tempNoise, x, y);
                float modifiedRain = _provinceConfig.GetModifiedRainfall(rainNoise, x, y);
                float modifiedElev = _provinceConfig.GetModifiedElevation(elevationNoise, x, y);

                // 放大省份配置的影响 - 权重提高到 0.7
                temperature = Mathf.Lerp(temperature, modifiedTemp, 0.7f);
                rainfall = Mathf.Lerp(rainfall, modifiedRain, 0.7f);
                elevationNoise = Mathf.Lerp(elevationNoise, modifiedElev, 0.7f);

                // 根据省份参数进行更强烈的生物群系调整
                // 森林覆盖率 - 极强影响
                if (_provinceConfig.ForestRatio > 0.3f)
                {
                    float forestBoost = (_provinceConfig.ForestRatio - 0.3f) * 0.8f;
                    rainfall = Mathf.Lerp(rainfall, Mathf.Min(rainfall + 0.4f, 1.0f), forestBoost);
                }
                else if (_provinceConfig.ForestRatio < 0.2f)
                {
                    float forestPenalty = (0.2f - _provinceConfig.ForestRatio) * 0.6f;
                    rainfall = Mathf.Lerp(rainfall, Mathf.Max(rainfall - 0.3f, 0.0f), forestPenalty);
                }

                // 沙漠覆盖率 - 极强影响
                if (_provinceConfig.DesertRatio > 0.15f)
                {
                    float desertStrength = (_provinceConfig.DesertRatio - 0.15f) * 1.2f;
                    rainfall = Mathf.Lerp(rainfall, Mathf.Max(rainfall - 0.5f, 0.0f), desertStrength);
                }

                // 山脉覆盖率 - 极强影响
                if (_provinceConfig.MountainRatio > 0.25f)
                {
                    float mountainStrength = (_provinceConfig.MountainRatio - 0.25f) * 1.0f;
                    elevationNoise = Mathf.Lerp(elevationNoise, Mathf.Min(elevationNoise + 0.25f, 1.0f), mountainStrength);
                }
                else if (_provinceConfig.MountainRatio < 0.15f)
                {
                    float mountainReduction = (0.15f - _provinceConfig.MountainRatio) * 0.8f;
                    elevationNoise = Mathf.Lerp(elevationNoise, Mathf.Max(elevationNoise - 0.2f, 0.0f), mountainReduction);
                }

                // 平原覆盖率 - 极强影响
                if (_provinceConfig.PlainRatio > 0.4f)
                {
                    float plainStrength = (_provinceConfig.PlainRatio - 0.4f) * 0.8f;
                    elevationNoise = Mathf.Lerp(elevationNoise, Mathf.Max(elevationNoise - 0.15f, 0.0f), plainStrength);
                }
            }

            // 确定生物群系（带平滑过渡）
            var (biome, blendFactor, secondaryBiome) = DetermineBiomeWithBlend(elevationNoise, temperature, rainfall);

            // 计算肥沃度（考虑混合）
            float fertility = CalculateFertilityWithBlend(biome, secondaryBiome, blendFactor,
                fertilityNoise, elevationNoise, rainfall);

            // 计算灵气值
            float spirit = CalculateSpirit(elevationNoise, temperature, rainfall, fertilityNoise, x, y);

            // 检查是否为特殊区域
            bool hasSpecialRegion = false;
            SpecialRegionType specialType = SpecialRegionType.None;
            if (_provinceConfig != null)
            {
                // 根据省份类型生成特殊区域
                hasSpecialRegion = CheckSpecialRegion(x, y, (SpecialRegionType)_provinceConfig.SpecialRegionType);
                if (hasSpecialRegion)
                {
                    specialType = (SpecialRegionType)_provinceConfig.SpecialRegionType;
                }
            }

            // 检查是否为灵脉
            bool hasSpiritVein = CheckSpiritVein(x, y);

            return new MapTile(biome, elevationNoise, temperature, rainfall, fertility,
                spirit, hasSpiritVein, hasSpecialRegion, specialType, blendFactor, secondaryBiome);
        }

        /// <summary>
        /// 计算灵气值
        /// </summary>
        private float CalculateSpirit(float elevation, float temperature, float rainfall, float noise, int x, int y)
        {
            float baseSpirit = _spiritNoise.GetNoise2D(x, y) * 0.5f + 0.5f;

            // 地形对灵气的影响
            float elevationEffect = elevation > WaterLevel ? elevation : 0.3f;
            elevationEffect = Mathf.Pow(elevationEffect, 1.5f);

            // 温度对灵气的影响（温和温度最适合）
            float tempEffect = 1.0f - Mathf.Abs(temperature - 0.6f) * 2.0f;
            tempEffect = Mathf.Clamp(tempEffect, 0.0f, 1.0f);

            // 降雨量对灵气的影响（适中最适合）
            float rainEffect = 1.0f - Mathf.Abs(rainfall - 0.5f) * 2.0f;
            rainEffect = Mathf.Clamp(rainEffect, 0.0f, 1.0f);

            // 灵气密度修正
            float spiritDensity = BaseSpiritDensity;
            if (_provinceConfig != null)
            {
                spiritDensity = _provinceConfig.SpiritDensity;
            }

            // 综合计算
            float spirit = baseSpirit * 0.2f + elevationEffect * 0.3f + tempEffect * 0.15f + rainEffect * 0.15f + spiritDensity * 0.2f;

            return Mathf.Clamp(spirit, 0.0f, 1.0f);
        }

        /// <summary>
        /// 检查是否为特殊区域
        /// </summary>
        private bool CheckSpecialRegion(int x, int y, SpecialRegionType type)
        {
            if (type == SpecialRegionType.None) return false;

            // 根据区域类型设置不同的检查条件
            float threshold = type switch
            {
                SpecialRegionType.SacredMountain => 0.85f,
                SpecialRegionType.ForbiddenLand => 0.90f,
                SpecialRegionType.AncientBattlefield => 0.75f,
                SpecialRegionType.SpiritValley => 0.80f,
                SpecialRegionType.DragonLair => 0.70f,
                SpecialRegionType.FairyResidence => 0.78f,
                SpecialRegionType.DemonicRealm => 0.82f,
                SpecialRegionType.AncientTomb => 0.72f,
                SpecialRegionType.SpiritForest => 0.76f,
                SpecialRegionType.FloatingIsland => 0.88f,
                _ => 0.70f
            };

            float noise = _spiritNoise.GetNoise2D(x * 10, y * 10) * 0.5f + 0.5f;
            return noise > threshold;
        }

        /// <summary>
        /// 检查是否为灵脉
        /// </summary>
        private bool CheckSpiritVein(int x, int y)
        {
            float veinNoise = _spiritNoise.GetNoise2D(x * 3, y * 3) * 0.5f + 0.5f;
            return veinNoise > 0.88f;
        }

        private float ApplyVariation(float baseValue, float variation, float strength)
        {
            return Mathf.Clamp(baseValue + (variation - 0.5f) * strength, 0.0f, 1.0f);
        }

        /// <summary>
        /// 确定生物群系并计算混合参数（用于平滑过渡）
        /// </summary>
        private (BiomeType primary, float blendFactor, BiomeType secondary) DetermineBiomeWithBlend(
            float elevation, float temperature, float rainfall)
        {
            // 水域特殊处理（不需要过渡）
            if (elevation < WaterLevel)
            {
                if (temperature < 0.15f)
                    return (BiomeType.IceSheetOcean, 0f, BiomeType.IceSheetOcean);
                return (BiomeType.Ocean, 0f, BiomeType.Ocean);
            }

            // 计算所有生物群系的匹配分数
            var candidates = CalculateBiomeScores(elevation, temperature, rainfall);

            // 如果没有候选，默认温带森林
            if (candidates.Count == 0)
                return (BiomeType.TemperateForest, 0f, BiomeType.TemperateForest);

            // 只有一个候选，无需混合
            if (candidates.Count == 1)
                return (candidates[0].Type, 0f, candidates[0].Type);

            // 计算混合因子（基于前两个候选的分数差异）
            float primaryScore = candidates[0].Score;
            float secondaryScore = candidates[1].Score;
            float totalScore = primaryScore + secondaryScore;

            // 混合因子：0 = 完全主群系, 1 = 完全次群系
            float blendFactor = secondaryScore / totalScore;

            // 只在分数接近时才启用混合（避免所有边界都模糊）
            float scoreRatio = secondaryScore / primaryScore;
            if (scoreRatio < 0.6f)
            {
                // 主次差异太大，不混合
                blendFactor = 0f;
            }
            else
            {
                // 平滑过渡混合因子
                blendFactor = Mathf.SmoothStep(0f, 1f, blendFactor);
            }

            return (candidates[0].Type, blendFactor, candidates[1].Type);
        }

        /// <summary>
        /// 计算所有生物群系对当前条件的匹配分数
        /// </summary>
        private List<BiomeCandidate> CalculateBiomeScores(float elevation, float temperature, float rainfall)
        {
            var candidates = new List<BiomeCandidate>();

            // 冰架
            float iceSheetScore = ScoreBiome(elevation > WaterLevel + 0.05f && temperature < 0.1f,
                new[] { temperature < 0.1f ? 1f : 0f, 1f - temperature * 10f });
            if (iceSheetScore > 0) candidates.Add(new BiomeCandidate(BiomeType.IceSheet, iceSheetScore));

            // 苔原
            float tundraScore = ScoreBiome(temperature < 0.25f && rainfall < 0.5f,
                new[] { 1f - temperature * 4f, (0.5f - rainfall) * 2f });
            if (tundraScore > 0) candidates.Add(new BiomeCandidate(BiomeType.Tundra, tundraScore));

            // 寒冷沼泽
            float coldBogScore = ScoreBiome(temperature < 0.3f && rainfall > 0.6f,
                new[] { 1f - temperature * 3.33f, (rainfall - 0.6f) * 2.5f });
            if (coldBogScore > 0) candidates.Add(new BiomeCandidate(BiomeType.ColdBog, coldBogScore));

            // 北方针叶林
            float borealScore = ScoreBiome(temperature < 0.4f && rainfall > 0.3f,
                new[] { 1f - temperature * 2.5f, (rainfall - 0.3f) * 1.43f });
            if (borealScore > 0) candidates.Add(new BiomeCandidate(BiomeType.BorealForest, borealScore));

            // 温带森林
            float temperateForestScore = ScoreBiome(
                temperature >= 0.35f && temperature < 0.65f && rainfall > 0.35f && rainfall < 0.75f,
                new[] { 1f - Mathf.Abs(temperature - 0.5f) * 3.33f, 1f - Mathf.Abs(rainfall - 0.55f) * 2.5f });
            if (temperateForestScore > 0) candidates.Add(new BiomeCandidate(BiomeType.TemperateForest, temperateForestScore));

            // 温带沼泽
            float temperateSwampScore = ScoreBiome(temperature >= 0.35f && temperature < 0.7f && rainfall >= 0.7f,
                new[] { 1f - Mathf.Abs(temperature - 0.525f) * 2.86f, (rainfall - 0.7f) * 3.33f });
            if (temperateSwampScore > 0) candidates.Add(new BiomeCandidate(BiomeType.TemperateSwamp, temperateSwampScore));

            // 干旱灌木地
            float aridScore = ScoreBiome(temperature >= 0.4f && temperature < 0.75f && rainfall < 0.35f,
                new[] { 1f - Mathf.Abs(temperature - 0.575f) * 2.86f, (0.35f - rainfall) * 2.86f });
            if (aridScore > 0) candidates.Add(new BiomeCandidate(BiomeType.AridShrubland, aridScore));

            // 沙漠
            float desertScore = ScoreBiome(temperature >= 0.5f && temperature < 0.8f && rainfall < 0.2f,
                new[] { 1f - Mathf.Abs(temperature - 0.65f) * 3.33f, (0.2f - rainfall) * 5f });
            if (desertScore > 0) candidates.Add(new BiomeCandidate(BiomeType.Desert, desertScore));

            // 极端沙漠
            float extremeDesertScore = ScoreBiome(temperature >= 0.7f && rainfall < 0.15f,
                new[] { (temperature - 0.7f) * 3.33f, (0.15f - rainfall) * 6.67f });
            if (extremeDesertScore > 0) candidates.Add(new BiomeCandidate(BiomeType.ExtremeDesert, extremeDesertScore));

            // 热带雨林
            float tropicalRainforestScore = ScoreBiome(temperature >= 0.65f && rainfall >= 0.6f,
                new[] { (temperature - 0.65f) * 2.86f, (rainfall - 0.6f) * 2.5f });
            if (tropicalRainforestScore > 0) candidates.Add(new BiomeCandidate(BiomeType.TropicalRainforest, tropicalRainforestScore));

            // 热带沼泽
            float tropicalSwampScore = ScoreBiome(temperature >= 0.7f && rainfall >= 0.75f,
                new[] { (temperature - 0.7f) * 3.33f, (rainfall - 0.75f) * 4f });
            if (tropicalSwampScore > 0) candidates.Add(new BiomeCandidate(BiomeType.TropicalSwamp, tropicalSwampScore));

            // 应用省份地形类型的强烈偏好
            if (_provinceConfig != null)
            {
                ApplyProvinceTerrainBias(candidates);
            }

            // 按分数排序（降序）
            candidates.Sort((a, b) => b.Score.CompareTo(a.Score));

            return candidates;
        }

        /// <summary>
        /// 根据省份地形类型对生物群系分数进行强烈调整
        /// </summary>
        private void ApplyProvinceTerrainBias(List<BiomeCandidate> candidates)
        {
            float biasMultiplier = 2.0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                float multiplier = 1.0f;

                switch (_provinceConfig.TerrainType)
                {
                    case ProvinceTerrainType.Plateau:
                        if (candidate.Type == BiomeType.IceSheet ||
                            candidate.Type == BiomeType.Tundra ||
                            candidate.Type == BiomeType.BorealForest ||
                            candidate.Type == BiomeType.ColdBog)
                        {
                            multiplier = biasMultiplier;
                        }
                        else if (candidate.Type == BiomeType.TropicalRainforest ||
                                 candidate.Type == BiomeType.TropicalSwamp ||
                                 candidate.Type == BiomeType.Desert)
                        {
                            multiplier = 0.1f;
                        }
                        break;

                    case ProvinceTerrainType.Mountain:
                        if (candidate.Type == BiomeType.BorealForest ||
                            candidate.Type == BiomeType.TemperateForest ||
                            candidate.Type == BiomeType.TemperateSwamp ||
                            candidate.Type == BiomeType.TropicalRainforest)
                        {
                            multiplier = biasMultiplier;
                        }
                        else if (candidate.Type == BiomeType.Desert ||
                                 candidate.Type == BiomeType.ExtremeDesert ||
                                 candidate.Type == BiomeType.IceSheet ||
                                 candidate.Type == BiomeType.Tundra)
                        {
                            multiplier = 0.15f;
                        }
                        break;

                    case ProvinceTerrainType.Plain:
                        if (candidate.Type == BiomeType.TemperateForest ||
                            candidate.Type == BiomeType.AridShrubland)
                        {
                            multiplier = biasMultiplier;
                        }
                        else if (candidate.Type == BiomeType.BorealForest ||
                                 candidate.Type == BiomeType.TropicalRainforest ||
                                 candidate.Type == BiomeType.IceSheet ||
                                 candidate.Type == BiomeType.Tundra ||
                                 candidate.Type == BiomeType.ColdBog)
                        {
                            multiplier = 0.15f;
                        }
                        break;

                    case ProvinceTerrainType.Coastal:
                        if (candidate.Type == BiomeType.TemperateForest ||
                            candidate.Type == BiomeType.TemperateSwamp)
                        {
                            multiplier = biasMultiplier;
                        }
                        break;

                    case ProvinceTerrainType.Basin:
                        if (candidate.Type == BiomeType.Desert ||
                            candidate.Type == BiomeType.ExtremeDesert ||
                            candidate.Type == BiomeType.AridShrubland)
                        {
                            multiplier = biasMultiplier * 1.5f;
                        }
                        else if (candidate.Type == BiomeType.IceSheet ||
                                 candidate.Type == BiomeType.Tundra)
                        {
                            multiplier = 0.05f;
                        }
                        break;

                    case ProvinceTerrainType.Mixed:
                    default:
                        multiplier = 1.0f;
                        break;
                }

                candidates[i] = new BiomeCandidate(candidate.Type, candidate.Score * multiplier);
            }
        }

        /// <summary>
        /// 计算生物群系的匹配分数
        /// </summary>
        private float ScoreBiome(bool baseCondition, float[] factors)
        {
            if (!baseCondition) return 0f;

            float score = 1f;
            foreach (var factor in factors)
            {
                score *= Mathf.Clamp(factor, 0f, 1f);
            }
            return score;
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

        /// <summary>
        /// 计算混合区域的肥沃度
        /// </summary>
        private float CalculateFertilityWithBlend(BiomeType primaryBiome, BiomeType secondaryBiome,
            float blendFactor, float noise, float elevation, float rainfall)
        {
            // 如果无混合，使用标准计算
            if (blendFactor <= 0.01f || primaryBiome == secondaryBiome)
                return CalculateFertility(primaryBiome, noise, elevation, rainfall);

            // 计算两个群系的肥沃度
            float primaryFertility = CalculateFertility(primaryBiome, noise, elevation, rainfall);
            float secondaryFertility = CalculateFertility(secondaryBiome, noise, elevation, rainfall);

            // 使用平滑步进进行混合
            float smoothBlend = Mathf.SmoothStep(0f, 1f, blendFactor);
            return Mathf.Lerp(primaryFertility, secondaryFertility, smoothBlend);
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
            
            // 重新生成地图时重新应用省份配置
            if (_provinceConfig != null)
            {
                // 重新应用省份特定的参数设置
                WaterLevel = _provinceConfig.OceanThreshold;
                BaseTemperature = _provinceConfig.BaseTemperature;
                BasePrecipitation = _provinceConfig.BaseRainfall;
                Continentality = 0.5f;
                ElevationVariation = 1.0f;
                BaseSpiritDensity = _provinceConfig.SpiritDensity;
            }
            
            GenerateMap();
        }

        public MapTile GetTile(int x, int y)
        {
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
                return new MapTile(BiomeType.Ocean, 0, 0, 0, 0, 0.5f, false, false, SpecialRegionType.None);
            return MapTiles[x, y];
        }

        /// <summary>
        /// 获取灵气分布图（用于宗门生成等系统）
        /// </summary>
        public float[,] GetSpiritPowerMap()
        {
            float[,] spiritMap = new float[MapWidth, MapHeight];
            
            int tileCount = MapWidth * MapHeight;
            
            // 使用多线程并行复制
            Parallel.For(0, tileCount, i =>
            {
                int x = i % MapWidth;
                int y = i / MapWidth;
                spiritMap[x, y] = MapTiles[x, y].Spirit;
            });
            
            return spiritMap;
        }
    }
}
