using System;
using Godot;

using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;

namespace TianYanShop.MapGeneration.Generation
{
    public class HeightmapGenerator
    {
        private RandomManager _random;

        public HeightmapGenerator(RandomManager random)
        {
            _random = random;
        }

        public void Generate(VoronoiGraph graph, MapSettings settings)
        {
            int cellsCount = graph.CellsCount;

            for (int i = 0; i < cellsCount; i++)
            {
                float noiseValue = Noise2D(
                    graph.Points[i].X * 0.01f,
                    graph.Points[i].Y * 0.01f
                );
                graph.Heights[i] = (byte)global::System.Math.Clamp((noiseValue + 1) * 50, 0, 100);
            }

            AddHills(graph);
            AddMountains(graph);
        }

        private float Noise2D(float x, float y)
        {
            return SimplexNoise(x, y);
        }

        private float SimplexNoise(float x, float y)
        {
            float result = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;

            for (int i = 0; i < 6; i++)
            {
                result += Noise(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            return result / maxValue;
        }

        private float Noise(float x, float y)
        {
            int X = (int)MathF.Floor(x) & 255;
            int Y = (int)MathF.Floor(y) & 255;
            x -= MathF.Floor(x);
            y -= MathF.Floor(y);

            float u = Fade(x);
            float v = Fade(y);

            int A = (X + 0) + (Y + 0) % 256;
            int B = (X + 1) + (Y + 0) % 256;

            return Lerp(v,
                Lerp(u, Grad(Hash(A), x, y), Grad(Hash(B), x - 1, y)),
                Lerp(u, Grad(Hash(A + 1), x, y - 1), Grad(Hash(B + 1), x - 1, y - 1))
            );
        }

        private float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
        private float Lerp(float t, float a, float b) => a + t * (b - a);
        private float Grad(int hash, float x, float y)
        {
            int h = hash & 7;
            float u = h < 4 ? x : y;
            float v = h < 4 ? y : x;
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -2f * v : 2f * v);
        }

        private int Hash(int n)
        {
            n = (n << 13) ^ n;
            return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff);
        }

        private void AddHills(VoronoiGraph graph)
        {
            int numHills = graph.CellsCount / 200;
            for (int i = 0; i < numHills; i++)
            {
                int cell = _random.NextInt(graph.CellsCount);
                float radius = _random.NextRange(2f, 8f);
                float strength = _random.NextRange(0.1f, 0.3f);

                ApplyGaussianHill(graph, cell, radius, strength);
            }
        }

        private void AddMountains(VoronoiGraph graph)
        {
            int numMountains = graph.CellsCount / 500;
            for (int i = 0; i < numMountains; i++)
            {
                int cell = _random.NextInt(graph.CellsCount);
                float radius = _random.NextRange(3f, 10f);
                float strength = _random.NextRange(0.2f, 0.5f);

                ApplyMountain(graph, cell, radius, strength);
            }
        }

        private void ApplyGaussianHill(VoronoiGraph graph, int center, float radius, float strength)
        {
            float radiusSq = radius * radius;

            foreach (var neighbor in GetNeighborsInRadius(graph, center, radius))
            {
                float distSq = DistanceSquared(graph.Points[center], graph.Points[neighbor]);
                float falloff = MathF.Exp(-distSq / (2 * radiusSq * radiusSq));
                float increment = strength * falloff * 50f;
                graph.Heights[neighbor] = (byte)global::System.Math.Clamp(graph.Heights[neighbor] + increment, 0, 100);
            }
        }

        private void ApplyMountain(VoronoiGraph graph, int center, float radius, float strength)
        {
            float radiusSq = radius * radius;

            foreach (var neighbor in GetNeighborsInRadius(graph, center, radius))
            {
                float dist = MathF.Sqrt(DistanceSquared(graph.Points[center], graph.Points[neighbor]));
                float falloff = 1f - global::System.Math.Min(dist / radius, 1f);
                falloff = falloff * falloff;
                float increment = strength * falloff * 60f;
                graph.Heights[neighbor] = (byte)global::System.Math.Clamp(graph.Heights[neighbor] + increment, 0, 100);
            }
        }

        private System.Collections.Generic.List<int> GetNeighborsInRadius(VoronoiGraph graph, int center, float radius)
        {
            var result = new System.Collections.Generic.List<int>();
            float radiusSq = radius * radius;
            Vector2 centerPoint = graph.Points[center];

            for (int i = 0; i < graph.CellsCount; i++)
            {
                float distSq = DistanceSquared(centerPoint, graph.Points[i]);
                if (distSq <= radiusSq)
                    result.Add(i);
            }

            return result;
        }

        private float DistanceSquared(Vector2 a, Vector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }
    }
}
