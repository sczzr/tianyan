using System;
using Godot;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Tests
{
    public class AleaPRNGTests : TestSuite
    {
        public override string Name => "AleaPRNG";

        protected override void ExecuteTests()
        {
            TestRange();
            TestSeeding();
        }

        private void TestRange()
        {
            var prng = new AleaPRNG(12345);
            int iterations = 10000;
            double min = 1.0;
            double max = 0.0;

            for (int i = 0; i < iterations; i++)
            {
                double val = prng.NextDouble();
                if (val < min) min = val;
                if (val > max) max = val;

                Assert(val >= 0.0 && val < 1.0, $"Value {val} should be in [0, 1)");
            }

            GD.Print($"[AleaPRNG] Min: {min}, Max: {max}");
            Assert(max < 1.0, $"Max value {max} should be < 1.0");
        }

        private void TestSeeding()
        {
            var prng1 = new AleaPRNG("seed1");
            var prng2 = new AleaPRNG("seed1");
            var prng3 = new AleaPRNG("seed2");

            Assert(prng1.NextDouble() == prng2.NextDouble(), "Same seed should produce same sequence");
            Assert(prng1.NextDouble() != prng3.NextDouble(), "Different seed should produce different sequence");
        }
    }
}
