using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Map.Heightmap;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Tests
{
    /// <summary>
    /// Debug test for AddHill to understand the height calculation
    /// </summary>
    public partial class DebugHillTest : Node
    {
        public override void _Ready()
        {
            GD.Print("=== DEBUG ADDHILL TEST ===");

            var prng = new AleaPRNG("test");
            int width = 100;
            int height = 100;

            GD.Print($"Test grid: {width}x{height}");

            // Create cells exactly like the test
            var cells = CreateEmptyCells(width, height);

            GD.Print($"Initial cell[0].Height = {cells[0].Height}");

            var tools = new HeightmapTools(prng, cells, width, height);

            GD.Print("Calling AddHill('1', '50', '50-50', '50-50')...");
            tools.AddHill("1", "50", "50-50", "50-50");

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

            GD.Print($"Max height: {maxHeight}");
            GD.Print($"Max index: {maxIndex}");
            GD.Print($"Max position: {cells[maxIndex].Position}");

            // Show some neighbor heights
            GD.Print("\nNeighbor heights around max:");
            int x = maxIndex % width;
            int y = maxIndex / width;
            int startRow = Math.Max(0, y - 2);
            int endRow = Math.Min(height - 1, y + 2);
            int startCol = Math.Max(0, x - 2);
            int endCol = Math.Min(width - 1, x + 2);

            for (int ry = startRow; ry <= endRow; ry++)
            {
                string line = "";
                for (int rx = startCol; rx <= endCol; rx++)
                {
                    int idx = ry * width + rx;
                    line += $"{cells[idx].Height:F2} ";
                }
                GD.Print(line);
            }

            GD.Print("\n=== END DEBUG ===");
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
