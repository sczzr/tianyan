using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Map.Heightmap;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Tests
{
    /// <summary>
    /// Debug test for AddHill - Step-by-step execution
    /// </summary>
    public partial class HillTestDebug : Node
    {
        public override void _Ready()
        {
            GD.Print("\n=== ADDHILL STEP-BY-STEP DEBUG ===\n");

            // Recreate the exact test scenario
            var prng = new AleaPRNG("test");
            int width = 100;
            int height = 100;

            GD.Print("Step 1: Create 100x100 grid with initial height 0");
            var cells = CreateEmptyCells(width, height);
            GD.Print($"  Grid created: {cells.Length} cells");
            GD.Print($"  Sample heights: cells[0]={cells[0].Height}, cells[5000]={cells[5000].Height}");
            GD.Print("");

            GD.Print("Step 2: Create HeightmapTools with PRNG and cells");
            var tools = new HeightmapTools(prng, cells, width, height);
            GD.Print("  HeightmapTools created");
            GD.Print("");

            GD.Print("Step 3: Call AddHill('1', '50', '50-50', '50-50')");
            GD.Print("  countRange='1', heightRange='50', rangeX='50-50', rangeY='50-50'");
            GD.Print("");

            tools.AddHill("1", "50", "50-50", "50-50");

            GD.Print("Step 4: Find maximum height");
            float maxHeight = 0;
            int maxIndex = -1;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].Height > maxHeight)
                {
                    maxHeight = cells[i].Height;
                    maxIndex = i;
                }
            }

            GD.Print($"  Maximum height: {maxHeight:F6}");
            GD.Print($"  Maximum index: {maxIndex}");
            GD.Print($"  Maximum position: ({cells[maxIndex].Position.X}, {cells[maxIndex].Position.Y})");
            GD.Print("");

            GD.Print("Step 5: Analyze height distribution");
            int positiveCount = 0;
            float avgHeight = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].Height > 0.01f)
                {
                    positiveCount++;
                    avgHeight += cells[i].Height;
                }
            }
            if (positiveCount > 0) avgHeight /= positiveCount;

            GD.Print($"  Cells with height > 0.01: {positiveCount} ({positiveCount * 100.0 / cells.Length:F1}%)");
            GD.Print($"  Average positive height: {avgHeight:F6}");
            GD.Print("");

            GD.Print("Step 6: Test assertions");
            GD.Print($"  Test 1: maxHeight > 0");
            GD.Print($"    Result: {maxHeight > 0} (actual: {maxHeight})");
            GD.Print("");

            GD.Print($"  Test 2: maxHeight ≈ 0.5 ± 0.1");
            float diff = Math.Abs(maxHeight - 0.5f);
            GD.Print($"    Expected: 0.5");
            GD.Print($"    Actual: {maxHeight}");
            GD.Print($"    Difference: {diff}");
            GD.Print($"    Tolerance: 0.1");
            GD.Print($"    Result: {diff <= 0.1}");
            GD.Print("");

            if (diff > 0.1)
            {
                GD.PrintErr("TEST FAILED!");
                GD.PrintErr($"The expected behavior (height=50 / 100 = 0.5) is not matching.");
                GD.PrintErr($"Actual maxHeight = {maxHeight:F6}");

                // Show what heights are present
                GD.PrintErr("");
                GD.PrintErr("Height distribution:");
                Dictionary<string, int> buckets = new();
                foreach (var c in cells)
                {
                    string key = c.Height.ToString("F2");
                    if (!buckets.ContainsKey(key)) buckets[key] = 0;
                    buckets[key]++;
                }
                foreach (var kvp in buckets)
                {
                    if (kvp.Value > 10) GD.PrintErr($"  {kvp.Key}: {kvp.Value} cells");
                }
            }
            else
            {
                GD.Print("TEST PASSED!");
            }

            GD.Print("\n=== END DEBUG ===\n");

            QueueFree();
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
