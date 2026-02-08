using System;
using Godot;
using FantasyMapGenerator.Scripts.Tests;

namespace FantasyMapGenerator.Scripts.Tests
{
    public partial class ManualTestRunner : Node
    {
        public override void _Ready()
        {
            GD.Print("Manual Test Runner Started");
            try 
            {
                var runner = new TestRunner();
                runner.RunAllTests();
            }
            catch (Exception e)
            {
                GD.PrintErr($"Test execution failed: {e.Message}\n{e.StackTrace}");
            }
            GD.Print("Manual Test Runner Finished. You can close the window.");
            // GetTree().Quit(); // Uncomment to auto-quit if needed
        }
    }
}
