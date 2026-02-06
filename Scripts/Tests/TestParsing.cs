using System;
using System.Globalization;
using Godot;

namespace FantasyMapGenerator.Scripts.Tests
{
    public partial class TestParsing : Node
    {
        public override void _Ready()
        {
            GD.Print("=== Testing String Parsing ===");

            string[] testCases = { "50", "50.0", "0.5", "100", "50-50", "0.5-0.5" };

            foreach (var tc in testCases)
            {
                if (tc.Contains('-'))
                {
                    GD.Print($"  '{tc}' is a range (needs parsing)");
                }
                else
                {
                    if (float.TryParse(tc, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                    {
                        GD.Print($"  '{tc}' -> {f} (as float)");
                        GD.Print($"      {f}/100 = {f/100}");
                        GD.Print($"      {f}*100 = {f*100}");
                    }
                    else
                    {
                        GD.Print($"  '{tc}' -> PARSE FAILED");
                    }
                }
            }

            GD.Print("");
            GD.Print("Expected behavior for heightRange='50':");
            GD.Print("  - Parse: '50' -> 50.0f");
            GD.Print("  - Clamp(50) -> 50 (between 0-100)");
            GD.Print("  - change[start] = 50");
            GD.Print("  - final height = (0 * 100 + 50) / 100 = 0.5");
            GD.Print("");
            GD.Print("If actual is 1.0, then either:");
            GD.Print("  - heightRange is being interpreted as '100' somehow");
            GD.Print("  - Or there's multiplication somewhere");
            GD.Print("  - Or the change[] array value is 100");

            GD.Print("=== End Test ===");

            QueueFree();
        }
    }
}
