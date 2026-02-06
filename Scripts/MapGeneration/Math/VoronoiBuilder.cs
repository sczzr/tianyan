using System;
using System.Collections.Generic;
using Godot;

using TianYanShop.MapGeneration.Data;

namespace TianYanShop.MapGeneration.Math
{
    public static class VoronoiBuilder
    {
        public static void BuildVoronoi(VoronoiGraph graph)
        {
            if (graph.Points == null || graph.Points.Length == 0)
                return;

            int numPoints = graph.Points.Length;
            double[] points = new double[numPoints * 2];

            for (int i = 0; i < numPoints; i++)
            {
                points[2 * i] = graph.Points[i].X;
                points[2 * i + 1] = graph.Points[i].Y;
            }

            var delaunator = new Delaunator(points);
            delaunator.Triangulate();

            int[] triangles = delaunator.Triangles;
            int[] halfedges = delaunator.Halfedges;

            graph.Neighbors = new int[numPoints][];

            for (int i = 0; i < numPoints; i++)
            {
                graph.Neighbors[i] = Array.Empty<int>();
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int t0 = triangles[i];
                int t1 = triangles[i + 1];
                int t2 = triangles[i + 2];

                if (t0 < 0 || t1 < 0 || t2 < 0) continue;

                AddNeighbor(graph.Neighbors, t0, t1);
                AddNeighbor(graph.Neighbors, t1, t2);
                AddNeighbor(graph.Neighbors, t2, t0);
            }

            for (int i = 0; i < numPoints; i++)
            {
                if (graph.Neighbors[i] != null)
                {
                    graph.Neighbors[i] = RemoveDuplicates(graph.Neighbors[i]);
                }
            }
        }

        private static void AddNeighbor(int[][] neighbors, int a, int b)
        {
            if (neighbors[a] == null)
            {
                neighbors[a] = new int[] { b };
            }
            else
            {
                var list = new List<int>(neighbors[a]);
                if (!list.Contains(b))
                {
                    list.Add(b);
                    neighbors[a] = list.ToArray();
                }
            }
        }

        private static int[] RemoveDuplicates(int[] array)
        {
            if (array == null || array.Length <= 1)
                return array ?? Array.Empty<int>();

            var unique = new HashSet<int>(array);
            var result = new int[unique.Count];
            unique.CopyTo(result);
            return result;
        }

        public static void ClipVoronoiCells(VoronoiGraph graph, int width, int height)
        {
            graph.Width = width;
            graph.Height = height;
            graph.IsBorder = new bool[graph.Points.Length];

            for (int i = 0; i < graph.Points.Length; i++)
            {
                Vector2 p = graph.Points[i];
                if (p.X < 0 || p.X > width || p.Y < 0 || p.Y > height)
                {
                    graph.IsBorder[i] = true;
                }
            }
        }
    }
}
