using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop.World.Map
{
    /// <summary>
    /// 生物群系纹理生成器 - 使用噪声算法程序化生成高分辨率地形纹理
    /// 为每种生物群系生成 512x512 或 1024x1024 的大纹理，支持世界空间映射
    /// </summary>
    public partial class BiomeTextureGenerator : RefCounted
    {
        [Export] public int TextureSize { get; set; } = 512;
        [Export] public int TileSize { get; set; } = 32;
        [Export] public int Seed { get; set; } = -1;
        [Export] public bool RandomSeed { get; set; } = true;

        private Dictionary<BiomeType, ImageTexture> _generatedTextures = new();
        private Dictionary<BiomeType, Image> _generatedImages = new();

        private FastNoiseLite _baseNoise;
        private FastNoiseLite _detailNoise;
        private FastNoiseLite _cloudNoise;

        public BiomeTextureGenerator()
        {
            Initialize(TextureSize, TileSize, -1);
        }

        public BiomeTextureGenerator(int textureSize = 512, int tileSize = 32, int seed = -1)
        {
            Initialize(textureSize, tileSize, seed);
        }

        private void Initialize(int textureSize, int tileSize, int seed)
        {
            TextureSize = textureSize;
            TileSize = tileSize;

            if (RandomSeed || seed == -1)
            {
                Seed = (int)Time.GetUnixTimeFromSystem();
            }
            else
            {
                Seed = seed;
            }

            InitializeNoiseGenerators();
        }

        private void InitializeNoiseGenerators()
        {
            _baseNoise = new FastNoiseLite();
            _baseNoise.Seed = Seed;
            _baseNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _baseNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _baseNoise.FractalOctaves = 6;
            _baseNoise.FractalLacunarity = 2.0f;
            _baseNoise.FractalGain = 0.5f;
            _baseNoise.Frequency = 0.015f;

            _detailNoise = new FastNoiseLite();
            _detailNoise.Seed = Seed + 1000;
            _detailNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _detailNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _detailNoise.FractalOctaves = 4;
            _detailNoise.Frequency = 0.05f;

            _cloudNoise = new FastNoiseLite();
            _cloudNoise.Seed = Seed + 2000;
            _cloudNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _cloudNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _cloudNoise.FractalOctaves = 3;
            _cloudNoise.Frequency = 0.03f;
        }

        /// <summary>
        /// 获取指定生物群系的纹理（懒加载，必要时生成）
        /// </summary>
        public ImageTexture GetBiomeTexture(BiomeType biomeType)
        {
            if (_generatedTextures.TryGetValue(biomeType, out var existing))
            {
                return existing;
            }

            var texture = GenerateBiomeTexture(biomeType);
            _generatedTextures[biomeType] = texture;
            return texture;
        }

        /// <summary>
        /// 获取指定生物群系的图像（用于调试或高级操作）
        /// </summary>
        public Image GetBiomeImage(BiomeType biomeType)
        {
            if (_generatedImages.TryGetValue(biomeType, out var existing))
            {
                return existing;
            }

            var image = GenerateBiomeImage(biomeType);
            _generatedImages[biomeType] = image;
            return image;
        }

        /// <summary>
        /// 强制重新生成所有纹理
        /// </summary>
        public void RegenerateAll(int newSeed = -1)
        {
            if (newSeed != -1)
            {
                Seed = newSeed;
                RandomSeed = false;
            }
            else if (RandomSeed)
            {
                Seed = (int)Time.GetUnixTimeFromSystem();
            }

            InitializeNoiseGenerators();
            _generatedTextures.Clear();
            _generatedImages.Clear();
            GD.Print($"BiomeTextureGenerator: 使用新种子 {Seed} 重新生成所有纹理");
        }

        /// <summary>
        /// 预生成所有生物群系的纹理
        /// </summary>
        public void PreGenerateAll()
        {
            foreach (BiomeType biomeType in Enum.GetValues(typeof(BiomeType)))
            {
                GetBiomeTexture(biomeType);
            }
            GD.Print($"BiomeTextureGenerator: 预生成完成，共 {Enum.GetValues(typeof(BiomeType)).Length} 种生物群系");
        }

        /// <summary>
        /// 获取纹理网格宽度（纹理能分成多少个瓦片）
        /// </summary>
        public int GetTextureGridWidth()
        {
            return TextureSize / TileSize;
        }

        /// <summary>
        /// 获取纹理网格高度
        /// </summary>
        public int GetTextureGridHeight()
        {
            return TextureSize / TileSize;
        }

        private ImageTexture GenerateBiomeTexture(BiomeType biomeType)
        {
            var image = GenerateBiomeImage(biomeType);
            return ImageTexture.CreateFromImage(image);
        }

        private Image GenerateBiomeImage(BiomeType biomeType)
        {
            var image = Image.Create(TextureSize, TextureSize, false, Image.Format.Rgba8);

            for (int x = 0; x < TextureSize; x++)
            {
                for (int y = 0; y < TextureSize; y++)
                {
                    Color color = CalculateBiomePixelColor(biomeType, x, y);
                    image.SetPixel(x, y, color);
                }
            }

            return image;
        }

        private Color CalculateBiomePixelColor(BiomeType biomeType, int x, int y)
        {
            float baseNoise = _baseNoise.GetNoise2D(x, y) * 0.5f + 0.5f;
            float detailNoise = _detailNoise.GetNoise2D(x, y) * 0.5f + 0.5f;
            float cloudNoise = _cloudNoise.GetNoise2D(x, y) * 0.5f + 0.5f;

            float combinedNoise = baseNoise * 0.6f + detailNoise * 0.3f + cloudNoise * 0.1f;

            return biomeType switch
            {
                BiomeType.Ocean => GenerateOceanColor(combinedNoise, detailNoise),
                BiomeType.IceSheetOcean => GenerateIceSheetOceanColor(combinedNoise, detailNoise),
                BiomeType.IceSheet => GenerateIceSheetColor(combinedNoise, detailNoise),
                BiomeType.Tundra => GenerateTundraColor(combinedNoise, detailNoise, cloudNoise),
                BiomeType.ColdBog => GenerateColdBogColor(combinedNoise, detailNoise),
                BiomeType.BorealForest => GenerateBorealForestColor(combinedNoise, detailNoise, cloudNoise),
                BiomeType.TemperateForest => GenerateTemperateForestColor(combinedNoise, detailNoise, cloudNoise),
                BiomeType.TemperateSwamp => GenerateTemperateSwampColor(combinedNoise, detailNoise),
                BiomeType.AridShrubland => GenerateAridShrublandColor(combinedNoise, detailNoise),
                BiomeType.Desert => GenerateDesertColor(combinedNoise, detailNoise, cloudNoise),
                BiomeType.ExtremeDesert => GenerateExtremeDesertColor(combinedNoise, detailNoise),
                BiomeType.TropicalRainforest => GenerateTropicalRainforestColor(combinedNoise, detailNoise, cloudNoise),
                BiomeType.TropicalSwamp => GenerateTropicalSwampColor(combinedNoise, detailNoise),
                _ => GenerateTemperateForestColor(combinedNoise, detailNoise, cloudNoise)
            };
        }

        private Color GenerateOceanColor(float noise, float detail)
        {
            float depth = Mathf.Clamp(noise, 0.0f, 1.0f);
            float ripples = Mathf.Sin(detail * (float)Math.PI * 4) * 0.05f;

            float r = 0.1f + depth * 0.15f + ripples;
            float g = 0.25f + depth * 0.25f + ripples;
            float b = 0.5f + depth * 0.35f + ripples;

            return new Color(r, g, b, 1.0f);
        }

        private Color GenerateIceSheetOceanColor(float noise, float detail)
        {
            float icePresence = Mathf.Clamp(noise, 0.3f, 1.0f);
            float ripples = Mathf.Sin(detail * (float)Math.PI * 3) * 0.03f;

            float r = 0.65f + icePresence * 0.2f + ripples;
            float g = 0.8f + icePresence * 0.12f + ripples;
            float b = 0.9f + icePresence * 0.08f + ripples;

            return new Color(r, g, b, 1.0f);
        }

        private Color GenerateIceSheetColor(float noise, float detail)
        {
            float surface = Mathf.Clamp(noise, 0.4f, 1.0f);
            float cracks = Mathf.Sin(detail * (float)Math.PI * 8) * 0.02f;

            float r = 0.85f + surface * 0.1f + cracks;
            float g = 0.9f + surface * 0.08f + cracks;
            float b = 0.95f + surface * 0.05f + cracks;

            return new Color(r, g, b, 1.0f);
        }

        private Color GenerateTundraColor(float noise, float detail, float cloud)
        {
            float ground = Mathf.Clamp(noise, 0.2f, 0.9f);
            float snowPatches = cloud > 0.6f ? (cloud - 0.6f) * 2.0f : 0.0f;

            float r = Mathf.Lerp(0.5f, 0.7f, ground) + snowPatches * 0.15f;
            float g = Mathf.Lerp(0.55f, 0.75f, ground) + snowPatches * 0.18f;
            float b = Mathf.Lerp(0.6f, 0.8f, ground) + snowPatches * 0.2f;

            return new Color(r, g, b, 1.0f);
        }

        private Color GenerateColdBogColor(float noise, float detail)
        {
            float wetness = Mathf.Clamp(noise, 0.3f, 0.9f);
            float mossPatches = Mathf.Sin(detail * (float)Math.PI * 5) * 0.5f + 0.5f;

            float r = 0.35f + wetness * 0.15f + mossPatches * 0.1f;
            float g = 0.45f + wetness * 0.15f + mossPatches * 0.15f;
            float b = 0.4f + wetness * 0.2f + mossPatches * 0.05f;

            return new Color(r, g, b, 1.0f);
        }

        private Color GenerateBorealForestColor(float noise, float detail, float cloud)
        {
            float forestDensity = Mathf.Clamp(noise, 0.2f, 0.95f);
            float treeCoverage = Mathf.Pow(forestDensity, 1.5f);
            float canopyShadow = cloud * 0.1f;

            float r = Mathf.Lerp(0.25f, 0.35f, forestDensity) - canopyShadow * 0.05f;
            float g = Mathf.Lerp(0.35f, 0.5f, treeCoverage) - canopyShadow * 0.03f;
            float b = Mathf.Lerp(0.25f, 0.35f, forestDensity) - canopyShadow * 0.05f;

            return new Color(r, g, b, 1.0f);
        }

        private Color GenerateTemperateForestColor(float noise, float detail, float cloud)
        {
            float density = Mathf.Clamp(noise, 0.15f, 0.95f);
            float leavesVariation = Mathf.Sin(detail * (float)Math.PI * 6) * 0.5f + 0.5f;
            float lightFilter = 1.0f - cloud * 0.15f;

            float r = Mathf.Lerp(0.2f, 0.35f, density) * lightFilter + leavesVariation * 0.05f;
            float g = Mathf.Lerp(0.5f, 0.7f, density) * lightFilter + leavesVariation * 0.08f;
            float b = Mathf.Lerp(0.15f, 0.3f, density) * lightFilter + leavesVariation * 0.03f;

            return new Color(r, g, b, 1.0f);
        }

        private Color GenerateTemperateSwampColor(float noise, float detail)
        {
            float swampiness = Mathf.Clamp(noise, 0.3f, 0.9f);
            float waterPatches = Mathf.Sin(detail * (float)Math.PI * 4) * 0.5f + 0.5f;
            float waterLevel = waterPatches > 0.5f ? (waterPatches - 0.5f) * 2.0f : 0.0f;

            float vegetation = 1.0f - waterLevel;
            float r = Mathf.Lerp(0.3f, 0.4f, swampiness) * vegetation + waterLevel * 0.2f;
            float g = Mathf.Lerp(0.45f, 0.55f, swampiness) * vegetation + waterLevel * 0.3f;
            float b = Mathf.Lerp(0.3f, 0.35f, swampiness) * vegetation + waterLevel * 0.4f;

            return new Color(r, g, b, 1.0f);
        }

        private Color GenerateAridShrublandColor(float noise, float detail)
        {
            float dryness = Mathf.Clamp(noise, 0.1f, 0.85f);
            float shrubPatches = Mathf.Sin(detail * (float)Math.PI * 3) * 0.5f + 0.5f;

            float r = Mathf.Lerp(0.6f, 0.75f, dryness) + shrubPatches * 0.1f;
            float g = Mathf.Lerp(0.5f, 0.6f, dryness) + shrubPatches * 0.12f;
            float b = Mathf.Lerp(0.25f, 0.35f, dryness) + shrubPatches * 0.05f;

            return new Color(r, g, b, 1.0f);
        }

        private Color GenerateDesertColor(float noise, float detail, float cloud)
        {
            float dunePattern = Mathf.Sin(noise * (float)Math.PI * 3 + detail * (float)Math.PI * 2) * 0.5f + 0.5f;
            float sandVariation = dunePattern * 0.15f;
            float rockyPatches = cloud < 0.3f ? (0.3f - cloud) * 0.5f : 0.0f;

            float r = Mathf.Lerp(0.75f, 0.9f, noise) + sandVariation + rockyPatches * 0.1f;
            float g = Mathf.Lerp(0.6f, 0.75f, noise) + sandVariation + rockyPatches * 0.08f;
            float b = Mathf.Lerp(0.35f, 0.5f, noise) + sandVariation + rockyPatches * 0.05f;

            return new Color(r, g, b, 1.0f);
        }

        private Color GenerateExtremeDesertColor(float noise, float detail)
        {
            float extremeHeat = Mathf.Clamp(noise, 0.0f, 1.0f);
            float crackedEarth = Mathf.Sin(detail * (float)Math.PI * 8) * 0.5f + 0.5f;
            float cracks = crackedEarth > 0.7f ? (crackedEarth - 0.7f) * 3.0f : 0.0f;

            float r = Mathf.Lerp(0.85f, 1.0f, extremeHeat) - cracks * 0.2f;
            float g = Mathf.Lerp(0.7f, 0.9f, extremeHeat) - cracks * 0.15f;
            float b = Mathf.Lerp(0.4f, 0.6f, extremeHeat) - cracks * 0.1f;

            return new Color(r, g, b, 1.0f);
        }

        private Color GenerateTropicalRainforestColor(float noise, float detail, float cloud)
        {
            float density = Mathf.Clamp(noise, 0.3f, 0.95f);
            float canopy = Mathf.Pow(density, 1.3f);
            float lightPatches = cloud * 0.2f;
            float leafVariation = Mathf.Sin(detail * (float)Math.PI * 10) * 0.5f + 0.5f;

            float r = Mathf.Lerp(0.1f, 0.25f, canopy) + lightPatches * 0.1f + leafVariation * 0.05f;
            float g = Mathf.Lerp(0.4f, 0.6f, canopy) + lightPatches * 0.12f + leafVariation * 0.08f;
            float b = Mathf.Lerp(0.05f, 0.2f, canopy) + lightPatches * 0.05f + leafVariation * 0.03f;

            return new Color(r, g, b, 1.0f);
        }

        private Color GenerateTropicalSwampColor(float noise, float detail)
        {
            float swampIntensity = Mathf.Clamp(noise, 0.4f, 0.95f);
            float waterLevel = Mathf.Sin(detail * (float)Math.PI * 3) * 0.5f + 0.5f;
            float openWater = waterLevel > 0.6f ? (waterLevel - 0.6f) * 2.5f : 0.0f;

            float vegetation = 1.0f - openWater;
            float r = Mathf.Lerp(0.15f, 0.3f, swampIntensity) * vegetation + openWater * 0.15f;
            float g = Mathf.Lerp(0.35f, 0.5f, swampIntensity) * vegetation + openWater * 0.25f;
            float b = Mathf.Lerp(0.1f, 0.25f, swampIntensity) * vegetation + openWater * 0.35f;

            return new Color(r, g, b, 1.0f);
        }
    }
}
