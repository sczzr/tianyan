using System;
using System.Linq;
using Godot;
using FantasyMapGenerator.Scripts.Map.Voronoi;
using FantasyMapGenerator.Scripts.Data;

namespace FantasyMapGenerator.Scripts.Tests
{
    public class VoronoiTests : TestSuite
    {
        public override string Name => "VoronoiGenerator";

        protected override void ExecuteTests()
        {
            TestDelaunay();
            TestVoronoi();
        }

        private void TestDelaunay()
        {
            // Create 10 random points
            var points = new Vector2[10];
            var rng = new Random();
            for(int i=0; i<10; i++)
            {
                points[i] = new Vector2(rng.Next(0, 100), rng.Next(0, 100));
            }

            var triangles = Delaunay.Triangulate(points);
            
            GD.Print($"[TestDelaunay] 10 points -> {triangles.Length} triangles");
            
            // Euler's formula for triangulation: T approx 2*V
            // Max is 2*V + 1 (including super triangle stuff usually)
            Assert(triangles.Length <= 25, $"Triangle count {triangles.Length} should be reasonable for 10 points (expected ~18-20)");
            Assert(triangles.Length > 0, "Should generate triangles");
            
            // Check circumcenters are populated
            Assert(triangles[0].Circumcenter != Vector2.Zero, "Triangle circumcenter should be calculated");
        }

        private void TestVoronoi()
        {
             // Create 5 points in a cross shape + center
             //   0
             // 1 2 3
             //   4
             var points = new Vector2[]
             {
                 new Vector2(50, 10),
                 new Vector2(10, 50),
                 new Vector2(50, 50), // Center
                 new Vector2(90, 50),
                 new Vector2(50, 90)
             };

             var triangles = Delaunay.Triangulate(points);
             var cells = VoronoiGenerator.GenerateVoronoi(points, 100, 100, triangles);

             Assert(cells.Length == 5, $"Should generate 5 cells, got {cells.Length}");
             
             // Check center cell (index 2)
             var centerCell = cells[2];
             Assert(centerCell.Vertices.Count >= 3, $"Center cell should have vertices (got {centerCell.Vertices.Count})");
             Assert(centerCell.NeighborIds.Count >= 3, $"Center cell should have neighbors (got {centerCell.NeighborIds.Count})");
             
             GD.Print($"[TestVoronoi] Center cell vertices: {centerCell.Vertices.Count}");
        }
    }
}
