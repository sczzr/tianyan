using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using FantasyMapGenerator.Scripts.Map.Rivers;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Tests
{
    public class RiverGeneratorTests : TestSuite
    {
        public override string Name => "RiverGenerator";

        protected override void ExecuteTests()
        {
            TestDepressionResolution();
            TestRiverFlow();
        }

        private void TestDepressionResolution()
        {
            // Create a 3x3 grid with a pit in the middle
            // 0.8 0.8 0.8
            // 0.8 0.5 0.8
            // 0.8 0.8 0.8
            // With a border cell at 0,0 having 0.0 height
            int width = 3;
            int height = 3;
            var cells = CreateGridCells(width, height);
            
            foreach(var c in cells) c.Height = 0.8f;
            cells[4].Height = 0.5f; // Pit
            cells[0].IsBorder = true;
            cells[0].Height = 0.1f; // Target for drainage

            float waterLevel = 0.35f;
            var resolver = new DepressionResolver(cells, new List<Feature>(), waterLevel);
            var resolvedHeights = resolver.Resolve();

            Assert(resolvedHeights[4] > 0.5f, $"Depression should be filled (original 0.5, resolved {resolvedHeights[4]})");
            
            // Check if it's strictly descending towards the border somewhere
            // In a small 3x3, the pit at 4 should be lifted to at least some neighbor's height + epsilon
        }

        private void TestRiverFlow()
        {
            // Simple slope
            // 1.0 -> 0.8 -> 0.6 -> 0.4 -> 0.2 (water)
            int width = 5;
            int height = 1;
            var cells = CreateGridCells(width, height);
            cells[0].Height = 1.0f;
            cells[1].Height = 0.8f;
            cells[2].Height = 0.6f;
            cells[3].Height = 0.4f;
            cells[4].Height = 0.1f; // Water

            var prng = new AleaPRNG("test");
            float waterLevel = 0.35f;
            
            // Give cell 0 some precipitation to trigger river
            cells[0].Precipitation = 100;
            cells[1].Precipitation = 100;

            float[] heights = cells.Select(c => c.Height).ToArray();
            var generator = new RiverGenerator(cells, new List<Feature>(), prng, heights, waterLevel);
            
            // We need to trigger DrainWater and DefineRivers
            // Generate calls these
            var rivers = generator.Generate();

            Assert(rivers.Count > 0, "Should generate at least one river on a slope");
            if (rivers.Count > 0)
            {
                Assert(rivers[0].Cells.Count >= 4, $"River should flow through at least 4 cells, got {rivers[0].Cells.Count}");
                Assert(rivers[0].Mouth == 3 || rivers[0].Mouth == 4, "River mouth should be near the water");
            }
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
