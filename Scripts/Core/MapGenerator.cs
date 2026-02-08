using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public float BoundaryPaddingScale { get; set; } = 1.5f;
    public float BoundaryStepScale { get; set; } = 1f;
    public bool UseMultithreading { get; set; } = true;

    private HeightmapProcessor _heightmapProcessor;

    // 生成选项
    public float WaterLevel { get; set; } = 0.35f;
    public bool UseTemplate { get; set; } = true;
    public HeightmapTemplateType TemplateType { get; set; } = HeightmapTemplateType.HighIsland;
    public bool RandomTemplate { get; set; } = true;

    private const int LloydRelaxIterations = 2;

    public event Action<float> ProgressChanged;

    private void ReportProgress(float progress)
    {
        ProgressChanged?.Invoke(Mathf.Clamp(progress, 0f, 1f));
    }


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

        float MapProgress(float stageStart, float stageEnd, float stageProgress)
        {
            return Mathf.Lerp(stageStart, stageEnd, Mathf.Clamp(stageProgress, 0f, 1f));
        }

        ReportProgress(0f);

        // 阶段1: 基础几何生成
        GD.Print("[MapGenerator] 阶段1: 生成基础几何...");
        var points = GenerateRandomPoints(cellCount, width, height);
        ReportProgress(0.06f);

        var triangulationPoints = BuildTriangulationPoints(points, width, height, BoundaryPaddingScale, BoundaryStepScale);
        ReportProgress(0.1f);

        var triangles = Delaunay.Triangulate(triangulationPoints, value =>
        {
            ReportProgress(MapProgress(0.1f, 0.24f, value));
        });

        var cells = VoronoiGenerator.GenerateVoronoi(triangulationPoints, width, height, triangles, points.Length, value =>
        {
            ReportProgress(MapProgress(0.24f, 0.34f, value));
        });

        var displayTriangles = Delaunay.Triangulate(points, value =>
        {
            ReportProgress(MapProgress(0.34f, 0.38f, value));
        });

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
            _heightmapProcessor.ApplyToCells(cells, heightmap, width, height, UseMultithreading);
        }
        ReportProgress(0.5f);

        // 确保边界为陆地，避免出现海洋露出边缘
        ForceBorderLand(cells, width, height);
        ReportProgress(0.52f);

        Task precipitationTask = null;
        if (UseMultithreading)
        {
            precipitationTask = Task.Run(() => CalculatePrecipitation(cells, width, height));
        }
        else
        {
            CalculatePrecipitation(cells, width, height, value =>
            {
                ReportProgress(MapProgress(0.52f, 0.6f, value));
            });
        }

        // 阶段3: 特征识别
        GD.Print("[MapGenerator] 阶段3: 特征识别...");
        var featureDetector = new FeatureDetector(cells, WaterLevel);
        var features = featureDetector.Detect();
        GD.Print($"[MapGenerator] 识别到 {features.Count} 个地貌特征");
        ReportProgress(0.64f);

        // 阶段4: 距离场计算
        GD.Print("[MapGenerator] 阶段4: 计算距离场...");
        var distanceCalculator = new DistanceFieldCalculator(cells, WaterLevel);
        distanceCalculator.Calculate();
        ReportProgress(0.7f);

        // 阶段5: 湖泊处理
        GD.Print("[MapGenerator] 阶段5: 处理湖泊...");
        var lakeProcessor = new LakeProcessor(cells, features, WaterLevel);
        lakeProcessor.Process();
        ReportProgress(0.76f);

        // 阶段6: 洼地填充
        GD.Print("[MapGenerator] 阶段6: 解决洼地问题...");
        var depressionResolver = new DepressionResolver(cells, features, WaterLevel);
        var resolvedHeights = depressionResolver.Resolve();
        ReportProgress(0.82f);

        // 等待降水量计算完成
        if (precipitationTask != null)
        {
            GD.Print("[MapGenerator] 阶段Pre: 等待降水量计算...");
            precipitationTask.Wait();
            ReportProgress(0.86f);
        }
        else
        {
            GD.Print("[MapGenerator] 阶段Pre: 计算降水量...");
        }

        // 阶段7: 河流生成
        GD.Print("[MapGenerator] 阶段7: 生成河流...");
        var riverGenerator = new RiverGenerator(cells, features, PRNG, resolvedHeights, RiverDensity, WaterLevel);
        var rivers = riverGenerator.Generate();
        GD.Print($"[MapGenerator] 生成了 {rivers.Count} 条河流");
        ReportProgress(0.91f);

        // 阶段8: 生物群落分配 / 河流路径构建（并行）
        GD.Print("[MapGenerator] 阶段8: 分配生物群落 / 构建河流渲染路径...");
        var biomeAssigner = new BiomeAssigner(cells, WaterLevel);

        if (UseMultithreading)
        {
            var biomeTask = Task.Run(() => biomeAssigner.Assign(true));
            var riverPathTask = Task.Run(() =>
            {
                var riverPathBuilder = new RiverPathBuilder(cells, PRNG, width, height);
                foreach (var river in rivers)
                {
                    riverPathBuilder.AddMeandering(river);
                }
            });
            Task.WaitAll(biomeTask, riverPathTask);
        }
        else
        {
            biomeAssigner.Assign();

            var riverPathBuilder = new RiverPathBuilder(cells, PRNG, width, height);
            foreach (var river in rivers)
            {
                riverPathBuilder.AddMeandering(river);
            }
        }
        ReportProgress(0.96f);

        // 阶段10: 分配渲染颜色
        GD.Print("[MapGenerator] 阶段10: 分配渲染颜色...");
        AssignRenderColors(cells);
        ReportProgress(0.985f);

        // 构建最终数据
        Data = new MapData
        {
            Points = points,
            Cells = cells,
            Triangles = displayTriangles,
            Heightmap = heightmap,
            MapSize = MapSize,
            Seed = PRNG.NextInt(),
            Features = features.ToArray(),
            Rivers = rivers.ToArray()
        };

        ReportProgress(1f);
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
        var triangulationPoints = BuildTriangulationPoints(points, width, height, BoundaryPaddingScale, BoundaryStepScale);
        var triangles = Delaunay.Triangulate(triangulationPoints);
        var cells = VoronoiGenerator.GenerateVoronoi(triangulationPoints, width, height, triangles, points.Length);
        var displayTriangles = Delaunay.Triangulate(points);

        _heightmapProcessor = new HeightmapProcessor(PRNG);
        _heightmapProcessor.WaterLevel = WaterLevel;

        float[] heightmap = _heightmapProcessor.GenerateHeightmap(width, height);
        _heightmapProcessor.ApplyToCells(cells, heightmap, width, height, UseMultithreading);
        ForceBorderLand(cells, width, height);
        _heightmapProcessor.AssignColors(cells, UseMultithreading);

        Data = new MapData
        {
            Points = points,
            Cells = cells,
            Triangles = displayTriangles,
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
        if (UseMultithreading)
        {
            Parallel.For(0, cells.Length, i =>
            {
                var cell = cells[i];
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
            });
            return;
        }

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

    private void CalculatePrecipitation(Cell[] cells, int width, int height, Action<float> progressCallback = null)
    {
        progressCallback?.Invoke(0f);
        var noise = new FastNoiseLite();
        noise.Seed = PRNG.NextInt();
        noise.Frequency = 0.02f;
        noise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;

        int total = Math.Max(1, cells.Length);
        int progressStep = Math.Max(1, total / 200);
        for (int i = 0; i < cells.Length; i++)
        {
            var cell = cells[i];

            // 基础降水噪音
            float noiseVal = noise.GetNoise2D(cell.Position.X, cell.Position.Y);
            // 归一化到 0-1
            float precipitation = (noiseVal + 1) / 2f;
            
            // 简单模拟: 高度越高/靠近水源 降水可能不同 (此处简化)
            // 调整强度
            precipitation *= 255;
            
            cell.Precipitation = (byte)Mathf.Clamp(precipitation, 0, 255);

            if (i == cells.Length - 1 || i % progressStep == 0)
            {
                progressCallback?.Invoke((i + 1f) / total);
            }
        }

        progressCallback?.Invoke(1f);
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

    private void ForceBorderLand(Cell[] cells, int width, int height)
    {
        float margin = 1f;
        float minLandHeight = WaterLevel + 0.02f;
        float maxX = width - margin;
        float maxY = height - margin;

        foreach (var cell in cells)
        {
            bool touchesBorder = cell.Position.X <= margin || cell.Position.X >= maxX ||
                cell.Position.Y <= margin || cell.Position.Y >= maxY;

            if (!touchesBorder && cell.Vertices != null && cell.Vertices.Count > 0)
            {
                foreach (var v in cell.Vertices)
                {
                    if (v.X <= margin || v.X >= maxX || v.Y <= margin || v.Y >= maxY)
                    {
                        touchesBorder = true;
                        break;
                    }
                }
            }

            if (touchesBorder)
            {
                cell.Height = Mathf.Max(cell.Height, minLandHeight);
                cell.IsLand = true;
            }
        }
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
            var triangulationPoints = BuildTriangulationPoints(current, width, height, BoundaryPaddingScale, BoundaryStepScale);
            var triangles = Delaunay.Triangulate(triangulationPoints);
            var cells = VoronoiGenerator.GenerateVoronoi(triangulationPoints, width, height, triangles, current.Length);
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

    private static Vector2[] BuildTriangulationPoints(
        Vector2[] points,
        int width,
        int height,
        float paddingScale,
        float stepScale)
    {
        if (points.Length == 0)
        {
            return points;
        }

        float area = width * height;
        float spacing = Mathf.Sqrt(area / Math.Max(1, points.Length));
        float step = Mathf.Max(1f, spacing * Math.Max(0.2f, stepScale));
        float padding = step * Math.Max(0.5f, paddingScale);

        var expanded = new List<Vector2>(points.Length + 256);
        expanded.AddRange(points);

        for (float x = -padding; x <= width + padding; x += step)
        {
            expanded.Add(new Vector2(x, -padding));
            expanded.Add(new Vector2(x, height + padding));
        }

        for (float y = -padding + step; y <= height + padding - step; y += step)
        {
            expanded.Add(new Vector2(-padding, y));
            expanded.Add(new Vector2(width + padding, y));
        }

        return expanded.ToArray();
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
