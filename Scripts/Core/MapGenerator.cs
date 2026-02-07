using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;
using FantasyMapGenerator.Scripts.Map.Heightmap;
using FantasyMapGenerator.Scripts.Map.Voronoi;
using FantasyMapGenerator.Scripts.Map.Features;
using FantasyMapGenerator.Scripts.Map.Lakes;
using FantasyMapGenerator.Scripts.Map.Rivers;
using FantasyMapGenerator.Scripts.Map.Biomes;

namespace FantasyMapGenerator.Scripts.Core;

/// <summary>
/// 地图生成器，整合所有地图生成模块
/// </summary>
public class MapGenerator
{
    public Vector2 MapSize { get; private set; }
    public int CellCount { get; private set; }
    public MapData Data { get; private set; }
    public AleaPRNG PRNG { get; private set; }
    public int MapWidth { get; set; } = 512;
    public int MapHeight { get; set; } = 512;
    public float RiverDensity { get; set; } = 1f;

    private HeightmapProcessor _heightmapProcessor;

    // 生成选项
    public float WaterLevel { get; set; } = 0.35f;
    public bool UseTemplate { get; set; } = true;
    public HeightmapTemplateType TemplateType { get; set; } = HeightmapTemplateType.HighIsland;
    public bool RandomTemplate { get; set; } = true;

    private const int LloydRelaxIterations = 2;

    /// <summary>
    /// 完整地图生成（包含河流、湖泊、生物群落等）
    /// </summary>
    public void Generate(string seed, int cellCount = 500)
    {
        CellCount = cellCount;
        PRNG = new AleaPRNG(seed);

        int width = Math.Max(128, MapWidth);
        int height = Math.Max(128, MapHeight);
        MapSize = new Vector2(width, height);

        GD.Print($"[MapGenerator] 开始生成地图 Seed={seed}, Cells={cellCount}");

        // 阶段1: 基础几何生成
        GD.Print("[MapGenerator] 阶段1: 生成基础几何...");
        var points = GenerateRandomPoints(cellCount, width, height);
        var triangles = Delaunay.Triangulate(points);
        var cells = VoronoiGenerator.GenerateVoronoi(points, width, height, triangles);

        // 阶段2: 高度图生成
        GD.Print("[MapGenerator] 阶段2: 生成高度图...");
        _heightmapProcessor = new HeightmapProcessor(PRNG);
        _heightmapProcessor.WaterLevel = WaterLevel;

        float[] heightmap;
        if (UseTemplate)
        {
            if (RandomTemplate)
            {
                _heightmapProcessor.GenerateFromRandomTemplate(cells, width, height);
            }
            else
            {
                _heightmapProcessor.GenerateFromTemplate(cells, width, height, TemplateType);
            }
            heightmap = ExtractHeightmapFromCells(cells, width, height);
        }
        else
        {
            heightmap = _heightmapProcessor.GenerateHeightmap(width, height);
            _heightmapProcessor.ApplyToCells(cells, heightmap, width, height);
        }

        // 阶段3: 特征识别
        GD.Print("[MapGenerator] 阶段3: 特征识别...");
        var featureDetector = new FeatureDetector(cells, WaterLevel);
        var features = featureDetector.Detect();
        GD.Print($"[MapGenerator] 识别到 {features.Count} 个地貌特征");

        // 阶段4: 距离场计算
        GD.Print("[MapGenerator] 阶段4: 计算距离场...");
        var distanceCalculator = new DistanceFieldCalculator(cells, WaterLevel);
        distanceCalculator.Calculate();

        // 阶段5: 湖泊处理
        GD.Print("[MapGenerator] 阶段5: 处理湖泊...");
        var lakeProcessor = new LakeProcessor(cells, features, WaterLevel);
        lakeProcessor.Process();

        // 阶段6: 洼地填充
        GD.Print("[MapGenerator] 阶段6: 解决洼地问题...");
        var depressionResolver = new DepressionResolver(cells, features, WaterLevel);
        var resolvedHeights = depressionResolver.Resolve();

        // 阶段_Pre: 计算降水量
        GD.Print("[MapGenerator] 阶段Pre: 计算降水量...");
        CalculatePrecipitation(cells, width, height);

        // 阶段7: 河流生成
        GD.Print("[MapGenerator] 阶段7: 生成河流...");
        var riverGenerator = new RiverGenerator(cells, features, PRNG, resolvedHeights, RiverDensity, WaterLevel);
        var rivers = riverGenerator.Generate();
        GD.Print($"[MapGenerator] 生成了 {rivers.Count} 条河流");

        // 阶段8: 生物群落分配
        GD.Print("[MapGenerator] 阶段8: 分配生物群落...");
        var biomeAssigner = new BiomeAssigner(cells, WaterLevel);
        biomeAssigner.Assign();

        // 阶段9: 河流路径构建
        GD.Print("[MapGenerator] 阶段9: 构建河流渲染路径...");
        var riverPathBuilder = new RiverPathBuilder(cells, PRNG, width, height);
        foreach (var river in rivers)
        {
            riverPathBuilder.AddMeandering(river);
        }

        // 阶段10: 分配渲染颜色
        GD.Print("[MapGenerator] 阶段10: 分配渲染颜色...");
        AssignRenderColors(cells);

        // 构建最终数据
        Data = new MapData
        {
            Points = points,
            Cells = cells,
            Triangles = triangles,
            Heightmap = heightmap,
            MapSize = MapSize,
            Seed = PRNG.NextInt(),
            Features = features.ToArray(),
            Rivers = rivers.ToArray()
        };

        GD.Print($"[MapGenerator] 地图生成完成！Features={features.Count}, Rivers={rivers.Count}");
    }

    /// <summary>
    /// 简化版生成（仅基础地形，用于快速预览）
    /// </summary>
    public void GenerateSimple(string seed, int cellCount = 500)
    {
        CellCount = cellCount;
        PRNG = new AleaPRNG(seed);

        int width = Math.Max(128, MapWidth);
        int height = Math.Max(128, MapHeight);
        MapSize = new Vector2(width, height);

        var points = GenerateRandomPoints(cellCount, width, height);
        var triangles = Delaunay.Triangulate(points);
        var cells = VoronoiGenerator.GenerateVoronoi(points, width, height, triangles);

        _heightmapProcessor = new HeightmapProcessor(PRNG);
        _heightmapProcessor.WaterLevel = WaterLevel;

        float[] heightmap = _heightmapProcessor.GenerateHeightmap(width, height);
        _heightmapProcessor.ApplyToCells(cells, heightmap, width, height);
        _heightmapProcessor.AssignColors(cells);

        Data = new MapData
        {
            Points = points,
            Cells = cells,
            Triangles = triangles,
            Heightmap = heightmap,
            MapSize = MapSize,
            Seed = PRNG.NextInt()
        };
    }

    /// <summary>
    /// 从Cell数组提取高度图
    /// </summary>
    private float[] ExtractHeightmapFromCells(Cell[] cells, int width, int height)
    {
        var heightmap = new float[width * height];

        // 使用最近邻插值填充高度图
        foreach (var cell in cells)
        {
            int mapX = (int)Mathf.Clamp(cell.Position.X, 0, width - 1);
            int mapY = (int)Mathf.Clamp(cell.Position.Y, 0, height - 1);
            heightmap[mapY * width + mapX] = cell.Height;
        }

        return heightmap;
    }

    /// <summary>
    /// 分配渲染颜色（支持生物群落颜色）
    /// </summary>
    private void AssignRenderColors(Cell[] cells)
    {
        foreach (var cell in cells)
        {
            if (cell.BiomeId > 0)
            {
                // 使用生物群落颜色
                cell.RenderColor = BiomeData.GetColor(cell.BiomeId);
            }
            else
            {
                // 回退到高度颜色
                cell.RenderColor = _heightmapProcessor.GetColorForHeight(cell.Height, cell.IsLand);
            }
        }
    }

    private void CalculatePrecipitation(Cell[] cells, int width, int height)
    {
        var noise = new FastNoiseLite();
        noise.Seed = PRNG.NextInt();
        noise.Frequency = 0.02f;
        noise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;

        foreach (var cell in cells)
        {
            // 基础降水噪音
            float noiseVal = noise.GetNoise2D(cell.Position.X, cell.Position.Y);
            // 归一化到 0-1
            float precipitation = (noiseVal + 1) / 2f;
            
            // 简单模拟: 高度越高/靠近水源 降水可能不同 (此处简化)
            // 调整强度
            precipitation *= 255;
            
            cell.Precipitation = (byte)Mathf.Clamp(precipitation, 0, 255);
        }
    }

    private Vector2[] GenerateRandomPoints(int count, int width, int height)
    {
        if (count <= 0)
        {
            return Array.Empty<Vector2>();
        }

        float area = width * height;
        float spacing = Mathf.Sqrt(area / Math.Max(1, count));
        int cols = Mathf.Max(1, Mathf.CeilToInt(width / spacing));
        int rows = Mathf.Max(1, Mathf.CeilToInt(height / spacing));

        float cellWidth = width / (float)cols;
        float cellHeight = height / (float)rows;
        float jitterX = cellWidth * 0.45f;
        float jitterY = cellHeight * 0.45f;

        var points = new List<Vector2>(rows * cols);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                float baseX = (x + 0.5f) * cellWidth;
                float baseY = (y + 0.5f) * cellHeight;
                float px = baseX + PRNG.NextRange(-jitterX, jitterX);
                float py = baseY + PRNG.NextRange(-jitterY, jitterY);

                points.Add(new Vector2(
                    Mathf.Clamp(px, 0, width),
                    Mathf.Clamp(py, 0, height)
                ));
            }
        }

        for (int i = points.Count - 1; i > 0; i--)
        {
            int j = PRNG.NextInt(0, i);
            (points[i], points[j]) = (points[j], points[i]);
        }

        if (points.Count > count)
        {
            points.RemoveRange(count, points.Count - count);
        }
        else
        {
            while (points.Count < count)
            {
                points.Add(new Vector2(
                    PRNG.NextRange(0, width),
                    PRNG.NextRange(0, height)
                ));
            }
        }

        var relaxed = RelaxPoints(points.ToArray(), width, height, LloydRelaxIterations);

        for (int i = 0; i < Math.Min(3, relaxed.Length); i++)
        {
            GD.Print($"[GenerateRandomPoints] Point {i}: ({relaxed[i].X}, {relaxed[i].Y})");
        }

        return relaxed;
    }

    private Vector2[] RelaxPoints(Vector2[] points, int width, int height, int iterations)
    {
        if (iterations <= 0 || points.Length == 0)
        {
            return points;
        }

        var current = points;
        for (int iter = 0; iter < iterations; iter++)
        {
            var triangles = Delaunay.Triangulate(current);
            var cells = VoronoiGenerator.GenerateVoronoi(current, width, height, triangles);
            var next = new Vector2[current.Length];

            for (int i = 0; i < current.Length; i++)
            {
                var vertices = cells[i].Vertices;
                if (vertices != null && vertices.Count >= 3)
                {
                    var centroid = ComputePolygonCentroid(vertices);
                    next[i] = new Vector2(
                        Mathf.Clamp(centroid.X, 0, width),
                        Mathf.Clamp(centroid.Y, 0, height)
                    );
                }
                else
                {
                    next[i] = current[i];
                }
            }

            current = next;
        }

        return current;
    }

    private static Vector2 ComputePolygonCentroid(List<Vector2> vertices)
    {
        int count = vertices.Count;
        if (count == 0)
        {
            return Vector2.Zero;
        }

        double area = 0.0;
        double cx = 0.0;
        double cy = 0.0;

        for (int i = 0; i < count; i++)
        {
            var a = vertices[i];
            var b = vertices[(i + 1) % count];
            double cross = a.X * b.Y - b.X * a.Y;
            area += cross;
            cx += (a.X + b.X) * cross;
            cy += (a.Y + b.Y) * cross;
        }

        if (Math.Abs(area) < 0.00001)
        {
            Vector2 sum = Vector2.Zero;
            foreach (var v in vertices) sum += v;
            return sum / count;
        }

        area *= 0.5;
        double factor = 1.0 / (6.0 * area);
        return new Vector2((float)(cx * factor), (float)(cy * factor));
    }

    public void Generate(int seed, int cellCount = 500)
    {
        Generate(seed.ToString(), cellCount);
    }

    public void GenerateSimple(int seed, int cellCount = 500)
    {
        GenerateSimple(seed.ToString(), cellCount);
    }

    public void Regenerate()
    {
        if (Data != null && PRNG != null)
        {
            Generate(PRNG.NextInt().ToString(), CellCount);
        }
    }

    public void RegenerateSimple()
    {
        if (Data != null && PRNG != null)
        {
            GenerateSimple(PRNG.NextInt().ToString(), CellCount);
        }
    }

    public void GenerateWithNewSeed(int cellCount = 500)
    {
        Generate(DateTime.Now.GetHashCode().ToString(), cellCount);
    }

    public void GenerateSimpleWithNewSeed(int cellCount = 500)
    {
        GenerateSimple(DateTime.Now.GetHashCode().ToString(), cellCount);
    }
}
