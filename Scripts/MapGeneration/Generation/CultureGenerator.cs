using System;
using System.Collections.Generic;
using Godot;

using TianYanShop.MapGeneration.Core;
using TianYanShop.MapGeneration.Data;
using TianYanShop.MapGeneration.Data.Types;
using TianYanShop.MapGeneration.Data.Entities;

namespace TianYanShop.MapGeneration.Generation
{
    public class CultureGenerator
    {
        private RandomManager _random;

        public CultureGenerator(RandomManager random)
        {
            _random = random;
        }

        public void Generate(VoronoiGraph graph, MapSettings settings)
        {
            int numCultures = global::System.Math.Max(5, settings.StatesNumber / 2);

            GenerateInitialCultures(graph, numCultures);
            ExpandCultures(graph);
        }

        private void GenerateInitialCultures(VoronoiGraph graph, int numCultures)
        {
            var cultureCenters = new HashSet<int>();

            for (int i = 0; i < numCultures; i++)
            {
                int center = FindValidCultureCenter(graph, cultureCenters);
                if (center >= 0)
                {
                    cultureCenters.Add(center);

                    var culture = new CultureData
                    {
                        Id = graph.CulturesList.Count,
                        Name = $"Culture_{i}",
                        Color = $"#{_random.NextInt(0xFFFFFF):X6}",
                        Center = center,
                        Type = "Generic",
                        Expansionism = _random.NextFloat() * 0.4f + 0.1f,
                        Cells = 1,
                        Area = 1
                    };

                    graph.Cultures[center] = (ushort)culture.Id;
                    graph.CulturesList.Add(culture);
                }
            }
        }

        private int FindValidCultureCenter(VoronoiGraph graph, HashSet<int> existingCenters)
        {
            int maxAttempts = 100;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int cell = _random.NextInt(graph.CellsCount);
                if (!existingCenters.Contains(cell) &&
                    graph.Heights[cell] >= 32 &&
                    graph.Biomes[cell] != (ushort)BiomeType.Ocean)
                {
                    return cell;
                }
            }
            return _random.NextInt(graph.CellsCount);
        }

        private void ExpandCultures(VoronoiGraph graph)
        {
            int expansionRounds = 5;

            for (int round = 0; round < expansionRounds; round++)
            {
                var newCultureMap = new ushort[graph.CellsCount];
                Array.Copy(graph.Cultures, newCultureMap, graph.CellsCount);

                foreach (var culture in graph.CulturesList)
                {
                    ExpandCulture(graph, culture, newCultureMap);
                }

                Array.Copy(newCultureMap, graph.Cultures, graph.CellsCount);
            }
        }

        private void ExpandCulture(VoronoiGraph graph, CultureData culture, ushort[] newCultureMap)
        {
            var expansionCandidates = new List<int>();

            for (int i = 0; i < graph.CellsCount; i++)
            {
                if (graph.Cultures[i] == culture.Id)
                {
                    if (graph.Neighbors[i] != null)
                    {
                        foreach (int neighbor in graph.Neighbors[i])
                        {
                            if (graph.Cultures[neighbor] != culture.Id &&
                                CanCellJoinCulture(graph, neighbor, culture))
                            {
                                expansionCandidates.Add(neighbor);
                            }
                        }
                    }
                }
            }

            expansionCandidates = ShuffleAndLimit(expansionCandidates, _random);

            foreach (int candidate in expansionCandidates)
            {
                if (_random.NextFloat() < 0.3f)
                {
                    newCultureMap[candidate] = (ushort)culture.Id;
                }
            }
        }

        private bool CanCellJoinCulture(VoronoiGraph graph, int cell, CultureData culture)
        {
            return graph.Heights[cell] >= 32 &&
                   graph.Biomes[cell] != (ushort)BiomeType.Ocean;
        }

        private List<int> ShuffleAndLimit(List<int> list, RandomManager random)
        {
            var result = new List<int>(list);
            random.Shuffle(result.ToArray());
            if (result.Count > 50)
                result = result.GetRange(0, 50);
            return result;
        }
    }
}
