using System;
using Godot;
using FantasyMapGenerator.Scripts.Map.Heightmap;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Tests
{
    /// <summary>
    /// Debug the GetValueInRange method
    /// </summary>
    public partial class GetValueDebug : Node
    {
        public override void _Ready()
        {
            GD.Print("\n=== GETVALUERANGE DEBUG ===\n");

            var prng = new AleaPRNG("test");
            int width = 100;
            int height = 100;
            var cells = new Cell[1];
            cells[0] = new Cell { Id = 0, Position = Vector2.Zero, Height = 0, NeighborIds = new System.Collections.Generic.List<int>() };

            var tools = new HeightmapTools(prng, cells, width, height);

            // Create a reflection-based test to call GetValueInRange
            var method = typeof(HeightmapTools).GetMethod("GetValueInRange",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method != null)
            {
                GD.Print("Testing GetValueInRange with different inputs:");

                float result1 = (float)method.Invoke(tools, new object[] { "50" });
                GD.Print($"  GetValueInRange('50') = {result1}");

                float result2 = (float)method.Invoke(tools, new object[] { "50-50" });
                GD.Print($"  GetValueInRange('50-50') = {result2}");

                float result3 = (float)method.Invoke(tools, new object[] { "0.5" });
                GD.Print($"  GetValueInRange('0.5') = {result3}");

                float result4 = (float)method.Invoke(tools, new object[] { "" });
                GD.Print($"  GetValueInRange('') = {result4}");

                // Test Clamp behavior
                var clampMethod = typeof(HeightmapTools).GetMethod("Clamp",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                GD.Print("");
                GD.Print("Testing Clamp:");

                float c1 = (float)clampMethod.Invoke(null, new object[] { 50.0f, 0.0f, 100.0f });
                GD.Print($"  Clamp(50) = {c1}");

                float c2 = (float)clampMethod.Invoke(null, new object[] { 0.5f, 0.0f, 100.0f });
                GD.Print($"  Clamp(0.5) = {c2}");

                float c3 = (float)clampMethod.Invoke(null, new object[] { 150.0f, 0.0f, 100.0f });
                GD.Print($"  Clamp(150) = {c3}");
            }
            else
            {
                GD.PrintErr("Could not find GetValueInRange method!");
            }

            GD.Print("\n=== END DEBUG ===\n");
            QueueFree();
        }
    }
}
