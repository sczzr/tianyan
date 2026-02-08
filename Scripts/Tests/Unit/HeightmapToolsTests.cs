using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Map.Heightmap;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Tests
{
    public class HeightmapToolsTests : TestSuite
    {
        public override string Name => "HeightmapTools";

        protected override void ExecuteTests()
        {
            TestAddHill();
            TestAddPit();
            TestSmooth();
            TestMask();
        }

        private void TestAddHill()
        {
            var prng = new AleaPRNG("test");
            int width = 100;
            int height = 100;
            var cells = CreateEmptyCells(width, height);
            var tools = new HeightmapTools(prng, cells, width, height);

            tools.AddHill("1", "50", "50-50", "50-50");

            float maxHeight = 0;
            foreach (var cell in cells)
            {
                if (cell.Height > maxHeight) maxHeight = cell.Height;
            }

            Assert(maxHeight > 0, "AddHill should increase height");
            AssertAlmostEqual(maxHeight, 0.5f, 0.1f, "AddHill height should be near specified value");
        }

        private void TestAddPit()
        {
            var prng = new AleaPRNG("test");
            int width = 100;
            int height = 100;
            var cells = CreateEmptyCells(width, height, 0.8f);
            var tools = new HeightmapTools(prng, cells, width, height);

            tools.AddPit("1", "50", "50-50", "50-50");

            float minHeight = 1.0f;
            foreach (var cell in cells)
            {
                if (cell.Height < minHeight) minHeight = cell.Height;
            }

            Assert(minHeight < 0.8f, "AddPit should decrease height");
        }

        private void TestSmooth()
        {
            var prng = new AleaPRNG("test");
            int width = 10;
            int height = 10;
            var cells = CreateEmptyCells(width, height);
            cells[5 * width + 5].Height = 1.0f; // Single spike

            var tools = new HeightmapTools(prng, cells, width, height);
            tools.Smooth("1");

            Assert(cells[5 * width + 5].Height < 1.0f, "Smooth should reduce spike height");
            Assert(cells[5 * width + 4].Height > 0, "Smooth should distribute height to neighbors");
        }

        private void TestMask()
        {
            var prng = new AleaPRNG("test");
            int width = 100;
            int height = 100;
            var cells = CreateEmptyCells(width, height, 1.0f);
            var tools = new HeightmapTools(prng, cells, width, height);

            tools.Mask("1.0");

            Assert(cells[0].Height < 0.1f, "Mask should reduce height at corners");
            Assert(cells[50 * width + 50].Height > 0.9f, "Mask should keep height at center");
        }

        private Cell[] CreateEmptyCells(int width, int height, float initialHeight = 0)
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
                        Height = initialHeight,
                        NeighborIds = new List<int>()
                    };
                }
            }

            // Setup simple 4-connectivity for smoothing
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
