using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Map.Features;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Tests
{
    public class FeatureDetectorTests : TestSuite
    {
        public override string Name => "FeatureDetector";

        protected override void ExecuteTests()
        {
            TestBasicDetection();
            TestIslandClassification();
            TestLakeDetection();
        }

        private void TestBasicDetection()
        {
            // Create a 10x10 grid. Center 4x4 is island, rest is ocean.
            int width = 10;
            int height = 10;
            var cells = CreateGridCells(width, height);
            
            float waterLevel = 0.5f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    if (x >= 3 && x <= 6 && y >= 3 && y <= 6)
                        cells[i].Height = 1.0f; // Land
                    else
                        cells[i].Height = 0.0f; // Water
                }
            }

            var detector = new FeatureDetector(cells, waterLevel);
            var features = detector.Detect();

            Assert(features.Count >= 2, $"Should detect at least 2 features, found {features.Count}");
            
            bool foundIsland = false;
            bool foundOcean = false;
            foreach (var f in features)
            {
                if (f.Type == FeatureType.Island) foundIsland = true;
                if (f.Type == FeatureType.Ocean) foundOcean = true;
            }

            Assert(foundIsland, "Should detect an island");
            Assert(foundOcean, "Should detect an ocean");
        }

        private void TestIslandClassification()
        {
            // Create a small island
            int width = 100;
            int height = 100;
            var cells = CreateGridCells(width, height);
            
            // Single cell island -> Isle
            cells[50 * width + 50].Height = 1.0f;
            
            var detector = new FeatureDetector(cells, 0.5f);
            var features = detector.Detect();
            
            Feature island = features.Find(f => f.IsLand);
            Assert(island != null, "Island should be found");
            if (island != null)
            {
                Assert(island.Group == FeatureGroup.Isle, $"Single cell island should be Isle, but got {island.Group}");
            }
        }

        private void TestLakeDetection()
        {
            // Center is water, surrounded by land, surrounded by ocean
            // Actually simpler: center 2x2 water, surrounded by land
            int width = 10;
            int height = 10;
            var cells = CreateGridCells(width, height);
            
            for (int i = 0; i < cells.Length; i++) cells[i].Height = 1.0f; // All land
            
            // Center lake
            cells[4 * width + 4].Height = 0.0f;
            cells[4 * width + 5].Height = 0.0f;
            cells[5 * width + 4].Height = 0.0f;
            cells[5 * width + 5].Height = 0.0f;

            var detector = new FeatureDetector(cells, 0.5f);
            var features = detector.Detect();

            bool foundLake = features.Exists(f => f.Type == FeatureType.Lake);
            Assert(foundLake, "Should detect a lake");
        }

        private Cell[] CreateGridCells(int width, int height)
        {
            var cells = new Cell[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    cells[i] = new Cell
                    {
                        Id = i,
                        Position = new Vector2(x, y),
                        Height = 0,
                        NeighborIds = new List<int>()
                    };
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    if (x > 0) cells[i].NeighborIds.Add(i - 1);
                    if (x < width - 1) cells[i].NeighborIds.Add(i + 1);
                    if (y > 0) cells[i].NeighborIds.Add(i - width);
                    if (y < height - 1) cells[i].NeighborIds.Add(i + width);
                }
            }

            return cells;
        }
    }
}
