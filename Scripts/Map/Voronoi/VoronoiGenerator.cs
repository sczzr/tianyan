using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Map.Voronoi;

/// <summary>
/// Voronoi 多边形生成器 - 基于 Delaunay 三角形的简化实现
/// </summary>
public class VoronoiGenerator
{
    public static Cell[] GenerateVoronoi(
        Vector2[] points,
        float width,
        float height,
        Triangle[] triangles)
    {
        return GenerateVoronoi(points, width, height, triangles, points.Length);
    }

    public static Cell[] GenerateVoronoi(
        Vector2[] points,
        float width,
        float height,
        Triangle[] triangles,
        int realPointCount)
    {
        int totalPoints = points.Length;
        if (realPointCount <= 0 || realPointCount > totalPoints)
        {
            realPointCount = totalPoints;
        }

        var cells = new Cell[realPointCount];

        // 为每个点创建一个 cell
        for (int i = 0; i < realPointCount; i++)
        {
            cells[i] = new Cell
            {
                Id = i,
                Position = points[i],
                Vertices = new List<Vector2>(),
                NeighborIds = new List<int>()
            };
        }

        // 收集每个点关联的三角形
        var pointTriangles = new Dictionary<int, List<Triangle>>();

        foreach (var tri in triangles)
        {
            if (tri.V0 < realPointCount) AddTriangleToPoint(tri.V0, tri, pointTriangles);
            if (tri.V1 < realPointCount) AddTriangleToPoint(tri.V1, tri, pointTriangles);
            if (tri.V2 < realPointCount) AddTriangleToPoint(tri.V2, tri, pointTriangles);
            
            // 构建邻居关系
            if (tri.V0 < realPointCount && tri.V1 < realPointCount) AddNeighbor(cells, tri.V0, tri.V1);
            if (tri.V0 < realPointCount && tri.V2 < realPointCount) AddNeighbor(cells, tri.V0, tri.V2);
            if (tri.V1 < realPointCount && tri.V0 < realPointCount) AddNeighbor(cells, tri.V1, tri.V0);
            if (tri.V1 < realPointCount && tri.V2 < realPointCount) AddNeighbor(cells, tri.V1, tri.V2);
            if (tri.V2 < realPointCount && tri.V0 < realPointCount) AddNeighbor(cells, tri.V2, tri.V0);
            if (tri.V2 < realPointCount && tri.V1 < realPointCount) AddNeighbor(cells, tri.V2, tri.V1);
        }

        // 为每个 cell 构建多边形 (使用三角形外心)
        for (int i = 0; i < realPointCount; i++)
        {
            if (!pointTriangles.TryGetValue(i, out var tris) || tris.Count == 0)
                continue;

            var center = points[i];

            var vertices = new List<Vector2>();
            foreach (var tri in tris)
            {
                Vector2 v = tri.Circumcenter;
                // 裁剪到地图范围内
                v = ClampToMap(v, width, height);
                vertices.Add(v);
            }
            
            // 去重并按角度重新排序，确保多边形不自交
            vertices = vertices.Distinct().ToList();
            if (vertices.Count >= 3)
            {
                vertices.Sort((a, b) =>
                {
                    float angleA = Mathf.Atan2(a.Y - center.Y, a.X - center.X);
                    float angleB = Mathf.Atan2(b.Y - center.Y, b.X - center.X);
                    return angleA.CompareTo(angleB);
                });
            }

            cells[i].Vertices = vertices;
            cells[i].Centroid = GetCentroid(vertices);
        }

        return cells;
    }

    private static void AddNeighbor(Cell[] cells, int a, int b)
    {
        if (!cells[a].NeighborIds.Contains(b))
            cells[a].NeighborIds.Add(b);
    }

    private static Vector2 GetCentroid(List<Vector2> vertices)
    {
        if (vertices.Count == 0) return Vector2.Zero;
        Vector2 sum = Vector2.Zero;
        foreach (var v in vertices) sum += v;
        return sum / vertices.Count;
    }

    private static void AddTriangleToPoint(int pointId, Triangle tri, Dictionary<int, List<Triangle>> pointTriangles)
    {
        if (!pointTriangles.ContainsKey(pointId))
        {
            pointTriangles[pointId] = new List<Triangle>();
        }
        pointTriangles[pointId].Add(tri);
    }

    private static Vector2 ClampToMap(Vector2 v, float width, float height)
    {
        // 裁剪到地图范围内，稍微向外扩展一点
        float margin = 0;
        return new Vector2(
            Mathf.Clamp(v.X, -margin, width + margin),
            Mathf.Clamp(v.Y, -margin, height + margin)
        );
    }

    private static List<Vector2> SimplifyVertices(List<Vector2> vertices, float width, float height)
    {
        if (vertices.Count < 3)
            return vertices;

        // 简单的凸包或边界计算
        // 由于是 Delaunay 三角形，点的顺序可能不是按边的顺序

        // 收集所有唯一的顶点
        var unique = vertices.Distinct().ToList();

        // 如果顶点太多，选择边界上的点
        if (unique.Count > 10)
        {
            // 找到边界点
            var boundary = unique.Where(v =>
                v.X <= 0 || v.X >= width || v.Y <= 0 || v.Y >= height
            ).ToList();

            if (boundary.Count >= 3)
            {
                // 按角度排序形成多边形
                var center = unique.Aggregate(Vector2.Zero, (s, v) => s + v) / unique.Count;
                boundary = boundary.OrderBy(v =>
                    Mathf.Atan2(v.Y - center.Y, v.X - center.X)
                ).ToList();
                return boundary;
            }
        }

        return unique;
    }
}
