using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Map.Biomes;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Tests
{
    public class BiomeAssignerTests : TestSuite
    {
        public override string Name => "BiomeAssigner";

        protected override void ExecuteTests()
        {
            TestMarineBiome();
            TestGlacierBiome();
            TestDesertBiome();
            TestClimateDataGeneration();
        }

        private void TestMarineBiome()
        {
            int width = 1;
            int height = 1;
            var cells = CreateGridCells(width, height);
            cells[0].Height = 0.1f; // Below water level

            var assigner = new BiomeAssigner(cells, 0.35f);
            assigner.Assign();

            Assert(cells[0].BiomeId == (byte)BiomeType.Marine, "Cell with low height should be Marine");
        }

        private void TestGlacierBiome()
        {
            int width = 10;
            int height = 10;
            var cells = CreateGridCells(width, height);
            
            // Set all cells to high altitude and northern latitude (simulated)
            foreach(var c in cells)
            {
                c.Height = 0.95f; // Very high
                // Position.Y far from center (e.g. at 0 or 100) to get extreme latitude
                c.Position = new Vector2(0, 0); 
            }
            
            // To ensure minY and maxY are different
            cells[cells.Length - 1].Position = new Vector2(0, 100);
            cells[cells.Length - 1].Height = 0.95f;

            var assigner = new BiomeAssigner(cells, 0.35f);
            assigner.Assign();

            // At cold temperatures, it should be glacier or tundra
            Assert(cells[0].BiomeId == (byte)BiomeType.Glacier || cells[0].BiomeId == (byte)BiomeType.Tundra, 
                $"Cold high altitude cell should be Glacier or Tundra, got {BiomeData.Names[cells[0].BiomeId]}");
        }

        private void TestDesertBiome()
        {
            int width = 10;
            int height = 10;
            var cells = CreateGridCells(width, height);
            
            // Center cell, high temperature, low precipitation
            var center = cells[55];
            center.Height = 0.5f;
            center.Position = new Vector2(50, 50); // Near equator (simulated)

            var assigner = new BiomeAssigner(cells, 0.35f);
            assigner.Assign();

            // This is harder to test directly without mocking GenerateClimateData, 
            // but we can check if it at least runs and assigns something plausible.
            Assert(cells[55].BiomeId != (byte)BiomeType.Marine, "Equator land cell should not be Marine");
        }

        private void TestClimateDataGeneration()
        {
            int width = 10;
            int height = 10;
            var cells = CreateGridCells(width, height);
            
            // Setup positions to cover a range
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells[y * width + x].Position = new Vector2(x * 10, y * 10);
                }
            }

            var assigner = new BiomeAssigner(cells, 0.35f);
            assigner.Assign(); // Calls GenerateClimateData internally

            Assert(cells[0].Temperature < cells[5 * width].Temperature, "Northern cells (Y=0) should be colder than equatorial (Y=50)");
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
                        Height = 0.5f,
                        NeighborIds = new List<int>()
                    };
                }
            }
            return cells;
        }
    }
}
