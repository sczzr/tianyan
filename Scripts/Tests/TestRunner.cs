using System;
using System.Collections.Generic;
using Godot;

namespace FantasyMapGenerator.Scripts.Tests
{
    public abstract class TestSuite
    {
        public abstract string Name { get; }
        public int Passed { get; private set; }
        public int Failed { get; private set; }

        public void Run()
        {
            Passed = 0;
            Failed = 0;
            GD.Print($"\n--- Running Test Suite: {Name} ---");
            ExecuteTests();
            GD.Print($"--- {Name} Finished: {Passed} Passed, {Failed} Failed ---\n");
        }

        protected abstract void ExecuteTests();

        protected void Assert(bool condition, string message)
        {
            if (condition)
            {
                Passed++;
                GD.Print($"[PASS] {message}");
            }
            else
            {
                Failed++;
                GD.PrintErr($"[FAIL] {message}");
            }
        }

        protected void AssertAlmostEqual(float actual, float expected, float tolerance, string message)
        {
            if (Math.Abs(actual - expected) <= tolerance)
            {
                Passed++;
                GD.Print($"[PASS] {message} (Expected: {expected}, Actual: {actual})");
            }
            else
            {
                Failed++;
                GD.PrintErr($"[FAIL] {message} (Expected: {expected}, Actual: {actual}, Diff: {Math.Abs(actual - expected)})");
            }
        }
    }

    public partial class TestRunner : Node
    {
        public override void _Ready()
        {
            RunAllTests();
        }

        public void RunAllTests()
        {
            GD.Print("========================================");
            GD.Print("   STARTING MAP GENERATION VERIFICATION ");
            GD.Print("========================================");

            var suites = new List<TestSuite>
            {
                new HeightmapToolsTests(),
                new FeatureDetectorTests(),
                new RiverGeneratorTests(),
                new BiomeAssignerTests(),
                new IntegrationTests(),
                // new PerformanceTests(), // Temporarily commented out
                new AleaPRNGTests(),
                new VoronoiTests()
            };

            int totalPassed = 0;
            int totalFailed = 0;

            foreach (var suite in suites)
            {
                suite.Run();
                totalPassed += suite.Passed;
                totalFailed += suite.Failed;
            }

            GD.Print("========================================");
            GD.Print($" FINAL RESULTS: {totalPassed} Passed, {totalFailed} Failed");
            GD.Print("========================================");

            if (totalFailed == 0)
            {
                GD.Print("ALL VERIFICATION STEPS COMPLETED SUCCESSFULLY!");
            }
            else
            {
                GD.PrintErr("VERIFICATION FAILED WITH ERRORS.");
            }
        }
    }
}
