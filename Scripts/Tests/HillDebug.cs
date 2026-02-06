using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Map.Heightmap;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Tests
{
    /// <summary>
    /// Standalone test to debug the AddHill function
    /// </summary>
    public partial class HillDebug : Node
    {
        public override void _Ready()
        {
            GD.Print("\n=== ADDHILL DEBUG ===\n");

            var prng = new AleaPRNG("test");
            int width = 100;
            int height = 100;
            var cells = CreateEmptyCells(width, height);
            var tools = new HeightmapTools(prng, cells, width, height);

            GD.Print("Test parameters:");
            GD.Print("  countRange = '1'");
            GD.Print("  heightRange = '50'");
            GD.Print("  rangeX = '50-50'");
            GD.Print("  rangeY = '50-50'");
            GD.Print("");

            tools.AddHill("1", "50", "50-50", "50-50");

            float maxHeight = 0;
            float minHeight = 1.0f;
            int maxIndex = -1;
            int minIndex = -1;

            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].Height > maxHeight)
                {
                    maxHeight = cells[i].Height;
                    maxIndex = i;
                }
                if (cells[i].Height < minHeight)
                {
                    minHeight = cells[i].Height;
                    minIndex = i;
                }
            }

            GD.Print("Results:");
            GD.Print($"  Max height: {maxHeight:F4} at index {maxIndex} (pos: {cells[maxIndex].Position})");
            GD.Print($"  Min height: {minHeight:F4} at index {minIndex} (pos: {cells[minIndex].Position})");
            GD.Print("");

            GD.Print("Test assertion:");
            GD.Print($"  Expected: 0.5 ± 0.1");
            GD.Print($"  Actual: {maxHeight:F4}");
            GD.Print($"  Diff: {Math.Abs(maxHeight - 0.5f):F4}");
            GD.Print($"  Pass: {Math.Abs(maxHeight - 0.5f) <= 0.1f}");
            GD.Print("");

            GD.Print("=== END DEBUG ===\n");

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
