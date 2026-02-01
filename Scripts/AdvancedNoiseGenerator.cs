using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TianYanShop.Scripts
{
    /// <summary>
    /// 高级噪声生成器 - 封装 FastNoiseLite 提供多层噪声系统
    /// 支持：基础地形、山脉、河流、洞穴、侵蚀模拟等
    /// </summary>
    public class AdvancedNoiseGenerator
    {
        // 各种噪声生成器
        private FastNoiseLite _baseNoise;        // 基础地形噪声
        private FastNoiseLite _mountainNoise;    // 山脉噪声
        private FastNoiseLite _detailNoise;      // 细节噪声
        private FastNoiseLite _caveNoise;        // 洞穴噪声
        private FastNoiseLite _riverNoise;     // 河流噪声
        private FastNoiseLite _erosionNoise;   // 侵蚀噪声

        // 配置参数
        public int Seed { get; set; }
        public int MapWidth { get; set; } = 512;
        public int MapHeight { get; set; } = 512;

        // 基础噪声参数
        public int Octaves { get; set; } = 4;
        public float Persistence { get; set; } = 0.5f;
        public float Lacunarity { get; set; } = 2.0f;

        // 山脉参数
        public float MountainScale { get; set; } = 80.0f;
        public float MountainHeight { get; set; } = 0.4f;
        public float MountainThreshold { get; set; } = 0.65f;
        public int MountainOctaves { get; set; } = 6;

        // 洞穴参数
        public float CaveScale { get; set; } = 60.0f;
        public float CaveThreshold { get; set; } = 0.45f;

        // 侵蚀参数
        public bool EnableErosion { get; set; } = true;
        public float ErosionStrength { get; set; } = 0.3f;

        /// <summary>
        /// 初始化噪声生成器
        /// </summary>
        public void Initialize(int? seed = null)
        {
            Seed = seed ?? new Random().Next();

            // 基础噪声 - 用于大陆形状
            _baseNoise = CreateNoise(Seed, FastNoiseLite.NoiseTypeEnum.SimplexSmooth, 4, 0.5f, 2.0f);

            // 山脉噪声 - 高频细节
            _mountainNoise = CreateNoise(Seed + 1, FastNoiseLite.NoiseTypeEnum.SimplexSmooth, MountainOctaves, 0.5f, 2.0f);

            // 细节噪声 - 微小变化
            _detailNoise = CreateNoise(Seed + 2, FastNoiseLite.NoiseTypeEnum.SimplexSmooth, 8, 0.4f, 2.5f);

            // 洞穴噪声 - 3D噪声用于洞穴
            _caveNoise = CreateNoise(Seed + 3, FastNoiseLite.NoiseTypeEnum.Cellular, 3, 0.5f, 2.0f);
            _caveNoise.CellularDistanceFunction = FastNoiseLite.CellularDistanceFunctionEnum.Euclidean;
            _caveNoise.CellularReturnType = FastNoiseLite.CellularReturnTypeEnum.Distance;

            // 河流噪声 - 使用 Perlin 噪声生成河流路径
            _riverNoise = CreateNoise(Seed + 4, FastNoiseLite.NoiseTypeEnum.Perlin, 3, 0.5f, 2.0f);

            // 侵蚀噪声 - 用于模拟侵蚀效果
            _erosionNoise = CreateNoise(Seed + 5, FastNoiseLite.NoiseTypeEnum.SimplexSmooth, 6, 0.6f, 2.2f);

            GD.Print($"[AdvancedNoiseGenerator] 初始化完成，种子: {Seed}");
        }

        /// <summary>
        /// 创建配置好的噪声生成器
        /// </summary>
        private FastNoiseLite CreateNoise(int seed, FastNoiseLite.NoiseTypeEnum type, int octaves, float gain, float lacunarity)
        {
            var noise = new FastNoiseLite();
            noise.Seed = seed;
            noise.NoiseType = type;
            noise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
            noise.FractalOctaves = octaves;
            noise.FractalGain = gain;
            noise.FractalLacunarity = lacunarity;
            return noise;
        }

        /// <summary>
        /// 生成完整的高度图，包含所有地形特征
        /// </summary>
        public float[,] GenerateHeightMap()
        {
            float[,] heightMap = new float[MapWidth, MapHeight];

            // 并行生成以提高性能
            Parallel.For(0, MapWidth, x =>
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    heightMap[x, y] = GetHeightAt(x, y);
                }
            });

            // 应用侵蚀效果
            if (EnableErosion)
            {
                ApplyErosion(heightMap);
            }

            return heightMap;
        }

        /// <summary>
        /// 获取指定位置的高度值
        /// </summary>
        public float GetHeightAt(int x, int y)
        {
            // 必须显式转换为 float，否则整数除法会导致矩形块状图案
            float nx = (float)x / NoiseScale;
            float ny = (float)y / NoiseScale;

            // 基础地形（大陆形状）
            float baseHeight = (_baseNoise.GetNoise2D(nx, ny) + 1.0f) * 0.5f;

            // 山脉地形
            float mountainValue = (_mountainNoise.GetNoise2D((float)x / MountainScale, (float)y / MountainScale) + 1.0f) * 0.5f;
            float mountainEffect = 0.0f;

            if (mountainValue > MountainThreshold)
            {
                // 山脉增强效果
                float mountainIntensity = (mountainValue - MountainThreshold) / (1.0f - MountainThreshold);
                mountainEffect = mountainIntensity * MountainHeight;

                // 细节叠加
                float detail = (_detailNoise.GetNoise2D((float)x / 20.0f, (float)y / 20.0f) + 1.0f) * 0.5f;
                mountainEffect *= (0.7f + detail * 0.3f);
            }

            // 组合高度
            float finalHeight = baseHeight * 0.6f + mountainEffect * 0.4f;

            // 应用侵蚀噪声进行微调
            float erosion = (_erosionNoise.GetNoise2D(nx * 2, ny * 2) + 1.0f) * 0.5f;
            finalHeight = finalHeight * (1.0f - ErosionStrength) + erosion * ErosionStrength;

            return Mathf.Clamp(finalHeight, 0.0f, 1.0f);
        }

        /// <summary>
        /// 获取指定位置的基础高度（仅基础噪声）
        /// </summary>
        public float GetBaseHeightAt(int x, int y)
        {
            float nx = (float)x / NoiseScale;
            float ny = (float)y / NoiseScale;
            return (_baseNoise.GetNoise2D(nx, ny) + 1.0f) * 0.5f;
        }

        /// <summary>
        /// 获取指定位置的山脉值
        /// </summary>
        public float GetMountainValueAt(int x, int y)
        {
            float mountainValue = (_mountainNoise.GetNoise2D((float)x / MountainScale, (float)y / MountainScale) + 1.0f) * 0.5f;

            if (mountainValue > MountainThreshold)
            {
                float intensity = (mountainValue - MountainThreshold) / (1.0f - MountainThreshold);
                return intensity * MountainHeight;
            }
            return 0.0f;
        }

        /// <summary>
        /// 获取指定位置的侵蚀值
        /// </summary>
        public float GetErosionValueAt(int x, int y)
        {
            float nx = (float)x / NoiseScale;
            float ny = (float)y / NoiseScale;
            return (_erosionNoise.GetNoise2D(nx * 2, ny * 2) + 1.0f) * 0.5f;
        }

        /// <summary>
        /// 生成洞穴密度图
        /// </summary>
        public float[,] GenerateCaveMap()
        {
            float[,] caveMap = new float[MapWidth, MapHeight];

            Parallel.For(0, MapWidth, x =>
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    float nx = (float)x / CaveScale;
                    float ny = (float)y / CaveScale;

                    // 使用3D噪声，z轴基于高度变化
                    float heightFactor = (float)y / MapHeight;
                    float nz = heightFactor * 2.0f;

                    float caveValue = (_caveNoise.GetNoise3D(nx, ny, nz) + 1.0f) * 0.5f;

                    // 洞穴更可能出现在山脉区域
                    float mountainBias = 0.0f;
                    if (_mountainNoise != null)
                    {
                        float mountainValue = (_mountainNoise.GetNoise2D((float)x / MountainScale, (float)y / MountainScale) + 1.0f) * 0.5f;
                        if (mountainValue > MountainThreshold)
                        {
                            mountainBias = (mountainValue - MountainThreshold) * 0.3f;
                        }
                    }

                    caveMap[x, y] = Mathf.Clamp(caveValue + mountainBias, 0.0f, 1.0f);
                }
            });

            return caveMap;
        }

        /// <summary>
        /// 生成河流网络
        /// </summary>
        public RiverPath[] GenerateRivers(float[,] heightMap, int riverCount)
        {
            var rivers = new List<RiverPath>();
            var rng = new Random(Seed + 1000);

            for (int i = 0; i < riverCount; i++)
            {
                // 在高地寻找河流起点
                Vector2I startPoint = FindHighPoint(heightMap, rng);
                if (startPoint.X < 0) continue;

                var river = TraceRiver(startPoint, heightMap, rng);
                if (river.Points.Count > 20) // 只保留足够长的河流
                {
                    rivers.Add(river);
                }
            }

            return rivers.ToArray();
        }

        /// <summary>
        /// 寻找高点作为河流起点
        /// </summary>
        private Vector2I FindHighPoint(float[,] heightMap, Random rng)
        {
            int attempts = 100;
            for (int i = 0; i < attempts; i++)
            {
                int x = rng.Next(MapWidth / 4, MapWidth * 3 / 4);
                int y = rng.Next(MapHeight / 4, MapHeight * 3 / 4);

                if (heightMap[x, y] > 0.6f)
                {
                    return new Vector2I(x, y);
                }
            }
            return new Vector2I(-1, -1);
        }

        /// <summary>
        /// 追踪河流路径（沿着最陡下降方向）
        /// </summary>
        private RiverPath TraceRiver(Vector2I start, float[,] heightMap, Random rng)
        {
            var path = new RiverPath { Start = start };
            var current = start;
            var visited = new HashSet<Vector2I> { start };

            while (true)
            {
                path.Points.Add(current);

                // 找到最低邻居
                Vector2I? lowest = null;
                float lowestHeight = heightMap[current.X, current.Y];

                // 8方向搜索
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = current.X + dx;
                        int ny = current.Y + dy;

                        if (nx < 0 || nx >= MapWidth || ny < 0 || ny >= MapHeight)
                            continue;

                        if (visited.Contains(new Vector2I(nx, ny)))
                            continue;

                        float h = heightMap[nx, ny];
                        if (h < lowestHeight)
                        {
                            lowestHeight = h;
                            lowest = new Vector2I(nx, ny);
                        }
                    }
                }

                // 如果没有更低的地方，或者到达水域，停止
                if (lowest == null || lowestHeight < 0.3f)
                    break;

                current = lowest.Value;
                visited.Add(current);

                // 防止河流过长
                if (path.Points.Count > 1000)
                    break;
            }

            path.End = current;
            return path;
        }

        /// <summary>
        /// 应用简单的侵蚀模拟
        /// </summary>
        private void ApplyErosion(float[,] heightMap)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            float[,] erosionMap = new float[width, height];

            // 计算侵蚀强度
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    // 计算坡度
                    float dx = heightMap[x + 1, y] - heightMap[x - 1, y];
                    float dy = heightMap[x, y + 1] - heightMap[x, y - 1];
                    float slope = Mathf.Sqrt(dx * dx + dy * dy);

                    // 坡度越大，侵蚀越强
                    erosionMap[x, y] = slope * ErosionStrength;
                }
            }

            // 应用侵蚀
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    heightMap[x, y] = Mathf.Max(0.0f, heightMap[x, y] - erosionMap[x, y]);
                }
            }
        }

        /// <summary>
        /// 获取噪声缩放系数
        /// </summary>
        public float NoiseScale { get; set; } = 100.0f;
    }

    /// <summary>
    /// 河流路径数据
    /// </summary>
    public class RiverPath
    {
        public Vector2I Start { get; set; }
        public Vector2I End { get; set; }
        public List<Vector2I> Points { get; set; } = new List<Vector2I>();
        public float Width { get; set; } = 2.0f;
    }
}