using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Map.Voronoi;

/// <summary>
/// Voronoi 多边形生成器
/// </summary>
public class VoronoiGenerator
{
    public static Cell[] GenerateVoronoi(
        Vector2[] points,
        float width,
        float height,
        Triangle[] triangles)
    {
        int n = points.Length;
        var cells = new Cell[n];

        for (int i = 0; i < n; i++)
        {
            cells[i] = new Cell
            {
                Id = i,
                Position = points[i]
            };
        }

        foreach (var tri in triangles)
        {
            var circumcenter = Delaunay.GetCircumcenter(
                points[tri.V0],
                points[tri.V1],
                points[tri.V2]
            );

            AddNeighbor(cells[tri.V0], tri.V1);
            AddNeighbor(cells[tri.V0], tri.V2);
            AddNeighbor(cells[tri.V1], tri.V0);
            AddNeighbor(cells[tri.V1], tri.V2);
            AddNeighbor(cells[tri.V2], tri.V0);
            AddNeighbor(cells[tri.V2], tri.V1);
        }

        foreach (var cell in cells)
        {
            CalculateCellPolygon(cell, points, width, height);
        }

        return cells;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddNeighbor(Cell cell, int neighborId)
    {
        if (!cell.NeighborIds.Contains(neighborId))
        {
            cell.NeighborIds.Add(neighborId);
        }
    }

    private static void CalculateCellPolygon(
        Cell cell,
        Vector2[] points,
        float width,
        float height)
    {
        var pointsCopy = new Vector2[cell.NeighborIds.Count + 1];
        pointsCopy[0] = cell.Position;
        for (int i = 0; i < cell.NeighborIds.Count; i++)
        {
            pointsCopy[i + 1] = points[cell.NeighborIds[i]];
        }

        Vector2 center = CalculateCentroid(pointsCopy);

        var rays = new List<(Vector2 Direction, float Angle)>();
        for (int i = 0; i < cell.NeighborIds.Count; i++)
        {
            Vector2 dir = (points[cell.NeighborIds[i]] - cell.Position).Normalized();
            rays.Add((dir, Mathf.Atan2(dir.Y, dir.X)));
        }

        rays = rays.OrderBy(r => r.Angle).ToList();

        var vertices = new List<Vector2>();

        for (int i = 0; i < rays.Count; i++)
        {
            Vector2 dir1 = rays[i].Direction;
            Vector2 dir2 = rays[(i + 1) % rays.Count].Direction;

            float angle = Mathf.Atan2(dir2.Y, dir2.X) - Mathf.Atan2(dir1.Y, dir1.X);
            while (angle > Math.PI) angle -= 2 * (float)Math.PI;
            while (angle < -Math.PI) angle += 2 * (float)Math.PI;

            Vector2 bisector = (dir1 + dir2).Normalized();
            float bisectorAngle = Mathf.Atan2(bisector.Y, bisector.X);

            vertices.Add(FindCellVertex(cell.Position, bisectorAngle, width, height, points, cell.Id));
        }

        cell.Vertices = vertices;
        cell.Centroid = CalculateCentroid(vertices.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 FindCellVertex(
        Vector2 center,
        float angle,
        float width,
        float height,
        Vector2[] allPoints,
        int ownId)
    {
        float minT = float.MaxValue;
        Vector2 bestPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * width;

        foreach (var p in allPoints)
        {
            if (p == center) continue;

            float dx = p.X - center.X;
            float dy = p.Y - center.Y;
            float perpX = -dy;
            float perpY = dx;

            float dot = dx * perpX + dy * perpY;
            if (Math.Abs(dot) < 0.0001f) continue;

            float t = ((center.X - p.X) * perpX + (center.Y - p.Y) * perpY) / dot;
            if (t <= 0) continue;

            float t2 = ((p.Y - center.Y) * dx - (p.X - center.X) * dy) / dot;

            float intersectionX = center.X + perpX * t;
            float intersectionY = center.Y + perpY * t;

            if (intersectionX < 0 || intersectionX > width ||
                intersectionY < 0 || intersectionY > height)
                t = Mathf.Min(t, float.MaxValue);
            else
                t = Mathf.Min(t, Mathf.Sqrt(t2));

            if (t < minT)
            {
                minT = t;
                bestPoint = new Vector2(intersectionX, intersectionY);
            }
        }

        return bestPoint;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 CalculateCentroid(Vector2[] points)
    {
        if (points.Length == 0) return Vector2.Zero;

        float signedArea = 0;
        float cx = 0;
        float cy = 0;

        for (int i = 0; i < points.Length; i++)
        {
            int j = (i + 1) % points.Length;
            float a = points[i].X * points[j].Y - points[j].X * points[i].Y;
            signedArea += a;
            cx += (points[i].X + points[j].X) * a;
            cy += (points[i].Y + points[j].Y) * a;
        }

        signedArea *= 0.5f;
        if (Math.Abs(signedArea) < 0.0001f) return Vector2.Zero;

        return new Vector2(cx / (6 * signedArea), cy / (6 * signedArea));
    }
}
