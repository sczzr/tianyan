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

    private struct EdgeHash
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

        public override bool Equals(object obj)
        {
            if (obj is EdgeHash other)
                return P == other.P && Q == other.Q;
            return false;
        }

        public override int GetHashCode()
        {
            return P * 73856093 ^ Q;
        }
    }

    public static Triangle[] Triangulate(Vector2[] points)
    {
        int n = points.Length;
        if (n < 3)
            return Array.Empty<Triangle>();

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

        for (int i = 0; i < n; i++)
        {
            var edges = new HashSet<EdgeHash>();

            for (int t = triangles.Count - 1; t >= 0; t--)
            {
                var tri = triangles[t];
                if (IsInCircumcircle(points[i], pointVertices[tri.V0], pointVertices[tri.V1], pointVertices[tri.V2]))
                {
                    edges.Add(new EdgeHash(tri.V0, tri.V1));
                    edges.Add(new EdgeHash(tri.V1, tri.V2));
                    edges.Add(new EdgeHash(tri.V2, tri.V0));
                    triangles.RemoveAt(t);
                }
            }

            foreach (var edge in edges)
            {
                var newTri = new Triangle(
                    pointVertices.Count - 1,
                    edge.P,
                    edge.Q
                );
                triangles.Add(newTri);
            }

            pointVertices.Add(points[i]);
        }

        for (int t = triangles.Count - 1; t >= 0; t--)
        {
            var tri = triangles[t];
            if (tri.V0 >= n || tri.V1 >= n || tri.V2 >= n)
            {
                triangles.RemoveAt(t);
            }
        }

        return triangles.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsInCircumcircle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float dx = a.X - p.X;
        float dy = a.Y - p.Y;
        float det = dx * dx + dy * dy;

        float cax = c.X - p.X;
        float cay = c.Y - p.Y;
        float detC = cax * cax + cay * cay;

        float bax = b.X - p.X;
        float bay = b.Y - p.Y;
        float detB = bax * bax + bay * bay;

        float matrix = (dx * (bay - cay) -
                        dy * (bax - cax) +
                        (cay - bay) * (c.X - a.X) +
                        (cax - bax) * (c.Y - a.Y)) * 2f;

        float abDet = det * (bax * cay - bay * cax);
        float bcDet = detB * cax - detC * bax;
        float caDet = detC * bay - abDet / detC;

        return matrix > 0 ? abDet + bcDet + caDet < 0 : abDet + bcDet + caDet > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 GetCircumcenter(Vector2 a, Vector2 b, Vector2 c)
    {
        float d = 2f * (a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y));

        float ax2ay2 = a.X * a.X + a.Y * a.Y;
        float bx2by2 = b.X * b.X + b.Y * b.Y;
        float cx2cy2 = c.X * c.X + c.Y * c.Y;

        float ux = (ax2ay2 * (b.Y - c.Y) + bx2by2 * (c.Y - a.Y) + cx2cy2 * (a.Y - b.Y)) / d;
        float uy = (ax2ay2 * (c.X - b.X) + bx2by2 * (a.X - c.X) + cx2cy2 * (b.X - a.X)) / d;

        return new Vector2(ux, uy);
    }
}
