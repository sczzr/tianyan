using Godot;
using System;
using System.Collections.Generic;

using TianYanShop.Data;
using TianYanShop.World.Config;

namespace TianYanShop.World.Map
{
    public partial class RealmMapGenerator : RefCounted
    {
        // 主要噪声生成器 - 控制大陆尺度的地形
        private FastNoiseLite _continentNoise;
        // 山脉噪声 - 控制中尺度的山地
        private FastNoiseLite _mountainNoise;
        // 丘陵噪声 - 小尺度起伏
        private FastNoiseLite _hillNoise;
        // 温度纬度梯度 + 噪声
        private FastNoiseLite _temperatureNoise;
        // 降雨量噪声
        private FastNoiseLite _rainfallNoise;
        // 河流路径噪声
        private FastNoiseLite _riverNoise;
        // 边界扰动噪声 - 用于打破地形边界的直线
        private FastNoiseLite _boundaryNoise;
        // 细节扰动噪声 - 用于更细致的边界扰动
        private FastNoiseLite _detailNoise;

        private Random _random;

        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }
        public int Seed { get; private set; }

        // 当前省份配置
        private ChinaProvinceConfig _currentProvince;
        public string CurrentProvinceName => _currentProvince?.Name ?? "未选择省份";

        private List<ElementTag> _mapElements = new List<ElementTag>();
        private List<ElementTag> _specialElements = new List<ElementTag>();

        public Data.RealmTile[,] MapTiles { get; private set; }

        // 默认构造函数（随机省份）
        public RealmMapGenerator(int width = 256, int height = 256, int seed = -1)
        {
            MapWidth = width;
            MapHeight = height;
            Seed = seed == -1 ? (int)Time.GetUnixTimeFromSystem() : seed;
            _random = new Random(Seed);

            InitializeProvinceConfig();
            InitializeNoiseGenerators();
            InitializeMapElements();
        }

        // 指定省份的构造函数
        public RealmMapGenerator(string provinceName, int width = 256, int height = 256, int seed = -1)
        {
            MapWidth = width;
            MapHeight = height;
            Seed = seed == -1 ? (int)Time.GetUnixTimeFromSystem() : seed;
            _random = new Random(Seed);

            InitializeProvinceConfig(provinceName);
            InitializeNoiseGenerators();
            InitializeMapElements();
        }

        private void InitializeProvinceConfig(string provinceName = null)
        {
            ProvinceConfigManager.Initialize();
            if (provinceName != null)
            {
                _currentProvince = ProvinceConfigManager.GetProvince(provinceName);
            }
            else
            {
                // 随机选择一个省份
                var provinces = ProvinceConfigManager.GetAllProvinces();
                _currentProvince = provinces[_random.Next(provinces.Count)];
            }
            GD.Print($"当前省份: {_currentProvince.Name} ({_currentProvince.Abbreviation}) - {_currentProvince.Description}");
        }

        private void InitializeNoiseGenerators()
        {
            // 大陆噪声 - 低频率、高振幅，形成大陆板块
            _continentNoise = new FastNoiseLite();
            _continentNoise.Seed = Seed;
            _continentNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _continentNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _continentNoise.FractalOctaves = 4;
            _continentNoise.FractalLacunarity = 2.0f;
            _continentNoise.FractalGain = 0.5f;
            _continentNoise.Frequency = 0.001f; // 非常低的频率，形成大型大陆

            // 山脉噪声 - 中等频率，形成山脉
            _mountainNoise = new FastNoiseLite();
            _mountainNoise.Seed = Seed + 1;
            _mountainNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _mountainNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _mountainNoise.FractalOctaves = 6;
            _mountainNoise.FractalLacunarity = 2.0f;
            _mountainNoise.FractalGain = 0.5f;
            _mountainNoise.Frequency = 0.005f; // 中等频率，形成中尺度的山脉

            // 丘陵噪声 - 较高频率，添加细节
            _hillNoise = new FastNoiseLite();
            _hillNoise.Seed = Seed + 2;
            _hillNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _hillNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _hillNoise.FractalOctaves = 3;
            _hillNoise.Frequency = 0.02f; // 高频率，小尺度细节

            // 温度噪声 - 添加随机变化到纬度温度
            _temperatureNoise = new FastNoiseLite();
            _temperatureNoise.Seed = Seed + 3;
            _temperatureNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _temperatureNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _temperatureNoise.FractalOctaves = 4;
            _temperatureNoise.Frequency = 0.003f; // 低频率，大尺度温度变化

            // 降雨量噪声
            _rainfallNoise = new FastNoiseLite();
            _rainfallNoise.Seed = Seed + 4;
            _rainfallNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _rainfallNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _rainfallNoise.FractalOctaves = 5;
            _rainfallNoise.Frequency = 0.004f; // 中等频率

            // 河流噪声 - 用于河流路径
            _riverNoise = new FastNoiseLite();
            _riverNoise.Seed = Seed + 10;
            _riverNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular;
            _riverNoise.Frequency = 0.008f;

            // 边界扰动噪声 - 使用 Worley 噪声产生自然的细胞状边界
            _boundaryNoise = new FastNoiseLite();
            _boundaryNoise.Seed = Seed + 20;
            _boundaryNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular;
            _boundaryNoise.Frequency = 0.03f;
            _boundaryNoise.CellularDistanceFunction = FastNoiseLite.CellularDistanceFunctionEnum.Euclidean;
            _boundaryNoise.CellularReturnType = FastNoiseLite.CellularReturnTypeEnum.Distance;

            // 细节扰动噪声 - 使用高频 Simplex 噪声
            _detailNoise = new FastNoiseLite();
            _detailNoise.Seed = Seed + 30;
            _detailNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
            _detailNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            _detailNoise.FractalOctaves = 4;
            _detailNoise.Frequency = 0.05f;
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

        public void GenerateMap()
        {
            InitializeMapElements();
            MapTiles = new Data.RealmTile[MapWidth, MapHeight];

            // 第一步：生成基础地形（高程、温度、降雨）
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    MapTiles[x, y] = GenerateBaseTile(x, y);
                }
            }

            // 第二步：识别水域、海洋、湖泊
            IdentifyWaterBodies();

            // 第三步：应用地形分类（考虑水域和距离）
            ApplyTerrainClassification();

            // 第四步：添加河流
            AddRivers();

            // 第五步：添加特殊地点和灵气
            AddSpecialFeatures();

            GD.Print($"灵域地图生成完成: {MapWidth}x{MapHeight}");
        }

        private Data.RealmTile GenerateBaseTile(int x, int y)
        {
            // 计算高程 - 使用多层噪声叠加，模拟真实地貌
            float elevation = CalculateElevation(x, y);

            // 计算温度 - 基于纬度 + 高程 + 噪声
            float temperature = CalculateTemperature(x, y, elevation);

            // 计算降雨量 - 基于噪声 + 湿度分布
            float rainfall = CalculateRainfall(x, y, elevation);

            return new Data.RealmTile(
                Data.TerrainType.Plain, // 临时地形，稍后确定
                Data.SpiritLevel.Normal,
                elevation,
                temperature,
                rainfall
            );
        }

        /// <summary>
        /// 计算高程 - 模拟真实地貌形成
        /// 使用多层噪声叠加，并结合省份特色参数
        /// </summary>
        private float CalculateElevation(int x, int y)
        {
            // 大陆尺度噪声 - 决定大陆轮廓
            float continentValue = _continentNoise.GetNoise2D(x, y) * 0.5f + 0.5f;

            // 山脉噪声 - 添加中尺度起伏
            float mountainValue = _mountainNoise.GetNoise2D(x, y) * 0.5f + 0.5f;

            // 丘陵噪声 - 添加小尺度细节
            float hillValue = _hillNoise.GetNoise2D(x, y) * 0.5f + 0.5f;

            // 使用省份配置调整高程
            float baseElevation = _currentProvince.GetModifiedElevation(continentValue, x, y);

            // 根据省份类型调整山脉影响
            float mountainInfluence = 0.25f;
            switch (_currentProvince.TerrainType)
            {
                case ProvinceTerrainType.Mountain:
                    mountainInfluence = 0.35f;
                    break;
                case ProvinceTerrainType.Plateau:
                    mountainInfluence = 0.30f;
                    break;
                case ProvinceTerrainType.Plain:
                    mountainInfluence = 0.15f;
                    break;
                case ProvinceTerrainType.Coastal:
                    mountainInfluence = 0.20f;
                    break;
            }

            float elevation;

            if (baseElevation < _currentProvince.OceanThreshold)
            {
                // 水域区域
                elevation = baseElevation * 0.5f;
            }
            else if (baseElevation < _currentProvince.ShallowWaterThreshold)
            {
                // 浅海/过渡区域
                float t = (baseElevation - _currentProvince.OceanThreshold) / 
                          (_currentProvince.ShallowWaterThreshold - _currentProvince.OceanThreshold);
                elevation = _currentProvince.OceanThreshold + t * 0.15f;
            }
            else
            {
                // 陆地区域
                elevation = baseElevation;

                // 添加山脉影响
                elevation += mountainValue * mountainInfluence;

                // 添加丘陵细节
                elevation += hillValue * 0.05f;
            }

            return Mathf.Clamp(elevation, 0.0f, 1.0f);
        }

        /// <summary>
        /// 计算温度 - 基于纬度、高程和局部变化
        /// </summary>
        private float CalculateTemperature(int x, int y, float elevation)
        {
            // 使用省份配置计算温度
            float baseTemp = _currentProvince.GetModifiedTemperature(0f, x, y);

            // 纬度影响
            float latitudeFactor = Mathf.Abs((float)y / MapHeight - 0.5f) * 0.5f;

            // 高程影响 - 高海拔降温
            float elevationEffect = elevation * 0.35f;

            // 局部温度变化
            float tempVariation = _temperatureNoise.GetNoise2D(x, y) * 0.12f;

            float temperature = baseTemp - latitudeFactor - elevationEffect + tempVariation;

            return Mathf.Clamp(temperature, 0.0f, 1.0f);
        }

        /// <summary>
        /// 计算降雨量 - 基于位置、高程和大气环流
        /// 使用省份配置调整降雨分布
        /// </summary>
        private float CalculateRainfall(int x, int y, float elevation)
        {
            // 使用省份配置计算基础降雨
            float rainNoise = _rainfallNoise.GetNoise2D(x, y);
            float baseRain = _currentProvince.GetModifiedRainfall(rainNoise, x, y);

            // 纬度影响（次要）
            float latitudeFactor = Mathf.Abs((float)y / MapHeight - 0.5f) * 0.3f;

            // 高程影响 - 山区通常降雨更多
            float elevationEffect = elevation * 0.15f;

            // 局部变化
            float localVariation = _rainfallNoise.GetNoise2D(x + 100, y + 100) * 0.2f;

            float rainfall = baseRain - latitudeFactor + elevationEffect + localVariation;

            return Mathf.Clamp(rainfall, 0.0f, 1.0f);
        }

        /// <summary>
        /// 识别水体（海洋、湖泊）
        /// </summary>
        private void IdentifyWaterBodies()
        {
            bool[,] isWater = new bool[MapWidth, MapHeight];
            bool[,] isLake = new bool[MapWidth, MapHeight];

            // 初始标记水域 - 降低阈值增加水域面积
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    // 使用省份配置的水域阈值
                    if (MapTiles[x, y].Elevation < _currentProvince.OceanThreshold)
                    {
                        isWater[x, y] = true;
                    }
                }
            }

            // 标记海洋（连接到地图边界的连续水域）
            bool[,] isOcean = new bool[MapWidth, MapHeight];
            Queue<Vector2I> oceanQueue = new Queue<Vector2I>();

            // 从边界开始BFS
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    if (isWater[x, y] && (x == 0 || x == MapWidth - 1 || y == 0 || y == MapHeight - 1))
                    {
                        isOcean[x, y] = true;
                        oceanQueue.Enqueue(new Vector2I(x, y));
                    }
                }
            }

            // BFS扩展海洋区域
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

            while (oceanQueue.Count > 0)
            {
                Vector2I pos = oceanQueue.Dequeue();

                for (int i = 0; i < 4; i++)
                {
                    int nx = pos.X + dx[i];
                    int ny = pos.Y + dy[i];

                    if (nx >= 0 && nx < MapWidth && ny >= 0 && ny < MapHeight)
                    {
                        if (isWater[nx, ny] && !isOcean[nx, ny])
                        {
                            isOcean[nx, ny] = true;
                            oceanQueue.Enqueue(new Vector2I(nx, ny));
                        }
                    }
                }
            }

            // 标记湖泊（不是海洋的水域 + 专门生成的内陆湖泊）
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    if (isOcean[x, y])
                    {
                        MapTiles[x, y].Terrain = Data.TerrainType.Ocean;
                    }
                }
            }

            // 专门生成符合中国特色的湖泊
            GenerateChinaLakes();
        }

        /// <summary>
        /// 生成符合中国特色的湖泊
        /// 包括：大型淡水湖、盐湖、高原湖泊等
        /// 使用省份配置调整湖泊数量和大小
        /// </summary>
        private void GenerateChinaLakes()
        {
            // 使用省份配置的湖泊数量和大小
            int numLakes = Mathf.Max(5, (int)(_currentProvince.LakeCount * Mathf.Min(MapWidth, MapHeight) / 400f));
            float baseLakeSize = _currentProvince.LakeSize;

            for (int i = 0; i < numLakes; i++)
            {
                int lakeX = _random.Next(MapWidth / 10, MapWidth * 9 / 10);
                int lakeY = _random.Next(MapHeight / 10, MapHeight * 9 / 10);

                // 根据省份类型和位置决定湖泊大小
                float elevation = MapTiles[lakeX, lakeY].Elevation;
                float rainfall = MapTiles[lakeX, lakeY].Rainfall;

                // 计算湖泊大小（根据省份配置调整）
                int minRadius = (int)(baseLakeSize * 0.6f);
                int maxRadius = (int)(baseLakeSize * 1.2f);

                // 高原地区大型湖泊
                if (elevation > 0.65f)
                {
                    CreateLake(lakeX, lakeY, (int)(minRadius * 1.2f), (int)(maxRadius * 1.2f));
                }
                // 平原大型淡水湖
                else if (elevation < 0.38f && rainfall > 0.45f)
                {
                    CreateLake(lakeX, lakeY, minRadius, maxRadius);
                }
                // 干旱区盐湖
                else if (rainfall < 0.35f)
                {
                    CreateLake(lakeX, lakeY, (int)(minRadius * 0.8f), (int)(maxRadius * 0.8f));
                }
                // 普通湖泊
                else
                {
                    CreateLake(lakeX, lakeY, minRadius, maxRadius);
                }
            }
        }

        /// <summary>
        /// 在指定位置创建湖泊
        /// </summary>
        private void CreateLake(int centerX, int centerY, int minRadius, int maxRadius)
        {
            int radius = _random.Next(minRadius, maxRadius);

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
                        continue;

                    // 椭圆形湖泊
                    float dx = (float)(x - centerX) / radius;
                    float dy = (float)(y - centerY) / (radius * 0.7f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    if (distance < 0.8f + _random.Next(2) * 0.1f)
                    {
                        // 浅水区（边缘）有几率保持为沼泽或平原
                        if (distance > 0.6f && _random.Next(100) < 20)
                            continue;

                        MapTiles[x, y].Terrain = Data.TerrainType.Lake;
                    }
                }
            }
        }

        /// <summary>
        /// 应用地形分类 - 基于高程、温度、降雨量和距离水域的距离
        /// </summary>
        private void ApplyTerrainClassification()
        {
            // 计算到水域的距离
            int[,] distanceToWater = CalculateDistanceToWater();

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    // 跳过已标记的水域
                    if (MapTiles[x, y].Terrain == Data.TerrainType.Ocean ||
                        MapTiles[x, y].Terrain == Data.TerrainType.Lake)
                    {
                        continue;
                    }

                    float elevation = MapTiles[x, y].Elevation;
                    float temperature = MapTiles[x, y].Temperature;
                    float rainfall = MapTiles[x, y].Rainfall;
                    int distToWater = distanceToWater[x, y];

                    // 确定最终地形
                    MapTiles[x, y].Terrain = DetermineTerrain(
                        elevation, temperature, rainfall, distToWater, x, y
                    );
                }
            }
        }

        /// <summary>
        /// 计算到最近水域的距离
        /// </summary>
        private int[,] CalculateDistanceToWater()
        {
            int[,] distance = new int[MapWidth, MapHeight];
            Queue<Vector2I> queue = new Queue<Vector2I>();

            // 初始化距离
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    if (MapTiles[x, y].Terrain == Data.TerrainType.Ocean ||
                        MapTiles[x, y].Terrain == Data.TerrainType.Lake)
                    {
                        distance[x, y] = 0;
                        queue.Enqueue(new Vector2I(x, y));
                    }
                    else
                    {
                        distance[x, y] = int.MaxValue;
                    }
                }
            }

            // BFS计算距离
            int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
            int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

            while (queue.Count > 0)
            {
                Vector2I pos = queue.Dequeue();

                for (int i = 0; i < 8; i++)
                {
                    int nx = pos.X + dx[i];
                    int ny = pos.Y + dy[i];

                    if (nx >= 0 && nx < MapWidth && ny >= 0 && ny < MapHeight)
                    {
                        if (distance[nx, ny] > distance[pos.X, pos.Y] + 1)
                        {
                            distance[nx, ny] = distance[pos.X, pos.Y] + 1;
                            queue.Enqueue(new Vector2I(nx, ny));
                        }
                    }
                }
            }

            return distance;
        }

        /// <summary>
        /// 确定地形类型 - 基于中国地理特色的地形分布规则
        /// 中国地形特征：三级阶梯、多种气候带、丰富的地貌类型
        /// 使用省份配置参数
        /// </summary>
        private Data.TerrainType DetermineTerrain(float elevation, float temperature, float rainfall, int distToWater, int x, int y)
        {
            // 获取边界扰动值 - 使用 Worley 噪声产生自然的细胞状边界
            float boundaryPerturb = _boundaryNoise.GetNoise2D(x, y);
            
            // 添加高频细节扰动
            float detailPerturb = _detailNoise.GetNoise2D(x, y) * 0.5f + 0.5f;
            
            // 组合扰动
            float totalPerturbation = (boundaryPerturb - 0.5f) * 0.3f + (detailPerturb - 0.5f) * 0.1f;

            // 获取省份阈值
            float beachThreshold = _currentProvince.ShallowWaterThreshold;
            float lowlandThreshold = _currentProvince.LowlandThreshold;
            float hillThreshold = _currentProvince.HillThreshold;
            float mountainThreshold = _currentProvince.MountainThreshold;

            // 海滩 - 距离水域很近的低地
            if (distToWater <= 2 && elevation < beachThreshold + totalPerturbation)
            {
                return Data.TerrainType.Beach;
            }

            // 低地
            if (elevation < lowlandThreshold + totalPerturbation)
            {
                // 非常湿润 -> 沼泽/湿地
                if (rainfall > 0.72f + totalPerturbation)
                {
                    return Data.TerrainType.Swamp;
                }

                // 温暖湿润 -> 平原
                if (temperature > 0.4f && rainfall > 0.42f)
                {
                    return Data.TerrainType.Plain;
                }

                // 炎热干燥 -> 沙漠/戈壁
                if (temperature > 0.62f + totalPerturbation && rainfall < 0.28f - totalPerturbation)
                {
                    return Data.TerrainType.Desert;
                }

                // 中等条件 -> 平原
                return Data.TerrainType.Plain;
            }

            // 丘陵地带
            if (elevation < hillThreshold + totalPerturbation)
            {
                // 温暖湿润 -> 森林
                if (rainfall > 0.48f + totalPerturbation && temperature > 0.38f)
                {
                    return Data.TerrainType.Forest;
                }

                // 炎热干燥 -> 沙漠丘陵
                if (temperature > 0.58f + totalPerturbation && rainfall < 0.28f - totalPerturbation)
                {
                    return Data.TerrainType.Desert;
                }

                // 温暖高降雨 -> 丛林
                if (rainfall > 0.62f + totalPerturbation && temperature > 0.52f)
                {
                    return Data.TerrainType.Jungle;
                }

                // 干燥少雨 -> 丘陵
                if (rainfall < 0.38f)
                {
                    return Data.TerrainType.Hill;
                }

                // 中等降雨 -> 丘陵
                return Data.TerrainType.Hill;
            }

            // 山地
            if (elevation < mountainThreshold + totalPerturbation)
            {
                // 湿润温暖 -> 山地森林
                if (rainfall > 0.48f + totalPerturbation && temperature > 0.32f)
                {
                    return Data.TerrainType.Forest;
                }

                // 寒冷高海拔 -> 裸岩
                if (temperature < 0.32f || elevation > 0.68f)
                {
                    return Data.TerrainType.Mountain;
                }

                // 干旱地区 -> 石质山地
                if (rainfall < 0.32f)
                {
                    return Data.TerrainType.Mountain;
                }

                return Data.TerrainType.Mountain;
            }

            // 高原或极高海拔
            if (elevation < _currentProvince.PlateauThreshold + totalPerturbation)
            {
                // 寒冷 -> 高原荒漠/草甸
                if (temperature < 0.28f + totalPerturbation)
                {
                    return Data.TerrainType.Plateau;
                }

                // 较湿润 -> 高原草甸
                if (rainfall > 0.42f + totalPerturbation)
                {
                    return Data.TerrainType.Plateau;
                }

                // 干旱 -> 高原荒漠
                return Data.TerrainType.Plateau;
            }

            // 极高海拔 -> 雪峰/冰川
            return Data.TerrainType.Mountain;
        }

        /// <summary>
        /// 添加河流 - 基于地形流向
        /// </summary>
        private void AddRivers()
        {
            int numRivers = Mathf.Max(5, Mathf.Min(MapWidth, MapHeight) / 30);

            for (int r = 0; r < numRivers; r++)
            {
                // 在山地或高地寻找河流起点
                Vector2I? startPoint = FindRiverSource();
                if (startPoint.HasValue)
                {
                    CreateRiverFromSource(startPoint.Value);
                }
            }
        }

        /// <summary>
        /// 寻找河流源头 - 在高海拔区域
        /// </summary>
        private Vector2I? FindRiverSource()
        {
            for (int attempts = 0; attempts < 50; attempts++)
            {
                int x = _random.Next(MapWidth);
                int y = _random.Next(MapHeight);

                if (MapTiles[x, y].Elevation > 0.5f &&
                    MapTiles[x, y].Terrain != Data.TerrainType.Ocean &&
                    MapTiles[x, y].Terrain != Data.TerrainType.Lake &&
                    MapTiles[x, y].Terrain != Data.TerrainType.River)
                {
                    return new Vector2I(x, y);
                }
            }
            return null;
        }

        /// <summary>
        /// 从源头创建河流 - 沿着最陡下降路径
        /// </summary>
        private void CreateRiverFromSource(Vector2I start)
        {
            int x = start.X;
            int y = start.Y;
            int length = 0;
            int maxLength = Mathf.Min(MapWidth, MapHeight) / 2;

            HashSet<Vector2I> riverPath = new HashSet<Vector2I>();

            while (length < maxLength)
            {
                riverPath.Add(new Vector2I(x, y));

                // 检查是否到达水域
                if (MapTiles[x, y].Terrain == Data.TerrainType.Ocean ||
                    MapTiles[x, y].Terrain == Data.TerrainType.Lake)
                {
                    break;
                }

                // 寻找最陡下降方向
                Vector2I? nextPos = FindSteepestDescent(x, y, riverPath);
                if (!nextPos.HasValue)
                {
                    break;
                }

                x = nextPos.Value.X;
                y = nextPos.Value.Y;
                length++;
            }

            // 应用河流地形
            foreach (var pos in riverPath)
            {
                if (MapTiles[pos.X, pos.Y].Terrain != Data.TerrainType.Ocean &&
                    MapTiles[pos.X, pos.Y].Terrain != Data.TerrainType.Lake)
                {
                    MapTiles[pos.X, pos.Y].Terrain = Data.TerrainType.River;
                }
            }
        }

        /// <summary>
        /// 寻找最陡下降方向
        /// </summary>
        private Vector2I? FindSteepestDescent(int x, int y, HashSet<Vector2I> visited)
        {
            float currentElevation = MapTiles[x, y].Elevation;
            Vector2I? bestPos = null;
            float steepestDescent = 0;

            int[] dx = { -1, 1, 0, 0, -1, -1, 1, 1 };
            int[] dy = { 0, 0, -1, 1, -1, 1, -1, 1 };

            for (int i = 0; i < 8; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                if (nx >= 0 && nx < MapWidth && ny >= 0 && ny < MapHeight)
                {
                    if (visited.Contains(new Vector2I(nx, ny)))
                    {
                        continue;
                    }

                    float neighborElevation = MapTiles[nx, ny].Elevation;
                    float descent = currentElevation - neighborElevation;

                    if (descent > steepestDescent)
                    {
                        steepestDescent = descent;
                        bestPos = new Vector2I(nx, ny);
                    }
                }
            }

            return bestPos;
        }

        /// <summary>
        /// 添加特殊地点和灵气
        /// </summary>
        private void AddSpecialFeatures()
        {
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    // 根据高程确定灵气等级
                    MapTiles[x, y].Spirit = DetermineSpiritLevel(MapTiles[x, y].Elevation, x, y);
                }
            }
        }

        private Data.SpiritLevel DetermineSpiritLevel(float elevation, int x, int y)
        {
            float spiritNoise = _continentNoise.GetNoise2D(x + 1000, y + 1000) * 0.5f + 0.5f;
            float spiritBase = spiritNoise;

            // 山地通常灵气更丰富
            if (elevation > 0.5f && elevation < 0.85f)
            {
                spiritBase += 0.15f;
            }

            // 极高海拔灵气稀薄
            if (elevation >= 0.85f)
            {
                spiritBase -= 0.1f;
            }

            spiritBase = Mathf.Clamp(spiritBase, 0.0f, 1.0f);

            if (spiritBase < 0.15f) return Data.SpiritLevel.Desolate;
            if (spiritBase < 0.3f) return Data.SpiritLevel.Barren;
            if (spiritBase < 0.45f) return Data.SpiritLevel.Sparse;
            if (spiritBase < 0.6f) return Data.SpiritLevel.Normal;
            if (spiritBase < 0.8f) return Data.SpiritLevel.Rich;
            return Data.SpiritLevel.Abundant;
        }

        public void Regenerate(int newSeed = -1)
        {
            Seed = newSeed == -1 ? (int)Time.GetUnixTimeFromSystem() : newSeed;
            _random = new Random(Seed);
            InitializeNoiseGenerators();
            GenerateMap();
        }

        public Data.RealmTile GetTile(int x, int y)
        {
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
            {
                return new Data.RealmTile(Data.TerrainType.Ocean, Data.SpiritLevel.Desolate, 0, 0, 0);
            }
            return MapTiles[x, y];
        }
    }
}
