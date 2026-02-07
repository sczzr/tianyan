using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;
using FantasyMapGenerator.Scripts.Map.Heightmap;
using FantasyMapGenerator.Scripts.Map.Voronoi;

namespace FantasyMapGenerator.Scripts.Core;

public class GameManager
{
    public Vector2 MapSize { get; private set; }
    public int CellCount { get; private set; }
    public MapData Data { get; private set; }
    public AleaPRNG PRNG { get; private set; }

    private HeightmapProcessor _heightmapProcessor;
    private const int LloydRelaxIterations = 2;

    public void Generate(string seed, int cellCount = 500)
    {
        CellCount = cellCount;
        PRNG = new AleaPRNG(seed);

        int width = 512;
        int height = 512;
        MapSize = new Vector2(width, height);

        var points = GenerateRandomPoints(cellCount, width, height);

        var triangles = Delaunay.Triangulate(points);
        var cells = VoronoiGenerator.GenerateVoronoi(points, width, height, triangles);

        _heightmapProcessor = new HeightmapProcessor(PRNG);
        _heightmapProcessor.WaterLevel = 0.38f;

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

        return RelaxPoints(points.ToArray(), width, height, LloydRelaxIterations);
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

    public void Regenerate()
    {
        if (Data != null && PRNG != null)
        {
            Generate(PRNG.NextInt().ToString(), CellCount);
        }
    }

    public void GenerateWithNewSeed(int cellCount = 500)
    {
        Generate(DateTime.Now.GetHashCode().ToString(), cellCount);
    }
}
