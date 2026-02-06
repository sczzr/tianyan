using System;
using System.Collections.Generic;
using Godot;

using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Types;
using TianYanShop.MapGeneration.Data.Entities;

namespace TianYanShop.MapGeneration.Generation
{
    public class RiverGenerator
    {
        private RandomManager _random;

        public RiverGenerator(RandomManager random)
        {
            _random = random;
        }

        public void Generate(VoronoiGraph graph, MapSettings settings)
        {
            CalculateFlowDirections(graph);
            FindRiverSources(graph);
        }

        private void CalculateFlowDirections(VoronoiGraph graph)
        {
            for (int i = 0; i < graph.CellsCount; i++)
            {
                int lowestNeighbor = FindLowestNeighbor(graph, i);
                graph.Rivers[i] = (ushort)(lowestNeighbor + 1);
            }
        }

        private int FindLowestNeighbor(VoronoiGraph graph, int cell)
        {
            float lowestHeight = graph.Heights[cell];
            int lowest = -1;

            if (graph.Neighbors[cell] != null)
            {
                foreach (int neighbor in graph.Neighbors[cell])
                {
                    if (graph.Heights[neighbor] < lowestHeight)
                    {
                        lowestHeight = graph.Heights[neighbor];
                        lowest = neighbor;
                    }
                }
            }

            return lowest;
        }

        private void FindRiverSources(VoronoiGraph graph)
        {
            int minRiverLength = 3;

            for (int i = 0; i < graph.CellsCount; i++)
            {
                if (IsPotentialSource(graph, i))
                {
                    int length = MeasureRiverLength(graph, i);
                    if (length >= minRiverLength)
                    {
                        CreateRiver(graph, i);
                    }
                }
            }
        }

        private bool IsPotentialSource(VoronoiGraph graph, int cell)
        {
            return graph.Heights[cell] > 50 &&
                   (graph.Neighbors[cell] == null ||
                    graph.Rivers[cell] == 0 ||
                    graph.Heights[cell] > graph.Heights[graph.Rivers[cell] - 1]);
        }

        private int MeasureRiverLength(VoronoiGraph graph, int startCell)
        {
            int length = 0;
            int current = startCell;
            var visited = new HashSet<int>();

            while (current >= 0 && !visited.Contains(current))
            {
                visited.Add(current);
                length++;

                int next = graph.Rivers[current] - 1;
                if (next < 0 || next >= graph.CellsCount)
                    break;

                if (graph.Heights[next] >= graph.Heights[current])
                    break;

                if (graph.Heights[next] < 32)
                    break;

                current = next;
            }

            return length;
        }

        private void CreateRiver(VoronoiGraph graph, int source)
        {
            var river = new RiverData
            {
                Id = graph.RiversList.Count,
                Source = source,
                Length = 0,
                Discharge = 0
            };

            var path = new List<int>();
            int current = source;
            var visited = new HashSet<int>();

            while (current >= 0 && !visited.Contains(current))
            {
                visited.Add(current);
                path.Add(current);
                river.Cells.Add(current);

                int next = graph.Rivers[current] - 1;
                if (next < 0 || next >= graph.CellsCount)
                    break;

                if (graph.Heights[next] >= graph.Heights[current])
                    break;

                if (graph.Heights[next] < 32)
                {
                    river.Mouth = next;
                    break;
                }

                current = next;
            }

            river.Length = river.Cells.Count;
            graph.RiversList.Add(river);
        }
    }
}
