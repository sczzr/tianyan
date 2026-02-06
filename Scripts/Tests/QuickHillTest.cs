using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Map.Heightmap;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Tests
{
    public partial class QuickHillTest : Node
    {
        public override void _Ready()
        {
            GD.Print("=== Quick Hill Test ===");

            var prng = new AleaPRNG("test");
            int w = 100, h = 100;
            var cells = new Cell[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    cells[i] = new Cell
                    {
                        Id = i,
                        Position = new Vector2(x, y),
                        Height = 0,
                        NeighborIds = new List<int>()
                    };
                }
            }

            // Setup 4-connectivity
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (x > 0) cells[i].NeighborIds.Add(i - 1);
                    if (x < w - 1) cells[i].NeighborIds.Add(i + 1);
                    if (y > 0) cells[i].NeighborIds.Add(i - w);
                    if (y < h - 1) cells[i].NeighborIds.Add(i + w);
                }
            }

            var tools = new HeightmapTools(prng, cells, w, h);

            // Test 1: Verify initial state
            bool allZero = true;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].Height != 0)
                {
                    allZero = false;
                    break;
                }
            }
            GD.Print($"Test 1 - All cells initially zero: {allZero}");

            // Test 2: AddHill with '50'
            tools.AddHill("1", "50", "50-50", "50-50");

            float maxH = 0, minH = 1.0f;
            int maxIdx = -1, countPos = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].Height > maxH) { maxH = cells[i].Height; maxIdx = i; }
                if (cells[i].Height < minH) minH = cells[i].Height;
                if (cells[i].Height > 0.001f) countPos++;
            }

            GD.Print($"");
            GD.Print($"Test 2 - After AddHill('1', '50', '50-50', '50-50'):");
            GD.Print($"  Max height: {maxH:F6} at index {maxIdx}");
            GD.Print($"  Min height: {minH:F6}");
            GD.Print($"  Cells with height > 0.001: {countPos}");
            GD.Print($"  Expected max height: 0.5 ± 0.1");
            GD.Print($"  Test passed: {Math.Abs(maxH - 0.5f) <= 0.1f}");

            GD.Print("");
            GD.Print("=== Test Complete ===");

            QueueFree();
        }
    }
}
