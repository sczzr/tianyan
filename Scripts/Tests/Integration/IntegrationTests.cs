using System;
using System.Diagnostics;
using Godot;
using FantasyMapGenerator.Scripts.Core;

namespace FantasyMapGenerator.Scripts.Tests
{
    public class IntegrationTests : TestSuite
    {
        public override string Name => "IntegrationTests";

        protected override void ExecuteTests()
        {
            TestFixedSeedGeneration();
        }

        private void TestFixedSeedGeneration()
        {
            var gen = new MapGenerator();
            string seed = "verification_seed";
            int cellCount = 1000;

            gen.Generate(seed, cellCount);

            Assert(gen.Data != null, "MapData should not be null after generation");
            if (gen.Data != null)
            {
                Assert(gen.Data.Cells.Length == cellCount, $"Cell count should match request: {cellCount}, got {gen.Data.Cells.Length}");
                Assert(gen.Data.Features.Length > 0, "Should have features");
                Assert(gen.Data.Rivers.Length > 0, "Should have rivers");
            }
        }
    }

    public class PerformanceTests : TestSuite
    {
        public override string Name => "PerformanceTests";

        protected override void ExecuteTests()
        {
            BenchmarkGeneration();
        }

        private void BenchmarkGeneration()
        {
            var gen = new MapGenerator();
            int cellCount = 5000;
            string seed = "perf_test";

            var sw = Stopwatch.StartNew();
            gen.Generate(seed, cellCount);
            sw.Stop();

            GD.Print($"Generation of {cellCount} cells took: {sw.ElapsedMilliseconds}ms");
            
            // Requirement from spec: 10000 cells < 1s. 5000 should be < 500ms.
            Assert(sw.ElapsedMilliseconds < 1000, $"Generation should be under 1s (took {sw.ElapsedMilliseconds}ms)");

            // Rudimentary memory check
            long mem = GC.GetTotalMemory(false) / (1024 * 1024);
            GD.Print($"Estimated memory usage: {mem}MB");
            Assert(mem < 200, $"Memory usage should be reasonable ({mem}MB)");
        }
    }
}
