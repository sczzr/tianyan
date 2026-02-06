using System;
using System.Collections.Generic;
using Godot;

using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Types;
using TianYanShop.MapGeneration.Data.Entities;

namespace TianYanShop.MapGeneration.Generation
{
    public class LakeGenerator
    {
        private RandomManager _random;

        public LakeGenerator(RandomManager random)
        {
            _random = random;
        }

        public void Generate(VoronoiGraph graph, MapSettings settings)
        {
            FindEnclosedDepressions(graph);
        }

        private void FindEnclosedDepressions(VoronoiGraph graph)
        {
            int minLakeArea = 3;
            int maxLakeArea = 100;

            for (int i = 0; i < graph.CellsCount; i++)
            {
                if (graph.Heights[i] < 40)
                {
                    var basin = FloodFillBasin(i, graph);
                    if (basin.Count >= minLakeArea && basin.Count <= maxLakeArea)
                    {
                        CreateLake(graph, basin);
                    }
                }
            }
        }

        private List<int> FloodFillBasin(int startCell, VoronoiGraph graph)
        {
            var result = new List<int>();
            var queue = new Queue<int>();
            var visited = new HashSet<int>();

            float startHeight = graph.Heights[startCell];

            queue.Enqueue(startCell);
            visited.Add(startCell);

            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                result.Add(cell);

                if (graph.Neighbors[cell] != null)
                {
                    foreach (int neighbor in graph.Neighbors[cell])
                    {
                        if (!visited.Contains(neighbor) && graph.Heights[neighbor] <= startHeight + 5)
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return result;
        }

        private void CreateLake(VoronoiGraph graph, List<int> cells)
        {
            var lake = new LakeData
            {
                Id = graph.LakesList.Count,
                CellCount = cells.Count,
                Height = 30f
            };

            foreach (int cell in cells)
            {
                lake.Cells.Add(cell);
                graph.Features[cell] = (ushort)(graph.FeaturesList.Count + lake.Id);
            }

            graph.LakesList.Add(lake);
        }
    }
}
