using System;
using Godot;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;
using FantasyMapGenerator.Scripts.Map.Heightmap;
using FantasyMapGenerator.Scripts.Map.Voronoi;

namespace FantasyMapGenerator.Scripts.Core;

public class MapGenerator
{
    public Vector2 MapSize { get; private set; }
    public int CellCount { get; private set; }
    public MapData Data { get; private set; }
    public AleaPRNG PRNG { get; private set; }

    private HeightmapProcessor _heightmapProcessor;

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
        var points = new Vector2[count];
        int margin = 50;

        for (int i = 0; i < count; i++)
        {
            points[i] = new Vector2(
                PRNG.NextRange(margin, width - margin),
                PRNG.NextRange(margin, height - margin)
            );
        }

        return points;
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
