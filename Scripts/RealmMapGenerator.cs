using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop
{
    public partial class RealmMapGenerator : RefCounted
    {
        private FastNoiseLite _elevationNoise;
        private FastNoiseLite _temperatureNoise;
        private FastNoiseLite _rainfallNoise;
        private FastNoiseLite _spiritNoise;
        private FastNoiseLite _elementNoise;
        private FastNoiseLite _variationNoise;

        private Random _random;

        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }
        public int Seed { get; private set; }

        private List<ElementTag> _mapElements = new List<ElementTag>();
        private List<ElementTag> _specialElements = new List<ElementTag>();

        public RealmTile[,] MapTiles { get; private set; }

        public RealmMapGenerator(int width = 256, int height = 256, int seed = -1)
        {
            MapWidth = width;
            MapHeight = height;
            Seed = seed == -1 ? (int)Time.GetUnixTimeFromSystem() : seed;
            _random = new Random(Seed);

            InitializeNoiseGenerators();
            InitializeMapElements();
        }

        private void InitializeMapElements()
        {
            _mapElements.Clear();
            _specialElements.Clear();

            var baseElements = new List<ElementTag>
            {
                ElementTag.Earth, ElementTag.Wood, ElementTag.Water, ElementTag.Fire, ElementTag.Metal
            };
            _mapElements.AddRange(baseElements);

            var allSpecialElements = new List<ElementTag>
            {
                ElementTag.Wind, ElementTag.Thunder, ElementTag.Light, ElementTag.Dark,
                ElementTag.Ice, ElementTag.Sound, ElementTag.Crystal, ElementTag.Swamp
            };

            int specialCount = _random.Next(2, 5);
            _specialElements = GetRandomElements(allSpecialElements, specialCount);
        }

        private List<ElementTag> GetRandomElements(List<ElementTag> source, int count)
        {
            var result = new List<ElementTag>();
            var shuffled = new List<ElementTag>(source);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }
            for (int i = 0; i < Math.Min(count, shuffled.Count); i++)
            {
                result.Add(shuffled[i]);
            }
            return result;
        }

        private void InitializeNoiseGenerators()
        {
            _elevationNoise = new FastNoiseLite();
            _elevationNoise.Seed = Seed;
            _elevationNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _elevationNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _elevationNoise.FractalOctaves = 6;
            _elevationNoise.FractalLacunarity = 2.0f;
            _elevationNoise.FractalGain = 0.5f;
            _elevationNoise.Frequency = 0.008f;

            _temperatureNoise = new FastNoiseLite();
            _temperatureNoise.Seed = Seed + 1;
            _temperatureNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _temperatureNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _temperatureNoise.FractalOctaves = 4;
            _temperatureNoise.Frequency = 0.005f;

            _rainfallNoise = new FastNoiseLite();
            _rainfallNoise.Seed = Seed + 2;
            _rainfallNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _rainfallNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _rainfallNoise.FractalOctaves = 5;
            _rainfallNoise.Frequency = 0.006f;

            _spiritNoise = new FastNoiseLite();
            _spiritNoise.Seed = Seed + 100;
            _spiritNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _spiritNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _spiritNoise.FractalOctaves = 5;
            _spiritNoise.Frequency = 0.006f;

            _elementNoise = new FastNoiseLite();
            _elementNoise.Seed = Seed + 200;
            _elementNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular;
            _elementNoise.Frequency = 0.015f;

            _variationNoise = new FastNoiseLite();
            _variationNoise.Seed = Seed + 400;
            _variationNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular;
            _variationNoise.Frequency = 0.02f;
        }

        public void GenerateMap()
        {
            InitializeMapElements();

            MapTiles = new RealmTile[MapWidth, MapHeight];

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    MapTiles[x, y] = GenerateTile(x, y);
                }
            }

            ApplyTransitions();
            GD.Print($"灵域地图生成完成: {MapWidth}x{MapHeight}");
            GD.Print($"地图元素: {string.Join(", ", _mapElements)}");
            GD.Print($"特殊元素: {string.Join(", ", _specialElements)}");
        }

        private RealmTile GenerateTile(int x, int y)
        {
            float elevation = _elevationNoise.GetNoise2D(x, y) * 0.5f + 0.5f;
            float tempNoise = _temperatureNoise.GetNoise2D(x, y) * 0.5f + 0.5f;
            float rainNoise = _rainfallNoise.GetNoise2D(x, y) * 0.5f + 0.5f;
            float spiritNoise = _spiritNoise.GetNoise2D(x, y) * 0.5f + 0.5f;
            float elementNoise = _elementNoise.GetNoise2D(x, y) * 0.5f + 0.5f;
            float variation = _variationNoise.GetNoise2D(x, y) * 0.5f + 0.5f;

            float latitudeEffect = Mathf.Abs((float)y / MapHeight - 0.5f) * 2.0f;
            float temperature = Mathf.Clamp(1.0f - latitudeEffect * 0.7f + (variation - 0.5f) * 0.2f, 0.0f, 1.0f);

            var terrain = DetermineTerrain(elevation, temperature, rainNoise);
            var spirit = DetermineSpiritLevel(elevation, spiritNoise);
            var element = DetermineElement(elementNoise, temperature, spirit);

            var tile = new RealmTile(terrain, spirit, elevation, temperature, rainNoise);
            tile.PrimaryElement = element;
            tile.Realm = RealmTransitionConfig.GetRealmFromSpirit(spirit);

            if (element != ElementTag.None)
            {
                tile.Realm = GetElementRealm(element);
            }

            return tile;
        }

        private TerrainType DetermineTerrain(float elevation, float temperature, float rainfall)
        {
            if (elevation < 0.25f)
            {
                if (temperature < 0.3f) return TerrainType.Ocean;
                return TerrainType.Beach;
            }

            if (elevation < 0.38f)
            {
                if (rainfall < 0.35f)
                {
                    return TerrainType.Plain;
                }
                else if (rainfall > 0.75f)
                {
                    return TerrainType.Swamp;
                }
                return TerrainType.Plain;
            }

            if (elevation < 0.55f)
            {
                if (temperature < 0.25f) return TerrainType.Tundra;
                if (rainfall < 0.4f)
                {
                    return TerrainType.Plain;
                }
                if (rainfall > 0.75f)
                {
                    return temperature > 0.6f ? TerrainType.Jungle : TerrainType.Forest;
                }
                return TerrainType.Hill;
            }

            if (elevation < 0.75f)
            {
                if (rainfall > 0.7f) return TerrainType.Forest;
                if (rainfall > 0.45f) return TerrainType.Mountain;
                if (temperature < 0.3f) return TerrainType.Tundra;
                return TerrainType.Mountain;
            }

            if (elevation < 0.88f)
            {
                if (temperature < 0.2f) return TerrainType.Tundra;
                if (rainfall > 0.6f) return TerrainType.Forest;
                return TerrainType.Mountain;
            }

            return TerrainType.Plateau;
        }

        private SpiritLevel DetermineSpiritLevel(float elevation, float spiritNoise)
        {
            float spiritBase = spiritNoise;

            if (elevation > 0.55f && elevation < 0.85f)
            {
                spiritBase += 0.2f;
            }

            if (elevation >= 0.85f)
            {
                spiritBase += 0.35f;
            }

            spiritBase = Mathf.Clamp(spiritBase, 0.0f, 1.0f);

            if (spiritBase < 0.15f) return SpiritLevel.Desolate;
            if (spiritBase < 0.3f) return SpiritLevel.Barren;
            if (spiritBase < 0.45f) return SpiritLevel.Sparse;
            if (spiritBase < 0.6f) return SpiritLevel.Normal;
            if (spiritBase < 0.8f) return SpiritLevel.Rich;
            return SpiritLevel.Abundant;
        }

        private ElementTag DetermineElement(float noise, float temperature, SpiritLevel spirit)
        {
            if (spirit < SpiritLevel.Rich) return ElementTag.None;

            int mapElementCount = _mapElements.Count;
            int specialElementCount = _specialElements.Count;

            float elementThreshold = noise * (mapElementCount + specialElementCount);

            int elementIndex = (int)elementThreshold;

            if (elementIndex >= mapElementCount && elementIndex < mapElementCount + specialElementCount)
            {
                return _specialElements[elementIndex - mapElementCount];
            }

            return _mapElements[elementIndex % mapElementCount];
        }

        private RealmType GetElementRealm(ElementTag element)
        {
            return element switch
            {
                ElementTag.Metal => RealmType.MetalPeak,
                ElementTag.Wood => RealmType.WoodForest,
                ElementTag.Water => RealmType.WaterAbyss,
                ElementTag.Fire => RealmType.FireLava,
                ElementTag.Wind => RealmType.WindCanyon,
                ElementTag.Thunder => RealmType.ThunderRealm,
                ElementTag.Light => RealmType.LightHoly,
                ElementTag.Dark => RealmType.DarkAbyss,
                ElementTag.Ice => RealmType.IcePlain,
                ElementTag.Sound => RealmType.SoundValley,
                ElementTag.Crystal => RealmType.CrystalCave,
                ElementTag.Swamp => RealmType.SwampForest,
                _ => RealmType.Normal
            };
        }

        private void ApplyTransitions()
        {
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    var currentTerrain = MapTiles[x, y].Terrain;
                    var currentSpirit = MapTiles[x, y].Spirit;

                    TerrainType neighborTerrain = currentTerrain;
                    SpiritLevel neighborSpirit = currentSpirit;
                    float minDiff = 1.0f;

                    int[] dx = { -1, 1, 0, 0 };
                    int[] dy = { 0, 0, -1, 1 };

                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + dx[i];
                        int ny = y + dy[i];

                        if (nx >= 0 && nx < MapWidth && ny >= 0 && ny < MapHeight)
                        {
                            var neighbor = MapTiles[nx, ny];
                            if (neighbor.Terrain != currentTerrain)
                            {
                                float diff = Math.Abs(currentSpirit - neighbor.Spirit);
                                if (diff < minDiff)
                                {
                                    minDiff = diff;
                                    neighborTerrain = neighbor.Terrain;
                                    neighborSpirit = neighbor.Spirit;
                                }
                            }
                        }
                    }

                    if (neighborTerrain != currentTerrain)
                    {
                        MapTiles[x, y].SecondaryTerrain = neighborTerrain;
                        MapTiles[x, y].SecondarySpirit = neighborSpirit;
                        MapTiles[x, y].BlendFactor = minDiff * 0.5f;
                    }
                }
            }
        }

        public void Regenerate(int newSeed = -1)
        {
            Seed = newSeed == -1 ? (int)Time.GetUnixTimeFromSystem() : newSeed;
            _random = new Random(Seed);
            InitializeNoiseGenerators();
            GenerateMap();
        }

        public RealmTile GetTile(int x, int y)
        {
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
            {
                return new RealmTile(TerrainType.Ocean, SpiritLevel.Desolate, 0, 0, 0);
            }
            return MapTiles[x, y];
        }
    }
}
