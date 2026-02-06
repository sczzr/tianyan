using System;
using System.Collections.Generic;
using Godot;

using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Types;
using TianYanShop.MapGeneration.Data.Entities;

namespace TianYanShop.MapGeneration.Generation
{
    public class FeatureGenerator
    {
        private RandomManager _random;

        public FeatureGenerator(RandomManager random)
        {
            _random = random;
        }

        public void Generate(VoronoiGraph graph, MapSettings settings)
        {
            DetectOceanAndLand(graph, settings);
            IdentifyIslands(graph);
            IdentifyLakes(graph);
        }

        private void DetectOceanAndLand(VoronoiGraph graph, MapSettings settings)
        {
            float oceanThreshold = 32f;
            int oceanFeatureId = 0;

            for (int i = 0; i < graph.CellsCount; i++)
            {
                if (graph.Heights[i] < oceanThreshold)
                {
                    graph.Features[i] = (ushort)oceanFeatureId;
                }
            }
        }

        private void IdentifyIslands(VoronoiGraph graph)
        {
            int oceanId = 0;

            for (int i = 0; i < graph.CellsCount; i++)
            {
                if (graph.Features[i] == oceanId && graph.Heights[i] >= 32)
                {
                    var islandCells = FloodFillLand(i, graph);
                    if (islandCells.Count > 0)
                    {
                        var feature = new Feature
                        {
                            Id = graph.FeaturesList.Count,
                            Type = FeatureType.Island,
                            CellCount = islandCells.Count
                        };

                        foreach (int cell in islandCells)
                        {
                            feature.Cells.Add(cell);
                            graph.Features[cell] = (ushort)feature.Id;
                        }

                        graph.FeaturesList.Add(feature);
                    }
                }
            }
        }

        private List<int> FloodFillLand(int startCell, VoronoiGraph graph)
        {
            var result = new List<int>();
            var queue = new Queue<int>();
            var visited = new HashSet<int>();

            queue.Enqueue(startCell);
            visited.Add(startCell);

            float oceanThreshold = 32f;

            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                result.Add(cell);

                if (graph.Neighbors[cell] != null)
                {
                    foreach (int neighbor in graph.Neighbors[cell])
                    {
                        if (!visited.Contains(neighbor) &&
                            graph.Heights[neighbor] >= oceanThreshold &&
                            graph.Features[neighbor] == graph.Features[startCell])
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return result;
        }

        private void IdentifyLakes(VoronoiGraph graph)
        {
        }
    }
}
