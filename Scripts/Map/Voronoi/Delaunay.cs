using System;
using System.Collections.Generic;
using Godot;
using System.Runtime.CompilerServices;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Map.Voronoi;

/// <summary>
/// Bowyer-Watson 算法实现 Delaunay 三角剖分
/// </summary>
public class Delaunay
{
    private struct Edge
    {
        public int P, Q;
        public int TriangleIndex;

        public Edge(int p, int q, int triIndex)
        {
            P = p;
            Q = q;
            TriangleIndex = triIndex;
        }
    }

    private struct EdgeHash : IEquatable<EdgeHash>
    {
        public int P, Q;

        public EdgeHash(int p, int q)
        {
            if (p > q)
            {
                P = q;
                Q = p;
            }
            else
            {
                P = p;
                Q = q;
            }
        }

        public bool Equals(EdgeHash other)
        {
            return P == other.P && Q == other.Q;
        }

        public override bool Equals(object obj)
        {
            return obj is EdgeHash other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return P * 73856093 ^ Q;
            }
        }
    }

    public static Triangle[] Triangulate(Vector2[] points, Action<float> progressCallback = null)
    {
        int n = points.Length;
        if (n < 3)
            return Array.Empty<Triangle>();

        progressCallback?.Invoke(0f);

        var triangles = new List<Triangle>();

        int minX = (int)points[0].X, maxX = (int)points[0].X;
        int minY = (int)points[0].Y, maxY = (int)points[0].Y;

        for (int i = 1; i < n; i++)
        {
            minX = Math.Min(minX, (int)points[i].X);
            maxX = Math.Max(maxX, (int)points[i].X);
            minY = Math.Min(minY, (int)points[i].Y);
            maxY = Math.Max(maxY, (int)points[i].Y);
        }

        float dx = maxX - minX;
        float dy = maxY - minY;
        float deltaMax = Math.Max(dx, dy);
        float midX = (minX + maxX) / 2f;
        float midY = (minY + maxY) / 2f;

        var superTriangle = new Triangle(0, 1, 2);
        var pointVertices = new List<Vector2>
        {
            new Vector2(midX - 20 * deltaMax, midY - deltaMax),
            new Vector2(midX, midY + 20 * deltaMax),
            new Vector2(midX + 20 * deltaMax, midY - deltaMax)
        };

        triangles.Add(new Triangle(0, 1, 2));

        int progressStep = Math.Max(1, n / 200);
        for (int i = 0; i < n; i++)
        {
            if (i % 50 == 0) GD.Print($"[Delaunay] Processing point {i}/{n}...");
            pointVertices.Add(points[i]);

            // Dictionary to count edge occurrences
            // Key: EdgeHash, Value: Count
            var edgeCounts = new Dictionary<EdgeHash, int>();
            int currentPointIndex = pointVertices.Count - 1;

            for (int t = triangles.Count - 1; t >= 0; t--)
            {
                var tri = triangles[t];
                if (IsInCircumcircle(pointVertices[currentPointIndex], pointVertices[tri.V0], pointVertices[tri.V1], pointVertices[tri.V2]))
                {
                    AddEdge(edgeCounts, new EdgeHash(tri.V0, tri.V1));
                    AddEdge(edgeCounts, new EdgeHash(tri.V1, tri.V2));
                    AddEdge(edgeCounts, new EdgeHash(tri.V2, tri.V0));
                    triangles.RemoveAt(t);
                }
            }

            foreach (var kvp in edgeCounts)
            {
                // Only edges shared by exactly 1 triangle (boundary of polygon) should be kept
                if (kvp.Value == 1)
                {
                    var edge = kvp.Key;
                    var newTri = new Triangle(currentPointIndex, edge.P, edge.Q);
                    
                    // Calculate and store circumcenter
                    Vector2 v0 = pointVertices[currentPointIndex];
                    Vector2 v1 = pointVertices[edge.P];
                    Vector2 v2 = pointVertices[edge.Q];
                    newTri.Circumcenter = GetCircumcenter(v0, v1, v2);

                    triangles.Add(newTri);
                }
            }

            if (i == n - 1 || i % progressStep == 0)
            {
                progressCallback?.Invoke((i + 1f) / n);
            }
        }

        for (int t = triangles.Count - 1; t >= 0; t--)
        {
            var tri = triangles[t];
            // Remove triangles connected to the super triangle (indices 0, 1, 2)
            if (tri.V0 < 3 || tri.V1 < 3 || tri.V2 < 3)
            {
                triangles.RemoveAt(t);
            }
            else
            {
                // Shift indices back to match the input points array (0-based)
                // The internal pointVertices list had 3 super triangle points at the beginning
                var fixedTri = new Triangle(tri.V0 - 3, tri.V1 - 3, tri.V2 - 3);
                fixedTri.Circumcenter = tri.Circumcenter;
                triangles[t] = fixedTri;
            }
        }

        // 调试：检查三角形
        GD.Print($"[Delaunay] Generated {triangles.Count} triangles");
        if (triangles.Count > 0)
        {
            var firstTri = triangles[0];
            GD.Print($"  First triangle indices: {firstTri.V0}, {firstTri.V1}, {firstTri.V2}");
            if (firstTri.V0 < points.Length && firstTri.V1 < points.Length && firstTri.V2 < points.Length)
            {
                GD.Print($"  First triangle points: ({points[firstTri.V0].X}, {points[firstTri.V0].Y}), ({points[firstTri.V1].X}, {points[firstTri.V1].Y}), ({points[firstTri.V2].X}, {points[firstTri.V2].Y})");
            }
        }

        progressCallback?.Invoke(1f);
        return triangles.ToArray();
    }

    private static void AddEdge(Dictionary<EdgeHash, int> edgeCounts, EdgeHash edge)
    {
        if (edgeCounts.ContainsKey(edge))
            edgeCounts[edge]++;
        else
            edgeCounts[edge] = 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInCircumcircle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        // 计算外接圆中心
        float d = 2f * (a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y));
        if (Math.Abs(d) < 0.0001f) return false; // 退化三角形或共线

        float ax2ay2 = a.X * a.X + a.Y * a.Y;
        float bx2by2 = b.X * b.X + b.Y * b.Y;
        float cx2cy2 = c.X * c.X + c.Y * c.Y;

        float ux = (ax2ay2 * (b.Y - c.Y) + bx2by2 * (c.Y - a.Y) + cx2cy2 * (a.Y - b.Y)) / d;
        float uy = (ax2ay2 * (c.X - b.X) + bx2by2 * (a.X - c.X) + cx2cy2 * (b.X - a.X)) / d;

        // 计算半径平方
        float radiusSq = (ux - a.X) * (ux - a.X) + (uy - a.Y) * (uy - a.Y);

        // 计算点到圆心的距离平方
        float distSq = (p.X - ux) * (p.X - ux) + (p.Y - uy) * (p.Y - uy);

        return distSq < radiusSq;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 GetCircumcenter(Vector2 a, Vector2 b, Vector2 c)
    {
        float d = 2f * (a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y));
        if (Math.Abs(d) < 0.0001f) return (a + b + c) / 3f; // 避免除零，返回重心

        float ax2ay2 = a.X * a.X + a.Y * a.Y;
        float bx2by2 = b.X * b.X + b.Y * b.Y;
        float cx2cy2 = c.X * c.X + c.Y * c.Y;

        float ux = (ax2ay2 * (b.Y - c.Y) + bx2by2 * (c.Y - a.Y) + cx2cy2 * (a.Y - b.Y)) / d;
        float uy = (ax2ay2 * (c.X - b.X) + bx2by2 * (a.X - c.X) + cx2cy2 * (b.X - a.X)) / d;

        return new Vector2(ux, uy);
    }
}
